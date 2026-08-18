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

export type FieldOrganization = Readonly<{
  organizationId: string;
  organizationName: string;
  role: string;
}>;
