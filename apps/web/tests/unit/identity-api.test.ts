import { afterEach, describe, expect, it, vi } from "vitest";

import {
  acceptOwnerInvitation,
  createOwnerInvitation,
  createOrganization,
  IdentityApiError,
  invalidateAntiforgeryToken,
  listOrganizationOwnerMemberships,
  listOwnSessions,
  parseCreatedOrganization,
  parseIdentityCapabilities,
  parseIdentitySession,
  parseLinkStart,
  parseCreatedOwnerInvitation,
  parseOwnerInvitationList,
  parseOwnSessionPage,
  parseOrganizationOwnerMembershipList,
  parseRemovedOrganizationOwnerMembership,
  parseStepUpAttempt,
  revokeAllOtherOwnSessions,
  revokeAllOwnSessionsAndLogout,
  revokeOwnerInvitation,
  revokeOwnSession,
  removeOrganizationOwnerMembership,
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

const createdOrganizationResponse = {
  organization: {
    organizationId: "1266395e-ec88-481e-9a72-81fa2cc2904a",
    displayName: "La Esperanza",
    status: "active",
  },
  membership: {
    membershipId: "2634c6ee-8d8a-49e7-95b7-84fa0660236a",
    role: "owner",
    status: "active",
    authorizationVersion: 1,
  },
};

const invitation = {
  invitationId: "3766395e-ec88-481e-9a72-81fa2cc2904a",
  organizationId: "1266395e-ec88-481e-9a72-81fa2cc2904a",
  status: "pending",
  createdAtUtc: "2026-08-11T10:00:00Z",
  expiresAtUtc: "2026-08-18T10:00:00Z",
  acceptedAtUtc: null,
  revokedAtUtc: null,
  version: "4766395e-ec88-481e-9a72-81fa2cc2904a",
};
const invitationToken = "a".repeat(43);
const ownerMembership = {
  membershipId: "2634c6ee-8d8a-49e7-95b7-84fa0660236a",
  organizationId: "1266395e-ec88-481e-9a72-81fa2cc2904a",
  displayName: "Ana Productora",
  isCurrentUser: true,
  role: "owner",
  status: "active",
  authorizationVersion: 1,
  createdAtUtc: "2026-08-10T10:00:00Z",
  removedAtUtc: null,
  version: "5766395e-ec88-481e-9a72-81fa2cc2904a",
} as const;
const ownSession = {
  sessionId: "6766395e-ec88-481e-9a72-81fa2cc2904a",
  authenticatedAtUtc: "2026-08-18T10:00:00Z",
  expiresAtUtc: "2026-08-25T10:00:00Z",
  isCurrent: false,
  version: "7766395e-ec88-481e-9a72-81fa2cc2904a",
} as const;

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
        ownerInvitationsAvailable: true,
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
      ownerInvitationsAvailable: true,
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

  it("accepts the manage-sessions purpose in attempts and assurance", () => {
    expect(
      parseStepUpAttempt({
        attemptId: "d68a20db-bae4-4822-99bd-e016517bd0ee",
        purpose: "manage_sessions",
        expiresAtUtc: "2026-08-18T10:05:00Z",
        authorizationUrl: "/provider",
      }),
    ).toMatchObject({ purpose: "manage_sessions" });
  });

  it("parses the closed paginated own-session envelope without private metadata", () => {
    expect(
      parseOwnSessionPage({
        items: [
          {
            ...ownSession,
            ipAddress: "203.0.113.10",
            userAgent: "private",
            token: "never-render",
          },
        ],
        total: 1,
        offset: 0,
        limit: 20,
        futureField: true,
      }),
    ).toEqual({ items: [ownSession], total: 1, offset: 0, limit: 20 });
  });

  it("rejects malformed own-session pages at the HTTP boundary", () => {
    expect(() =>
      parseOwnSessionPage({
        items: [{ ...ownSession, version: "weak" }],
        total: 1,
        offset: 0,
        limit: 20,
      }),
    ).toThrow(IdentityApiError);
    expect(() =>
      parseOwnSessionPage({
        items: Array.from({ length: 51 }, () => ownSession),
        total: 51,
        offset: 0,
        limit: 50,
      }),
    ).toThrow(IdentityApiError);
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

  it("parses the create-organization contract and ignores additive fields", () => {
    expect(
      parseCreatedOrganization({
        ...createdOrganizationResponse,
        futureField: "compatible",
      }),
    ).toEqual(createdOrganizationResponse);
  });

  it("parses invitation summaries without accepting a leaked token", () => {
    expect(
      parseOwnerInvitationList({ items: [{ ...invitation, token: "secret" }] }),
    ).toEqual([invitation]);
  });

  it("parses the flat active owner-membership array and rejects removed rows", () => {
    expect(
      parseOrganizationOwnerMembershipList([
        { ...ownerMembership, futureField: "compatible" },
      ]),
    ).toEqual([ownerMembership]);
    expect(() =>
      parseOrganizationOwnerMembershipList({ items: [ownerMembership] }),
    ).toThrow(IdentityApiError);
    expect(() =>
      parseOrganizationOwnerMembershipList([
        {
          ...ownerMembership,
          status: "removed",
          removedAtUtc: "2026-08-12T10:00:00Z",
        },
      ]),
    ).toThrow(IdentityApiError);
  });

  it("parses the flat removed owner-membership result with replay metadata", () => {
    expect(
      parseRemovedOrganizationOwnerMembership({
        ...ownerMembership,
        status: "removed",
        removedAtUtc: "2026-08-12T10:00:00Z",
        isReplay: false,
      }),
    ).toEqual({
      ...ownerMembership,
      status: "removed",
      removedAtUtc: "2026-08-12T10:00:00Z",
      isReplay: false,
    });
  });

  it("accepts a raw invitation token only on the first create response", () => {
    expect(
      parseCreatedOwnerInvitation({
        invitation,
        token: invitationToken,
        isReplay: false,
      }),
    ).toEqual({ invitation, token: invitationToken, isReplay: false });
    expect(
      parseCreatedOwnerInvitation({
        invitation,
        token: null,
        isReplay: true,
      }),
    ).toEqual({ invitation, token: null, isReplay: true });
  });

  it("rejects inconsistent invitation terminal timestamps and replay tokens", () => {
    expect(() =>
      parseOwnerInvitationList({
        items: [{ ...invitation, status: "accepted", acceptedAtUtc: null }],
      }),
    ).toThrow(IdentityApiError);
    expect(() =>
      parseCreatedOwnerInvitation({
        invitation,
        token: invitationToken,
        isReplay: true,
      }),
    ).toThrow(IdentityApiError);
  });

  it.each([
    {
      ...createdOrganizationResponse,
      organization: {
        ...createdOrganizationResponse.organization,
        status: "pending",
      },
    },
    {
      ...createdOrganizationResponse,
      membership: { ...createdOrganizationResponse.membership, role: "member" },
    },
    {
      ...createdOrganizationResponse,
      membership: {
        ...createdOrganizationResponse.membership,
        authorizationVersion: 0,
      },
    },
    {
      ...createdOrganizationResponse,
      organization: {
        ...createdOrganizationResponse.organization,
        organizationId: "not-a-uuid",
      },
    },
  ])("rejects malformed create-organization payloads", (payload) => {
    expect(() => parseCreatedOrganization(payload)).toThrow(IdentityApiError);
  });
});

describe("identity mutations", () => {
  it("lists a bounded own-session page with cookies and no cache", async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(
      new Response(
        JSON.stringify({
          items: [ownSession],
          total: 1,
          offset: 20,
          limit: 20,
        }),
        { status: 200 },
      ),
    );
    vi.stubGlobal("fetch", fetchMock);

    await expect(listOwnSessions(20, 20)).resolves.toMatchObject({
      items: [ownSession],
      offset: 20,
    });
    expect(fetchMock).toHaveBeenCalledWith(
      "/api/identity/sessions?offset=20&limit=20",
      expect.objectContaining({
        cache: "no-store",
        credentials: "include",
      }),
    );
  });

  it("revokes another own session with CSRF and a strong If-Match", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "safe-token" }), { status: 200 }),
      )
      .mockResolvedValueOnce(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(
      revokeOwnSession(ownSession.sessionId, ownSession.version),
    ).resolves.toBeUndefined();
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      `/api/identity/sessions/${ownSession.sessionId}`,
      expect.objectContaining({
        method: "DELETE",
        body: undefined,
        credentials: "include",
        headers: expect.objectContaining({
          "X-CSRF-TOKEN": "safe-token",
          "If-Match": `"${ownSession.version}"`,
        }),
      }),
    );
  });

  it("revokes all other own sessions with CSRF and no target payload or precondition", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "safe-token" }), { status: 200 }),
      )
      .mockResolvedValueOnce(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(revokeAllOtherOwnSessions()).resolves.toBeUndefined();
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      "/api/identity/sessions/others",
      expect.objectContaining({
        method: "DELETE",
        body: undefined,
        credentials: "include",
        headers: expect.objectContaining({
          "X-CSRF-TOKEN": "safe-token",
        }),
      }),
    );
    const request = fetchMock.mock.calls[1]?.[1];
    expect(request?.headers).not.toHaveProperty("If-Match");
  });

  it("revokes all own sessions through the collection and invalidates antiforgery after logout", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "first-token" }), { status: 200 }),
      )
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "next-token" }), { status: 200 }),
      )
      .mockResolvedValueOnce(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(revokeAllOwnSessionsAndLogout()).resolves.toBeUndefined();
    await expect(revokeAllOtherOwnSessions()).resolves.toBeUndefined();

    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      "/api/identity/sessions",
      expect.objectContaining({
        method: "DELETE",
        body: undefined,
        credentials: "include",
        headers: expect.objectContaining({
          "X-CSRF-TOKEN": "first-token",
        }),
      }),
    );
    expect(fetchMock).toHaveBeenNthCalledWith(
      3,
      "/api/identity/antiforgery",
      expect.anything(),
    );
    const request = fetchMock.mock.calls[1]?.[1];
    expect(request?.headers).not.toHaveProperty("If-Match");
  });

  it.each([
    [403, "identity.reauthentication_required", "reauthentication-required"],
    [404, "identity.session_not_available", "unavailable"],
    [409, "identity.current_session_requires_logout", "conflict"],
    [412, "identity.session_version_mismatch", "conflict"],
    [503, "identity.session_management_unavailable", "provider-down"],
  ] as const)(
    "keeps typed own-session failure %i/%s",
    async (status, code, kind) => {
      const fetchMock = vi
        .fn()
        .mockResolvedValueOnce(
          new Response(JSON.stringify({ token: "safe-token" }), {
            status: 200,
          }),
        )
        .mockResolvedValueOnce(
          new Response(JSON.stringify({ code }), { status }),
        );
      vi.stubGlobal("fetch", fetchMock);

      await expect(
        revokeOwnSession(ownSession.sessionId, ownSession.version),
      ).rejects.toMatchObject({ status, code, kind });
      invalidateAntiforgeryToken();
    },
  );

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

  it("sends the organization contract with CSRF and an idempotency key", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "safe-token" }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(createdOrganizationResponse), {
          status: 201,
          headers: { "Content-Type": "application/json" },
        }),
      );
    vi.stubGlobal("fetch", fetchMock);

    await expect(
      createOrganization("La Esperanza", "attempt-key-123456"),
    ).resolves.toEqual(createdOrganizationResponse);

    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      "/api/identity/organizations",
      expect.objectContaining({
        method: "POST",
        credentials: "include",
        headers: expect.objectContaining({
          "Idempotency-Key": "attempt-key-123456",
          "X-CSRF-TOKEN": "safe-token",
        }),
        body: JSON.stringify({ displayName: "La Esperanza" }),
      }),
    );
  });

  it("retains the idempotency key while refreshing stale CSRF", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "stale-token" }), { status: 200 }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ code: "request.invalid_antiforgery" }), {
          status: 400,
        }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "fresh-token" }), { status: 200 }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(createdOrganizationResponse), {
          status: 201,
        }),
      );
    vi.stubGlobal("fetch", fetchMock);

    await createOrganization("La Esperanza", "attempt-key-123456");

    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      "/api/identity/organizations",
      expect.objectContaining({
        headers: expect.objectContaining({
          "Idempotency-Key": "attempt-key-123456",
          "X-CSRF-TOKEN": "stale-token",
        }),
      }),
    );
    expect(fetchMock).toHaveBeenNthCalledWith(
      4,
      "/api/identity/organizations",
      expect.objectContaining({
        headers: expect.objectContaining({
          "Idempotency-Key": "attempt-key-123456",
          "X-CSRF-TOKEN": "fresh-token",
        }),
      }),
    );
  });

  it("uses closed create and optimistic revoke invitation contracts", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "safe-token" }), { status: 200 }),
      )
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({
            invitation,
            token: invitationToken,
            isReplay: false,
          }),
          { status: 201 },
        ),
      )
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({
            ...invitation,
            status: "revoked",
            revokedAtUtc: "2026-08-11T11:00:00Z",
          }),
          {
            status: 200,
          },
        ),
      );
    vi.stubGlobal("fetch", fetchMock);

    await createOwnerInvitation(
      invitation.organizationId,
      "attempt-key-123456",
    );
    await revokeOwnerInvitation(
      invitation.organizationId,
      invitation.invitationId,
      invitation.version,
    );

    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      `/api/identity/organizations/${invitation.organizationId}/owner-invitations`,
      expect.objectContaining({
        body: "{}",
        headers: expect.objectContaining({
          "Idempotency-Key": "attempt-key-123456",
        }),
      }),
    );
    expect(fetchMock).toHaveBeenNthCalledWith(
      3,
      `/api/identity/organizations/${invitation.organizationId}/owner-invitations/${invitation.invitationId}/revoke`,
      expect.objectContaining({
        body: undefined,
        headers: expect.objectContaining({
          "If-Match": `"${invitation.version}"`,
        }),
      }),
    );
  });

  it("keeps the bearer invitation token in the POST body", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "safe-token" }), { status: 200 }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(createdOrganizationResponse), {
          status: 200,
        }),
      );
    vi.stubGlobal("fetch", fetchMock);

    await acceptOwnerInvitation(invitationToken);

    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      "/api/identity/owner-invitations/accept",
      expect.objectContaining({
        body: JSON.stringify({ token: invitationToken }),
      }),
    );
  });

  it.each([
    [
      404,
      "identity.organization_owner_invitation_not_available",
      "unavailable",
    ],
    [409, "identity.organization_owner_invitation_conflict", "conflict"],
    [
      412,
      "identity.organization_owner_invitation_version_mismatch",
      "conflict",
    ],
    [
      503,
      "identity.organization_owner_invitation_unavailable",
      "provider-down",
    ],
  ] as const)(
    "classifies invitation failure %i/%s as %s",
    async (status, code, kind) => {
      const fetchMock = vi
        .fn()
        .mockResolvedValueOnce(
          new Response(JSON.stringify({ token: "safe-token" }), {
            status: 200,
          }),
        )
        .mockResolvedValueOnce(
          new Response(JSON.stringify({ code }), { status }),
        );
      vi.stubGlobal("fetch", fetchMock);

      await expect(
        acceptOwnerInvitation(invitationToken),
      ).rejects.toMatchObject({ status, code, kind });
      invalidateAntiforgeryToken();
    },
  );

  it.each([
    [403, "identity.reauthentication_required", "reauthentication-required"],
    [409, "idempotency.in_progress", "in-progress"],
    [409, "idempotency.fingerprint_mismatch", "conflict"],
    [429, "request.rate_limited", "rate-limited"],
    [503, "idempotency.reconciliation_required", "reconciliation-required"],
  ] as const)(
    "classifies organization failure %i/%s as %s",
    async (status, code, expectedKind) => {
      const fetchMock = vi
        .fn()
        .mockResolvedValueOnce(
          new Response(JSON.stringify({ token: "safe-token" }), {
            status: 200,
          }),
        )
        .mockResolvedValueOnce(
          new Response(JSON.stringify({ code }), { status }),
        );
      vi.stubGlobal("fetch", fetchMock);

      await expect(
        createOrganization("La Esperanza", "attempt-key-123456"),
      ).rejects.toMatchObject({ kind: expectedKind, status, code });

      invalidateAntiforgeryToken();
    },
  );
});

