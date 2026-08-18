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
  strongAuthenticationAvailable: boolean;
  ownerInvitationsAvailable: boolean;
  connections: readonly AvailableConnection[];
}>;

export type StepUpPurpose =
  | "manage_authentication_methods"
  | "manage_organization_owners"
  | "manage_sessions";

export type AuthenticationAssurance = Readonly<{
  level: "primary" | "strong";
  authenticatedAtUtc: string;
  purpose: StepUpPurpose | null;
  strongAuthenticatedAtUtc: string | null;
  expiresAtUtc: string | null;
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

export type OrganizationSummary = Readonly<{
  organizationId: string;
  displayName: string;
  status: "active";
}>;

export type CreatedOwnerMembership = Readonly<{
  membershipId: string;
  role: "owner";
  status: "active";
  authorizationVersion: number;
}>;

export type CreatedOrganization = Readonly<{
  organization: OrganizationSummary;
  membership: CreatedOwnerMembership;
}>;

export type OwnerInvitationStatus =
  "pending" | "accepted" | "revoked" | "expired";

export type OwnerInvitationSummary = Readonly<{
  invitationId: string;
  organizationId: string;
  status: OwnerInvitationStatus;
  createdAtUtc: string;
  expiresAtUtc: string;
  acceptedAtUtc: string | null;
  revokedAtUtc: string | null;
  version: string;
}>;

export type CreatedOwnerInvitation = Readonly<{
  invitation: OwnerInvitationSummary;
  token: string | null;
  isReplay: boolean;
}>;

export type OrganizationOwnerMembershipSummary = Readonly<{
  membershipId: string;
  organizationId: string;
  displayName: string;
  isCurrentUser: boolean;
  role: "owner";
  status: "active";
  authorizationVersion: number;
  createdAtUtc: string;
  removedAtUtc: null;
  version: string;
}>;

export type RemovedOrganizationOwnerMembership = Readonly<{
  membershipId: string;
  organizationId: string;
  displayName: string;
  isCurrentUser: boolean;
  role: "owner";
  status: "removed";
  authorizationVersion: number;
  createdAtUtc: string;
  removedAtUtc: string;
  version: string;
  isReplay: boolean;
}>;

export type OwnerMembershipResourceState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{ kind: "loading" }>
  | Readonly<{
      kind: "ready";
      items: readonly OrganizationOwnerMembershipSummary[];
    }>
  | Readonly<{ kind: "offline" }>
  | Readonly<{ kind: "unavailable" }>
  | Readonly<{ kind: "service-unavailable" }>
  | Readonly<{ kind: "error" }>;

export type OwnerMembershipActionState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{
      kind: "confirming";
      membership: OrganizationOwnerMembershipSummary;
    }>
  | Readonly<{
      kind: "removing";
      organizationId: string;
      membershipId: string;
    }>
  | Readonly<{ kind: "reauthentication-required"; message: string }>
  | Readonly<{ kind: "last-owner"; message: string }>
  | Readonly<{ kind: "stale"; message: string }>
  | Readonly<{ kind: "unavailable"; message: string }>
  | Readonly<{ kind: "service-unavailable"; message: string }>
  | Readonly<{ kind: "rate-limited"; message: string }>
  | Readonly<{ kind: "conflict"; message: string }>
  | Readonly<{ kind: "offline"; message: string }>
  | Readonly<{ kind: "error"; message: string }>
  | Readonly<{
      kind: "removed";
      organizationId: string;
      membershipId: string;
      message: string;
    }>;

export type OwnerInvitationResourceState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{ kind: "loading" }>
  | Readonly<{ kind: "ready"; items: readonly OwnerInvitationSummary[] }>
  | Readonly<{ kind: "offline" }>
  | Readonly<{ kind: "error" }>;

export type OwnerInvitationActionState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{ kind: "creating"; organizationId: string }>
  | Readonly<{
      kind: "revoking";
      organizationId: string;
      invitationId: string;
    }>
  | Readonly<{
      kind: "revealed";
      organizationId: string;
      invitationId: string;
      shareLink: string;
    }>
  | Readonly<{ kind: "reauthentication-required"; message: string }>
  | Readonly<{ kind: "conflict"; message: string }>
  | Readonly<{ kind: "offline"; message: string }>
  | Readonly<{ kind: "error"; message: string }>;

export type OwnerInvitationAcceptanceState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{ kind: "accepting" }>
  | Readonly<{ kind: "reauthentication-required"; message: string }>
  | Readonly<{ kind: "unavailable"; message: string }>
  | Readonly<{ kind: "conflict"; message: string }>
  | Readonly<{ kind: "offline"; message: string }>
  | Readonly<{ kind: "error"; message: string }>;

export type OrganizationCreationState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{ kind: "submitting" }>
  | Readonly<{ kind: "validation"; message: string }>
  | Readonly<{ kind: "reauthentication-required"; message: string }>
  | Readonly<{ kind: "in-progress"; message: string }>
  | Readonly<{ kind: "conflict"; message: string }>
  | Readonly<{ kind: "reconciliation-required"; message: string }>
  | Readonly<{ kind: "rate-limited"; message: string }>
  | Readonly<{ kind: "offline"; message: string }>
  | Readonly<{ kind: "error"; message: string }>;

export type IdentitySession = Readonly<{
  userId: string;
  displayName: string;
  authentication: AuthenticationAssurance;
  identities: readonly LinkedIdentity[];
  memberships: readonly MembershipSummary[];
}>;

export type OwnSessionSummary = Readonly<{
  sessionId: string;
  authenticatedAtUtc: string;
  expiresAtUtc: string;
  isCurrent: boolean;
  version: string;
}>;

export type OwnSessionPage = Readonly<{
  items: readonly OwnSessionSummary[];
  total: number;
  offset: number;
  limit: number;
}>;

export type OwnSessionResourceState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{ kind: "loading" }>
  | Readonly<{ kind: "ready"; page: OwnSessionPage }>
  | Readonly<{ kind: "offline" }>
  | Readonly<{ kind: "unavailable" }>
  | Readonly<{ kind: "error" }>;

export type OwnSessionActionState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{ kind: "confirming"; session: OwnSessionSummary }>
  | Readonly<{ kind: "revoking"; sessionId: string }>
  | Readonly<{
      kind: "reauthentication-required";
      session: OwnSessionSummary;
      message: string;
    }>
  | Readonly<{ kind: "stale"; message: string }>
  | Readonly<{ kind: "unavailable"; message: string }>
  | Readonly<{ kind: "offline"; message: string }>
  | Readonly<{ kind: "error"; message: string }>
  | Readonly<{ kind: "revoked"; message: string }>;

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
  "link-email" | "link-google" | "step-up" | "unlink" | "revoke" | "synthetic";

export type LinkStart = Readonly<{
  attemptId: string;
  connection: IdentityConnection;
  expiresAtUtc: string;
  authorizationUrl: string;
}>;

export type StepUpAttempt = Readonly<{
  attemptId: string;
  purpose: StepUpPurpose;
  expiresAtUtc: string;
  authorizationUrl: string;
}>;

export const NO_IDENTITY_NOTICE: IdentityNotice = { kind: "none" };
