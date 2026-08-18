import {
  act,
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import type { FieldSummary } from "../../features/fields/field-types";
import type {
  CreatedOwnerInvitation,
  CreatedOrganization,
  IdentityCapabilities,
  IdentityConnection,
  IdentitySession,
  OrganizationOwnerMembershipSummary,
  OwnSessionPage,
  RemovedOrganizationOwnerMembership,
  StepUpAttempt,
} from "../../features/identity/identity-types";

const fieldApiMocks = vi.hoisted(() => ({
  listFields:
    vi.fn<
      (
        organizationId: string,
        signal?: AbortSignal,
      ) => Promise<readonly FieldSummary[]>
    >(),
}));

const apiMocks = vi.hoisted(() => ({
  acceptOwnerInvitation:
    vi.fn<(token: string) => Promise<CreatedOrganization>>(),
  completeDevelopmentStepUp:
    vi.fn<(attempt: StepUpAttempt) => Promise<IdentitySession>>(),
  createOwnerInvitation:
    vi.fn<
      (
        organizationId: string,
        idempotencyKey: string,
      ) => Promise<CreatedOwnerInvitation>
    >(),
  createOrganization:
    vi.fn<
      (
        displayName: string,
        idempotencyKey: string,
        signal?: AbortSignal,
      ) => Promise<CreatedOrganization>
    >(),
  loadCapabilities:
    vi.fn<(signal?: AbortSignal) => Promise<IdentityCapabilities>>(),
  loadSession: vi.fn<(signal?: AbortSignal) => Promise<IdentitySession>>(),
  identityLoginUrl: vi.fn<(connection: IdentityConnection) => string>(),
  listOwnerInvitations: vi.fn().mockResolvedValue([]),
  listOwnSessions: vi.fn<() => Promise<OwnSessionPage>>(),
  listOrganizationOwnerMemberships:
    vi.fn<
      (
        organizationId: string,
        signal?: AbortSignal,
      ) => Promise<readonly OrganizationOwnerMembershipSummary[]>
    >(),
  removeOrganizationOwnerMembership:
    vi.fn<
      (
        organizationId: string,
        membershipId: string,
        version: string,
        idempotencyKey: string,
      ) => Promise<RemovedOrganizationOwnerMembership>
    >(),
  revokeAllOtherOwnSessions: vi.fn<() => Promise<void>>(),
  revokeOwnSession:
    vi.fn<(sessionId: string, version: string) => Promise<void>>(),
  startStepUp:
    vi.fn<(purpose: StepUpAttempt["purpose"]) => Promise<StepUpAttempt>>(),
}));

vi.mock("../../features/identity/identity-api", async (importOriginal) => {
  const actual =
    await importOriginal<
      typeof import("../../features/identity/identity-api")
    >();
  return { ...actual, ...apiMocks };
});

vi.mock("../../features/fields/field-api", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("../../features/fields/field-api")>();
  return { ...actual, ...fieldApiMocks };
});

import {
  IdentityApiError,
  invalidateAntiforgeryToken,
} from "../../features/identity/identity-api";
import { IdentityHub } from "../../features/identity/identity-hub";
import { captureOwnerInvitationTokenFromHash } from "../../features/identity/identity-hub";

const capabilities: IdentityCapabilities = {
  environment: "Development",
  oidcConfigured: false,
  developmentProviderEnabled: true,
  strongAuthenticationAvailable: true,
  ownerInvitationsAvailable: false,
  connections: [{ id: "email", label: "Correo", available: true }],
};

const session: IdentitySession = {
  userId: "2e9f14bd-eec9-4bd8-b1ca-da0c6c615f78",
  displayName: "Ana Productora",
  authentication: {
    level: "primary",
    authenticatedAtUtc: "2026-08-05T10:00:00Z",
    purpose: null,
    strongAuthenticatedAtUtc: null,
    expiresAtUtc: null,
  },
  identities: [
    {
      identityId: "6f0b9239-264a-4237-9d18-b4324a714849",
      connection: "email",
      label: "a***@campo.ar",
      verifiedAtUtc: "2026-08-05T10:00:00Z",
    },
  ],
  memberships: [],
};

