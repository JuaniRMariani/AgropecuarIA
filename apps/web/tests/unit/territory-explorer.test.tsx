import {
  act,
  cleanup,
  fireEvent,
  render,
  screen,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import type {
  TerritoryResolutionResult,
  TerritorySearchParameters,
  TerritorySearchResult,
} from "../../features/territory/territory-types";

const apiMocks = vi.hoisted(() => ({
  searchTerritories:
    vi.fn<
      (
        parameters: TerritorySearchParameters,
        signal?: AbortSignal,
      ) => Promise<TerritorySearchResult>
    >(),
  resolveTerritory:
    vi.fn<
      (
        coordinate: Readonly<{ latitude: number; longitude: number }>,
        signal?: AbortSignal,
      ) => Promise<TerritoryResolutionResult>
    >(),
}));

vi.mock("../../features/territory/territory-api", async (importOriginal) => {
  const actual =
    await importOriginal<
      typeof import("../../features/territory/territory-api")
    >();
  return { ...actual, ...apiMocks };
});

import { TerritoryApiError } from "../../features/territory/territory-api";
import { TerritoryExplorer } from "../../features/territory/territory-explorer";

const source = {
  provider: "georef",
  version: "2026-08-11",
  capturedAtUtc: "2026-08-11T12:00:00Z",
} satisfies TerritorySearchResult["source"];

const cordoba = {
  officialCode: "14",
  name: "Córdoba",
  level: "province",
  parentCode: null,
  parentName: null,
  hierarchyLabel: "Córdoba",
} satisfies TerritorySearchResult["items"][number];

const sanLuis = {
  officialCode: "74",
  name: "San Luis",
  level: "province",
  parentCode: null,
  parentName: null,
  hierarchyLabel: "San Luis",
} satisfies TerritorySearchResult["items"][number];

const corrientesResult: TerritorySearchResult = {
  status: "fresh",
  source,
  items: [
    {
      officialCode: "18",
      name: "Corrientes",
      level: "province",
      parentCode: null,
      parentName: null,
      hierarchyLabel: "Corrientes",
    },
  ],
};

const cordobaResult: TerritorySearchResult = {
  status: "fresh",
  source,
  items: [cordoba],
};

function deferred<T>() {
  let resolvePromise: (value: T) => void = () => undefined;
  const promise = new Promise<T>((resolve) => {
    resolvePromise = resolve;
  });
  return { promise, resolve: resolvePromise };
}

async function advanceDebounce(milliseconds = 250): Promise<void> {
  await act(async () => {
    vi.advanceTimersByTime(milliseconds);
    await Promise.resolve();
  });
}

beforeEach(() => {
  vi.useFakeTimers();
  apiMocks.searchTerritories.mockResolvedValue(cordobaResult);
  apiMocks.resolveTerritory.mockResolvedValue({
    status: "unavailable",
    source: null,
    unit: null,
    fallback: { searchAvailable: true },
  });
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  vi.useRealTimers();
  globalThis.sessionStorage.clear();
  globalThis.localStorage.clear();
});

describe("TerritoryExplorer search", () => {
  it("waits 250 ms and never exposes a search button", async () => {
    render(<TerritoryExplorer />);
    const input = screen.getByRole("combobox", {
      name: /provincia, departamento/i,
    });

    fireEvent.change(input, { target: { value: "có" } });
    await advanceDebounce(249);
    expect(apiMocks.searchTerritories).not.toHaveBeenCalled();
    await advanceDebounce(1);

    expect(apiMocks.searchTerritories).toHaveBeenCalledOnce();
    expect(apiMocks.searchTerritories).toHaveBeenCalledWith(
      { query: "có", level: undefined, parentCode: undefined, limit: 10 },
      expect.any(AbortSignal),
    );
    expect(
      screen.queryByRole("button", { name: /buscar/i }),
    ).not.toBeInTheDocument();
  });

  it("aborts the previous request when the query changes", async () => {
    const first = deferred<TerritorySearchResult>();
    apiMocks.searchTerritories.mockReturnValueOnce(first.promise);
    render(<TerritoryExplorer />);
    const input = screen.getByRole("combobox", {
      name: /provincia, departamento/i,
    });

    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: "cor" } });
    await advanceDebounce();
    const firstSignal = apiMocks.searchTerritories.mock.calls[0]?.[1];
    expect(firstSignal?.aborted).toBe(false);

    fireEvent.change(input, { target: { value: "cord" } });
    expect(firstSignal?.aborted).toBe(true);
    await advanceDebounce();
    expect(apiMocks.searchTerritories).toHaveBeenCalledTimes(2);
  });

  it("ignores an older response even if its transport does not honor abort", async () => {
    const first = deferred<TerritorySearchResult>();
    const second = deferred<TerritorySearchResult>();
    apiMocks.searchTerritories
      .mockReturnValueOnce(first.promise)
      .mockReturnValueOnce(second.promise);
    render(<TerritoryExplorer />);
    const input = screen.getByRole("combobox", {
      name: /provincia, departamento/i,
    });

    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: "cor" } });
    await advanceDebounce();
    fireEvent.change(input, { target: { value: "cord" } });
    await advanceDebounce();
    await act(async () => {
      second.resolve(cordobaResult);
      await second.promise;
    });
    fireEvent.focus(input);
    expect(screen.getByRole("option", { name: /córdoba/i })).toBeVisible();

    await act(async () => {
      first.resolve(corrientesResult);
      await first.promise;
    });
    expect(
      screen.queryByRole("option", { name: /corrientes/i }),
    ).not.toBeInTheDocument();
    expect(screen.getByRole("option", { name: /córdoba/i })).toBeVisible();
  });

  it("supports listbox navigation and selection from the keyboard", async () => {
    apiMocks.searchTerritories.mockResolvedValue({
      status: "stale",
      source,
      items: [cordoba, sanLuis],
    });
    render(<TerritoryExplorer />);
    const input = screen.getByRole("combobox", {
      name: /provincia, departamento/i,
    });

    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: "sa" } });
    await advanceDebounce();
    fireEvent.focus(input);
    fireEvent.keyDown(input, { key: "ArrowDown" });
    fireEvent.keyDown(input, { key: "Enter" });

    expect(
      screen.getByRole("article", { name: "Referencia seleccionada" }),
    ).toHaveTextContent("San Luis");
    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
    expect(screen.getByRole("status")).toHaveTextContent(
      "Última referencia válida",
    );
  });

  it("keeps loading and failures inside the result region", async () => {
    const pending = deferred<TerritorySearchResult>();
    apiMocks.searchTerritories.mockReturnValueOnce(pending.promise);
    render(<TerritoryExplorer />);
    const input = screen.getByRole("combobox", {
      name: /provincia, departamento/i,
    });

    fireEvent.change(input, { target: { value: "ju" } });
    await advanceDebounce();
    expect(
      screen.getByRole("region", { name: "Resultados territoriales" }),
    ).toHaveAttribute("aria-busy", "true");
    expect(
      screen.getByRole("heading", { name: "Resolver una coordenada" }),
    ).toBeVisible();

    apiMocks.searchTerritories.mockRejectedValueOnce(
      new TerritoryApiError("rate-limited", 429),
    );
    fireEvent.change(input, { target: { value: "juj" } });
    await advanceDebounce();
    expect(screen.getByRole("alert")).toHaveTextContent("demasiadas consultas");
    expect(
      screen.getByRole("region", { name: "Resultados territoriales" }),
    ).toHaveAttribute("aria-busy", "false");
  });
});

