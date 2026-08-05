import "server-only";

import { readFile } from "node:fs/promises";
import { basename, resolve } from "node:path";

import type {
  CatalogDomain,
  CatalogEntry,
  CatalogPrototypeData,
  ReviewStatus,
  SupportLevel,
} from "./catalog-types";

const EVIDENCE_DIRECTORY = resolve(process.cwd(), "..");
const PLANTS_FILE = resolve(EVIDENCE_DIRECTORY, "catalog-plants-v1.json");
const ANIMALS_FILE = resolve(EVIDENCE_DIRECTORY, "catalog-animals-v1.json");

const CATALOG_CUTOFF_DATE = "2026-08-04";

type CatalogDocument = Readonly<{
  catalogVersion: string;
  domain: CatalogDomain;
  status: "CANDIDATE";
  denominatorDefinition: string;
  entries: readonly CatalogEntry[];
}>;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function readString(value: unknown, field: string): string {
  if (typeof value !== "string" || value.trim().length === 0) {
    throw new Error(`El catálogo no contiene un valor válido para ${field}.`);
  }

  return value;
}

function readNullableString(value: unknown, field: string): string | null {
  if (value === null) {
    return null;
  }

  return readString(value, field);
}

function readBoolean(value: unknown, field: string): boolean {
  if (typeof value !== "boolean") {
    throw new Error(`El catálogo no contiene un booleano válido para ${field}.`);
  }

  return value;
}

function readStringArray(value: unknown, field: string): readonly string[] {
  if (!Array.isArray(value)) {
    throw new Error(`El catálogo no contiene una lista válida para ${field}.`);
  }

  return value.map((item, index) => readString(item, `${field}[${index}]`));
}

function readDomain(value: unknown): CatalogDomain {
  if (value === "VEGETAL" || value === "ANIMAL") {
    return value;
  }

  throw new Error("El catálogo contiene un dominio desconocido.");
}

function readSupportLevel(value: unknown): SupportLevel {
  if (
    value === "CATALOGADA" ||
    value === "FLUJO_GENERICO" ||
    value === "ESPECIALIZADA_VALIDADA"
  ) {
    return value;
  }

  throw new Error("El catálogo contiene un nivel de soporte desconocido.");
}

function readReviewStatus(value: unknown): ReviewStatus {
  if (value === "APPROVED" || value === "REVIEW_REQUIRED") {
    return value;
  }

  throw new Error("El catálogo contiene un estado de revisión desconocido.");
}

function readEntry(value: unknown, domain: CatalogDomain, index: number): CatalogEntry {
  if (!isRecord(value)) {
    throw new Error(`La entrada ${domain}[${index}] no es un objeto.`);
  }

  return {
    domain,
    code: readString(value.code, `${domain}[${index}].code`),
    familyCode: readString(value.familyCode, `${domain}[${index}].familyCode`),
    canonicalName: readString(value.canonicalName, `${domain}[${index}].canonicalName`),
    scientificName: readNullableString(
      value.scientificName,
      `${domain}[${index}].scientificName`,
    ),
    supportLevel: readSupportLevel(value.supportLevel),
    reviewStatus: readReviewStatus(value.reviewStatus),
    sourceIds: readStringArray(value.sourceIds, `${domain}[${index}].sourceIds`),
    aliases: readStringArray(value.aliases, `${domain}[${index}].aliases`),
    regulated: readBoolean(value.regulated, `${domain}[${index}].regulated`),
    requiresValidatedProfile: readBoolean(
      value.requiresValidatedProfile,
      `${domain}[${index}].requiresValidatedProfile`,
    ),
  };
}

function parseCatalogDocument(raw: string, sourceName: string): CatalogDocument {
  const parsed: unknown = JSON.parse(raw);
  if (!isRecord(parsed)) {
    throw new Error(`${sourceName} no contiene un documento JSON válido.`);
  }

  const domain = readDomain(parsed.domain);
  const status = readString(parsed.status, `${sourceName}.status`);
  if (status !== "CANDIDATE") {
    throw new Error(`${sourceName} no está marcado como CANDIDATE.`);
  }

  if (!Array.isArray(parsed.entries)) {
    throw new Error(`${sourceName} no contiene una lista de entradas.`);
  }

  return {
    catalogVersion: readString(parsed.catalogVersion, `${sourceName}.catalogVersion`),
    domain,
    status,
    denominatorDefinition: readString(
      parsed.denominatorDefinition,
      `${sourceName}.denominatorDefinition`,
    ),
    entries: parsed.entries.map((entry, index) => readEntry(entry, domain, index)),
  };
}

async function loadDocument(path: string): Promise<CatalogDocument> {
  const raw = await readFile(path, "utf8");
  return parseCatalogDocument(raw, basename(path));
}

export async function loadCatalogPrototypeData(): Promise<CatalogPrototypeData> {
  const [plants, animals] = await Promise.all([
    loadDocument(PLANTS_FILE),
    loadDocument(ANIMALS_FILE),
  ]);

  if (plants.catalogVersion !== animals.catalogVersion) {
    throw new Error("Los catálogos vegetal y animal no comparten la misma versión.");
  }

  return {
    metadata: {
      catalogVersion: plants.catalogVersion,
      cutoffDate: CATALOG_CUTOFF_DATE,
      status: "CANDIDATE",
      assumptions: [plants.denominatorDefinition, animals.denominatorDefinition],
    },
    entries: [...plants.entries, ...animals.entries],
  };
}
