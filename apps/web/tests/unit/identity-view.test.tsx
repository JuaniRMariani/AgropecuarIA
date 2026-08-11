import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { IdentityView } from "../../features/identity/identity-view";
import type {
  IdentityCapabilities,
  IdentityNotice,
  IdentityResourceState,
  IdentitySession,
  OrganizationCreationState,
  OwnerInvitationAcceptanceState,
  OwnerInvitationActionState,
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

function renderView(
  resource: IdentityResourceState,
  notice: IdentityNotice = { kind: "none" },
  organizationOptions: Readonly<{
    draft?: string;
    formOpen?: boolean;
    creation?: OrganizationCreationState;
  }> = {},
  invitationOptions: Partial<typeof invitationProps> = {},
) {
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
  };
  const result = render(
    <IdentityView
      {...invitationProps}
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

  it("renders only short IDs and an explicit private onboarding CTA", () => {
    const { container } = renderView({
      kind: "authenticated",
      capabilities: productionCapabilities,
      session,
    });

    expect(screen.getByText(/usuario 2E9F14/i)).toBeVisible();
    expect(screen.getByText(/ID 6F0B92/i)).toBeVisible();
    expect(screen.getByText(/creá tu primera organización/i)).toBeVisible();
    expect(
      screen.getByRole("button", { name: "Crear mi organización" }),
    ).toBeEnabled();
    expect(container).not.toHaveTextContent(session.userId);
    expect(container).not.toHaveTextContent(
      session.identities[0]?.identityId ?? "missing",
    );
  });

  it("does not allow removing the last identity", () => {
    renderView({
      kind: "authenticated",
      capabilities: productionCapabilities,
      session,
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
    const { container } = render(
      <IdentityView
        {...invitationProps}
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
          session,
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
      session,
    });

    fireEvent.click(screen.getByRole("button", { name: "Verificar con MFA" }));

    expect(handlers.onStepUp).toHaveBeenCalledOnce();
    expect(screen.getByText(/custodiados por el proveedor/i)).toBeVisible();
    expect(screen.queryByRole("button", { name: /crear passkey/i })).toBeNull();
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

    expect(screen.getByText("Organización 126639")).toBeVisible();
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
    expect(screen.getByText("Ana Productora")).toBeVisible();
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
    expect(screen.getAllByText("Organización 126639")).toHaveLength(2);
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
});