const created: CreatedOrganization = {
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

beforeEach(() => {
  globalThis.sessionStorage.clear();
  apiMocks.loadCapabilities.mockResolvedValue(capabilities);
  apiMocks.loadSession.mockResolvedValue(session);
  apiMocks.listOrganizationOwnerMemberships.mockResolvedValue([]);
  apiMocks.listOwnSessions.mockResolvedValue({
    items: [],
    total: 0,
    offset: 0,
    limit: 20,
  });
  apiMocks.revokeAllOtherOwnSessions.mockResolvedValue();
  apiMocks.revokeOwnSession.mockResolvedValue();
  apiMocks.identityLoginUrl.mockReturnValue("#reauthenticate");
  fieldApiMocks.listFields.mockResolvedValue([]);
});

function deferred<T>(): Readonly<{
  promise: Promise<T>;
  resolve: (value: T) => void;
}> {
  let resolvePromise: ((value: T) => void) | undefined;
  const promise = new Promise<T>((resolve) => {
    resolvePromise = resolve;
  });
  return {
    promise,
    resolve(value) {
      if (resolvePromise === undefined) {
        throw new Error("Deferred promise was not initialized.");
      }
      resolvePromise(value);
    },
  };
}

afterEach(() => {
  cleanup();
  invalidateAntiforgeryToken();
  globalThis.sessionStorage.clear();
  globalThis.history.replaceState({}, "", "/");
  vi.clearAllMocks();
  vi.restoreAllMocks();
});

async function openAndSubmitOrganization(): Promise<void> {
  render(<IdentityHub />);
  fireEvent.click(
    await screen.findByRole("button", { name: "Crear mi organización" }),
  );
  fireEvent.change(
    screen.getByRole("textbox", { name: "Nombre de la organización" }),
    { target: { value: "La Esperanza" } },
  );
  fireEvent.click(screen.getByRole("button", { name: "Crear organización" }));
  await waitFor(() =>
    expect(apiMocks.createOrganization).toHaveBeenCalledOnce(),
  );
}

describe("IdentityHub owner workspace scoping", () => {
  const organizationAId = "1266395e-ec88-481e-9a72-81fa2cc2904a";
  const organizationBId = "64eb2297-6603-4fc9-b2b0-a582a7bf1992";
  const multiOrganizationSession: IdentitySession = {
    ...session,
    memberships: [
      {
        organizationId: organizationAId,
        organizationName: "Establecimiento Gemelo",
        role: "owner",
      },
      {
        organizationId: organizationBId,
        organizationName: "Establecimiento Gemelo",
        role: "owner",
      },
    ],
  };

  it("loads fields, owner memberships and invitations only for the active organization", async () => {
    globalThis.history.replaceState({}, "", "/?org=126639&view=fields");
    apiMocks.loadCapabilities.mockResolvedValueOnce({
      ...capabilities,
      ownerInvitationsAvailable: true,
    });
    apiMocks.loadSession.mockResolvedValueOnce(multiOrganizationSession);

    render(<IdentityHub />);

    await waitFor(() => {
      expect(fieldApiMocks.listFields).toHaveBeenCalledWith(
        organizationAId,
        expect.any(AbortSignal),
      );
      expect(apiMocks.listOrganizationOwnerMemberships).toHaveBeenCalledWith(
        organizationAId,
        expect.any(AbortSignal),
      );
      expect(apiMocks.listOwnerInvitations).toHaveBeenCalledWith(
        organizationAId,
        expect.any(AbortSignal),
      );
    });
    expect(fieldApiMocks.listFields).not.toHaveBeenCalledWith(
      organizationBId,
      expect.anything(),
    );
    expect(apiMocks.listOrganizationOwnerMemberships).not.toHaveBeenCalledWith(
      organizationBId,
      expect.anything(),
    );
    expect(apiMocks.listOwnerInvitations).not.toHaveBeenCalledWith(
      organizationBId,
      expect.anything(),
    );
  });

  it("aborts A and rejects its late field and team responses after switching to B", async () => {
    const fieldsA = deferred<readonly FieldSummary[]>();
    const membershipsA =
      deferred<readonly OrganizationOwnerMembershipSummary[]>();
    const invitationsA = deferred<readonly []>();
    const fieldA: FieldSummary = {
      fieldId: "aaaaaa11-3ee8-4e43-adf8-083975f8fa77",
      organizationId: organizationAId,
      displayName: "Lote Central",
      type: "field",
      status: "draft",
      spatialStatus: "not_configured",
      createdAtUtc: "2026-08-18T18:00:00Z",
      version: "5a5ed442-141f-4fd4-a8e8-52598a5d7426",
    };
    const fieldB: FieldSummary = {
      ...fieldA,
      fieldId: "bbbbbb77-3ee8-4e43-adf8-083975f8fa77",
      organizationId: organizationBId,
    };
    const ownerA: OrganizationOwnerMembershipSummary = {
      membershipId: "aaaaaa39-8d8a-49e7-95b7-84fa0660236a",
      organizationId: organizationAId,
      displayName: "Owner tardío de A",
      isCurrentUser: false,
      role: "owner",
      status: "active",
      authorizationVersion: 1,
      createdAtUtc: "2026-08-18T18:00:00Z",
      removedAtUtc: null,
      version: "aaaaaa42-141f-4fd4-a8e8-52598a5d7426",
    };
    const ownerB: OrganizationOwnerMembershipSummary = {
      ...ownerA,
      membershipId: "bbbbbb39-8d8a-49e7-95b7-84fa0660236a",
      organizationId: organizationBId,
      displayName: "Owner vigente de B",
      version: "bbbbbb42-141f-4fd4-a8e8-52598a5d7426",
    };
    globalThis.history.replaceState({}, "", "/?org=126639&view=fields");
    apiMocks.loadCapabilities.mockResolvedValueOnce({
      ...capabilities,
      ownerInvitationsAvailable: true,
    });
    apiMocks.loadSession.mockResolvedValueOnce(multiOrganizationSession);
    fieldApiMocks.listFields.mockImplementation((organizationId) =>
      organizationId === organizationAId
        ? fieldsA.promise
        : Promise.resolve([fieldB]),
    );
    apiMocks.listOrganizationOwnerMemberships.mockImplementation(
      (organizationId) =>
        organizationId === organizationAId
          ? membershipsA.promise
          : Promise.resolve([ownerB]),
    );
    apiMocks.listOwnerInvitations.mockImplementation((organizationId) =>
      organizationId === organizationAId
        ? invitationsA.promise
        : Promise.resolve([]),
    );

    render(<IdentityHub />);
    await waitFor(() =>
      expect(fieldApiMocks.listFields).toHaveBeenCalledWith(
        organizationAId,
        expect.any(AbortSignal),
      ),
    );

    act(() => {
      globalThis.history.pushState({}, "", "/?org=64EB22&view=fields");
      globalThis.dispatchEvent(new PopStateEvent("popstate"));
    });

    expect(await screen.findByText("Campo BBBBBB")).toBeVisible();
    const fieldsASignal = fieldApiMocks.listFields.mock.calls.find(
      ([organizationId]) => organizationId === organizationAId,
    )?.[1];
    expect(fieldsASignal?.aborted).toBe(true);

    act(() => {
      fieldsA.resolve([fieldA]);
      membershipsA.resolve([ownerA]);
      invitationsA.resolve([]);
    });
    await waitFor(() =>
      expect(screen.queryByText("Campo AAAAAA")).not.toBeInTheDocument(),
    );

    fireEvent.click(screen.getByRole("button", { name: "Equipo" }));
    expect(await screen.findByText("Owner vigente de B")).toBeVisible();
    expect(screen.queryByText("Owner tardío de A")).not.toBeInTheDocument();
    expect(document.body).not.toHaveTextContent(organizationAId);
    expect(document.body).not.toHaveTextContent(organizationBId);
  });
});

describe("IdentityHub own session management", () => {
  const organizationId = "1266395e-ec88-481e-9a72-81fa2cc2904a";
  const currentSession = {
    sessionId: "8766395e-ec88-481e-9a72-81fa2cc2904a",
    authenticatedAtUtc: "2026-08-18T10:00:00Z",
    expiresAtUtc: "2026-08-25T10:00:00Z",
    isCurrent: true,
    version: "b766395e-ec88-481e-9a72-81fa2cc2904a",
  } as const;
  const otherSession = {
    sessionId: "9766395e-ec88-481e-9a72-81fa2cc2904a",
    authenticatedAtUtc: "2026-08-17T10:00:00Z",
    expiresAtUtc: "2026-08-24T10:00:00Z",
    isCurrent: false,
    version: "a766395e-ec88-481e-9a72-81fa2cc2904a",
  } as const;
  const accountSession: IdentitySession = {
    ...session,
    memberships: [
      { organizationId, organizationName: "La Esperanza", role: "owner" },
    ],
  };

  beforeEach(() => {
    globalThis.history.replaceState({}, "", "/?org=126639&view=account");
    apiMocks.loadSession.mockResolvedValue(accountSession);
    apiMocks.listOwnSessions.mockResolvedValue({
      items: [currentSession, otherSession],
      total: 2,
      offset: 0,
      limit: 20,
    });
  });

  it("requests exact manage_sessions assurance and revokes only the confirmed other session", async () => {
    const attempt: StepUpAttempt = {
      attemptId: "c766395e-ec88-481e-9a72-81fa2cc2904a",
      purpose: "manage_sessions",
      expiresAtUtc: "2026-08-18T10:05:00Z",
      authorizationUrl: "/provider",
    };
    apiMocks.startStepUp.mockResolvedValue(attempt);
    apiMocks.completeDevelopmentStepUp.mockResolvedValue({
      ...accountSession,
      authentication: {
        level: "strong",
        authenticatedAtUtc: "2026-08-18T10:00:00Z",
        purpose: "manage_sessions",
        strongAuthenticatedAtUtc: "2026-08-18T10:01:00Z",
        expiresAtUtc: "2030-08-18T10:06:00Z",
      },
    });
    apiMocks.listOwnSessions
      .mockResolvedValueOnce({
        items: [currentSession, otherSession],
        total: 2,
        offset: 0,
        limit: 20,
      })
      .mockResolvedValue({
        items: [currentSession],
        total: 1,
        offset: 0,
        limit: 20,
      });

    render(<IdentityHub />);
    fireEvent.click(
      await screen.findByRole("button", { name: "Cerrar sesión 976639" }),
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: "Confirmar cierre de sesión 976639",
      }),
    );

    await waitFor(() =>
      expect(apiMocks.startStepUp).toHaveBeenCalledWith("manage_sessions"),
    );
    await waitFor(() =>
      expect(apiMocks.revokeOwnSession).toHaveBeenCalledWith(
        otherSession.sessionId,
        otherSession.version,
      ),
    );
    expect(apiMocks.revokeOwnSession).not.toHaveBeenCalledWith(
      currentSession.sessionId,
      expect.anything(),
    );
    expect(await screen.findByRole("status")).toHaveTextContent(
      "La otra sesión fue cerrada",
    );
  });

  it("closes every other session through the server-side command and refreshes the inventory", async () => {
    const attempt: StepUpAttempt = {
      attemptId: "c766395e-ec88-481e-9a72-81fa2cc2904a",
      purpose: "manage_sessions",
      expiresAtUtc: "2026-08-18T10:05:00Z",
      authorizationUrl: "/provider",
    };
    apiMocks.startStepUp.mockResolvedValue(attempt);
    apiMocks.completeDevelopmentStepUp.mockResolvedValue({
      ...accountSession,
      authentication: {
        level: "strong",
        authenticatedAtUtc: "2026-08-18T10:00:00Z",
        purpose: "manage_sessions",
        strongAuthenticatedAtUtc: "2026-08-18T10:01:00Z",
        expiresAtUtc: "2030-08-18T10:06:00Z",
      },
    });
    apiMocks.listOwnSessions
      .mockResolvedValueOnce({
        items: [currentSession, otherSession],
        total: 2,
        offset: 0,
        limit: 20,
      })
      .mockResolvedValue({
        items: [currentSession],
        total: 1,
        offset: 0,
        limit: 20,
      });

    render(<IdentityHub />);
    fireEvent.click(
      await screen.findByRole("button", {
        name: "Cerrar las otras sesiones",
      }),
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: "Confirmar cierre de las otras sesiones",
      }),
    );

    await waitFor(() =>
      expect(apiMocks.startStepUp).toHaveBeenCalledWith("manage_sessions"),
    );
    await waitFor(() =>
      expect(apiMocks.revokeAllOtherOwnSessions).toHaveBeenCalledOnce(),
    );
    expect(apiMocks.revokeOwnSession).not.toHaveBeenCalled();
    expect(
      await screen.findByText(
        "Las otras sesiones fueron cerradas. Esta sesión sigue abierta.",
      ),
    ).toBeVisible();
    await waitFor(() =>
      expect(apiMocks.listOwnSessions.mock.calls.length).toBeGreaterThanOrEqual(
        2,
      ),
    );
    expect(
      screen.getByRole("button", { name: "Cerrar las otras sesiones" }),
    ).toBeDisabled();
  });

  it("resumes the all-other-sessions intent after a backend 403 step-up challenge", async () => {
    const strongSession: IdentitySession = {
      ...accountSession,
      authentication: {
        level: "strong",
        authenticatedAtUtc: "2026-08-18T10:00:00Z",
        purpose: "manage_sessions",
        strongAuthenticatedAtUtc: "2026-08-18T10:01:00Z",
        expiresAtUtc: "2030-08-18T10:06:00Z",
      },
    };
    apiMocks.loadSession.mockResolvedValue(strongSession);
    apiMocks.revokeAllOtherOwnSessions
      .mockRejectedValueOnce(
        new IdentityApiError(
          "reauthentication-required",
          403,
          "identity.strong_authentication_required",
        ),
      )
      .mockResolvedValueOnce();
    apiMocks.startStepUp.mockResolvedValue({
      attemptId: "c766395e-ec88-481e-9a72-81fa2cc2904a",
      purpose: "manage_sessions",
      expiresAtUtc: "2026-08-18T10:05:00Z",
      authorizationUrl: "/provider",
    });
    apiMocks.completeDevelopmentStepUp.mockResolvedValue({
      ...strongSession,
      authentication: {
        ...strongSession.authentication,
        strongAuthenticatedAtUtc: "2026-08-18T10:02:00Z",
      },
    });

    render(<IdentityHub />);
    fireEvent.click(
      await screen.findByRole("button", {
        name: "Cerrar las otras sesiones",
      }),
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: "Confirmar cierre de las otras sesiones",
      }),
    );

    const resume = await screen.findByRole("button", {
      name: "Verificar con MFA y continuar",
    });
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.identity.session-revocation.v1",
      ),
    ).toContain("all-others");
    fireEvent.click(resume);

    await waitFor(() =>
      expect(apiMocks.revokeAllOtherOwnSessions).toHaveBeenCalledTimes(2),
    );
    expect(apiMocks.startStepUp).toHaveBeenCalledWith("manage_sessions");
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.identity.session-revocation.v1",
      ),
    ).toBeNull();
  });

  it("resumes a stored all-other-sessions intent after an external step-up return", async () => {
    globalThis.sessionStorage.setItem(
      "agropecuaria.identity.session-revocation.v1",
      JSON.stringify({ kind: "all-others" }),
    );
    apiMocks.loadSession.mockResolvedValue({
      ...accountSession,
      authentication: {
        level: "strong",
        authenticatedAtUtc: "2026-08-18T10:00:00Z",
        purpose: "manage_sessions",
        strongAuthenticatedAtUtc: "2026-08-18T10:01:00Z",
        expiresAtUtc: "2030-08-18T10:06:00Z",
      },
    });

    render(<IdentityHub />);

    await waitFor(() =>
      expect(apiMocks.revokeAllOtherOwnSessions).toHaveBeenCalledOnce(),
    );
    expect(apiMocks.revokeOwnSession).not.toHaveBeenCalled();
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.identity.session-revocation.v1",
      ),
    ).toBeNull();
  });

  it("fails stale revocation closed, refreshes the page, and announces review", async () => {
    apiMocks.loadSession.mockResolvedValue({
      ...accountSession,
      authentication: {
        level: "strong",
        authenticatedAtUtc: "2026-08-18T10:00:00Z",
        purpose: "manage_sessions",
        strongAuthenticatedAtUtc: "2026-08-18T10:01:00Z",
        expiresAtUtc: "2030-08-18T10:06:00Z",
      },
    });
    apiMocks.revokeOwnSession.mockRejectedValue(
      new IdentityApiError(
        "conflict",
        412,
        "identity.session_version_mismatch",
      ),
    );

    render(<IdentityHub />);
    fireEvent.click(
      await screen.findByRole("button", { name: "Cerrar sesión 976639" }),
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: "Confirmar cierre de sesión 976639",
      }),
    );

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "cambió en paralelo",
    );
    await waitFor(() =>
      expect(apiMocks.listOwnSessions).toHaveBeenCalledTimes(2),
    );
  });

  it("resumes the exact pending target after purpose-bound reauthentication", async () => {
    globalThis.sessionStorage.setItem(
      "agropecuaria.identity.session-revocation.v1",
      JSON.stringify(otherSession),
    );
    apiMocks.loadSession.mockResolvedValue({
      ...accountSession,
      authentication: {
        level: "strong",
        authenticatedAtUtc: "2026-08-18T10:00:00Z",
        purpose: "manage_sessions",
        strongAuthenticatedAtUtc: "2026-08-18T10:01:00Z",
        expiresAtUtc: "2030-08-18T10:06:00Z",
      },
    });

    render(<IdentityHub />);

    await waitFor(() =>
      expect(apiMocks.revokeOwnSession).toHaveBeenCalledWith(
        otherSession.sessionId,
        otherSession.version,
      ),
    );
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.identity.session-revocation.v1",
      ),
    ).toBeNull();
  });

  it("refetches the inventory when another step-up rotates the current session context", async () => {
    const rotatedCurrentSession = {
      ...currentSession,
      sessionId: "d766395e-ec88-481e-9a72-81fa2cc2904a",
      version: "e766395e-ec88-481e-9a72-81fa2cc2904a",
    };
    apiMocks.listOwnSessions
      .mockResolvedValueOnce({
        items: [currentSession],
        total: 1,
        offset: 0,
        limit: 20,
      })
      .mockResolvedValue({
        items: [rotatedCurrentSession],
        total: 1,
        offset: 0,
        limit: 20,
      });
    apiMocks.startStepUp.mockResolvedValue({
      attemptId: "c766395e-ec88-481e-9a72-81fa2cc2904a",
      purpose: "manage_authentication_methods",
      expiresAtUtc: "2026-08-18T10:05:00Z",
      authorizationUrl: "/provider",
    });
    apiMocks.completeDevelopmentStepUp.mockResolvedValue({
      ...accountSession,
      authentication: {
        level: "strong",
        authenticatedAtUtc: "2026-08-18T10:00:00Z",
        purpose: "manage_authentication_methods",
        strongAuthenticatedAtUtc: "2026-08-18T10:01:00Z",
        expiresAtUtc: "2030-08-18T10:06:00Z",
      },
    });

    render(<IdentityHub />);
    expect(await screen.findByText("Sesión 876639")).toBeVisible();
    fireEvent.click(screen.getByRole("button", { name: "Verificar con MFA" }));

    await waitFor(() =>
      expect(apiMocks.listOwnSessions).toHaveBeenCalledTimes(2),
    );
    const rotatedRow = (await screen.findByText("Sesión D76639")).closest("li");
    if (rotatedRow === null) throw new Error("Rotated session row is missing.");
    expect(within(rotatedRow).getByText("Actual")).toBeVisible();
    expect(screen.queryByText("Sesión 876639")).not.toBeInTheDocument();
  });

  it("transitions globally when the inventory GET reports a revoked actor", async () => {
    globalThis.sessionStorage.setItem(
      "agropecuaria.identity.session-revocation.v1",
      JSON.stringify(otherSession),
    );
    apiMocks.listOwnSessions.mockRejectedValue(
      new IdentityApiError("revoked", 401, "identity.session_revoked"),
    );

    render(<IdentityHub />);

    expect(
      await screen.findByRole("heading", {
        name: "Esta sesión fue revocada",
      }),
    ).toBeVisible();
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.identity.session-revocation.v1",
      ),
    ).toBeNull();
    expect(screen.queryByText(/Verificá tu identidad con MFA/i)).toBeNull();
  });

  it("transitions globally when DELETE reports a revoked actor", async () => {
    apiMocks.loadSession.mockResolvedValue({
      ...accountSession,
      authentication: {
        level: "strong",
        authenticatedAtUtc: "2026-08-18T10:00:00Z",
        purpose: "manage_sessions",
        strongAuthenticatedAtUtc: "2026-08-18T10:01:00Z",
        expiresAtUtc: "2030-08-18T10:06:00Z",
      },
    });
    apiMocks.revokeOwnSession.mockRejectedValue(
      new IdentityApiError("revoked", 401, "identity.session_revoked"),
    );

    render(<IdentityHub />);
    fireEvent.click(
      await screen.findByRole("button", { name: "Cerrar sesión 976639" }),
    );
    globalThis.sessionStorage.setItem(
      "agropecuaria.identity.session-revocation.v1",
      JSON.stringify(otherSession),
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: "Confirmar cierre de sesión 976639",
      }),
    );

    expect(
      await screen.findByRole("heading", {
        name: "Esta sesión fue revocada",
      }),
    ).toBeVisible();
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.identity.session-revocation.v1",
      ),
    ).toBeNull();
    expect(screen.queryByText(/Verificá tu identidad con MFA/i)).toBeNull();
  });

  it.each([
    [
      "rate limit",
      new IdentityApiError("rate-limited", 429, "request.rate_limited"),
      "límite temporal de seguridad",
    ],
    [
      "service outage",
      new IdentityApiError(
        "provider-down",
        503,
        "identity.session_management_unavailable",
      ),
      "no está disponible temporalmente",
    ],
    ["offline", new TypeError("Failed to fetch"), "sin conexión"],
  ])(
    "preserves and retries the all-other-sessions intent after %s",
    async (_caseName, error, message) => {
      if (error instanceof TypeError) {
        vi.spyOn(window.navigator, "onLine", "get").mockReturnValue(false);
      }
      apiMocks.loadSession.mockResolvedValue({
        ...accountSession,
        authentication: {
          level: "strong",
          authenticatedAtUtc: "2026-08-18T10:00:00Z",
          purpose: "manage_sessions",
          strongAuthenticatedAtUtc: "2026-08-18T10:01:00Z",
          expiresAtUtc: "2030-08-18T10:06:00Z",
        },
      });
      apiMocks.revokeAllOtherOwnSessions
        .mockRejectedValueOnce(error)
        .mockResolvedValueOnce();

      render(<IdentityHub />);
      fireEvent.click(
        await screen.findByRole("button", {
          name: "Cerrar las otras sesiones",
        }),
      );
      fireEvent.click(
        screen.getByRole("button", {
          name: "Confirmar cierre de las otras sesiones",
        }),
      );

      expect(await screen.findByRole("alert")).toHaveTextContent(message);
      fireEvent.click(
        screen.getByRole("button", { name: "Reintentar cierre" }),
      );
      await waitFor(() =>
        expect(apiMocks.revokeAllOtherOwnSessions).toHaveBeenCalledTimes(2),
      );
      expect(
        await screen.findByText(/Las otras sesiones fueron cerradas/i),
      ).toBeVisible();
    },
  );

  it("transitions globally when revoke-all reports a revoked actor", async () => {
    apiMocks.loadSession.mockResolvedValue({
      ...accountSession,
      authentication: {
        level: "strong",
        authenticatedAtUtc: "2026-08-18T10:00:00Z",
        purpose: "manage_sessions",
        strongAuthenticatedAtUtc: "2026-08-18T10:01:00Z",
        expiresAtUtc: "2030-08-18T10:06:00Z",
      },
    });
    apiMocks.revokeAllOtherOwnSessions.mockRejectedValue(
      new IdentityApiError("revoked", 401, "identity.session_revoked"),
    );

    render(<IdentityHub />);
    fireEvent.click(
      await screen.findByRole("button", {
        name: "Cerrar las otras sesiones",
      }),
    );
    globalThis.sessionStorage.setItem(
      "agropecuaria.identity.session-revocation.v1",
      JSON.stringify({ kind: "all-others" }),
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: "Confirmar cierre de las otras sesiones",
      }),
    );

    expect(
      await screen.findByRole("heading", {
        name: "Esta sesión fue revocada",
      }),
    ).toBeVisible();
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.identity.session-revocation.v1",
      ),
    ).toBeNull();
    expect(screen.queryByText(/Verificá tu identidad con MFA/i)).toBeNull();
  });
});

