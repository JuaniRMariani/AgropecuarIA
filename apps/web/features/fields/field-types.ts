export type FieldSummary = Readonly<{
  fieldId: string;
  organizationId: string;
  displayName: string;
  type: "field";
  status: "draft";
  spatialStatus: "not_configured";
  createdAtUtc: string;
  version: string;
}>;

export type CreatedField = FieldSummary &
  Readonly<{
    isReplay: boolean;
  }>;

export type RenamedField = FieldSummary &
  Readonly<{
    isReplay: boolean;
    revision: number;
  }>;

export type FieldOrganization = Readonly<{
  organizationId: string;
  organizationName: string;
  role: string;
}>;
