import type {
  TerritoryCoordinate,
  TerritoryLevel,
  TerritoryResolutionResult,
  TerritorySearchParameters,
  TerritorySearchResult,
  TerritorySource,
  TerritoryUnit,
} from "./territory-types";

type JsonRecord = Record<string, unknown>;

export type TerritoryFailureKind =
  | "signed-out"
  | "validation"
  | "rate-limited"
  | "unavailable"
  | "contract"
  | "network"
  | "error";

export class TerritoryApiError extends Error {
  readonly kind: TerritoryFailureKind;
  readonly status: number;

  constructor(kind: TerritoryFailureKind, status: number) {
    super("Territory request failed");
    this.name = "TerritoryApiError";
    this.kind = kind;
    this.status = status;
  }
}

const TERRITORY_LEVELS = new Set<TerritoryLevel>([
  "province",
  "department",
  "municipality",
  "locality",
]);

function isRecord(value: unknown): value is JsonRecord {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function hasExactKeys(
  record: JsonRecord,
  required: readonly string[],
): boolean {
  const actual = Object.keys(record);
  return (
    actual.length === required.length &&
    actual.every((key) => required.includes(key))
  );
}

function contractFailure(): never {
  throw new TerritoryApiError("contract", 502);
}

function requiredString(
  record: JsonRecord,
  key: string,
  maximumLength: number,
): string {
  const value = record[key];
  if (
    typeof value !== "string" ||
    value.length === 0 ||
    value.length > maximumLength
  ) {
    return contractFailure();
  }
  return value;
}

function nullableString(
  record: JsonRecord,
  key: string,
  maximumLength: number,
): string | null {
  if (record[key] === null) {
    return null;
  }
  return requiredString(record, key, maximumLength);
}

function parseLevel(value: unknown): TerritoryLevel {
  if (
    value === "province" ||
    value === "department" ||
    value === "municipality" ||
    value === "locality"
  ) {
    return value;
  }
  return contractFailure();
}

function parseSource(value: unknown): TerritorySource {
  if (
    !isRecord(value) ||
    !hasExactKeys(value, ["provider", "version", "capturedAtUtc"]) ||
    value.provider !== "georef"
  ) {
    return contractFailure();
  }
  const capturedAtUtc = requiredString(value, "capturedAtUtc", 64);
  if (!capturedAtUtc.includes("T") || Number.isNaN(Date.parse(capturedAtUtc))) {
    return contractFailure();
  }
  return {
    provider: "georef",
    version: requiredString(value, "version", 80),
    capturedAtUtc,
  };
}

function parseNullableSource(value: unknown): TerritorySource | null {
  return value === null ? null : parseSource(value);
}

export function parseTerritoryUnit(value: unknown): TerritoryUnit {
  if (
    !isRecord(value) ||
    !hasExactKeys(value, [
      "officialCode",
      "name",
      "level",
      "parentCode",
      "parentName",
      "hierarchyLabel",
    ])
  ) {
    return contractFailure();
  }
  return {
    officialCode: requiredString(value, "officialCode", 16),
    name: requiredString(value, "name", 200),
    level: parseLevel(value.level),
    parentCode: nullableString(value, "parentCode", 16),
    parentName: nullableString(value, "parentName", 200),
    hierarchyLabel: requiredString(value, "hierarchyLabel", 700),
  };
}

export function parseTerritorySearchResult(
  value: unknown,
): TerritorySearchResult {
  if (
    !isRecord(value) ||
    !hasExactKeys(value, ["status", "source", "items"]) ||
    (value.status !== "fresh" && value.status !== "stale") ||
    !Array.isArray(value.items) ||
    value.items.length > 20
  ) {
    return contractFailure();
  }
  return {
    status: value.status,
    source: parseSource(value.source),
    items: value.items.map(parseTerritoryUnit),
  };
}

export function parseTerritoryResolutionResult(
  value: unknown,
): TerritoryResolutionResult {
  if (
    !isRecord(value) ||
    !hasExactKeys(value, ["status", "source", "unit", "fallback"]) ||
    (value.status !== "fresh" &&
      value.status !== "stale" &&
      value.status !== "unavailable") ||
    !isRecord(value.fallback) ||
    !hasExactKeys(value.fallback, ["searchAvailable"]) ||
    value.fallback.searchAvailable !== true
  ) {
    return contractFailure();
  }
  return {
    status: value.status,
    source: parseNullableSource(value.source),
    unit: value.unit === null ? null : parseTerritoryUnit(value.unit),
    fallback: { searchAvailable: true },
  };
}

async function readJson(response: Response): Promise<unknown> {
  const text = await response.text();
  if (text.length === 0) {
    return contractFailure();
  }
  try {
    return JSON.parse(text);
  } catch {
    return contractFailure();
  }
}

function classifyFailure(status: number): TerritoryFailureKind {
  if (status === 401) {
    return "signed-out";
  }
  if (status === 400) {
    return "validation";
  }
  if (status === 429) {
    return "rate-limited";
  }
  if (status === 503) {
    return "unavailable";
  }
  return "error";
}

async function getJson(path: string, signal?: AbortSignal): Promise<unknown> {
  let response: Response;
  try {
    response = await fetch(path, {
      cache: "no-store",
      credentials: "include",
      headers: { Accept: "application/json" },
      signal,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") {
      throw error;
    }
    throw new TerritoryApiError("network", 0);
  }
  if (!response.ok) {
    throw new TerritoryApiError(
      classifyFailure(response.status),
      response.status,
    );
  }
  return readJson(response);
}

function validateSearchParameters(
  parameters: TerritorySearchParameters,
): Required<Pick<TerritorySearchParameters, "query" | "limit">> &
  Pick<TerritorySearchParameters, "level" | "parentCode"> {
  const query = parameters.query.trim();
  const parentCode = parameters.parentCode?.trim();
  const limit = parameters.limit ?? 10;
  if (
    query.length < 2 ||
    query.length > 80 ||
    (parameters.level !== undefined &&
      !TERRITORY_LEVELS.has(parameters.level)) ||
    (parentCode !== undefined &&
      (parentCode.length < 2 ||
        parentCode.length > 16 ||
        !/^[0-9]+$/.test(parentCode))) ||
    !Number.isInteger(limit) ||
    limit < 1 ||
    limit > 20
  ) {
    throw new TerritoryApiError("validation", 400);
  }
  return {
    query,
    limit,
    level: parameters.level,
    parentCode,
  };
}

export async function searchTerritories(
  parameters: TerritorySearchParameters,
  signal?: AbortSignal,
): Promise<TerritorySearchResult> {
  const validated = validateSearchParameters(parameters);
  const query = new URLSearchParams({
    query: validated.query,
    limit: String(validated.limit),
  });
  if (validated.level !== undefined) {
    query.set("level", validated.level);
  }
  if (validated.parentCode !== undefined) {
    query.set("parentCode", validated.parentCode);
  }
  return parseTerritorySearchResult(
    await getJson(`/api/territory/search?${query.toString()}`, signal),
  );
}

function validateCoordinate(coordinate: TerritoryCoordinate): void {
  if (
    !Number.isFinite(coordinate.latitude) ||
    !Number.isFinite(coordinate.longitude) ||
    coordinate.latitude < -90 ||
    coordinate.latitude > 90 ||
    coordinate.longitude < -180 ||
    coordinate.longitude > 180
  ) {
    throw new TerritoryApiError("validation", 400);
  }
}

export async function resolveTerritory(
  coordinate: TerritoryCoordinate,
  signal?: AbortSignal,
): Promise<TerritoryResolutionResult> {
  validateCoordinate(coordinate);
  const query = new URLSearchParams({
    latitude: String(coordinate.latitude),
    longitude: String(coordinate.longitude),
  });
  return parseTerritoryResolutionResult(
    await getJson(`/api/territory/resolve?${query.toString()}`, signal),
  );
}
