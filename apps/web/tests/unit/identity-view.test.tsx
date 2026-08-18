import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

const workspaceMockState = vi.hoisted(() => ({
  view: "account" as "fields" | "team" | "territory" | "account",
}));

vi.mock("../../features/workspace/owner-workspace", async (importOriginal) => {
  const actual =
    await importOriginal<
      typeof import("../../features/workspace/owner-workspace")
    >();
  return {
    ...actual,
    OwnerWorkspaceShell: ({
      memberships,
      onboarding,
      children,
    }: import("../../features/workspace/owner-workspace").OwnerWorkspaceShellProps) => {
      const membership = memberships.find(
        (candidate) => candidate.role.toLowerCase() === "owner",
      );
      if (membership === undefined) return onboarding;
      return children({
        kind: "active",
        membership,
        view: workspaceMockState.view,
        source: "automatic",
      });
    },
  };
});

import { IdentityView } from "../../features/identity/identity-view";
import type {
  IdentityCapabilities,
  IdentityNotice,
  IdentityResourceState,
  IdentitySession,
  OrganizationCreationState,
  OwnerInvitationAcceptanceState,
  OwnerInvitationActionState,
  OwnerMembershipActionState,
  OwnSessionActionState,
  OwnSessionResourceState,
} from "../../features/identity/identity-types";

