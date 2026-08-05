import { readFileSync } from "node:fs";
import { resolve } from "node:path";

export type EvidenceNature = "observed" | "estimated" | "forecast";
export type Freshness = "fresh" | "stale" | "unavailable";

export type TerritoryPoint = Readonly<{
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  evidence: EvidenceNature;
  freshness: Freshness;
}>;

export type TerritoryFixture = Readonly<{
  fixtureVersion: string;
  source: string;
  purpose: string;
  points: readonly TerritoryPoint[];
}>;

type RawPoint = Readonly<{
  id: string;
  name: string;
  latitude: number;
  longitude: number;
}>;

type RawFixture = Readonly<{
  fixtureVersion: string;
  source: string;
  purpose: string;
  points: readonly RawPoint[];
}>;

const ID_PATTERN = /^\d{2}$/;
function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function isRawPoint(value: unknown): value is RawPoint {
  if (!isRecord(value)) return false;

  return (
    typeof value.id === "string" &&
    ID_PATTERN.test(value.id) &&
    typeof value.name === "string" &&
    value.name.length > 0 &&
    typeof value.latitude === "number" &&
    Number.isFinite(value.latitude) &&
    value.latitude >= -90 &&
    value.latitude <= 90 &&
    typeof value.longitude === "number" &&
    Number.isFinite(value.longitude) &&
    value.longitude >= -180 &&
    value.longitude <= 180
  );
}

function isRawFixture(value: unknown): value is RawFixture {
  if (!isRecord(value)) return false;

  return (
    typeof value.fixtureVersion === "string" &&
    typeof value.source === "string" &&
    typeof value.purpose === "string" &&
    Array.isArray(value.points) &&
    value.points.every(isRawPoint)
  );
}

export function parseTerritoryFixture(value: unknown): TerritoryFixture {
  if (!isRawFixture(value)) {
    throw new Error("El fixture territorial no cumple el contrato esperado.");
  }

  if (value.points.length !== 24) {
    throw new Error(
      `Se esperaban 24 jurisdicciones y se recibieron ${value.points.length}.`,
    );
  }

  const ids = new Set(value.points.map((point) => point.id));
  if (ids.size !== value.points.length) {
    throw new Error(
      "El fixture territorial contiene identificadores duplicados.",
    );
  }

  return {
    fixtureVersion: value.fixtureVersion,
    source: value.source,
    purpose: value.purpose,
    points: value.points.map((point) => ({
      ...point,
      evidence: "observed",
      freshness: "fresh",
    })),
  };
}

function readTerritoryFixture(): unknown {
  const fixturePath = resolve(
    process.cwd(),
    "../../fixtures/territory/national-points.json",
  );
  return JSON.parse(readFileSync(fixturePath, "utf8"));
}

export const territoryFixture = parseTerritoryFixture(readTerritoryFixture());
