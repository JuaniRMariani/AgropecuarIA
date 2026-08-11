import { afterEach, describe, expect, it, vi } from "vitest";

import {
  parseTerritoryResolutionResult,
  parseTerritorySearchResult,
  resolveTerritory,
  searchTerritories,
  TerritoryApiError,
} from "../../features/territory/territory-api";

const source = {
  provider: "georef",
  version: "2026-08-11",
  capturedAtUtc: "2026-08-11T12:00:00Z",
};

const unit = {
  officialCode: "14014",
  name: "Capital",
  level: "department",
  parentCode: "14",
  parentName: "Córdoba",
  hierarchyLabel: "Capital · Córdoba",
};

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe("territory contract parsing", () => {
  it("accepts only the frozen search response shape", () => {
    expect(
      parseTerritorySearchResult({
        status: "fresh",
        source,
        items: [unit],
      }),
    ).toEqual({
      status: "fresh",
      source,
      items: [unit],
    });

    expect(() =>
      parseTerritorySearchResult({
        status: "fresh",
        source,
        items: [unit],
        ignoredServerField: true,
      }),
    ).toThrowError(TerritoryApiError);
    expect(() =>
      parseTerritorySearchResult({
        status: "cached",
        source,
        items: [],
      }),
    ).toThrowError(TerritoryApiError);
    expect(() =>
      parseTerritorySearchResult({
        status: "fresh",
        source,
        items: [{ ...unit, hierarchyLabel: "x".repeat(646) }],
      }),
    ).not.toThrow();
    expect(() =>
      parseTerritorySearchResult({
        status: "fresh",
        source,
        items: [{ ...unit, hierarchyLabel: "x".repeat(701) }],
      }),
    ).toThrowError(TerritoryApiError);
  });

  it("requires explicit nullable fields and the manual-search fallback", () => {
    expect(
      parseTerritoryResolutionResult({
        status: "unavailable",
        source: null,
        unit: null,
        fallback: { searchAvailable: true },
      }),
    ).toEqual({
      status: "unavailable",
      source: null,
      unit: null,
      fallback: { searchAvailable: true },
    });

    expect(() =>
      parseTerritoryResolutionResult({
        status: "unavailable",
        source: null,
        unit: null,
        fallback: { searchAvailable: false },
      }),
    ).toThrowError(TerritoryApiError);
    expect(() =>
      parseTerritoryResolutionResult({
        status: "fresh",
        source,
        unit: { ...unit, level: "field" },
        fallback: { searchAvailable: true },
      }),
    ).toThrowError(TerritoryApiError);
  });
});

describe("territory API client", () => {
  it("sends a credentialed GET with encoded optional search filters", async () => {
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(
        new Response(
          JSON.stringify({ status: "fresh", source, items: [unit] }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        ),
      );
    vi.stubGlobal("fetch", fetchMock);
    const controller = new AbortController();

    await searchTerritories(
      {
        query: " General Roca ",
        level: "department",
        parentCode: "62",
        limit: 7,
      },
      controller.signal,
    );

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/territory/search?query=General+Roca&limit=7&level=department&parentCode=62",
      {
        cache: "no-store",
        credentials: "include",
        headers: { Accept: "application/json" },
        signal: controller.signal,
      },
    );
  });

  it("resolves coordinates through GET and never adds a request body", async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(
        JSON.stringify({
          status: "unavailable",
          source: null,
          unit: null,
          fallback: { searchAvailable: true },
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    );
    vi.stubGlobal("fetch", fetchMock);

    await resolveTerritory({ latitude: -34.6037, longitude: -58.3816 });

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/territory/resolve?latitude=-34.6037&longitude=-58.3816",
      {
        cache: "no-store",
        credentials: "include",
        headers: { Accept: "application/json" },
        signal: undefined,
      },
    );
    expect(fetchMock.mock.calls[0]?.[1]).not.toHaveProperty("body");
    expect(fetchMock.mock.calls[0]?.[1]).not.toHaveProperty("method");
  });

  it("fails locally for invalid parameters and classifies an unavailable snapshot", async () => {
    await expect(searchTerritories({ query: "x" })).rejects.toMatchObject({
      kind: "validation",
      status: 400,
    });
    await expect(
      searchTerritories({ query: "Córdoba", parentCode: "AR-14" }),
    ).rejects.toMatchObject({ kind: "validation", status: 400 });
    await expect(
      resolveTerritory({ latitude: -91, longitude: 20 }),
    ).rejects.toMatchObject({ kind: "validation", status: 400 });

    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(new Response("", { status: 503 }));
    vi.stubGlobal("fetch", fetchMock);
    await expect(searchTerritories({ query: "Córdoba" })).rejects.toMatchObject(
      {
        kind: "unavailable",
        status: 503,
      },
    );
  });
});
