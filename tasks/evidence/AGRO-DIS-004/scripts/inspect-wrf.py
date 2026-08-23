from __future__ import annotations

import argparse
import ctypes
import hashlib
import json
import math
import os
import sys
import time
import tracemalloc
from pathlib import Path
from typing import Any

import numpy as np
from netCDF4 import Dataset

MAX_FILE_BYTES = 25 * 1024 * 1024
MAX_GRID_CELLS = 2_000_000
MAX_TIME_STEPS = 73
REQUIRED_VARIABLES = {
    "PP": ("millimeter", ("time", "y", "x")),
    "T2": ("degree_Celsius", ("time", "y", "x")),
    "HR2": ("percent", ("time", "y", "x")),
    "dirViento10": ("degree", ("time", "y", "x")),
    "magViento10": ("meter / second", ("time", "y", "x")),
    "lat": ("degrees_north", ("y", "x")),
    "lon": ("degrees_east", ("y", "x")),
    "time": (None, ("time",)),
}


class ProcessMemoryCounters(ctypes.Structure):
    _fields_ = [
        ("cb", ctypes.c_ulong),
        ("PageFaultCount", ctypes.c_ulong),
        ("PeakWorkingSetSize", ctypes.c_size_t),
        ("WorkingSetSize", ctypes.c_size_t),
        ("QuotaPeakPagedPoolUsage", ctypes.c_size_t),
        ("QuotaPagedPoolUsage", ctypes.c_size_t),
        ("QuotaPeakNonPagedPoolUsage", ctypes.c_size_t),
        ("QuotaNonPagedPoolUsage", ctypes.c_size_t),
        ("PagefileUsage", ctypes.c_size_t),
        ("PeakPagefileUsage", ctypes.c_size_t),
    ]


def peak_working_set_bytes() -> int | None:
    if os.name == "nt":
        kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
        psapi = ctypes.WinDLL("psapi", use_last_error=True)
        kernel32.GetCurrentProcess.restype = ctypes.c_void_p
        psapi.GetProcessMemoryInfo.argtypes = [
            ctypes.c_void_p,
            ctypes.POINTER(ProcessMemoryCounters),
            ctypes.c_ulong,
        ]
        psapi.GetProcessMemoryInfo.restype = ctypes.c_int

        counters = ProcessMemoryCounters()
        counters.cb = ctypes.sizeof(counters)
        success = psapi.GetProcessMemoryInfo(
            kernel32.GetCurrentProcess(), ctypes.byref(counters), counters.cb
        )
        return int(counters.PeakWorkingSetSize) if success else None

    try:
        import resource

        peak = resource.getrusage(resource.RUSAGE_SELF).ru_maxrss
        return int(peak if sys.platform == "darwin" else peak * 1024)
    except (ImportError, ValueError):
        return None


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def finite_range(values: np.ndarray) -> tuple[float, float]:
    finite = np.asarray(values)[np.isfinite(values)]
    if finite.size == 0:
        raise ValueError("coordinate variable contains no finite values")
    return float(finite.min()), float(finite.max())


