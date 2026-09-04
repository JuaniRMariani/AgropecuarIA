import type {
  CatalogFilters,
  CatalogItem,
  CatalogSearch,
  CatalogVersion,
  CatalogVersions,
} from "./catalog-types";

export type CatalogFailure =
  | "signed-out"
  | "forbidden"
  | "validation"
  | "not-found"
  | "rate-limited"
  | "unavailable"
  | "contract"
  | "network";
export class CatalogApiError extends Error {
  constructor(
    readonly kind: CatalogFailure,
    readonly status: number,
  ) {
    super("Catalog read failed");
    this.name = "CatalogApiError";
  }
}

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
function invalid(): never {
  throw new CatalogApiError("contract", 502);
}
function record(value: unknown): Record<string, unknown> {
  if (!isRecord(value)) return invalid();
  return value;
}
function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}
function text(value: unknown): string {
  if (
    typeof value !== "string" ||
    value.length === 0 ||
    value.length > 4096 ||
    /[\u0000-\u001f\u007f]/.test(value)
  )
    return invalid();
  return value;
}
function uuid(value: unknown): string {
  const result = text(value);
  if (!UUID.test(result)) return invalid();
  return result;
}
function nullableUuid(value: unknown): string | null {
  return value === null ? null : uuid(value);
}
function date(value: unknown): string {
  const result = text(value);
  if (!result.includes("T") || Number.isNaN(Date.parse(result)))
    return invalid();
  return result;
}
function integer(value: unknown): number {
  if (typeof value !== "number" || !Number.isSafeInteger(value) || value < 0)
    return invalid();
  return value;
}
function boolean(value: unknown): boolean {
  if (typeof value !== "boolean") return invalid();
  return value;
}
function array(value: unknown): readonly unknown[] {
  if (!Array.isArray(value) || value.length > 100) return invalid();
  return value;
}

export function parseCatalogItem(value: unknown): CatalogItem {
  const item = record(value);
  if (
    !["CATALOGADA", "FLUJO_GENERICO", "ESPECIALIZADA_VALIDADA"].includes(
      text(item.supportLevel),
    ) ||
    ![
      "AGRICULTURA",
      "GANADERIA",
      "FORRAJE",
      "HORTICULTURA",
      "FRUTICULTURA",
      "FORESTACION",
      "OTROS",
    ].includes(text(item.category))
  )
    return invalid();
  if (
    item.provenanceStatus !== "verified_snapshot" &&
    item.provenanceStatus !== "legacy_unavailable"
  )
    return invalid();
  const sourceSnapshotId = nullableUuid(item.sourceSnapshotId);
  const sourceId = item.sourceId === null ? null : text(item.sourceId);
  const sourceHash = item.sourceHash === null ? null : text(item.sourceHash);
  const sourceIngestedAtUtc =
    item.sourceIngestedAtUtc === null ? null : date(item.sourceIngestedAtUtc);
  if (
    item.provenanceStatus === "legacy_unavailable" &&
    [sourceSnapshotId, sourceId, sourceHash, sourceIngestedAtUtc].some(
      (value) => value !== null,
    )
  )
    return invalid();
  const capabilities = array(item.capabilities).map(text);
  const absentCapabilities = array(item.absentCapabilities).map(text);
  if (
    capabilities.length !== 0 ||
    JSON.stringify(absentCapabilities) !==
      JSON.stringify([
        "specialized_rules",
        "specialized_kpis",
        "ai_recommendations",
      ])
  )
    return invalid();
  if (
    item.provenanceStatus === "verified_snapshot" &&
    (sourceSnapshotId === null ||
      sourceId === null ||
      sourceHash === null ||
      sourceIngestedAtUtc === null)
  )
    return invalid();
  if (sourceHash !== null && !/^[0-9a-f]{64}$/i.test(sourceHash))
    return invalid();
  return {
    id: uuid(item.id),
    code: text(item.code),
    displayName: text(item.displayName),
    jurisdiction: text(item.jurisdiction),
    supportLevel: text(item.supportLevel),
    category: text(item.category),
    synonyms: array(item.synonyms).map(text),
    versionId: uuid(item.versionId),
    versionTag: text(item.versionTag),
    activeVersionId: nullableUuid(item.activeVersionId),
    sourceSnapshotId,
    sourceId,
    sourceHash,
    sourceIngestedAtUtc,
    provenanceStatus: item.provenanceStatus,
    capabilities,
    absentCapabilities,
  };
}

