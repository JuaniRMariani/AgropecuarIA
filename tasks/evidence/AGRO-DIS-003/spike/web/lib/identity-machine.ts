export type OrganizationRole = "OWNER" | "ADVISOR";

export type OrganizationSummary = Readonly<{
  id: string;
  name: string;
  locality: string;
  role: OrganizationRole;
  type: "PRODUCER" | "COMPANY";
}>;

export type PrototypeUser = Readonly<{
  id: string;
  name: string;
  email: string;
}>;

export type JourneyIntent = "sign-in" | "switch-organization";

export type IdentityState =
  | Readonly<{ status: "signed-out" }>
  | Readonly<{ status: "loading"; intent: JourneyIntent }>
  | Readonly<{ status: "zero-organizations"; user: PrototypeUser }>
  | Readonly<{
      status: "one-organization";
      user: PrototypeUser;
      organization: OrganizationSummary;
    }>
  | Readonly<{
      status: "many-organizations";
      user: PrototypeUser;
      organizations: readonly OrganizationSummary[];
    }>
  | Readonly<{
      status: "active";
      user: PrototypeUser;
      organization: OrganizationSummary;
      organizations: readonly OrganizationSummary[];
    }>
  | Readonly<{ status: "provider-down"; intent: JourneyIntent }>
  | Readonly<{
      status: "linking";
      user: PrototypeUser;
      organization: OrganizationSummary;
      organizations: readonly OrganizationSummary[];
      primaryReauthenticated: boolean;
      secondaryReauthenticated: boolean;
    }>
  | Readonly<{
      status: "linking-conflict";
      user: PrototypeUser;
      organization: OrganizationSummary;
      organizations: readonly OrganizationSummary[];
    }>
  | Readonly<{ status: "recovery-accepted" }>
  | Readonly<{ status: "rate-limited"; retryAfterMinutes: number }>
  | Readonly<{ status: "revoked"; reason: "access-removed" | "invalid-organization" }>;

export type IdentityEvent =
  | Readonly<{ type: "START_LOGIN" }>
  | Readonly<{
      type: "AUTHENTICATED";
      user: PrototypeUser;
      organizations: readonly OrganizationSummary[];
    }>
  | Readonly<{ type: "ACTIVATE_ORGANIZATION"; organizationId: string }>
  | Readonly<{ type: "OPEN_ORGANIZATION_PICKER" }>
  | Readonly<{ type: "PROVIDER_FAILED" }>
  | Readonly<{ type: "START_LINKING" }>
  | Readonly<{ type: "PRIMARY_REAUTHENTICATED" }>
  | Readonly<{ type: "SECONDARY_REAUTHENTICATED" }>
  | Readonly<{ type: "LINKING_CONFLICT" }>
  | Readonly<{ type: "LINKING_CONFIRMED" }>
  | Readonly<{ type: "CANCEL_LINKING" }>
  | Readonly<{ type: "RECOVERY_REQUESTED" }>
  | Readonly<{ type: "RATE_LIMIT_REACHED"; retryAfterMinutes: number }>
  | Readonly<{ type: "SESSION_REVOKED" }>
  | Readonly<{ type: "RESET" }>;

export type LinkPolicyDecision =
  | "blocked-conflict"
  | "blocked-reauthentication"
  | "requires-explicit-confirmation";

export function shortUuid(id: string): string {
  return id.replaceAll("-", "").slice(0, 6).toUpperCase();
}

export function canAccessOrganization(
  organizations: readonly OrganizationSummary[],
  organizationId: string,
): boolean {
  return organizations.some((organization) => organization.id === organizationId);
}

export function evaluateLinkPolicy(input: Readonly<{
  accountOwnedByAnotherIdentity: boolean;
  primaryReauthenticated: boolean;
  secondaryReauthenticated: boolean;
  emailsMatch: boolean;
}>): LinkPolicyDecision {
  if (input.accountOwnedByAnotherIdentity) {
    return "blocked-conflict";
  }

  if (!input.primaryReauthenticated || !input.secondaryReauthenticated) {
    return "blocked-reauthentication";
  }

  // Email coincidence is only evidence for the operator, never authorization.
  return "requires-explicit-confirmation";
}