describe("owner-membership requests", () => {
  it("loads the flat directory without mutation headers", async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(
      new Response(JSON.stringify([ownerMembership]), {
        status: 200,
      }),
    );
    vi.stubGlobal("fetch", fetchMock);

    await expect(
      listOrganizationOwnerMemberships(ownerMembership.organizationId),
    ).resolves.toEqual([ownerMembership]);
    expect(fetchMock).toHaveBeenCalledWith(
      `/api/identity/organizations/${ownerMembership.organizationId}/owner-memberships`,
      expect.objectContaining({ credentials: "include" }),
    );
  });

  it("removes with CSRF, If-Match, and a stable idempotency key", async () => {
    const removed = {
      ...ownerMembership,
      status: "removed",
      removedAtUtc: "2026-08-12T10:00:00Z",
      isReplay: false,
    };
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "safe-token" }), { status: 200 }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(removed), { status: 200 }),
      );
    vi.stubGlobal("fetch", fetchMock);

    await expect(
      removeOrganizationOwnerMembership(
        ownerMembership.organizationId,
        ownerMembership.membershipId,
        ownerMembership.version,
        "attempt-key-123456",
      ),
    ).resolves.toEqual(removed);
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      `/api/identity/organizations/${ownerMembership.organizationId}/owner-memberships/${ownerMembership.membershipId}`,
      expect.objectContaining({
        method: "DELETE",
        body: undefined,
        credentials: "include",
        headers: expect.objectContaining({
          "X-CSRF-TOKEN": "safe-token",
          "If-Match": `"${ownerMembership.version}"`,
          "Idempotency-Key": "attempt-key-123456",
        }),
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
