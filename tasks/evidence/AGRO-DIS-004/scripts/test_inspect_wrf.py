from __future__ import annotations

import importlib.util
import tempfile
import unittest
from pathlib import Path

from netCDF4 import Dataset


def load_module():
    module_path = Path(__file__).with_name("inspect-wrf.py")
    spec = importlib.util.spec_from_file_location("inspect_wrf", module_path)
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load inspect-wrf.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


inspect_wrf = load_module()


class WrfInspectorSafetyTests(unittest.TestCase):
    def test_rejects_file_over_budget_before_parser(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "oversize.nc"
            with path.open("wb") as stream:
                stream.seek(inspect_wrf.MAX_FILE_BYTES)
                stream.write(b"x")

            with self.assertRaisesRegex(ValueError, "exceeds"):
                inspect_wrf.inspect(path, None)

    def test_rejects_hash_mismatch(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "small.nc"
            path.write_bytes(b"not-netcdf")

            with self.assertRaisesRegex(ValueError, "SHA-256"):
                inspect_wrf.inspect(path, "0" * 64)

    def test_rejects_dimension_bomb_before_reading_grid(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "dimension-bomb.nc"
            with Dataset(path, "w", format="NETCDF4") as dataset:
                dataset.createDimension("time", 1)
                dataset.createDimension("y", 2_000)
                dataset.createDimension("x", 2_000)

            with self.assertRaisesRegex(ValueError, "cell budget"):
                inspect_wrf.inspect(path, None)

    def test_rejects_invalid_netcdf(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "invalid.nc"
            path.write_bytes(b"invalid")

            with self.assertRaises(OSError):
                inspect_wrf.inspect(path, None)

    def test_rejects_coordinate_shape_bomb_before_materializing_it(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "coordinate-shape-bomb.nc"
            with Dataset(path, "w", format="NETCDF4") as dataset:
                dataset.createDimension("time", 1)
                dataset.createDimension("y", 1)
                dataset.createDimension("x", 1)
                dataset.createDimension("bomb", 3_000_000)
                dataset.MAP_PROJ = 1
                for name, (unit, dimensions) in inspect_wrf.REQUIRED_VARIABLES.items():
                    actual_dimensions = ("bomb",) if name in {"lat", "lon"} else dimensions
                    variable = dataset.createVariable(name, "f4", actual_dimensions)
                    if unit is not None:
                        variable.units = unit

            with self.assertRaisesRegex(ValueError, "unexpected dimensions for lat"):
                inspect_wrf.inspect(path, None)


if __name__ == "__main__":
    unittest.main()
