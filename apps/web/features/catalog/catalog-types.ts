export type CatalogVersion = Readonly<{
  id: string;
  versionTag: string;
  publishedAtUtc: string;
  isActive: boolean;
  itemsCount: number;
}>;

export type CatalogVersions = Readonly<{
  activeVersionId: string | null;
  totalCount: number;
  hasMore: boolean;
  versions: readonly CatalogVersion[];
}>;

export type CatalogItem = Readonly<{
  id: string;
  code: string;
  displayName: string;
  jurisdiction: string;
  supportLevel: string;
  category: string;
  synonyms: readonly string[];
  versionId: string;
  versionTag: string;
  activeVersionId: string | null;
  sourceSnapshotId: string | null;
  sourceId: string | null;
  sourceHash: string | null;
  sourceIngestedAtUtc: string | null;
  provenanceStatus: "verified_snapshot" | "legacy_unavailable";
  capabilities: readonly string[];
  absentCapabilities: readonly string[];
}>;

export type CatalogSearch = Readonly<{
  versionId: string | null;
  versionTag: string | null;
  activeVersionId: string | null;
  publishedAtUtc: string | null;
  isHistorical: boolean;
  totalCount: number;
  items: readonly CatalogItem[];
}>;

export type CatalogFilters = Readonly<{
  query: string;
  jurisdiction: string;
  category: string;
  supportLevel: string;
  versionId: string | null;
}>;
