import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import type {
  CreatedOwnerInvitation,
  CreatedOrganization,
  IdentityCapabilities,
  IdentityConnection,
  IdentitySession,
  OrganizationOwnerMembershipSummary,
  RemovedOrganizationOwnerMembership,
  StepUpAttempt,
} from "../../features/identity/identity-types";

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
  apiMocks.identityLoginUrl.mockReturnValue("#reauthenticate");
});

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
    expect(await screen.findByText("Organización 126639")).toBeVisible();
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
  ])(
    "maps a %s removal response to an actionable UI state",
    async (_, error, copy) => {
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
      if (error.status === 503) {
        expect(persistedAction).toContain(otherMembership.membershipId);
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
    apiMocks.createOwnerInvitation.mockResolvedValueOnce({
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
