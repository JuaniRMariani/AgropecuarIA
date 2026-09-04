import { afterEach, describe, expect, it, vi } from "vitest";
import {
  CatalogApiError,
  getCatalogItem,
  listCatalogVersions,
  parseCatalogItem,
  parseCatalogSearch,
  parseCatalogVersions,
  searchCatalog,
} from "../../features/catalog/catalog-api";
import {
  catalogItem,
  catalogResult,
  catalogVersions,
  currentVersion,
  historicalVersion,
} from "../fixtures/catalog";

const filters = {
  query: "",
  jurisdiction: "",
  category: "",
  supportLevel: "",
  versionId: null,
};
const fetchMock = vi.fn<typeof fetch>();
function respond(payload: unknown, status = 200): Response {
  return new Response(JSON.stringify(payload), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}
afterEach(() => {
  fetchMock.mockReset();
  vi.unstubAllGlobals();
});

describe("catalog read boundary", () => {
  it("reads the locked search, item and history shapes", () => {
    expect(parseCatalogItem(catalogItem)).toEqual(catalogItem);
    expect(parseCatalogSearch(catalogResult)).toEqual(catalogResult);
    expect(parseCatalogVersions(catalogVersions)).toEqual(catalogVersions);
  });
  it("distinguishes absent publication from a published empty result", () => {
    expect(
      parseCatalogSearch({
        versionId: null,
        versionTag: null,
        activeVersionId: null,
        publishedAtUtc: null,
        isHistorical: false,
        totalCount: 0,
        items: [],
      }).versionId,
    ).toBeNull();
    expect(
      parseCatalogSearch({ ...catalogResult, totalCount: 0, items: [] })
        .versionId,
    ).toBe(currentVersion.id);
  });
  it("allows explicit legacy provenance without fabricating a source", () => {
    expect(
      parseCatalogItem({
        ...catalogItem,
        provenanceStatus: "legacy_unavailable",
        sourceSnapshotId: null,
        sourceId: null,
        sourceHash: null,
        sourceIngestedAtUtc: null,
      }).provenanceStatus,
    ).toBe("legacy_unavailable");
  });
  it("rejects capability activation and invented legacy provenance", () => {
    expect(() =>
      parseCatalogItem({ ...catalogItem, capabilities: ["specialized_rules"] }),
    ).toThrow(CatalogApiError);
    expect(() =>
      parseCatalogItem({ ...catalogItem, absentCapabilities: [] }),
    ).toThrow(CatalogApiError);
    expect(() =>
      parseCatalogItem({
        ...catalogItem,
        provenanceStatus: "legacy_unavailable",
      }),
    ).toThrow(CatalogApiError);
  });
  it.each([
    "sourceSnapshotId",
    "sourceId",
    "sourceHash",
    "sourceIngestedAtUtc",
  ])("rejects verified provenance missing %s", (key) => {
    expect(() => parseCatalogItem({ ...catalogItem, [key]: null })).toThrow(
      CatalogApiError,
    );
  });
  it.each([
    { versionId: "not-a-uuid" },
    { sourceHash: "invalid" },
    { provenanceStatus: "officially_approved" },
    { capabilities: "all" },
    { absentCapabilities: null },
    { synonyms: [1] },
    { sourceIngestedAtUtc: "yesterday" },
  ])("rejects malformed item metadata %j", (change) => {
    expect(() => parseCatalogItem({ ...catalogItem, ...change })).toThrow(
      CatalogApiError,
    );
  });
  it.each([
    { isHistorical: true },
    { totalCount: 0 },
    { items: [{ ...catalogItem, versionId: historicalVersion.id }] },
    { items: [{ ...catalogItem, activeVersionId: historicalVersion.id }] },
    { items: [catalogItem, catalogItem], totalCount: 2 },
    { publishedAtUtc: null },
  ])("rejects inconsistent search metadata %j", (change) => {
    expect(() => parseCatalogSearch({ ...catalogResult, ...change })).toThrow(
      CatalogApiError,
    );
  });
  it("rejects inconsistent version discovery", () => {
    expect(() =>
      parseCatalogVersions({
        ...catalogVersions,
        versions: [{ ...currentVersion, isActive: false }],
      }),
    ).toThrow(CatalogApiError);
    expect(() =>
      parseCatalogVersions({
        ...catalogVersions,
        versions: Array.from({ length: 101 }, () => currentVersion),
      }),
    ).toThrow(CatalogApiError);
  });
  it("sends bounded authenticated GET filters without writes or staging access", async () => {
    vi.stubGlobal("fetch", fetchMock);
    fetchMock.mockResolvedValue(respond(catalogResult));
    await searchCatalog({
      ...filters,
      query: "  maíz  ",
      category: "AGRICULTURA",
      jurisdiction: "AR",
      supportLevel: "FLUJO_GENERICO",
    });
    const [path, options] = fetchMock.mock.calls[0] ?? [];
    const url = new URL(String(path), "http://localhost");
    expect(url.pathname).toBe("/api/catalog/items");
    expect(Object.fromEntries(url.searchParams)).toEqual({
      limit: "50",
      query: "maíz",
      category: "AGRICULTURA",
      jurisdiction: "AR",
      supportLevel: "FLUJO_GENERICO",
    });
    expect(options).toMatchObject({
      method: "GET",
      credentials: "include",
      cache: "no-store",
    });
    expect(options?.body).toBeUndefined();
  });
  it("pins detail to the search version and rejects a substituted item", async () => {
    vi.stubGlobal("fetch", fetchMock);
    fetchMock
      .mockResolvedValueOnce(respond(catalogItem))
      .mockResolvedValueOnce(respond({ ...catalogItem, code: "OTRO" }));
    await expect(getCatalogItem("MAIZ", currentVersion.id)).resolves.toEqual(
      catalogItem,
    );
    expect(fetchMock.mock.calls[0]?.[0]).toBe(
      `/api/catalog/items/MAIZ?versionId=${currentVersion.id}`,
    );
    await expect(
      getCatalogItem("MAIZ", currentVersion.id),
    ).rejects.toMatchObject({ kind: "contract" });
  });
  it("rejects a substituted historical search version", async () => {
    vi.stubGlobal("fetch", fetchMock);
    fetchMock.mockResolvedValue(respond(catalogResult));
    await expect(
      searchCatalog({ ...filters, versionId: historicalVersion.id }),
    ).rejects.toMatchObject({ kind: "contract" });
  });
  it("uses bounded history pagination and passes cancellation", async () => {
    vi.stubGlobal("fetch", fetchMock);
    fetchMock.mockResolvedValue(respond(catalogVersions));
    const controller = new AbortController();
    await listCatalogVersions(20, controller.signal);
    expect(fetchMock).toHaveBeenCalledWith(
      "/api/catalog/versions?limit=20&offset=20",
      expect.objectContaining({ signal: controller.signal }),
    );
  });
  it.each([[-1], [10001], [1.5]])(
    "rejects invalid history offset %s before fetch",
    async (offset) => {
      vi.stubGlobal("fetch", fetchMock);
      await expect(listCatalogVersions(offset)).rejects.toMatchObject({
        kind: "validation",
      });
      expect(fetchMock).not.toHaveBeenCalled();
    },
  );
  it.each([
    ["query", 257],
    ["jurisdiction", 65],
    ["category", 65],
    ["supportLevel", 65],
  ] as const)("rejects excessive %s before fetch", async (key, length) => {
    vi.stubGlobal("fetch", fetchMock);
    await expect(
      searchCatalog({ ...filters, [key]: "a".repeat(length) }),
    ).rejects.toMatchObject({ kind: "validation" });
    expect(fetchMock).not.toHaveBeenCalled();
  });
  it.each([
    [401, "signed-out"],
    [403, "forbidden"],
    [404, "not-found"],
    [429, "rate-limited"],
    [503, "unavailable"],
  ])(
    "maps HTTP%s without showing private server messages",
    async (status, kind) => {
      vi.stubGlobal("fetch", fetchMock);
      fetchMock.mockResolvedValue(
        respond({ detail: "private diagnostic" }, Number(status)),
      );
      await expect(searchCatalog(filters)).rejects.toMatchObject({ kind });
    },
  );
  it("rejects malformed JSON and preserves AbortError", async () => {
    vi.stubGlobal("fetch", fetchMock);
    fetchMock
      .mockResolvedValueOnce(new Response("<html>not json</html>"))
      .mockRejectedValueOnce(new DOMException("aborted", "AbortError"));
    await expect(searchCatalog(filters)).rejects.toMatchObject({
      kind: "contract",
    });
    await expect(searchCatalog(filters)).rejects.toMatchObject({
      name: "AbortError",
    });
  });
});