export function parseCatalogSearch(value: unknown): CatalogSearch {
  const data = record(value);
  const result: CatalogSearch = {
    versionId: nullableUuid(data.versionId),
    versionTag: data.versionTag === null ? null : text(data.versionTag),
    activeVersionId: nullableUuid(data.activeVersionId),
    publishedAtUtc:
      data.publishedAtUtc === null ? null : date(data.publishedAtUtc),
    isHistorical: boolean(data.isHistorical),
    totalCount: integer(data.totalCount),
    items: array(data.items).map(parseCatalogItem),
  };
  if (result.items.length > result.totalCount) return invalid();
  if (result.versionId === null) {
    if (
      result.versionTag !== null ||
      result.publishedAtUtc !== null ||
      result.activeVersionId !== null ||
      result.isHistorical ||
      result.totalCount !== 0
    )
      return invalid();
  } else if (
    result.versionTag === null ||
    result.publishedAtUtc === null ||
    result.isHistorical !== (result.versionId !== result.activeVersionId)
  )
    return invalid();
  if (
    result.items.some(
      (item) =>
        item.versionId !== result.versionId ||
        item.versionTag !== result.versionTag ||
        item.activeVersionId !== result.activeVersionId,
    )
  )
    return invalid();
  if (
    new Set(result.items.map((item) => item.code)).size !== result.items.length
  )
    return invalid();
  return result;
}

export function parseCatalogVersions(value: unknown): CatalogVersions {
  const data = record(value);
  const activeVersionId = nullableUuid(data.activeVersionId);
  const versions = array(data.versions).map((value): CatalogVersion => {
    const version = record(value);
    const result = {
      id: uuid(version.id),
      versionTag: text(version.versionTag),
      publishedAtUtc: date(version.publishedAtUtc),
      isActive: boolean(version.isActive),
      itemsCount: integer(version.itemsCount),
    };
    if (result.isActive !== (result.id === activeVersionId)) return invalid();
    return result;
  });
  const totalCount = integer(data.totalCount);
  if (
    totalCount < versions.length ||
    new Set(versions.map((version) => version.id)).size !== versions.length
  )
    return invalid();
  return {
    activeVersionId,
    totalCount,
    hasMore: boolean(data.hasMore),
    versions,
  };
}

function validateVersion(versionId: string | null): void {
  if (versionId !== null && !UUID.test(versionId))
    throw new CatalogApiError("validation", 400);
}

async function get(path: string, signal?: AbortSignal): Promise<unknown> {
  let response: Response;
  try {
    response = await fetch(path, {
      method: "GET",
      credentials: "include",
      cache: "no-store",
      headers: { Accept: "application/json" },
      signal,
    });
  } catch (error) {
    if (
      (error instanceof Error || error instanceof DOMException) &&
      error.name === "AbortError"
    )
      throw error;
    throw new CatalogApiError("network", 0);
  }
  if (!response.ok) {
    const status = response.status;
    const kind: CatalogFailure =
      status === 401
        ? "signed-out"
        : status === 403
          ? "forbidden"
          : status === 400
            ? "validation"
            : status === 404
              ? "not-found"
              : status === 429
                ? "rate-limited"
                : "unavailable";
    throw new CatalogApiError(kind, status);
  }
  try {
    const value: unknown = await response.json();
    return value;
  } catch {
    return invalid();
  }
}

export async function searchCatalog(
  filters: CatalogFilters,
  signal?: AbortSignal,
): Promise<CatalogSearch> {
  validateVersion(filters.versionId);
  const parameters = new URLSearchParams({ limit: "50" });
  for (const key of [
    "query",
    "jurisdiction",
    "category",
    "supportLevel",
  ] as const) {
    const value = filters[key].trim();
    if (
      value.length > (key === "query" ? 256 : 64) ||
      /[\u0000-\u001f\u007f]/.test(value)
    )
      throw new CatalogApiError("validation", 400);
    if (value.length > 0) parameters.set(key, value);
  }
  if (filters.versionId !== null)
    parameters.set("versionId", filters.versionId);
  const result = parseCatalogSearch(
    await get(`/api/catalog/items?${parameters}`, signal),
  );
  if (filters.versionId !== null && result.versionId !== filters.versionId)
    return invalid();
  return result;
}

export async function getCatalogItem(
  code: string,
  versionId: string,
  signal?: AbortSignal,
): Promise<CatalogItem> {
  validateVersion(versionId);
  if (
    code.length === 0 ||
    code.length > 64 ||
    /[\u0000-\u001f\u007f]/.test(code)
  )
    throw new CatalogApiError("validation", 400);
  const parameters = new URLSearchParams({ versionId });
  const item = parseCatalogItem(
    await get(
      `/api/catalog/items/${encodeURIComponent(code)}?${parameters}`,
      signal,
    ),
  );
  if (item.code !== code || item.versionId !== versionId) return invalid();
  return item;
}

export async function listCatalogVersions(
  offset: number,
  signal?: AbortSignal,
): Promise<CatalogVersions> {
  if (!Number.isSafeInteger(offset) || offset < 0 || offset > 10000)
    throw new CatalogApiError("validation", 400);
  return parseCatalogVersions(
    await get(`/api/catalog/versions?limit=20&offset=${offset}`, signal),
  );
}
