import { afterEach, describe, expect, it, vi } from "vitest";

import {
  IdentityApiError,
  invalidateAntiforgeryToken,
  parseIdentityCapabilities,
  parseIdentitySession,
  parseLinkStart,
  parseStepUpAttempt,
  resolveAuthorizationUrl,
  startLink,
} from "../../features/identity/identity-api";

const primaryAuthentication = {
  level: "primary",
  authenticatedAtUtc: "2026-08-05T10:00:00Z",
  purpose: null,
  strongAuthenticatedAtUtc: null,
  expiresAtUtc: null,
};

afterEach(() => {
  invalidateAntiforgeryToken();
  vi.unstubAllGlobals();
});

describe("identity contract parsing", () => {
  it("accepts the documented capability contract and ignores additive response fields", () => {
    expect(
      parseIdentityCapabilities({
        environment: "Development",
        oidcConfigured: false,
        developmentProviderEnabled: true,
        strongAuthenticationAvailable: true,
        connections: [
          { id: "email", label: "Correo", available: true, futureField: 1 },
        ],
        futureField: "compatible",
      }),
    ).toEqual({
      environment: "Development",
      oidcConfigured: false,
      developmentProviderEnabled: true,
      strongAuthenticationAvailable: true,
      connections: [{ id: "email", label: "Correo", available: true }],
    });
  });

  it("parses a session without exposing fields outside the shared contract", () => {
    expect(
      parseIdentitySession({
        userId: "2e9f14bd-eec9-4bd8-b1ca-da0c6c615f78",
        displayName: "Ana Productora",
        authentication: primaryAuthentication,
        identities: [
          {
            identityId: "6f0b9239-264a-4237-9d18-b4324a714849",
            connection: "email",
            label: "a***@campo.ar",
            verifiedAtUtc: "2026-08-05T10:00:00Z",
          },
        ],
        memberships: [
          {
            organizationId: "1266395e-ec88-481e-9a72-81fa2cc2904a",
            organizationName: "El Ombú",
            role: "Owner",
          },
        ],
      }),
    ).toMatchObject({
      displayName: "Ana Productora",
      memberships: [{ role: "Owner" }],
    });
  });

  it("rejects a session without a verified identity", () => {
    expect(() =>
      parseIdentitySession({
        userId: "2e9f14bd-eec9-4bd8-b1ca-da0c6c615f78",
        displayName: "Ana Productora",
        authentication: primaryAuthentication,
        identities: [],
        memberships: [],
      }),
    ).toThrow(IdentityApiError);
  });

  it("rejects an unknown identity connection", () => {
    expect(() =>
      parseLinkStart({
        attemptId: "d68a20db-bae4-4822-99bd-e016517bd0ee",
        connection: "synthetic",
        expiresAtUtc: "2026-08-05T10:05:00Z",
        authorizationUrl: "/provider",
      }),
    ).toThrow(IdentityApiError);
  });

  it("rejects malformed UUIDs at the HTTP boundary", () => {
    expect(() =>
      parseIdentitySession({
        userId: "not-a-uuid",
        displayName: "Ana Productora",
        authentication: primaryAuthentication,
        identities: [
          {
            identityId: "6f0b9239-264a-4237-9d18-b4324a714849",
            connection: "email",
            label: "a***@campo.ar",
            verifiedAtUtc: "2026-08-05T10:00:00Z",
          },
        ],
        memberships: [],
      }),
    ).toThrow(IdentityApiError);
  });

  it("parses a purpose-bound one-time step-up attempt", () => {
    expect(
      parseStepUpAttempt({
        attemptId: "d68a20db-bae4-4822-99bd-e016517bd0ee",
        purpose: "manage_authentication_methods",
        expiresAtUtc: "2026-08-05T10:05:00Z",
        authorizationUrl:
          "/api/identity/step-up/d68a20db-bae4-4822-99bd-e016517bd0ee",
      }),
    ).toMatchObject({ purpose: "manage_authentication_methods" });
  });

  it("rejects assurance with an unknown purpose", () => {
    expect(() =>
      parseIdentitySession({
        userId: "2e9f14bd-eec9-4bd8-b1ca-da0c6c615f78",
        displayName: "Ana Productora",
        authentication: { ...primaryAuthentication, purpose: "transfer_funds" },
        identities: [
          {
            identityId: "6f0b9239-264a-4237-9d18-b4324a714849",
            connection: "email",
            label: "a***@campo.ar",
            verifiedAtUtc: "2026-08-05T10:00:00Z",
          },
        ],
        memberships: [],
      }),
    ).toThrow(IdentityApiError);
  });

  it("rejects a strong level without a complete purpose-bound window", () => {
    expect(() =>
      parseIdentitySession({
        userId: "2e9f14bd-eec9-4bd8-b1ca-da0c6c615f78",
        displayName: "Ana Productora",
        authentication: { ...primaryAuthentication, level: "strong" },
        identities: [
          {
            identityId: "6f0b9239-264a-4237-9d18-b4324a714849",
            connection: "email",
            label: "a***@campo.ar",
            verifiedAtUtc: "2026-08-05T10:00:00Z",
          },
        ],
        memberships: [],
      }),
    ).toThrow(IdentityApiError);
  });
});