const productionCapabilities: IdentityCapabilities = {
  environment: "Production",
  oidcConfigured: true,
  developmentProviderEnabled: true,
  strongAuthenticationAvailable: true,
  ownerInvitationsAvailable: false,
  connections: [
    { id: "email", label: "Correo", available: true },
    { id: "google", label: "Google", available: true },
  ],
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

const accountSession: IdentitySession = {
  ...session,
  memberships: [
    {
      organizationId: "1266395e-ec88-481e-9a72-81fa2cc2904a",
      organizationName: "La Esperanza",
      role: "owner",
    },
  ],
};

afterEach(cleanup);

const invitationProps = {
  ownerInvitations: {},
  ownerInvitationAction: { kind: "idle" } as OwnerInvitationActionState,
  ownerInvitationAcceptance: {
    kind: "idle",
  } as OwnerInvitationAcceptanceState,
  onCreateOwnerInvitation: vi.fn(),
  onRevokeOwnerInvitation: vi.fn(),
  onRefreshOwnerInvitations: vi.fn(),
  onCopyOwnerInvitation: vi.fn(),
  onResumeOwnerManagement: vi.fn(),
  onAcceptOwnerInvitation: vi.fn(),
  onReauthenticateOwnerInvitation: vi.fn(),
};

const ownerMembershipProps = {
  ownerMemberships: {},
  ownerMembershipAction: { kind: "idle" } as OwnerMembershipActionState,
  onBeginOwnerRemoval: vi.fn(),
  onCancelOwnerRemoval: vi.fn(),
  onConfirmOwnerRemoval: vi.fn(),
  onRefreshOwnerMemberships: vi.fn(),
  onRetryOwnerRemoval: vi.fn(),
  onResumeOwnerRemoval: vi.fn(),
  onDismissOwnerRemoval: vi.fn(),
};

const ownSessionProps = {
  ownSessions: { kind: "idle" } as const,
  ownSessionAction: { kind: "idle" } as const,
  onBeginAllOtherOwnSessionRevocation: vi.fn(),
  onBeginOwnSessionRevocation: vi.fn(),
  onCancelOwnSessionRevocation: vi.fn(),
  onConfirmOwnSessionRevocation: vi.fn(),
  onRefreshOwnSessions: vi.fn(),
  onPreviousOwnSessions: vi.fn(),
  onNextOwnSessions: vi.fn(),
  onRetryOwnSessionRevocation: vi.fn(),
  onResumeOwnSessionRevocation: vi.fn(),
  onDismissOwnSessionNotice: vi.fn(),
};

function renderView(
  resource: IdentityResourceState,
  notice: IdentityNotice = { kind: "none" },
  organizationOptions: Readonly<{
    draft?: string;
    formOpen?: boolean;
    creation?: OrganizationCreationState;
  }> = {},
  invitationOptions: Partial<typeof invitationProps> = {},
  ownerMembershipOptions: Partial<typeof ownerMembershipProps> = {},
  ownSessionOptions: Readonly<{
    resources?: OwnSessionResourceState;
    action?: OwnSessionActionState;
  }> = {},
) {
  workspaceMockState.view =
    Object.keys(invitationOptions).length > 0 ||
    Object.keys(ownerMembershipOptions).length > 0
      ? "team"
      : "account";
  const handlers = {
    onRetry: vi.fn(),
    onLogin: vi.fn(),
    onLink: vi.fn(),
    onStepUp: vi.fn(),
    onUnlink: vi.fn(),
    onRevoke: vi.fn(),
    onSyntheticSignIn: vi.fn(),
    onOrganizationDraftChange: vi.fn(),
    onStartOrganization: vi.fn(),
    onCancelOrganization: vi.fn(),
    onCreateOrganization: vi.fn(),
    onReauthenticateOrganization: vi.fn(),
    onBeginAllOtherOwnSessionRevocation: vi.fn(),
    onBeginOwnSessionRevocation: vi.fn(),
    onCancelOwnSessionRevocation: vi.fn(),
    onConfirmOwnSessionRevocation: vi.fn(),
    onRefreshOwnSessions: vi.fn(),
    onPreviousOwnSessions: vi.fn(),
    onNextOwnSessions: vi.fn(),
    onRetryOwnSessionRevocation: vi.fn(),
    onResumeOwnSessionRevocation: vi.fn(),
    onDismissOwnSessionNotice: vi.fn(),
  };
  const result = render(
    <IdentityView
      {...invitationProps}
      {...ownerMembershipProps}
      {...ownSessionProps}
      ownSessionAction={ownSessionOptions.action ?? { kind: "idle" }}
      ownSessions={ownSessionOptions.resources ?? { kind: "idle" }}
      {...ownerMembershipOptions}
      {...invitationOptions}
      notice={notice}
      organizationCreation={organizationOptions.creation ?? { kind: "idle" }}
      organizationDraft={organizationOptions.draft ?? ""}
      organizationFormOpen={organizationOptions.formOpen ?? false}
      pendingAction={null}
      resource={resource}
      {...handlers}
    />,
  );
  return { ...result, handlers };
}

describe("IdentityView", () => {
  it("keeps loading scoped to the identity region", () => {
    const { container } = renderView({ kind: "loading" });

    expect(
      screen.getByRole("heading", { name: /cuaderno que trabaja/i }),
    ).toBeVisible();
    expect(container.querySelector(".identity-region")).toHaveAttribute(
      "aria-busy",
      "true",
    );
    expect(screen.getByRole("status")).toHaveTextContent(
      "Comprobando tu sesión",
    );
  });

  it("never exposes synthetic access in Production even when the capability flag is true", () => {
    renderView({ kind: "signed-out", capabilities: productionCapabilities });

    expect(
      screen.queryByRole("button", { name: /fixture seguro/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /continuar con correo/i }),
    ).toBeEnabled();
    expect(
      screen.getByRole("button", { name: /continuar con google/i }),
    ).toBeEnabled();
  });

  it("shows synthetic access only in an explicitly enabled development environment", () => {
    renderView({
      kind: "signed-out",
      capabilities: { ...productionCapabilities, environment: "Development" },
    });

    expect(
      screen.getByRole("button", { name: /fixture seguro/i }),
    ).toBeEnabled();
  });

  it("starts login directly when a provider is selected", () => {
    const { handlers } = renderView({
      kind: "signed-out",
      capabilities: productionCapabilities,
    });

    fireEvent.click(
      screen.getByRole("button", { name: /continuar con google/i }),
    );

    expect(handlers.onLogin).toHaveBeenCalledWith("google");
  });

  it("renders only short IDs in the private account workspace", () => {
    const { container } = renderView({
      kind: "authenticated",
      capabilities: productionCapabilities,
      session: accountSession,
    });

    expect(screen.getByText(/usuario 2E9F14/i)).toBeVisible();
    expect(screen.getByText(/ID 6F0B92/i)).toBeVisible();
    expect(container).not.toHaveTextContent(session.userId);
    expect(container).not.toHaveTextContent(
      session.identities[0]?.identityId ?? "missing",
    );
  });

  it("lists only the public own-session fields and directs current logout to the existing control", () => {
    const currentSessionId = "8766395e-ec88-481e-9a72-81fa2cc2904a";
    const otherSessionId = "9766395e-ec88-481e-9a72-81fa2cc2904a";
    const other = {
      sessionId: otherSessionId,
      authenticatedAtUtc: "2026-08-17T10:00:00Z",
      expiresAtUtc: "2026-08-24T10:00:00Z",
      isCurrent: false,
      version: "a766395e-ec88-481e-9a72-81fa2cc2904a",
    } as const;
    const { container, handlers } = renderView(
      {
        kind: "authenticated",
        capabilities: productionCapabilities,
        session: accountSession,
      },
      { kind: "none" },
      {},
      {},
      {},
      {
        resources: {
          kind: "ready",
          page: {
            items: [
              {
                sessionId: currentSessionId,
                authenticatedAtUtc: "2026-08-18T10:00:00Z",
                expiresAtUtc: "2026-08-25T10:00:00Z",
                isCurrent: true,
                version: "b766395e-ec88-481e-9a72-81fa2cc2904a",
              },
              other,
            ],
            total: 2,
            offset: 0,
            limit: 20,
          },
        },
      },
    );

    expect(screen.getByText("Sesión 876639")).toBeVisible();
    expect(screen.getByText("Actual")).toBeVisible();
    expect(screen.getByText(/usa el control Cerrar sesión/i)).toBeVisible();
    expect(
      screen.queryByRole("button", { name: "Cerrar sesión 876639" }),
    ).not.toBeInTheDocument();
    fireEvent.click(
      screen.getByRole("button", {
        name: "Cerrar sesión 976639",
      }),
    );
    expect(handlers.onBeginOwnSessionRevocation).toHaveBeenCalledWith(other);
    expect(container).not.toHaveTextContent(currentSessionId);
    expect(container).not.toHaveTextContent(otherSessionId);
    expect(container).not.toHaveTextContent("user-agent");
    expect(container).not.toHaveTextContent("token");
  });

  it("keeps the session confirmation keyboard-accessible and restores the trigger", () => {
    const target = {
      sessionId: "9766395e-ec88-481e-9a72-81fa2cc2904a",
      authenticatedAtUtc: "2026-08-17T10:00:00Z",
      expiresAtUtc: "2026-08-24T10:00:00Z",
      isCurrent: false,
      version: "a766395e-ec88-481e-9a72-81fa2cc2904a",
    } as const;
    const { handlers } = renderView(
      {
        kind: "authenticated",
        capabilities: productionCapabilities,
        session: accountSession,
      },
      { kind: "none" },
      {},
      {},
      {},
      {
        resources: {
          kind: "ready",
          page: { items: [target], total: 1, offset: 0, limit: 20 },
        },
        action: {
          kind: "confirming",
          intent: { kind: "single", session: target },
        },
      },
    );

    const dialog = screen.getByRole("alertdialog", {
      name: "¿Cerrar la sesión 976639?",
    });
    expect(screen.getByRole("button", { name: "Cancelar" })).toHaveFocus();
    expect(dialog).toHaveAccessibleDescription(/próxima solicitud/i);
    fireEvent.keyDown(dialog, { key: "Escape" });
    expect(handlers.onCancelOwnSessionRevocation).toHaveBeenCalledOnce();
    fireEvent.click(
      screen.getByRole("button", {
        name: "Confirmar cierre de sesión 976639",
      }),
    );
    expect(handlers.onConfirmOwnSessionRevocation).toHaveBeenCalledOnce();
  });

  it("offers an accessible all-other-sessions confirmation only when another session exists", () => {
    const current = {
      sessionId: "8766395e-ec88-481e-9a72-81fa2cc2904a",
      authenticatedAtUtc: "2026-08-18T10:00:00Z",
      expiresAtUtc: "2026-08-25T10:00:00Z",
      isCurrent: true,
      version: "b766395e-ec88-481e-9a72-81fa2cc2904a",
    } as const;
    const other = {
      sessionId: "9766395e-ec88-481e-9a72-81fa2cc2904a",
      authenticatedAtUtc: "2026-08-17T10:00:00Z",
      expiresAtUtc: "2026-08-24T10:00:00Z",
      isCurrent: false,
      version: "a766395e-ec88-481e-9a72-81fa2cc2904a",
    } as const;
    const authenticated = {
      kind: "authenticated",
      capabilities: productionCapabilities,
      session: accountSession,
    } as const;

    renderView(
      authenticated,
      { kind: "none" },
      {},
      {},
      {},
      {
        resources: {
          kind: "ready",
          page: { items: [current], total: 1, offset: 0, limit: 20 },
        },
      },
    );
    expect(
      screen.getByRole("button", { name: "Cerrar las otras sesiones" }),
    ).toBeDisabled();

    cleanup();
    const { handlers } = renderView(
      authenticated,
      { kind: "none" },
      {},
      {},
      {},
      {
        resources: {
          kind: "ready",
          page: { items: [current, other], total: 2, offset: 0, limit: 20 },
        },
      },
    );
    const trigger = screen.getByRole("button", {
      name: "Cerrar las otras sesiones",
    });
    expect(trigger).toBeEnabled();
    fireEvent.click(trigger);
    expect(handlers.onBeginAllOtherOwnSessionRevocation).toHaveBeenCalledOnce();

    cleanup();
    const confirmation = renderView(
      authenticated,
      { kind: "none" },
      {},
      {},
      {},
      {
        resources: {
          kind: "ready",
          page: { items: [current, other], total: 2, offset: 0, limit: 20 },
        },
        action: {
          kind: "confirming",
          intent: { kind: "all-others" },
        },
      },
    );
    const dialog = screen.getByRole("alertdialog", {
      name: "¿Cerrar las otras sesiones?",
    });
    expect(screen.getByRole("button", { name: "Cancelar" })).toHaveFocus();
    expect(dialog).toHaveAccessibleDescription(
      /sesión actual permanecerá abierta/i,
    );
    expect(confirmation.container).not.toHaveTextContent(current.sessionId);
    expect(confirmation.container).not.toHaveTextContent(other.sessionId);
    fireEvent.keyDown(dialog, { key: "Escape" });
    expect(
      confirmation.handlers.onCancelOwnSessionRevocation,
    ).toHaveBeenCalledOnce();
    fireEvent.click(
      screen.getByRole("button", {
        name: "Confirmar cierre de las otras sesiones",
      }),
    );
    expect(
      confirmation.handlers.onConfirmOwnSessionRevocation,
    ).toHaveBeenCalledOnce();
  });

  it.each([
    [
      { kind: "loading" } as OwnSessionResourceState,
      "Cargando sesiones",
      "status",
    ],
    [
      {
        kind: "ready",
        page: { items: [], total: 0, offset: 0, limit: 20 },
      } as OwnSessionResourceState,
      "No hay sesiones abiertas",
      "status",
    ],
    [{ kind: "offline" } as OwnSessionResourceState, "sin conexión", "alert"],
    [
      { kind: "unavailable" } as OwnSessionResourceState,
      "no está disponible",
      "alert",
    ],
    [
      { kind: "error" } as OwnSessionResourceState,
      "No pudimos cargar",
      "alert",
    ],
  ])("renders the own-session resource state", (resources, message, role) => {
    renderView(
      {
        kind: "authenticated",
        capabilities: productionCapabilities,
        session: accountSession,
      },
      { kind: "none" },
      {},
      {},
      {},
      { resources },
    );

    expect(
      screen
        .getAllByRole(role)
        .some((element) => element.textContent?.includes(message)),
    ).toBe(true);
  });

  it.each([
    [
      {
        kind: "reauthentication-required",
        intent: {
          kind: "single",
          session: {
            sessionId: "9766395e-ec88-481e-9a72-81fa2cc2904a",
            authenticatedAtUtc: "2026-08-17T10:00:00Z",
            expiresAtUtc: "2026-08-24T10:00:00Z",
            isCurrent: false,
            version: "a766395e-ec88-481e-9a72-81fa2cc2904a",
          },
        },
        message: "Verificá tu identidad con MFA.",
      } as OwnSessionActionState,
      "Verificá tu identidad",
      "status",
    ],
    [
      {
        kind: "stale",
        message: "La sesión cambió en paralelo.",
      } as OwnSessionActionState,
      "cambió en paralelo",
      "alert",
    ],
    [
      {
        kind: "revoked",
        message: "La otra sesión fue cerrada.",
      } as OwnSessionActionState,
      "fue cerrada",
      "status",
    ],
    [
      {
        kind: "rate-limited",
        message: "Esperá un momento antes de reintentar.",
      } as OwnSessionActionState,
      "Esperá un momento",
      "alert",
    ],
    [
      {
        kind: "service-unavailable",
        message: "La gestión de sesiones no está disponible.",
      } as OwnSessionActionState,
      "no está disponible",
      "alert",
    ],
  ])("announces the own-session action state", (action, message, role) => {
    renderView(
      {
        kind: "authenticated",
        capabilities: productionCapabilities,
        session: accountSession,
      },
      { kind: "none" },
      {},
      {},
      {},
      { action },
    );

    expect(
      screen
        .getAllByRole(role)
        .some((element) => element.textContent?.includes(message)),
    ).toBe(true);
  });

  it("does not allow removing the last identity", () => {
    renderView({
      kind: "authenticated",
      capabilities: productionCapabilities,
      session: accountSession,
    });

    expect(
      screen.queryByRole("button", { name: /desvincular/i }),
    ).not.toBeInTheDocument();
  });

  it("announces conflict and replay notices", () => {
    const { rerender } = renderView(
      { kind: "signed-out", capabilities: productionCapabilities },
      { kind: "conflict", message: "La identidad cambió en paralelo." },
    );
    expect(screen.getByRole("alert")).toHaveTextContent("cambió en paralelo");

    rerender(
      <IdentityView
        {...invitationProps}
        {...ownerMembershipProps}
        {...ownSessionProps}
        notice={{ kind: "replay", message: "Ese intento ya fue utilizado." }}
        onCancelOrganization={vi.fn()}
        onCreateOrganization={vi.fn()}
        onLink={vi.fn()}
        onStepUp={vi.fn()}
        onLogin={vi.fn()}
        onOrganizationDraftChange={vi.fn()}
        onReauthenticateOrganization={vi.fn()}
        onRetry={vi.fn()}
        onRevoke={vi.fn()}
        onStartOrganization={vi.fn()}
        onSyntheticSignIn={vi.fn()}
        onUnlink={vi.fn()}
        organizationCreation={{ kind: "idle" }}
        organizationDraft=""
        organizationFormOpen={false}
        pendingAction={null}
        resource={{ kind: "signed-out", capabilities: productionCapabilities }}
      />,
    );
    expect(screen.getByRole("alert")).toHaveTextContent("ya fue utilizado");
  });

  it("keeps MFA loading inside the assurance card", () => {
    const onStepUp = vi.fn();
    workspaceMockState.view = "account";
    const { container } = render(
      <IdentityView
        {...invitationProps}
        {...ownerMembershipProps}
        {...ownSessionProps}
        notice={{ kind: "none" }}
        onCancelOrganization={vi.fn()}
        onCreateOrganization={vi.fn()}
        onLink={vi.fn()}
        onLogin={vi.fn()}
        onOrganizationDraftChange={vi.fn()}
        onReauthenticateOrganization={vi.fn()}
        onRetry={vi.fn()}
        onRevoke={vi.fn()}
        onStartOrganization={vi.fn()}
        onStepUp={onStepUp}
        onSyntheticSignIn={vi.fn()}
        onUnlink={vi.fn()}
        organizationCreation={{ kind: "idle" }}
        organizationDraft=""
        organizationFormOpen={false}
        pendingAction="step-up"
        resource={{
          kind: "authenticated",
          capabilities: productionCapabilities,
          session: accountSession,
        }}
      />,
    );

    expect(container.querySelector(".identity-region")).toHaveAttribute(
      "aria-busy",
      "false",
    );
    expect(container.querySelector(".assurance-card")).toHaveAttribute(
      "aria-busy",
      "true",
    );
    expect(
      screen.getByRole("button", { name: /verificando con mfa/i }),
    ).toBeDisabled();
  });

  it("requests a purpose-bound MFA step-up without claiming factor enrollment", () => {
    const { handlers } = renderView({
      kind: "authenticated",
      capabilities: productionCapabilities,
      session: accountSession,
    });

    fireEvent.click(screen.getByRole("button", { name: "Verificar con MFA" }));

    expect(handlers.onStepUp).toHaveBeenCalledOnce();
    expect(screen.getByText(/custodiados por el proveedor/i)).toBeVisible();
    expect(screen.queryByRole("button", { name: /crear passkey/i })).toBeNull();
  });

  it("describes manage-sessions assurance without claiming authentication-method scope", () => {
    renderView({
      kind: "authenticated",
      capabilities: productionCapabilities,
      session: {
        ...accountSession,
        authentication: {
          level: "strong",
          authenticatedAtUtc: "2026-08-18T10:00:00Z",
          purpose: "manage_sessions",
          strongAuthenticatedAtUtc: "2026-08-18T10:01:00Z",
          expiresAtUtc: "2026-08-18T10:06:00Z",
        },
      },
    });

    const assurance = screen.getByRole("region", {
      name: "Identidad verificada con MFA",
    });
    expect(assurance).toHaveTextContent("administrar tus sesiones abiertas");
    expect(assurance).not.toHaveTextContent("administrar métodos de acceso");
  });

  it("opens zero-membership onboarding and exposes an accessible form", () => {
    const { handlers } = renderView({
      kind: "authenticated",
      capabilities: productionCapabilities,
      session,
    });

    fireEvent.click(
      screen.getByRole("button", { name: "Crear mi organización" }),
    );
    expect(handlers.onStartOrganization).toHaveBeenCalledOnce();

    cleanup();
    renderView(
      {
        kind: "authenticated",
        capabilities: productionCapabilities,
        session,
      },
      { kind: "none" },
      { draft: "La Esperanza", formOpen: true },
    );

    const input = screen.getByRole("textbox", {
      name: "Nombre de la organización",
    });
    expect(input).toHaveFocus();
    expect(input).toHaveValue("La Esperanza");
    expect(input).toHaveAccessibleDescription(/entre 2 y 160 caracteres/i);
  });

  it("lists memberships with short IDs and offers creating another", () => {
    const organizationId = "1266395e-ec88-481e-9a72-81fa2cc2904a";
    const { container, handlers } = renderView({
      kind: "authenticated",
      capabilities: productionCapabilities,
      session: {
        ...session,
        memberships: [
          {
            organizationId,
            organizationName: "La Esperanza",
            role: "owner",
          },
        ],
      },
    });

    expect(screen.getAllByText("Organización 126639")).toHaveLength(1);
    expect(container).not.toHaveTextContent(organizationId);
    fireEvent.click(screen.getByRole("button", { name: "Crear otra" }));
    expect(handlers.onStartOrganization).toHaveBeenCalledOnce();
  });

  it("limits the loader to onboarding and preserves the shell", () => {
    const { container } = renderView(
      {
        kind: "authenticated",
        capabilities: productionCapabilities,
        session,
      },
      { kind: "none" },
      {
        draft: "La Esperanza",
        formOpen: true,
        creation: { kind: "submitting" },
      },
    );

    expect(container.querySelector(".identity-region")).toHaveAttribute(
      "aria-busy",
      "false",
    );
    expect(container.querySelector(".organization-onboarding")).toHaveAttribute(
      "aria-busy",
      "true",
    );
    expect(screen.getByRole("heading", { name: "Tu acceso" })).toBeVisible();
    expect(
      screen.getByRole("button", { name: /creando organización/i }),
    ).toBeDisabled();
  });

  it.each([
    ["validation", "Revisá el nombre", "alert", false],
    ["reauthentication-required", "Volvé a verificarte", "alert", false],
    ["in-progress", "Sigue en curso", "status", true],
    ["conflict", "El intento no coincide", "alert", false],
    ["reconciliation-required", "Resultado pendiente", "status", true],
    ["rate-limited", "Esperá un momento", "alert", false],
    ["offline", "Sin conexión", "alert", false],
  ] as const)(
    "announces the %s state and preserves the draft",
    (kind, message, role, disabled) => {
      renderView(
        {
          kind: "authenticated",
          capabilities: productionCapabilities,
          session,
        },
        { kind: "none" },
        {
          draft: "La Esperanza",
          formOpen: true,
          creation: { kind, message },
        },
      );

      const input = screen.getByRole("textbox", {
        name: "Nombre de la organización",
      });
      expect(input).toHaveValue("La Esperanza");
      expect(input).toHaveProperty("disabled", disabled);
      expect(screen.getByRole(role)).toHaveTextContent(message);
    },
  );

  it("offers an accessible reauthentication action without submitting the form", () => {
    const { handlers } = renderView(
      {
        kind: "authenticated",
        capabilities: productionCapabilities,
        session,
      },
      { kind: "none" },
      {
        draft: "La Esperanza",
        formOpen: true,
        creation: {
          kind: "reauthentication-required",
          message: "Volvé a verificar tu identidad.",
        },
      },
    );

    fireEvent.click(screen.getByRole("button", { name: "Volver a verificar" }));

    expect(handlers.onReauthenticateOrganization).toHaveBeenCalledOnce();
    expect(handlers.onCreateOrganization).not.toHaveBeenCalled();
    expect(
      screen.getByRole("textbox", { name: "Nombre de la organización" }),
    ).toHaveValue("La Esperanza");
  });

  it("renders owner invitations with short IDs and regional empty/loading states", () => {
    const ownerSession: IdentitySession = {
      ...session,
      memberships: [
        {
          organizationId: "1266395e-ec88-481e-9a72-81fa2cc2904a",
          organizationName: "La Esperanza",
          role: "owner",
        },
      ],
    };
    const ownerCapabilities = {
      ...productionCapabilities,
      ownerInvitationsAvailable: true,
    };
    const onCreateOwnerInvitation = vi.fn();
    renderView(
      {
        kind: "authenticated",
        capabilities: ownerCapabilities,
        session: ownerSession,
      },
      { kind: "none" },
      {},
      {
        ownerInvitations: {
          "1266395e-ec88-481e-9a72-81fa2cc2904a": {
            kind: "ready",
            items: [],
          },
        },
        onCreateOwnerInvitation,
      },
    );

    expect(
      screen.getByRole("heading", { name: "Invitaciones de co-owner" }),
    ).toBeVisible();
    expect(screen.getAllByText("Organización 126639")).toHaveLength(1);
    expect(screen.getByText("No hay invitaciones")).toBeVisible();
    fireEvent.click(
      screen.getByRole("button", { name: "Verificar y crear enlace" }),
    );
    expect(onCreateOwnerInvitation).toHaveBeenCalledWith(
      "1266395e-ec88-481e-9a72-81fa2cc2904a",
    );
  });

  it("shows a bearer invitation once with a privilege warning and copy action", () => {
    const onCopyOwnerInvitation = vi.fn();
    const ownerSession: IdentitySession = {
      ...session,
      memberships: [
        {
          organizationId: "1266395e-ec88-481e-9a72-81fa2cc2904a",
          organizationName: "La Esperanza",
          role: "owner",
        },
      ],
    };
    renderView(
      {
        kind: "authenticated",
        capabilities: {
          ...productionCapabilities,
          ownerInvitationsAvailable: true,
        },
        session: ownerSession,
      },
      { kind: "none" },
      {},
      {
        ownerInvitationAcceptance: { kind: "idle" },
        ownerInvitationAction: {
          kind: "revealed",
          organizationId: "1266395e-ec88-481e-9a72-81fa2cc2904a",
          invitationId: "3766395e-ec88-481e-9a72-81fa2cc2904a",
          shareLink: `${window.location.origin}/#owner-invitation=${"a".repeat(43)}`,
        },
        onCopyOwnerInvitation,
      },
    );

    expect(screen.getByText("Guardá el enlace ahora")).toBeVisible();
    expect(screen.getByText(/podrá convertirse en owner/i)).toBeVisible();
    fireEvent.click(screen.getByRole("button", { name: "Copiar y ocultar" }));
    expect(onCopyOwnerInvitation).toHaveBeenCalledOnce();
  });

  it("offers login for an invitation without exposing the token", () => {
    const onReauthenticateOwnerInvitation = vi.fn();
    renderView(
      { kind: "signed-out", capabilities: productionCapabilities },
      { kind: "none" },
      {},
      {
        ownerInvitationAcceptance: {
          kind: "reauthentication-required",
          message: "Ingresá para aceptar la invitación como co-owner.",
        },
        onReauthenticateOwnerInvitation,
      },
    );

    fireEvent.click(screen.getByRole("button", { name: "Ingresar y aceptar" }));
    expect(onReauthenticateOwnerInvitation).toHaveBeenCalledOnce();
    expect(document.body).not.toHaveTextContent("a".repeat(43));
  });

  it("renders co-owners with short membership IDs and never offers self-removal", () => {
    const organizationId = "1266395e-ec88-481e-9a72-81fa2cc2904a";
    const currentMembershipId = "2634c6ee-8d8a-49e7-95b7-84fa0660236a";
    const otherMembershipId = "3766395e-ec88-481e-9a72-81fa2cc2904a";
    const ownerSession: IdentitySession = {
      ...session,
      memberships: [
        { organizationId, organizationName: "La Esperanza", role: "owner" },
      ],
    };
    const onBeginOwnerRemoval = vi.fn();
    renderView(
      {
        kind: "authenticated",
        capabilities: productionCapabilities,
        session: ownerSession,
      },
      { kind: "none" },
      {},
      {},
      {
        ownerMemberships: {
          [organizationId]: {
            kind: "ready",
            items: [
              {
                membershipId: currentMembershipId,
                organizationId,
                displayName: "Ana Productora",
                isCurrentUser: true,
                role: "owner",
                status: "active",
                authorizationVersion: 1,
                createdAtUtc: "2026-08-10T10:00:00Z",
                removedAtUtc: null,
                version: "4766395e-ec88-481e-9a72-81fa2cc2904a",
              },
              {
                membershipId: otherMembershipId,
                organizationId,
                displayName: "Bruno Productor",
                isCurrentUser: false,
                role: "owner",
                status: "active",
                authorizationVersion: 2,
                createdAtUtc: "2026-08-11T10:00:00Z",
                removedAtUtc: null,
                version: "5766395e-ec88-481e-9a72-81fa2cc2904a",
              },
            ],
          },
        },
        onBeginOwnerRemoval,
      },
    );

    expect(screen.getByRole("heading", { name: "Co-owners" })).toBeVisible();
    expect(screen.getByText("Vos")).toBeVisible();
    expect(screen.getByText("Membresía 2634C6")).toBeVisible();
    expect(screen.getByText("Membresía 376639")).toBeVisible();
    expect(document.body).not.toHaveTextContent(currentMembershipId);
    expect(document.body).not.toHaveTextContent(otherMembershipId);
    expect(
      screen.queryByRole("button", { name: "Quitar acceso a Ana Productora" }),
    ).not.toBeInTheDocument();
    fireEvent.click(
      screen.getByRole("button", { name: "Quitar acceso a Bruno Productor" }),
    );
    expect(onBeginOwnerRemoval).toHaveBeenCalledOnce();
  });

  it("focuses the accessible removal confirmation and cancels it with Escape", () => {
    const organizationId = "1266395e-ec88-481e-9a72-81fa2cc2904a";
    const membership = {
      membershipId: "3766395e-ec88-481e-9a72-81fa2cc2904a",
      organizationId,
      displayName: "Bruno Productor",
      isCurrentUser: false,
      role: "owner" as const,
      status: "active" as const,
      authorizationVersion: 2,
      createdAtUtc: "2026-08-11T10:00:00Z",
      removedAtUtc: null,
      version: "5766395e-ec88-481e-9a72-81fa2cc2904a",
    };
    const onCancelOwnerRemoval = vi.fn();
    renderView(
      {
        kind: "authenticated",
        capabilities: productionCapabilities,
        session: {
          ...session,
          memberships: [
            {
              organizationId,
              organizationName: "La Esperanza",
              role: "owner",
            },
          ],
        },
      },
      { kind: "none" },
      {},
      {},
      {
        ownerMemberships: {
          [organizationId]: { kind: "ready", items: [membership] },
        },
        ownerMembershipAction: { kind: "confirming", membership },
        onCancelOwnerRemoval,
      },
    );

    const dialog = screen.getByRole("alertdialog", {
      name: "¿Quitar acceso a Bruno Productor?",
    });
    expect(screen.getByRole("button", { name: "Cancelar" })).toHaveFocus();
    fireEvent.keyDown(dialog, { key: "Escape" });
    expect(onCancelOwnerRemoval).toHaveBeenCalledOnce();
  });

  it.each([
    ["loading", "Cargando co-owners"],
    ["offline", "Estás sin conexión"],
    ["unavailable", "ya no están disponibles"],
    ["service-unavailable", "no está disponible temporalmente"],
    ["error", "No pudimos cargar los co-owners"],
  ] as const)("renders the %s co-owner resource state", (kind, copy) => {
    const organizationId = "1266395e-ec88-481e-9a72-81fa2cc2904a";
    renderView(
      {
        kind: "authenticated",
        capabilities: productionCapabilities,
        session: {
          ...session,
          memberships: [
            {
              organizationId,
              organizationName: "La Esperanza",
              role: "owner",
            },
          ],
        },
      },
      { kind: "none" },
      {},
      {},
      { ownerMemberships: { [organizationId]: { kind } } },
    );

    expect(screen.getByText(new RegExp(copy, "i"))).toBeVisible();
  });
});
