import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { IdentityView } from "../../features/identity/identity-view";
import type {
  IdentityCapabilities,
  IdentityNotice,
  IdentityResourceState,
  IdentitySession,
} from "../../features/identity/identity-types";

const productionCapabilities: IdentityCapabilities = {
  environment: "Production",
  oidcConfigured: true,
  developmentProviderEnabled: true,
  connections: [
    { id: "email", label: "Correo", available: true },
    { id: "google", label: "Google", available: true },
  ],
};

const session: IdentitySession = {
  userId: "2e9f14bd-eec9-4bd8-b1ca-da0c6c615f78",
  displayName: "Ana Productora",
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

function renderView(
  resource: IdentityResourceState,
  notice: IdentityNotice = { kind: "none" },
) {
  const handlers = {
    onRetry: vi.fn(),
    onLogin: vi.fn(),
    onLink: vi.fn(),
    onUnlink: vi.fn(),
    onRevoke: vi.fn(),
    onSyntheticSignIn: vi.fn(),
  };
  const result = render(
    <IdentityView
      notice={notice}
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

  it("renders only short IDs and an explicit empty membership state", () => {
    const { container } = renderView({
      kind: "authenticated",
      capabilities: productionCapabilities,
      session,
    });

    expect(screen.getByText(/usuario 2E9F14/i)).toBeVisible();
    expect(screen.getByText(/ID 6F0B92/i)).toBeVisible();
    expect(
      screen.getByText(/todavía no tenés una organización/i),
    ).toBeVisible();
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
        notice={{ kind: "replay", message: "Ese intento ya fue utilizado." }}
        onLink={vi.fn()}
        onLogin={vi.fn()}
        onRetry={vi.fn()}
        onRevoke={vi.fn()}
        onSyntheticSignIn={vi.fn()}
        onUnlink={vi.fn()}
        pendingAction={null}
        resource={{ kind: "signed-out", capabilities: productionCapabilities }}
      />,
    );
    expect(screen.getByRole("alert")).toHaveTextContent("ya fue utilizado");
  });
});