describe("IdentityHub organization attempts", () => {
  it("creates a visible-ASCII key from exactly 128 CSPRNG bits", async () => {
    const getRandomValues = vi.spyOn(globalThis.crypto, "getRandomValues");
    apiMocks.createOrganization.mockResolvedValueOnce(created);

    await openAndSubmitOrganization();

    const key = apiMocks.createOrganization.mock.calls[0]?.[1];
    expect(key).toMatch(/^[0-9a-f]{32}$/);
    expect(key).not.toContain("-");
    expect(getRandomValues).toHaveBeenCalled();
    const randomBuffer = getRandomValues.mock.calls[0]?.[0];
    expect(randomBuffer).toBeInstanceOf(Uint8Array);
    if (!(randomBuffer instanceof Uint8Array)) {
      throw new Error("Expected the CSPRNG input to be a byte array.");
    }
    expect(randomBuffer.byteLength).toBe(16);
  });

  it("retains the idempotency key for an in-progress retry", async () => {
    apiMocks.createOrganization
      .mockRejectedValueOnce(
        new IdentityApiError("in-progress", 409, "idempotency.in_progress"),
      )
      .mockResolvedValueOnce(created);

    await openAndSubmitOrganization();
    const firstKey = apiMocks.createOrganization.mock.calls[0]?.[1];
    expect(firstKey).toMatch(/^[0-9a-f]{32}$/);

    fireEvent.click(
      await screen.findByRole("button", { name: "Consultar nuevamente" }),
    );
    await waitFor(() =>
      expect(apiMocks.createOrganization).toHaveBeenCalledTimes(2),
    );

    expect(apiMocks.createOrganization.mock.calls[1]?.[1]).toBe(firstKey);
    expect(await screen.findAllByText("Organización 126639")).toHaveLength(1);
    expect(
      screen
        .getByText("La Esperanza ya está lista y sos owner.")
        .closest('[role="status"]'),
    ).toBeVisible();
  });

  it("regenerates the key after a fingerprint conflict", async () => {
    apiMocks.createOrganization
      .mockRejectedValueOnce(
        new IdentityApiError(
          "conflict",
          409,
          "idempotency.fingerprint_mismatch",
        ),
      )
      .mockResolvedValueOnce(created);

    await openAndSubmitOrganization();
    const firstKey = apiMocks.createOrganization.mock.calls[0]?.[1];
    expect(
      await screen.findByRole("button", { name: "Iniciar nuevo intento" }),
    ).toBeEnabled();
    expect(
      screen.getByRole("textbox", { name: "Nombre de la organización" }),
    ).toHaveValue("La Esperanza");

    fireEvent.click(
      screen.getByRole("button", { name: "Iniciar nuevo intento" }),
    );
    await waitFor(() =>
      expect(apiMocks.createOrganization).toHaveBeenCalledTimes(2),
    );

    expect(apiMocks.createOrganization.mock.calls[1]?.[1]).not.toBe(firstKey);
  });

  it("preserves draft and key across the reauthentication redirect", async () => {
    apiMocks.createOrganization
      .mockRejectedValueOnce(
        new IdentityApiError(
          "reauthentication-required",
          403,
          "identity.reauthentication_required",
        ),
      )
      .mockResolvedValueOnce(created);

    await openAndSubmitOrganization();
    const firstKey = apiMocks.createOrganization.mock.calls[0]?.[1];

    fireEvent.click(
      await screen.findByRole("button", { name: "Volver a verificar" }),
    );

    expect(apiMocks.identityLoginUrl).toHaveBeenCalledWith("email");
    const storedAttempt = globalThis.sessionStorage.getItem(
      "agropecuaria.identity.organization-reauth.v1",
    );
    expect(storedAttempt).toContain("La Esperanza");
    expect(storedAttempt).toContain(firstKey);

    cleanup();
    render(<IdentityHub />);
    expect(
      await screen.findByRole("textbox", {
        name: "Nombre de la organización",
      }),
    ).toHaveValue("La Esperanza");
    fireEvent.click(screen.getByRole("button", { name: "Crear organización" }));
    await waitFor(() =>
      expect(apiMocks.createOrganization).toHaveBeenCalledTimes(2),
    );

    expect(apiMocks.createOrganization.mock.calls[1]?.[1]).toBe(firstKey);
  });
});