function classifyAuthenticatedUser(
  user: PrototypeUser,
  organizations: readonly OrganizationSummary[],
): IdentityState {
  const firstOrganization = organizations[0];

  if (firstOrganization === undefined) {
    return { status: "zero-organizations", user };
  }

  if (organizations.length === 1) {
    return { status: "one-organization", user, organization: firstOrganization };
  }

  return { status: "many-organizations", user, organizations };
}

function activeFromLinking(
  state: Extract<IdentityState, { status: "linking" | "linking-conflict" }>,
): IdentityState {
  return {
    status: "active",
    user: state.user,
    organization: state.organization,
    organizations: state.organizations,
  };
}

export function identityReducer(state: IdentityState, event: IdentityEvent): IdentityState {
  if (event.type === "RESET") {
    return { status: "signed-out" };
  }

  if (event.type === "RECOVERY_REQUESTED") {
    return { status: "recovery-accepted" };
  }

  if (event.type === "RATE_LIMIT_REACHED") {
    return { status: "rate-limited", retryAfterMinutes: event.retryAfterMinutes };
  }

  if (event.type === "SESSION_REVOKED") {
    return { status: "revoked", reason: "access-removed" };
  }

  switch (state.status) {
    case "signed-out":
      return event.type === "START_LOGIN" ? { status: "loading", intent: "sign-in" } : state;
    case "loading":
      if (event.type === "AUTHENTICATED") {
        return classifyAuthenticatedUser(event.user, event.organizations);
      }

      return event.type === "PROVIDER_FAILED"
        ? { status: "provider-down", intent: state.intent }
        : state;
    case "provider-down":
      return event.type === "START_LOGIN" ? { status: "loading", intent: state.intent } : state;
    case "zero-organizations":
    case "recovery-accepted":
    case "rate-limited":
    case "revoked":
      return state;
    case "one-organization":
      if (event.type !== "ACTIVATE_ORGANIZATION") {
        return state;
      }

      return event.organizationId === state.organization.id
        ? {
            status: "active",
            user: state.user,
            organization: state.organization,
            organizations: [state.organization],
          }
        : { status: "revoked", reason: "invalid-organization" };
    case "many-organizations": {
      if (event.type !== "ACTIVATE_ORGANIZATION") {
        return state;
      }

      const organization = state.organizations.find((candidate) => candidate.id === event.organizationId);
      return organization === undefined
        ? { status: "revoked", reason: "invalid-organization" }
        : {
            status: "active",
            user: state.user,
            organization,
            organizations: state.organizations,
          };
    }
    case "active":
      if (event.type === "OPEN_ORGANIZATION_PICKER") {
        return classifyAuthenticatedUser(state.user, state.organizations);
      }

      if (event.type === "START_LINKING") {
        return {
          status: "linking",
          user: state.user,
          organization: state.organization,
          organizations: state.organizations,
          primaryReauthenticated: false,
          secondaryReauthenticated: false,
        };
      }

      return state;
    case "linking":
      if (event.type === "PRIMARY_REAUTHENTICATED") {
        return { ...state, primaryReauthenticated: true };
      }

      if (event.type === "SECONDARY_REAUTHENTICATED") {
        return { ...state, secondaryReauthenticated: true };
      }

      if (event.type === "LINKING_CONFLICT") {
        return {
          status: "linking-conflict",
          user: state.user,
          organization: state.organization,
          organizations: state.organizations,
        };
      }

      if (event.type === "LINKING_CONFIRMED") {
        return state.primaryReauthenticated && state.secondaryReauthenticated
          ? activeFromLinking(state)
          : state;
      }

      return event.type === "CANCEL_LINKING" ? activeFromLinking(state) : state;
    case "linking-conflict":
      return event.type === "CANCEL_LINKING" ? activeFromLinking(state) : state;
  }
}
