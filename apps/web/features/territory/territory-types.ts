export type TerritoryLevel =
  "province" | "department" | "municipality" | "locality";

export type TerritorySource = Readonly<{
  provider: "georef";
  version: string;
  capturedAtUtc: string;
}>;

export type TerritoryUnit = Readonly<{
  officialCode: string;
  name: string;
  level: TerritoryLevel;
  parentCode: string | null;
  parentName: string | null;
  hierarchyLabel: string;
}>;

export type TerritorySearchResult = Readonly<{
  status: "fresh" | "stale";
  source: TerritorySource;
  items: readonly TerritoryUnit[];
}>;

export type TerritoryResolutionResult = Readonly<{
  status: "fresh" | "stale" | "unavailable";
  source: TerritorySource | null;
  unit: TerritoryUnit | null;
  fallback: Readonly<{ searchAvailable: true }>;
}>;

export type TerritorySearchParameters = Readonly<{
  query: string;
  level?: TerritoryLevel;
  parentCode?: string;
  limit?: number;
}>;

export type TerritoryCoordinate = Readonly<{
  latitude: number;
  longitude: number;
}>;