describe("IdentityHub owner removal", () => {
  beforeEach(() => {
    globalThis.history.replaceState({}, "", "/?org=126639&view=team");
  });

  it("requires purpose-bound step-up and removes another owner with the frozen concurrency contract", async () => {
    const organizationId = "1266395e-ec88-481e-9a72-81fa2cc2904a";
    const currentMembership: OrganizationOwnerMembershipSummary = {
      membershipId: "2634c6ee-8d8a-49e7-95b7-84fa0660236a",
      organizationId,
      displayName: "Ana Productora",
      isCurrentUser: true,
      role: "owner",
      status: "active",
      authorizationVersion: 1,
      createdAtUtc: "2026-08-10T10:00:00Z",
      removedAtUtc: null,
      version: "4766395e-ec88-481e-9a72-81fa2cc2904a",
    };
    const otherMembership: OrganizationOwnerMembershipSummary = {
      ...currentMembership,
      membershipId: "3766395e-ec88-481e-9a72-81fa2cc2904a",
      displayName: "Bruno Productor",
      isCurrentUser: false,
      authorizationVersion: 2,
      version: "5766395e-ec88-481e-9a72-81fa2cc2904a",
    };
    const ownerSession: IdentitySession = {
      ...session,
      memberships: [
        { organizationId, organizationName: "La Esperanza", role: "owner" },
      ],
    };
    const strongSession: IdentitySession = {
      ...ownerSession,
      authentication: {
        level: "strong",
        authenticatedAtUtc: "2026-08-11T20:00:00Z",
        purpose: "manage_organization_owners",
        strongAuthenticatedAtUtc: "2026-08-11T20:01:00Z",
        expiresAtUtc: "2099-08-11T20:06:00Z",
      },
    };
    const stepUp: StepUpAttempt = {
      attemptId: "d68a20db-bae4-4822-99bd-e016517bd0ee",
      purpose: "manage_organization_owners",
      expiresAtUtc: "2099-08-11T20:06:00Z",
      authorizationUrl: "/provider",
    };
    apiMocks.loadSession.mockResolvedValueOnce(ownerSession);
    apiMocks.listOrganizationOwnerMemberships
      .mockResolvedValueOnce([currentMembership, otherMembership])
      .mockResolvedValueOnce([currentMembership, otherMembership])
      .mockResolvedValueOnce([currentMembership]);
    apiMocks.startStepUp.mockResolvedValueOnce(stepUp);
    apiMocks.completeDevelopmentStepUp.mockResolvedValueOnce(strongSession);
    apiMocks.removeOrganizationOwnerMembership.mockResolvedValueOnce({
      ...otherMembership,
      status: "removed",
      removedAtUtc: "2026-08-12T10:00:00Z",
      isReplay: false,
    });

    render(<IdentityHub />);
    const removeButton = await screen.findByRole("button", {
      name: "Quitar acceso a Bruno Productor",
    });
    expect(screen.getByText("Membresía 376639")).toBeVisible();
    fireEvent.click(removeButton);

    let dialog = screen.getByRole("alertdialog");
    expect(
      within(dialog).getByRole("button", { name: "Cancelar" }),
    ).toHaveFocus();
    fireEvent.keyDown(dialog, { key: "Escape" });
    expect(apiMocks.removeOrganizationOwnerMembership).not.toHaveBeenCalled();

    fireEvent.click(removeButton);
    dialog = screen.getByRole("alertdialog");
    fireEvent.click(
      within(dialog).getByRole("button", {
        name: "Quitar acceso a Bruno Productor",
      }),
    );

    await waitFor(() =>
      expect(apiMocks.startStepUp).toHaveBeenCalledWith(
        "manage_organization_owners",
      ),
    );
    await waitFor(() =>
      expect(apiMocks.removeOrganizationOwnerMembership).toHaveBeenCalledWith(
        organizationId,
        otherMembership.membershipId,
        otherMembership.version,
        expect.stringMatching(/^[0-9a-f]{32}$/),
      ),
    );
    expect(
      await screen.findByText(
        "Bruno Productor ya no tiene acceso a la organización.",
      ),
    ).toBeVisible();
    expect(
      screen.queryByRole("button", {
        name: "Quitar acceso a Bruno Productor",
      }),
    ).not.toBeInTheDocument();
    expect(apiMocks.listOrganizationOwnerMemberships).toHaveBeenCalledTimes(3);
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.identity.owner-removal-action.v1",
      ),
    ).toBeNull();
  });

  it.each([
    [
      "404",
      new IdentityApiError("unavailable", 404, "membership.not_found"),
      "La membresía ya no está disponible",
    ],
    [
      "409 last-owner",
      new IdentityApiError("conflict", 409, "organization.last_owner"),
      "No se puede quitar al último owner activo",
    ],
    [
      "412",
      new IdentityApiError("conflict", 412, "membership.version_mismatch"),
      "La membresía cambió en paralelo",
    ],
    [
      "503",
      new IdentityApiError("provider-down", 503, "service.unavailable"),
      "El servicio no está disponible temporalmente",
    ],
    [
      "429",
      new IdentityApiError("rate-limited", 429, "request.rate_limited"),
      "demasiados intentos",
    ],
    ["offline", new TypeError("Failed to fetch"), "Sin conexi"],
  ])(
    "maps a %s removal response to an actionable UI state",
    async (_, error, copy) => {
      const status = error instanceof IdentityApiError ? error.status : 0;
      if (error instanceof TypeError) {
        vi.spyOn(window.navigator, "onLine", "get").mockReturnValue(false);
      }
      const organizationId = "1266395e-ec88-481e-9a72-81fa2cc2904a";
      const currentMembership: OrganizationOwnerMembershipSummary = {
        membershipId: "2634c6ee-8d8a-49e7-95b7-84fa0660236a",
        organizationId,
        displayName: "Ana Productora",
        isCurrentUser: true,
        role: "owner",
        status: "active",
        authorizationVersion: 1,
        createdAtUtc: "2026-08-10T10:00:00Z",
        removedAtUtc: null,
        version: "4766395e-ec88-481e-9a72-81fa2cc2904a",
      };
      const otherMembership: OrganizationOwnerMembershipSummary = {
        ...currentMembership,
        membershipId: "3766395e-ec88-481e-9a72-81fa2cc2904a",
        displayName: "Bruno Productor",
        isCurrentUser: false,
        version: "5766395e-ec88-481e-9a72-81fa2cc2904a",
      };
      apiMocks.loadSession.mockResolvedValueOnce({
        ...session,
        authentication: {
          level: "strong",
          authenticatedAtUtc: "2026-08-11T20:00:00Z",
          purpose: "manage_organization_owners",
          strongAuthenticatedAtUtc: "2026-08-11T20:01:00Z",
          expiresAtUtc: "2099-08-11T20:06:00Z",
        },
        memberships: [
          { organizationId, organizationName: "La Esperanza", role: "owner" },
          ...(status === 503 || status === 429 || error instanceof TypeError
            ? [
                {
                  organizationId: "64eb2297-6603-4fc9-b2b0-a582a7bf1992",
                  organizationName: "El Ombu",
                  role: "owner",
                },
              ]
            : []),
        ],
      });
      apiMocks.listOrganizationOwnerMemberships.mockResolvedValueOnce([
        currentMembership,
        otherMembership,
      ]);
      apiMocks.removeOrganizationOwnerMembership.mockRejectedValueOnce(error);

      render(<IdentityHub />);
      fireEvent.click(
        await screen.findByRole("button", {
          name: "Quitar acceso a Bruno Productor",
        }),
      );
      fireEvent.click(
        within(screen.getByRole("alertdialog")).getByRole("button", {
          name: "Quitar acceso a Bruno Productor",
        }),
      );

      expect(await screen.findByText(new RegExp(copy, "i"))).toBeVisible();
      const persistedAction = globalThis.sessionStorage.getItem(
        "agropecuaria.identity.owner-removal-action.v1",
      );
      if (status === 503 || error instanceof TypeError) {
        expect(persistedAction).toContain(otherMembership.membershipId);
        fireEvent.click(screen.getByRole("button", { name: "Campos" }));
        expect(await screen.findByText("Tus campos")).toBeVisible();
        expect(
          globalThis.sessionStorage.getItem(
            "agropecuaria.identity.owner-removal-action.v1",
          ),
        ).toBe(persistedAction);
        fireEvent.click(
          screen.getByRole("button", { name: /Cambiar organi/i }),
        );
        expect(screen.getByText(/Hay una operaci/i)).toBeInTheDocument();
        expect(
          screen.getByRole("heading", { name: "Espacio de La Esperanza" }),
        ).toBeVisible();
        expect(
          globalThis.sessionStorage.getItem(
            "agropecuaria.identity.owner-removal-action.v1",
          ),
        ).toBe(persistedAction);
      } else if (status === 429) {
        expect(persistedAction).toBeNull();
        fireEvent.click(
          screen.getByRole("button", { name: /Cambiar organi/i }),
        );
        expect(await screen.findByText(/Elegir organi/i)).toBeVisible();
      } else {
        expect(persistedAction).toBeNull();
      }
    },
  );
});

