import { describe, expect, it } from "vitest";
import {
  canAccessOrganization,
  evaluateLinkPolicy,
  identityReducer,
  shortUuid,
  type IdentityState,
  type OrganizationSummary,
  type PrototypeUser,
} from "./identity-machine";

const user: PrototypeUser = {
  id: "9f4c2d11-88a1-4ef8-99c1-5b4ba407a532",
  name: "Marina",
  email: "marina@example.test",
};

const organization: OrganizationSummary = {
  id: "a10b2c33-164d-48d3-8f8b-f712de9db101",
  name: "La Rinconada",
  locality: "Pergamino",
  role: "OWNER",
  type: "PRODUCER",
};

describe("identityReducer", () => {
  it("classifies zero, one and many organizations explicitly", () => {
    const loading: IdentityState = { status: "loading", intent: "sign-in" };
    const second = { ...organization, id: "b71d9e24-e529-4ed7-a709-03f1a5b456c2" };

    expect(identityReducer(loading, { type: "AUTHENTICATED", user, organizations: [] }).status).toBe("zero-organizations");
    expect(identityReducer(loading, { type: "AUTHENTICATED", user, organizations: [organization] }).status).toBe("one-organization");
    expect(identityReducer(loading, { type: "AUTHENTICATED", user, organizations: [organization, second] }).status).toBe("many-organizations");
  });

  it("fails closed when an organization outside membership is requested", () => {
    const state: IdentityState = { status: "many-organizations", user, organizations: [organization] };
    expect(identityReducer(state, { type: "ACTIVATE_ORGANIZATION", organizationId: "unknown" })).toEqual({
      status: "revoked",
      reason: "invalid-organization",
    });
  });

  it("does not complete linking until both identities reauthenticate", () => {
    const linking: IdentityState = {
      status: "linking",
      user,
      organization,
      organizations: [organization],
      primaryReauthenticated: true,
      secondaryReauthenticated: false,
    };

    expect(identityReducer(linking, { type: "LINKING_CONFIRMED" })).toBe(linking);
    const ready = identityReducer(linking, { type: "SECONDARY_REAUTHENTICATED" });
    expect(identityReducer(ready, { type: "LINKING_CONFIRMED" }).status).toBe("active");
  });

  it("preserves the active organization when a linking conflict is cancelled", () => {
    const conflict: IdentityState = {
      status: "linking-conflict",
      user,
      organization,
      organizations: [organization],
    };

    const recovered = identityReducer(conflict, { type: "CANCEL_LINKING" });
    expect(recovered.status).toBe("active");
    expect(recovered.status === "active" ? recovered.organization : undefined).toBe(organization);
  });
});

describe("identity policies", () => {
  it("shows only the first six normalized identifier characters", () => {
    expect(shortUuid(organization.id)).toBe("A10B2C");
  });

  it("checks tenant membership by the full internal identifier", () => {
    expect(canAccessOrganization([organization], organization.id)).toBe(true);
    expect(canAccessOrganization([organization], "A10B2C")).toBe(false);
  });

  it("never treats matching emails as sufficient account-link authority", () => {
    expect(evaluateLinkPolicy({
      accountOwnedByAnotherIdentity: false,
      primaryReauthenticated: true,
      secondaryReauthenticated: true,
      emailsMatch: true,
    })).toBe("requires-explicit-confirmation");
  });

  it("prioritizes ownership conflicts and otherwise requires both reauthentications", () => {
    expect(evaluateLinkPolicy({
      accountOwnedByAnotherIdentity: true,
      primaryReauthenticated: true,
      secondaryReauthenticated: true,
      emailsMatch: false,
    })).toBe("blocked-conflict");
    expect(evaluateLinkPolicy({
      accountOwnedByAnotherIdentity: false,
      primaryReauthenticated: true,
      secondaryReauthenticated: false,
      emailsMatch: false,
    })).toBe("blocked-reauthentication");
  });
});
