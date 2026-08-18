import {
  act,
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import type { MembershipSummary } from "../../features/identity/identity-types";
import {
  buildWorkspaceUrl,
  OwnerWorkspaceShell,
  resolveWorkspaceLocation,
  useWorkspaceFieldNavigation,
  type ActiveWorkspace,
  type WorkspaceFieldIntent,
} from "../../features/workspace/owner-workspace";

const organizationA: MembershipSummary = {
  organizationId: "1266395e-ec88-481e-9a72-81fa2cc2904a",
  organizationName: "La Esperanza",
  role: "owner",
};

const organizationB: MembershipSummary = {
  organizationId: "64eb2297-6603-4fc9-b2b0-a582a7bf1992",
  organizationName: "La Esperanza",
  role: "owner",
};

afterEach(() => {
  cleanup();
  globalThis.history.replaceState({}, "", "/");
  vi.restoreAllMocks();
});

describe("resolveWorkspaceLocation", () => {
  it("keeps an owner without organizations in onboarding", () => {
    expect(resolveWorkspaceLocation([], "?org=126639&view=team")).toMatchObject(
      {
        kind: "onboarding",
      },
    );
  });

  it("selects the only active owner membership deterministically", () => {
    const resolution = resolveWorkspaceLocation([organizationA], "");

    expect(resolution).toMatchObject({
      kind: "active",
      membership: organizationA,
      view: "fields",
    });
  });

  it("requires an explicit organization when several memberships are active", () => {
    expect(
      resolveWorkspaceLocation([organizationA, organizationB], "?view=team"),
    ).toMatchObject({ kind: "selector" });
  });

  it("resolves the public short prefix without granting authority from the locator", () => {
    const resolution = resolveWorkspaceLocation(
      [organizationA, organizationB],
      "?org=126639&view=team",
    );

    expect(resolution).toMatchObject({
      kind: "active",
      membership: organizationA,
      view: "team",
    });
    expect(buildWorkspaceUrl("/", organizationA, "team")).toBe(
      "/?org=126639&view=team",
    );
  });

  it.each([
    ["legacy", "?org=126639&view=fields", { kind: "none" }],
    [
      "canonical",
      "?org=126639&view=fields&field=f0185b",
      { kind: "requested", prefix: "F0185B" },
    ],
    [
      "invalid",
      "?org=126639&view=fields&field=F0185",
      { kind: "invalid", reason: "format" },
    ],
    [
      "wrong view",
      "?org=126639&view=team&field=F0185B",
      { kind: "invalid", reason: "view" },
    ],
  ] as const)(
    "resolves a %s field locator without treating it as authority",
    (_, search, field) => {
      expect(resolveWorkspaceLocation([organizationA], search)).toMatchObject({
        kind: "active",
        field,
      });
    },
  );

  it("builds legacy and field URLs with short canonical locators only", () => {
    expect(buildWorkspaceUrl("/", organizationA, "fields")).toBe(
      "/?org=126639&view=fields",
    );
    expect(
      buildWorkspaceUrl("/", organizationA, "fields", {
        kind: "requested",
        prefix: "F0185B",
      }),
    ).toBe("/?org=126639&view=fields&field=F0185B");
    expect(
      buildWorkspaceUrl("/", organizationA, "account", {
        kind: "requested",
        prefix: "F0185B",
      }),
    ).toBe("/?org=126639&view=account");
  });

  it.each([
    ["missing", "?org=FFFFFF&view=fields"],
    ["invalid", "?org=12663&view=fields"],
    ["full UUID", `?org=${organizationA.organizationId}&view=fields`],
  ])("fails closed for a %s organization locator", (_, search) => {
    expect(
      resolveWorkspaceLocation([organizationA, organizationB], search),
    ).toMatchObject({ kind: "selector" });
  });

  it("fails closed when a short prefix collides", () => {
    const collision: MembershipSummary = {
      ...organizationB,
      organizationId: "126639aa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
    };

    expect(
      resolveWorkspaceLocation(
        [organizationA, collision],
        "?org=126639&view=fields",
      ),
    ).toMatchObject({ kind: "selector" });
  });

  it("fails closed after the selected membership is removed from the session", () => {
    expect(
      resolveWorkspaceLocation([organizationB], "?org=126639&view=account"),
    ).toMatchObject({ kind: "selector" });
  });
});

function renderShell({
  memberships = [organizationA, organizationB],
  guard = "clear",
  onActiveOrganizationChange = vi.fn(),
  onDiscardDraft = vi.fn(),
}: Readonly<{
  memberships?: readonly MembershipSummary[];
  guard?: "clear" | "dirty" | "pending" | "context-pending";
  onActiveOrganizationChange?: (organizationId: string | null) => void;
  onDiscardDraft?: () => void;
}> = {}) {
  return {
    onActiveOrganizationChange,
    onDiscardDraft,
    ...render(
      <OwnerWorkspaceShell
        guard={guard}
        memberships={memberships}
        onActiveOrganizationChange={onActiveOrganizationChange}
        onDiscardDraft={onDiscardDraft}
        onboarding={<p>Alta inicial</p>}
      >
        {(workspace) => (
          <article>
            <p>Tenant visible: {workspace.membership.organizationName}</p>
            <p>Vista visible: {workspace.view}</p>
          </article>
        )}
      </OwnerWorkspaceShell>,
    ),
  };
}

function shellWithGuard(
  guard: "clear" | "dirty" | "pending" | "context-pending",
  onActiveOrganizationChange = vi.fn(),
  onDiscardDraft = vi.fn(),
) {
  return (
    <OwnerWorkspaceShell
      guard={guard}
      memberships={[organizationA, organizationB]}
      onActiveOrganizationChange={onActiveOrganizationChange}
      onDiscardDraft={onDiscardDraft}
      onboarding={<p>Alta inicial</p>}
    >
      {(workspace) => (
        <article>
          <p>Vista visible: {workspace.view}</p>
        </article>
      )}
    </OwnerWorkspaceShell>
  );
}

function FieldIntentControls({
  workspace,
}: Readonly<{ workspace: ActiveWorkspace }>) {
  const onFieldIntent = useWorkspaceFieldNavigation();
  const send = (intent: WorkspaceFieldIntent) => () => onFieldIntent(intent);
  return (
    <article>
      <p>
        Campo solicitado: {workspace.field?.kind ?? "none"}
        {workspace.field?.kind === "requested"
          ? ` ${workspace.field.prefix}`
          : ""}
      </p>
      <button
        onClick={send({ kind: "select", prefix: "F0185B" })}
        type="button"
      >
        Abrir campo uno
      </button>
      <button
        onClick={send({ kind: "select", prefix: "A1B2C3" })}
        type="button"
      >
        Abrir campo dos
      </button>
      <button onClick={send({ kind: "clear" })} type="button">
        Cerrar campo
      </button>
      <button
        onClick={send({
          kind: "reject",
          prefix: "F0185B",
          reason: "ambiguous",
        })}
        type="button"
      >
        Rechazar campo
      </button>
    </article>
  );
}

function fieldIntentShell(
  guard: "clear" | "dirty" | "pending" | "context-pending" = "clear",
  onDiscardDraft = vi.fn(),
) {
  return (
    <OwnerWorkspaceShell
      guard={guard}
      memberships={[organizationA]}
      onActiveOrganizationChange={vi.fn()}
      onDiscardDraft={onDiscardDraft}
      onboarding={<p>Alta inicial</p>}
    >
      {(workspace) => <FieldIntentControls workspace={workspace} />}
    </OwnerWorkspaceShell>
  );
}

describe("OwnerWorkspaceShell", () => {
  it("renders onboarding for zero owner memberships and never opens tenant content", async () => {
    const activeOrganization = vi.fn();
    renderShell({
      memberships: [],
      onActiveOrganizationChange: activeOrganization,
    });

    expect(await screen.findByText("Alta inicial")).toBeVisible();
    expect(screen.queryByText(/Tenant visible:/)).not.toBeInTheDocument();
    await waitFor(() =>
      expect(activeOrganization).toHaveBeenLastCalledWith(null),
    );
  });

  it("does not render a tenant or full UUID until one of N organizations is chosen", async () => {
    const activeOrganization = vi.fn();
    renderShell({ onActiveOrganizationChange: activeOrganization });

    expect(
      await screen.findByRole("heading", { name: "Elegir organización" }),
    ).toBeVisible();
    expect(screen.queryByText(/Tenant visible:/)).not.toBeInTheDocument();
    expect(document.body).not.toHaveTextContent(organizationA.organizationId);
    expect(document.body).not.toHaveTextContent(organizationB.organizationId);
    expect(document.body).toHaveTextContent("Organización 126639");
    expect(document.body).toHaveTextContent("Organización 64EB22");
    expect(globalThis.location.search).toBe("");
    await waitFor(() =>
      expect(activeOrganization).toHaveBeenLastCalledWith(null),
    );
  });

  it("selects one organization, scopes rendered content and exposes accessible navigation", async () => {
    const { onActiveOrganizationChange } = renderShell();

    fireEvent.click(
      await screen.findByRole("button", {
        name: "Abrir La Esperanza, organización 126639",
      }),
    );

    const heading = await screen.findByRole("heading", {
      name: "Espacio de La Esperanza",
    });
    expect(heading).toHaveFocus();
    expect(screen.getByText("Tenant visible: La Esperanza")).toBeVisible();
    expect(screen.getByText("Vista visible: fields")).toBeVisible();
    expect(globalThis.location.search).toBe("?org=126639&view=fields");
    expect(document.body).not.toHaveTextContent(organizationA.organizationId);
    expect(screen.getByRole("button", { name: "Campos" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    expect(
      screen.getByRole("navigation", {
        name: "Navegación del espacio de trabajo",
      }),
    ).toBeVisible();
    await waitFor(() =>
      expect(onActiveOrganizationChange).toHaveBeenLastCalledWith(
        organizationA.organizationId,
      ),
    );
  });

  it("canonicalizes field URLs and keeps invalid locators out of tenant requests", async () => {
    globalThis.history.replaceState(
      {},
      "",
      "/?org=126639&view=fields&field=f0185b",
    );
    render(fieldIntentShell());

    expect(
      await screen.findByText("Campo solicitado: requested F0185B"),
    ).toBeVisible();
    expect(globalThis.location.search).toBe(
      "?org=126639&view=fields&field=F0185B",
    );
    expect(document.body).not.toHaveTextContent(organizationA.organizationId);

    cleanup();
    globalThis.history.replaceState(
      {},
      "",
      `/?org=126639&view=fields&field=${organizationA.organizationId}`,
    );
    render(fieldIntentShell());

    expect(await screen.findByText("Campo solicitado: invalid")).toBeVisible();
    expect(globalThis.location.search).toBe("?org=126639&view=fields");
    expect(
      screen.getByText(/c.digo corto del campo no es v.lido/i),
    ).toBeVisible();
  });

  it("opens, closes and restores field locators through real history", async () => {
    globalThis.history.replaceState({}, "", "/?org=126639&view=fields");
    render(fieldIntentShell());
    await screen.findByText("Campo solicitado: none");

    fireEvent.click(screen.getByRole("button", { name: "Abrir campo uno" }));
    expect(
      await screen.findByText("Campo solicitado: requested F0185B"),
    ).toBeVisible();
    fireEvent.click(screen.getByRole("button", { name: "Abrir campo dos" }));
    expect(
      await screen.findByText("Campo solicitado: requested A1B2C3"),
    ).toBeVisible();

    act(() => globalThis.history.back());
    expect(
      await screen.findByText("Campo solicitado: requested F0185B"),
    ).toBeVisible();
    act(() => globalThis.history.back());
    expect(await screen.findByText("Campo solicitado: none")).toBeVisible();
    act(() => globalThis.history.forward());
    expect(
      await screen.findByText("Campo solicitado: requested F0185B"),
    ).toBeVisible();

    fireEvent.click(screen.getByRole("button", { name: "Cerrar campo" }));
    expect(await screen.findByText("Campo solicitado: none")).toBeVisible();
    expect(globalThis.location.search).toBe("?org=126639&view=fields");
  });

  it("replaces a rejected collision with a neutral announcement", async () => {
    globalThis.history.replaceState(
      {},
      "",
      "/?org=126639&view=fields&field=F0185B",
    );
    render(fieldIntentShell());
    await screen.findByText("Campo solicitado: requested F0185B");

    fireEvent.click(screen.getByRole("button", { name: "Rechazar campo" }));

    expect(await screen.findByText("Campo solicitado: none")).toBeVisible();
    expect(globalThis.location.search).toBe("?org=126639&view=fields");
    expect(screen.getByText(/coincide con m.s de un campo/i)).toBeVisible();
  });

  it("guards field changes while dirty or pending without losing the current locator", async () => {
    globalThis.history.replaceState(
      {},
      "",
      "/?org=126639&view=fields&field=F0185B",
    );
    const confirmation = vi
      .spyOn(globalThis, "confirm")
      .mockReturnValueOnce(false)
      .mockReturnValueOnce(true);
    const onDiscardDraft = vi.fn();
    const { rerender } = render(fieldIntentShell("dirty", onDiscardDraft));
    await screen.findByText("Campo solicitado: requested F0185B");

    fireEvent.click(screen.getByRole("button", { name: "Abrir campo dos" }));
    expect(confirmation).toHaveBeenCalledOnce();
    expect(globalThis.location.search).toContain("field=F0185B");
    expect(onDiscardDraft).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole("button", { name: "Abrir campo dos" }));
    expect(
      await screen.findByText("Campo solicitado: requested A1B2C3"),
    ).toBeVisible();
    expect(onDiscardDraft).toHaveBeenCalledOnce();

    rerender(fieldIntentShell("pending", onDiscardDraft));
    fireEvent.click(screen.getByRole("button", { name: "Abrir campo uno" }));
    expect(screen.getByText(/Hay una operaci.n pendiente/i)).toBeVisible();
    expect(globalThis.location.search).toContain("field=A1B2C3");
  });

  it("restores view and organization from back/forward navigation", async () => {
    globalThis.history.replaceState({}, "", "/?org=126639&view=fields");
    const { onActiveOrganizationChange } = renderShell();
    expect(await screen.findByText("Vista visible: fields")).toBeVisible();

    fireEvent.click(screen.getByRole("button", { name: "Equipo" }));
    expect(await screen.findByText("Vista visible: team")).toBeVisible();
    expect(globalThis.location.search).toBe("?org=126639&view=team");

    act(() => {
      globalThis.history.pushState({}, "", "/?org=64EB22&view=account");
      globalThis.dispatchEvent(new PopStateEvent("popstate"));
    });

    expect(await screen.findByText("Vista visible: account")).toBeVisible();
    expect(screen.getByRole("button", { name: "Cuenta" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    await waitFor(() =>
      expect(onActiveOrganizationChange).toHaveBeenLastCalledWith(
        organizationB.organizationId,
      ),
    );
  });

  it.each([
    ["unknown", [organizationA, organizationB], "?org=FFFFFF&view=team"],
    [
      "ambiguous",
      [
        organizationA,
        organizationB,
        {
          ...organizationA,
          organizationId: "126639aa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
        },
      ],
      "?org=126639&view=team",
    ],
  ] as const)(
    "clears tenant content for an %s locator reached through popstate",
    async (_, memberships, nextSearch) => {
      globalThis.history.replaceState({}, "", "/?org=64EB22&view=fields");
      const { onActiveOrganizationChange } = renderShell({ memberships });
      expect(await screen.findByText("Vista visible: fields")).toBeVisible();

      act(() => {
        globalThis.history.pushState({}, "", `/${nextSearch}`);
        globalThis.dispatchEvent(new PopStateEvent("popstate"));
      });

      expect(
        await screen.findByRole("heading", { name: "Elegir organización" }),
      ).toBeVisible();
      expect(screen.queryByText(/Tenant visible:/)).not.toBeInTheDocument();
      expect(globalThis.location.search).toBe("");
      await waitFor(() =>
        expect(onActiveOrganizationChange).toHaveBeenLastCalledWith(null),
      );
    },
  );

  it("requires confirmation before discarding a draft during an organization change", async () => {
    globalThis.history.replaceState({}, "", "/?org=126639&view=fields");
    const confirmation = vi
      .spyOn(globalThis, "confirm")
      .mockReturnValueOnce(false)
      .mockReturnValueOnce(true);
    const { onDiscardDraft } = renderShell({ guard: "dirty" });
    await screen.findByRole("heading", { name: "Espacio de La Esperanza" });

    fireEvent.click(
      screen.getByRole("button", { name: "Cambiar organización" }),
    );
    expect(confirmation).toHaveBeenCalledOnce();
    expect(
      screen.getByRole("heading", { name: "Espacio de La Esperanza" }),
    ).toBeVisible();
    expect(onDiscardDraft).not.toHaveBeenCalled();

    fireEvent.click(
      screen.getByRole("button", { name: "Cambiar organización" }),
    );
    expect(await screen.findByText("Elegir organización")).toBeVisible();
    expect(onDiscardDraft).toHaveBeenCalledOnce();
  });

  it("blocks context and view changes while an operation is pending", async () => {
    globalThis.history.replaceState({}, "", "/?org=126639&view=fields");
    renderShell({ guard: "pending" });
    await screen.findByRole("heading", { name: "Espacio de La Esperanza" });

    fireEvent.click(screen.getByRole("button", { name: "Equipo" }));
    fireEvent.click(
      screen.getByRole("button", { name: "Cambiar organización" }),
    );

    expect(screen.getByText("Vista visible: fields")).toBeVisible();
    expect(globalThis.location.search).toBe("?org=126639&view=fields");
    expect(
      screen.getByText(/Hay una operación pendiente/i),
    ).toBeInTheDocument();
  });

  it("keeps a contextual pending action in its tenant while allowing an in-tenant view change", async () => {
    globalThis.history.replaceState({}, "", "/?org=126639&view=team");
    renderShell({ guard: "context-pending" });
    await screen.findByText("Vista visible: team");

    fireEvent.click(screen.getByRole("button", { name: "Campos" }));
    expect(await screen.findByText("Vista visible: fields")).toBeVisible();
    fireEvent.click(screen.getByRole("button", { name: /Cambiar organi/i }));

    expect(screen.getByText("Vista visible: fields")).toBeVisible();
    expect(globalThis.location.search).toBe("?org=126639&view=fields");
    expect(screen.getByText(/Hay una operaci/i)).toBeInTheDocument();
  });

  it("rejects a real Back while dirty without destroying Back or Forward", async () => {
    globalThis.history.replaceState({}, "", "/?org=126639&view=fields");
    const confirmation = vi.spyOn(globalThis, "confirm").mockReturnValue(false);
    const onActiveOrganizationChange = vi.fn();
    const onDiscardDraft = vi.fn();
    const { rerender } = render(
      shellWithGuard("clear", onActiveOrganizationChange, onDiscardDraft),
    );
    await screen.findByText("Vista visible: fields");
    fireEvent.click(screen.getByRole("button", { name: "Equipo" }));
    expect(await screen.findByText("Vista visible: team")).toBeVisible();

    rerender(
      shellWithGuard("dirty", onActiveOrganizationChange, onDiscardDraft),
    );
    act(() => globalThis.history.back());
    await waitFor(() => expect(confirmation).toHaveBeenCalledOnce());
    await waitFor(() =>
      expect(globalThis.location.search).toBe("?org=126639&view=team"),
    );
    expect(screen.getByText("Vista visible: team")).toBeVisible();
    expect(onDiscardDraft).not.toHaveBeenCalled();

    rerender(
      shellWithGuard("clear", onActiveOrganizationChange, onDiscardDraft),
    );
    act(() => globalThis.history.back());
    expect(await screen.findByText("Vista visible: fields")).toBeVisible();
    act(() => globalThis.history.forward());
    expect(await screen.findByText("Vista visible: team")).toBeVisible();
  });

  it("rejects a real Back while pending and restores navigation after it clears", async () => {
    globalThis.history.replaceState({}, "", "/?org=126639&view=fields");
    const onActiveOrganizationChange = vi.fn();
    const { rerender } = render(
      shellWithGuard("clear", onActiveOrganizationChange),
    );
    await screen.findByText("Vista visible: fields");
    fireEvent.click(screen.getByRole("button", { name: "Equipo" }));
    expect(await screen.findByText("Vista visible: team")).toBeVisible();

    rerender(shellWithGuard("pending", onActiveOrganizationChange));
    act(() => globalThis.history.back());
    expect(await screen.findByText(/Hay una operaci/i)).toBeInTheDocument();
    await waitFor(() =>
      expect(globalThis.location.search).toBe("?org=126639&view=team"),
    );

    rerender(shellWithGuard("clear", onActiveOrganizationChange));
    await waitFor(() =>
      expect(screen.queryByText(/Hay una operaci/i)).not.toBeInTheDocument(),
    );
    act(() => globalThis.history.back());
    expect(await screen.findByText("Vista visible: fields")).toBeVisible();
    act(() => globalThis.history.forward());
    expect(await screen.findByText("Vista visible: team")).toBeVisible();
    expect(
      screen.getByText("Contexto activo: La Esperanza. Vista Equipo."),
    ).toBeInTheDocument();
  });

  it("fails closed when the active membership disappears", async () => {
    globalThis.history.replaceState({}, "", "/?org=126639&view=fields");
    const props = {
      guard: "clear" as const,
      onActiveOrganizationChange: vi.fn(),
      onboarding: <p>Alta inicial</p>,
      children: (workspace: {
        membership: MembershipSummary;
        view: string;
      }) => (
        <p>{`${workspace.membership.organizationName}:${workspace.view}`}</p>
      ),
    };
    const { rerender } = render(
      <OwnerWorkspaceShell
        {...props}
        memberships={[organizationA, organizationB]}
      />,
    );
    expect(await screen.findByText("La Esperanza:fields")).toBeVisible();

    rerender(<OwnerWorkspaceShell {...props} memberships={[organizationB]} />);

    expect(
      await screen.findByRole("heading", { name: "Elegir organización" }),
    ).toBeVisible();
    expect(screen.queryByText("La Esperanza:fields")).not.toBeInTheDocument();
    await waitFor(() =>
      expect(props.onActiveOrganizationChange).toHaveBeenLastCalledWith(null),
    );
  });
});