describe("IdentityHub owner invitation bearer handling", () => {
  it("removes the bearer token from the fragment before preserving it in session storage", () => {
    const token = "a".repeat(43);
    globalThis.history.replaceState(
      {},
      "",
      `/?source=share#owner-invitation=${token}`,
    );

    expect(captureOwnerInvitationTokenFromHash()).toBe(token);
    expect(globalThis.location.hash).toBe("");
    expect(globalThis.location.search).toBe("?source=share");
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.identity.owner-invitation-token.v1",
      ),
    ).toBe(token);
  });

  it("clears and rejects a malformed invitation fragment", async () => {
    globalThis.history.replaceState(
      {},
      "",
      "/#owner-invitation=not-a-256-bit-token",
    );

    render(<IdentityHub />);

    expect(await screen.findByText("Enlace no disponible")).toBeVisible();
    expect(globalThis.location.hash).toBe("");
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.identity.owner-invitation-token.v1",
      ),
    ).toBeNull();
  });

  it("accepts a captured token and removes its browser storage after success", async () => {
    const token = "b".repeat(43);
    apiMocks.loadCapabilities.mockResolvedValueOnce({
      ...capabilities,
      ownerInvitationsAvailable: true,
    });
    apiMocks.acceptOwnerInvitation.mockResolvedValueOnce(created);
    globalThis.history.replaceState({}, "", `/#owner-invitation=${token}`);

    render(<IdentityHub />);

    await waitFor(() =>
      expect(apiMocks.acceptOwnerInvitation).toHaveBeenCalledWith(token),
    );
    expect(globalThis.location.hash).toBe("");
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.identity.owner-invitation-token.v1",
      ),
    ).toBeNull();
    expect(await screen.findByText(/Ya sos co-owner/i)).toBeVisible();
  });

  it("keeps the token for an explicit retry after an offline failure", async () => {
    const token = "c".repeat(43);
    vi.spyOn(window.navigator, "onLine", "get").mockReturnValue(false);
    apiMocks.loadCapabilities.mockResolvedValueOnce({
      ...capabilities,
      ownerInvitationsAvailable: true,
    });
    apiMocks.acceptOwnerInvitation
      .mockRejectedValueOnce(new TypeError("Failed to fetch"))
      .mockResolvedValueOnce(created);
    globalThis.history.replaceState({}, "", `/#owner-invitation=${token}`);

    render(<IdentityHub />);

    expect(
      await screen.findByRole("button", { name: "Reintentar" }),
    ).toBeVisible();
    expect(apiMocks.acceptOwnerInvitation).toHaveBeenCalledTimes(1);
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.identity.owner-invitation-token.v1",
      ),
    ).toBe(token);

    fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));
    await waitFor(() =>
      expect(apiMocks.acceptOwnerInvitation).toHaveBeenCalledTimes(2),
    );
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.identity.owner-invitation-token.v1",
      ),
    ).toBeNull();
  });
});

