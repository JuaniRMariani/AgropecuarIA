import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import type {
  CreatedOrganization,
  IdentityCapabilities,
  IdentityConnection,
  IdentitySession,
} from "../../features/identity/identity-types";

const apiMocks = vi.hoisted(() => ({
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

const capabilities: IdentityCapabilities = {
  environment: "Development",
  oidcConfigured: false,
  developmentProviderEnabled: true,
  strongAuthenticationAvailable: true,
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
    expect(screen.getByRole("status")).toHaveTextContent(
      "La Esperanza ya está lista y sos owner.",
    );
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