describe("TerritoryExplorer coordinate resolution", () => {
  it("validates locally and offers manual search when resolution is unavailable", async () => {
    render(<TerritoryExplorer />);
    fireEvent.change(screen.getByRole("textbox", { name: "Latitud" }), {
      target: { value: "-34,6037" },
    });
    fireEvent.change(screen.getByRole("textbox", { name: "Longitud" }), {
      target: { value: "-58.3816" },
    });
    fireEvent.click(
      screen.getByRole("button", { name: "Resolver referencia" }),
    );
    await act(async () => {
      await Promise.resolve();
    });

    expect(apiMocks.resolveTerritory).toHaveBeenCalledWith(
      { latitude: -34.6037, longitude: -58.3816 },
      expect.any(AbortSignal),
    );
    expect(
      screen.getByRole("button", { name: "Ir a búsqueda manual" }),
    ).toBeEnabled();
    expect(screen.getByRole("status")).toHaveTextContent(
      "Sin resolución automática",
    );
    expect(globalThis.localStorage).toHaveLength(0);
    expect(globalThis.sessionStorage).toHaveLength(0);
  });

  it("does not call the API for an invalid coordinate", () => {
    render(<TerritoryExplorer />);
    fireEvent.change(screen.getByRole("textbox", { name: "Latitud" }), {
      target: { value: "-91" },
    });
    fireEvent.change(screen.getByRole("textbox", { name: "Longitud" }), {
      target: { value: "-58" },
    });
    fireEvent.click(
      screen.getByRole("button", { name: "Resolver referencia" }),
    );

    expect(apiMocks.resolveTerritory).not.toHaveBeenCalled();
    expect(screen.getByRole("alert")).toHaveTextContent(
      "dentro de la Argentina continental",
    );
  });
});
