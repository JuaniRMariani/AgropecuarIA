import type {
  CatalogItem,
  CatalogSearch,
  CatalogVersion,
  CatalogVersions,
} from "../../features/catalog/catalog-types";

export const currentVersion: CatalogVersion = {
  id: "10000000-0000-4000-8000-000000000001",
  versionTag: "Fixture sintético v2",
  publishedAtUtc: "2026-09-04T12:00:00Z",
  isActive: true,
  itemsCount: 1,
};
export const historicalVersion: CatalogVersion = {
  id: "20000000-0000-4000-8000-000000000002",
  versionTag: "Fixture sintético v1",
  publishedAtUtc: "2026-09-03T12:00:00Z",
  isActive: false,
  itemsCount: 1,
};
export const catalogItem: CatalogItem = {
  id: "30000000-0000-4000-8000-000000000003",
  code: "MAIZ",
  displayName: "Maíz de prueba",
  jurisdiction: "AR",
  category: "AGRICULTURA",
  supportLevel: "FLUJO_GENERICO",
  synonyms: ["Zea mays", "Choclo"],
  versionId: currentVersion.id,
  versionTag: currentVersion.versionTag,
  activeVersionId: currentVersion.id,
  sourceSnapshotId: "40000000-0000-4000-8000-000000000004",
  sourceId: "fuente-sintetica-local",
  sourceHash: "a".repeat(64),
  sourceIngestedAtUtc: "2026-09-04T10:00:00Z",
  provenanceStatus: "verified_snapshot",
  capabilities: [],
  absentCapabilities: [
    "specialized_rules",
    "specialized_kpis",
    "ai_recommendations",
  ],
};
export const catalogResult: CatalogSearch = {
  versionId: currentVersion.id,
  versionTag: currentVersion.versionTag,
  activeVersionId: currentVersion.id,
  publishedAtUtc: currentVersion.publishedAtUtc,
  isHistorical: false,
  totalCount: 1,
  items: [catalogItem],
};
export const catalogVersions: CatalogVersions = {
  activeVersionId: currentVersion.id,
  totalCount: 2,
  hasMore: false,
  versions: [currentVersion, historicalVersion],
};