def inspect(path: Path, expected_hash: str | None) -> dict[str, Any]:
    started = time.perf_counter()
    size = path.stat().st_size
    if size > MAX_FILE_BYTES:
        raise ValueError(f"file exceeds {MAX_FILE_BYTES} bytes")

    content_hash = sha256(path)
    if expected_hash and content_hash.lower() != expected_hash.lower():
        raise ValueError("SHA-256 does not match the pinned fixture")

    tracemalloc.start()
    with Dataset(path, "r") as dataset:
        dimensions = {name: len(value) for name, value in dataset.dimensions.items()}
        x_size = dimensions.get("x", 0)
        y_size = dimensions.get("y", 0)
        time_size = dimensions.get("time", 0)
        if x_size * y_size <= 0 or x_size * y_size > MAX_GRID_CELLS:
            raise ValueError("grid dimensions are missing or exceed the cell budget")
        if time_size <= 0 or time_size > MAX_TIME_STEPS:
            raise ValueError("time dimension is missing or exceeds the step budget")
        if int(getattr(dataset, "MAP_PROJ", -1)) != 1:
            raise ValueError("expected WRF Lambert conformal projection (MAP_PROJ=1)")

        variables: dict[str, dict[str, Any]] = {}
        for name, (expected_unit, expected_dimensions) in REQUIRED_VARIABLES.items():
            if name not in dataset.variables:
                raise ValueError(f"missing required variable {name}")
            variable = dataset.variables[name]
            if tuple(variable.dimensions) != expected_dimensions:
                raise ValueError(
                    f"unexpected dimensions for {name}: {tuple(variable.dimensions)}"
                )
            expected_shape = tuple(dimensions[dimension] for dimension in expected_dimensions)
            if tuple(variable.shape) != expected_shape:
                raise ValueError(f"unexpected shape for {name}: {tuple(variable.shape)}")
            if math.prod(variable.shape) > MAX_GRID_CELLS * MAX_TIME_STEPS:
                raise ValueError(f"variable {name} exceeds the element budget")
            actual_unit = getattr(variable, "units", None)
            if expected_unit and actual_unit != expected_unit:
                raise ValueError(f"unexpected unit for {name}: {actual_unit}")
            variables[name] = {
                "dimensions": list(variable.dimensions),
                "shape": list(variable.shape),
                "units": actual_unit,
                "standardName": getattr(variable, "standard_name", None),
                "gridMapping": getattr(variable, "grid_mapping", None),
            }

        latitudes = np.asarray(dataset.variables["lat"][:], dtype=np.float64)
        longitudes = np.asarray(dataset.variables["lon"][:], dtype=np.float64)
        lat_range = finite_range(latitudes)
        lon_range = finite_range(longitudes)

        target_latitude = -34.6144420654301
        target_longitude = -58.4458763250916
        distance = np.square(latitudes - target_latitude) + np.square(longitudes - target_longitude)
        flat_index = int(np.nanargmin(distance))
        y_index, x_index = np.unravel_index(flat_index, distance.shape)

        sample: dict[str, float] = {}
        for name in ("PP", "T2", "HR2", "dirViento10", "magViento10"):
            value = float(dataset.variables[name][0, y_index, x_index])
            if not math.isfinite(value):
                raise ValueError(f"non-finite sample value for {name}")
            sample[name] = value

        _, peak_memory = tracemalloc.get_traced_memory()
        tracemalloc.stop()

        process_peak_memory = peak_working_set_bytes()
        result: dict[str, Any] = {
            "fixtureVersion": "1.0.0",
            "sourceFile": path.name,
            "sha256": content_hash,
            "fileBytes": size,
            "dataModel": dataset.data_model,
            "dimensions": dimensions,
            "projection": {
                "name": "Lambert conformal",
                "mapProj": int(getattr(dataset, "MAP_PROJ")),
                "dxMeters": float(getattr(dataset, "DX")),
                "dyMeters": float(getattr(dataset, "DY")),
                "centerLatitude": float(getattr(dataset, "CEN_LAT")),
                "centerLongitude": float(getattr(dataset, "CEN_LON")),
            },
            "startDate": str(getattr(dataset, "START_DATE")),
            "coordinateBounds": {
                "latitude": list(lat_range),
                "longitude": list(lon_range),
            },
            "variables": variables,
            "sample": {
                "requestedPoint": {"latitude": target_latitude, "longitude": target_longitude},
                "resolvedPoint": {
                    "latitude": float(latitudes[y_index, x_index]),
                    "longitude": float(longitudes[y_index, x_index]),
                },
                "gridIndex": {"x": int(x_index), "y": int(y_index)},
                "values": sample,
            },
            "peakPythonBytes": peak_memory,
            "peakWorkingSetBytes": process_peak_memory,
        }

    result["durationMs"] = round((time.perf_counter() - started) * 1000, 3)
    result["withinSpikeBudgets"] = {
        "fileBytes": size <= MAX_FILE_BYTES,
        "gridCells": dimensions["x"] * dimensions["y"] <= MAX_GRID_CELLS,
        "parseDuration": result["durationMs"] <= 10_000,
        "pythonMemory": result["peakPythonBytes"] <= 512 * 1024 * 1024,
        "processWorkingSet": result["peakWorkingSetBytes"] is not None
        and result["peakWorkingSetBytes"] <= 512 * 1024 * 1024,
    }
    if not all(result["withinSpikeBudgets"].values()):
        raise ValueError("WRF sample exceeds at least one spike budget")
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description="Inspect a pinned SMN WRF NetCDF fixture safely.")
    parser.add_argument("input", type=Path)
    parser.add_argument("--expected-sha256")
    parser.add_argument("--output", type=Path)
    arguments = parser.parse_args()

    result = inspect(arguments.input.resolve(), arguments.expected_sha256)
    payload = json.dumps(result, ensure_ascii=False, indent=2) + "\n"
    if arguments.output:
        arguments.output.parent.mkdir(parents=True, exist_ok=True)
        arguments.output.write_text(payload, encoding="utf-8")
    print(payload, end="")


if __name__ == "__main__":
    main()
