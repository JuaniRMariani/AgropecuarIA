export type IdentityConnection = "email" | "google";

export type AvailableConnection = Readonly<{
  id: IdentityConnection;
  label: string;
  available: boolean;
}>;

export type IdentityCapabilities = Readonly<{
  environment: string;
  oidcConfigured: boolean;
  developmentProviderEnabled: boolean;
  connections: readonly AvailableConnection[];
}>;

export type LinkedIdentity = Readonly<{
  identityId: string;
  connection: IdentityConnection;
  label: string;
  verifiedAtUtc: string;
}>;

export type MembershipSummary = Readonly<{
  organizationId: string;
  organizationName: string;
  role: string;
}>;

export type IdentitySession = Readonly<{
  userId: string;
  displayName: string;
  identities: readonly LinkedIdentity[];
  memberships: readonly MembershipSummary[];
}>;

export type IdentityResourceState =
  | Readonly<{ kind: "loading" }>
  | Readonly<{ kind: "signed-out"; capabilities: IdentityCapabilities }>
  | Readonly<{
      kind: "authenticated";
      capabilities: IdentityCapabilities;
      session: IdentitySession;
    }>
  | Readonly<{ kind: "revoked"; capabilities: IdentityCapabilities | null }>
  | Readonly<{
      kind: "provider-down";
      capabilities: IdentityCapabilities | null;
    }>
  | Readonly<{ kind: "error"; capabilities: IdentityCapabilities | null }>;

export type IdentityNotice =
  | Readonly<{ kind: "none" }>
  | Readonly<{ kind: "success"; message: string }>
  | Readonly<{ kind: "conflict"; message: string }>
  | Readonly<{ kind: "replay"; message: string }>
  | Readonly<{ kind: "provider-down"; message: string }>
  | Readonly<{ kind: "error"; message: string }>;

export type IdentityAction =
  "link-email" | "link-google" | "unlink" | "revoke" | "synthetic";

export type LinkStart = Readonly<{
  attemptId: string;
  connection: IdentityConnection;
  expiresAtUtc: string;
  authorizationUrl: string;
}>;

export const NO_IDENTITY_NOTICE: IdentityNotice = { kind: "none" };