describe("IdentityHub owner invitation management", () => {
  beforeEach(() => {
    globalThis.history.replaceState({}, "", "/?org=126639&view=team");
  });

  it("performs a purpose-bound step-up before revealing an owner link", async () => {
    const organizationId = "1266395e-ec88-481e-9a72-81fa2cc2904a";
    const ownerSession: IdentitySession = {
      ...session,
      memberships: [
        { organizationId, organizationName: "La Esperanza", role: "owner" },
      ],
    };
    const strongSession: IdentitySession = {
      ...ownerSession,
      authentication: {
        level: "strong",
        authenticatedAtUtc: "2026-08-11T20:00:00Z",
        purpose: "manage_organization_owners",
        strongAuthenticatedAtUtc: "2026-08-11T20:01:00Z",
        expiresAtUtc: "2099-08-11T20:06:00Z",
      },
    };
    const stepUp: StepUpAttempt = {
      attemptId: "d68a20db-bae4-4822-99bd-e016517bd0ee",
      purpose: "manage_organization_owners",
      expiresAtUtc: "2099-08-11T20:06:00Z",
      authorizationUrl: "/provider",
    };
    apiMocks.loadCapabilities.mockResolvedValueOnce({
      ...capabilities,
      ownerInvitationsAvailable: true,
    });
    apiMocks.loadSession.mockResolvedValueOnce(ownerSession);
    apiMocks.startStepUp.mockResolvedValueOnce(stepUp);
    apiMocks.completeDevelopmentStepUp.mockResolvedValueOnce(strongSession);
    apiMocks.createOwnerInvitation.mockResolvedValue({
      invitation: {
        invitationId: "3766395e-ec88-481e-9a72-81fa2cc2904a",
        organizationId,
        status: "pending",
        createdAtUtc: "2026-08-11T20:01:00Z",
        expiresAtUtc: "2026-08-18T20:01:00Z",
        acceptedAtUtc: null,
        revokedAtUtc: null,
        version: "4766395e-ec88-481e-9a72-81fa2cc2904a",
      },
      token: "d".repeat(43),
      isReplay: false,
    });

    render(<IdentityHub />);
    fireEvent.click(
      await screen.findByRole("button", {
        name: "Verificar y crear enlace",
      }),
    );

    await waitFor(() =>
      expect(apiMocks.startStepUp).toHaveBeenCalledWith(
        "manage_organization_owners",
      ),
    );
    await waitFor(() =>
      expect(apiMocks.createOwnerInvitation).toHaveBeenCalledWith(
        organizationId,
        expect.stringMatching(/^[0-9a-f]{32}$/),
      ),
    );
    expect(await screen.findByText("Guardá el enlace ahora")).toBeVisible();
  });
});
