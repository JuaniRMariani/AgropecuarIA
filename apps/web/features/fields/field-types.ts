export type FieldSummary = Readonly<{
  fieldId: string;
  organizationId: string;
  displayName: string;
  type: "field";
  status: "draft" | "archived";
  spatialStatus: "not_configured" | "configured";
  createdAtUtc: string;
  version: string;
}>;

export type CreatedField = FieldSummary &
  Readonly<{
    status: "draft";
    spatialStatus: "not_configured";
    isReplay: boolean;
  }>;

export type RenamedField = FieldSummary &
  Readonly<{
    status: "draft";
    isReplay: boolean;
    revision: number;
  }>;

export type ArchivedField = FieldSummary &
  Readonly<{
    status: "archived";
    isReplay: boolean;
    revision: number;
  }>;

export type FieldOrganization = Readonly<{
  organizationId: string;
  organizationName: string;
  role: string;
}>;