describe("identity mutations", () => {
  it("refreshes and retries once when the antiforgery token is stale", async () => {
    const linkResponse = {
      attemptId: "d68a20db-bae4-4822-99bd-e016517bd0ee",
      connection: "google",
      expiresAtUtc: "2026-08-05T10:05:00Z",
      authorizationUrl: "/provider",
    };
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "stale-token" }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ code: "request.invalid_antiforgery" }), {
          status: 400,
          headers: { "Content-Type": "application/json" },
        }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "fresh-token" }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(linkResponse), {
          status: 201,
          headers: { "Content-Type": "application/json" },
        }),
      );
    vi.stubGlobal("fetch", fetchMock);

    await expect(startLink("google")).resolves.toMatchObject(linkResponse);

    expect(fetchMock).toHaveBeenCalledTimes(4);
    expect(fetchMock).toHaveBeenNthCalledWith(
      4,
      "/api/identity/link-attempts",
      expect.objectContaining({
        headers: expect.objectContaining({ "X-CSRF-TOKEN": "fresh-token" }),
      }),
    );
  });

  it("sends cookies and an antiforgery token on a link attempt", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "safe-token" }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      )
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({
            attemptId: "d68a20db-bae4-4822-99bd-e016517bd0ee",
            connection: "google",
            expiresAtUtc: "2026-08-05T10:05:00Z",
            authorizationUrl: "/provider",
          }),
          { status: 201, headers: { "Content-Type": "application/json" } },
        ),
      );
    vi.stubGlobal("fetch", fetchMock);

    await expect(startLink("google")).resolves.toMatchObject({
      connection: "google",
    });

    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      "/api/identity/link-attempts",
      expect.objectContaining({
        method: "POST",
        credentials: "include",
        headers: expect.objectContaining({ "X-CSRF-TOKEN": "safe-token" }),
        body: JSON.stringify({ connection: "google" }),
      }),
    );
  });
});

describe("authorization redirect allowlist", () => {
  it("allows relative same-origin redirects", () => {
    expect(resolveAuthorizationUrl("/provider/authorize")).toBe(
      `${window.location.origin}/provider/authorize`,
    );
  });

  it("rejects insecure remote redirects in production", () => {
    expect(
      resolveAuthorizationUrl("http://attacker.example/collect"),
    ).toBeNull();
  });

  it("rejects remote HTTPS redirects because the API owns provider navigation", () => {
    expect(
      resolveAuthorizationUrl("https://accounts.google.com/o/oauth2/v2/auth"),
    ).toBeNull();
  });
});
