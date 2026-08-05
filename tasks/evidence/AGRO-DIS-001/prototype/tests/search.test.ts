import assert from "node:assert/strict";
import test from "node:test";

import type { CatalogEntry } from "../lib/catalog-types";
import { normalizeSearchValue, searchCatalog } from "../lib/search";

const entries: readonly CatalogEntry[] = [
  {
    domain: "VEGETAL",
    code: "AR-VEG-MAIZ",
    familyCode: "CEREALES",
    canonicalName: "maíz",
    scientificName: "Zea mays",
    supportLevel: "CATALOGADA",
    reviewStatus: "APPROVED",
    sourceIds: ["SRC-UNO"],
    aliases: ["choclo"],
    regulated: false,
    requiresValidatedProfile: false,
  },
  {
    domain: "ANIMAL",
    code: "AR-ANI-AVES-PONEDORAS",
    familyCode: "AVES",
    canonicalName: "Ponedoras",
    scientificName: null,
    supportLevel: "CATALOGADA",
    reviewStatus: "APPROVED",
    sourceIds: ["SRC-DOS"],
    aliases: ["gallinas de postura"],
    regulated: false,
    requiresValidatedProfile: false,
  },
];

test("normaliza tildes, mayúsculas y separadores", () => {
  assert.equal(normalizeSearchValue("  MAÍZ-del-Sur "), "maiz del sur");
});

test("busca por nombre canónico sin distinguir tildes", () => {
  const result = searchCatalog(entries, { query: "MAIZ", domain: "ALL", support: "ALL" });
  assert.deepEqual(result.map((entry) => entry.code), ["AR-VEG-MAIZ"]);
});

test("busca por código normalizado y por alias", () => {
  const byCode = searchCatalog(entries, {
    query: "ar ani aves ponedoras",
    domain: "ALL",
    support: "ALL",
  });
  const byAlias = searchCatalog(entries, {
    query: "GALLINAS DE POSTURA",
    domain: "ALL",
    support: "ALL",
  });

  assert.deepEqual(byCode.map((entry) => entry.code), ["AR-ANI-AVES-PONEDORAS"]);
  assert.deepEqual(byAlias.map((entry) => entry.code), ["AR-ANI-AVES-PONEDORAS"]);
});

test("combina filtros de dominio y soporte sin inventar coincidencias", () => {
  const animal = searchCatalog(entries, { query: "", domain: "ANIMAL", support: "CATALOGADA" });
  const unavailableSupport = searchCatalog(entries, {
    query: "",
    domain: "ALL",
    support: "ESPECIALIZADA_VALIDADA",
  });

  assert.deepEqual(animal.map((entry) => entry.code), ["AR-ANI-AVES-PONEDORAS"]);
  assert.deepEqual(unavailableSupport, []);
});
