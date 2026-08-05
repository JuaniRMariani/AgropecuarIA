export type CatalogDomain = "VEGETAL" | "ANIMAL";

export type SupportLevel =
  | "CATALOGADA"
  | "FLUJO_GENERICO"
  | "ESPECIALIZADA_VALIDADA";

export type ReviewStatus = "APPROVED" | "REVIEW_REQUIRED";

export type CatalogEntry = Readonly<{
  domain: CatalogDomain;
  code: string;
  familyCode: string;
  canonicalName: string;
  scientificName: string | null;
  supportLevel: SupportLevel;
  reviewStatus: ReviewStatus;
  sourceIds: readonly string[];
  aliases: readonly string[];
  regulated: boolean;
  requiresValidatedProfile: boolean;
}>;

export type CatalogMetadata = Readonly<{
  catalogVersion: string;
  cutoffDate: string;
  status: "CANDIDATE";
  assumptions: readonly string[];
}>;

export type CatalogPrototypeData = Readonly<{
  metadata: CatalogMetadata;
  entries: readonly CatalogEntry[];
}>;
