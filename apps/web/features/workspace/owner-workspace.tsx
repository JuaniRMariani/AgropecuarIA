"use client";

import type { ReactNode } from "react";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useEffectEvent,
  useMemo,
  useRef,
  useState,
} from "react";

import { formatShortId } from "../../lib/format-id";
import type { MembershipSummary } from "../identity/identity-types";

export const WORKSPACE_VIEWS = [
  "fields",
  "catalog",
  "team",
  "territory",
  "account",
] as const;

export type WorkspaceView = (typeof WORKSPACE_VIEWS)[number];
export type WorkspaceGuard = "clear" | "dirty" | "pending" | "context-pending";

export type WorkspaceFieldLocator =
  | Readonly<{ kind: "none" }>
  | Readonly<{ kind: "requested"; prefix: string }>
  | Readonly<{ kind: "invalid"; reason: "format" | "view" }>;

export type WorkspaceFieldIntent =
  | Readonly<{ kind: "select"; prefix: string }>
  | Readonly<{ kind: "clear" }>
  | Readonly<{
      kind: "reject";
      prefix: string;
      reason: "unknown" | "ambiguous" | "unavailable" | "signed-out";
    }>
  | Readonly<{ kind: "restore"; prefix: string }>;

type OwnerMembership = MembershipSummary;

export type WorkspaceLocation =
  | Readonly<{ kind: "onboarding" }>
  | Readonly<{
      kind: "selector";
      reason: "choose" | "unknown" | "ambiguous";
      memberships: readonly OwnerMembership[];
    }>
  | Readonly<{
      kind: "active";
      membership: OwnerMembership;
      view: WorkspaceView;
      field?: WorkspaceFieldLocator;
      source: "url" | "automatic";
    }>;

export type ActiveWorkspace = Extract<WorkspaceLocation, { kind: "active" }>;

export type OwnerWorkspaceShellProps = Readonly<{
  memberships: readonly MembershipSummary[];
  guard: WorkspaceGuard;
  onActiveOrganizationChange: (organizationId: string | null) => void;
  onDiscardDraft?: () => void;
  onboarding: ReactNode;
  children: (workspace: ActiveWorkspace) => ReactNode;
}>;

const SHORT_ID_PATTERN = /^[0-9A-F]{6}$/;
const WORKSPACE_HISTORY_STATE_KEY = "agropecuariaOwnerWorkspaceV1";
const NO_WORKSPACE_FIELD: WorkspaceFieldLocator = { kind: "none" };
const rejectDetachedFieldIntent = (): boolean => false;
const WorkspaceFieldNavigationContext = createContext<
  ((intent: WorkspaceFieldIntent) => boolean) | null
>(null);

export function useWorkspaceFieldNavigation(): (
  intent: WorkspaceFieldIntent,
) => boolean {
  return (
    useContext(WorkspaceFieldNavigationContext) ?? rejectDetachedFieldIntent
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function historyPosition(value: unknown): number | null {
  if (!isRecord(value)) return null;
  const workspaceState = value[WORKSPACE_HISTORY_STATE_KEY];
  if (!isRecord(workspaceState)) return null;
  const position = workspaceState.position;
  return typeof position === "number" &&
    Number.isSafeInteger(position) &&
    position >= 0
    ? position
    : null;
}

function workspaceHistoryState(position: number): Record<string, unknown> {
  const currentState: Record<string, unknown> = isRecord(
    globalThis.history.state,
  )
    ? { ...globalThis.history.state }
    : {};
  return {
    ...currentState,
    [WORKSPACE_HISTORY_STATE_KEY]: { position },
  };
}

function isWorkspaceView(value: string | null): value is WorkspaceView {
  return WORKSPACE_VIEWS.some((view) => view === value);
}

function resolveFieldLocator(
  params: URLSearchParams,
  view: WorkspaceView,
): WorkspaceFieldLocator {
  const requestedPrefix = params.get("field");
  if (requestedPrefix === null) return { kind: "none" };
  if (view !== "fields") return { kind: "invalid", reason: "view" };
  const normalized = requestedPrefix.toUpperCase();
  return SHORT_ID_PATTERN.test(normalized)
    ? { kind: "requested", prefix: normalized }
    : { kind: "invalid", reason: "format" };
}

function fieldLocatorKey(locator: WorkspaceFieldLocator): string {
  switch (locator.kind) {
    case "none":
      return "none";
    case "requested":
      return `requested:${locator.prefix}`;
    case "invalid":
      return `invalid:${locator.reason}`;
  }
}

function invalidFieldAnnouncement(locator: WorkspaceFieldLocator): string {
  if (locator.kind !== "invalid") return "";
  return locator.reason === "view"
    ? "El enlace de campo sólo se puede abrir desde la vista Campos. Conservamos el contexto vigente."
    : "El código corto del campo no es válido. Mostramos la lista vigente sin consultar una ficha.";
}

function ownerMemberships(
  memberships: readonly MembershipSummary[],
): readonly OwnerMembership[] {
  return memberships.filter(
    (membership) => membership.role.toLowerCase() === "owner",
  );
}

export function resolveWorkspaceLocation(
  memberships: readonly MembershipSummary[],
  search: string,
): WorkspaceLocation {
  const owners = ownerMemberships(memberships);
  if (owners.length === 0) return { kind: "onboarding" };

  const params = new URLSearchParams(search);
  const requestedPrefix = params.get("org")?.toUpperCase() ?? null;
  const requestedView = params.get("view");
  const view = isWorkspaceView(requestedView) ? requestedView : "fields";
  const field = resolveFieldLocator(params, view);

  if (requestedPrefix === null && owners.length === 1) {
    const membership = owners[0];
    return membership === undefined
      ? { kind: "onboarding" }
      : { kind: "active", membership, view, field, source: "automatic" };
  }

  if (requestedPrefix === null || !SHORT_ID_PATTERN.test(requestedPrefix)) {
    return {
      kind: "selector",
      reason: requestedPrefix === null ? "choose" : "unknown",
      memberships: owners,
    };
  }

  const matches = owners.filter(
    (membership) =>
      formatShortId(membership.organizationId) === requestedPrefix,
  );
  if (matches.length !== 1) {
    return {
      kind: "selector",
      reason: matches.length === 0 ? "unknown" : "ambiguous",
      memberships: owners,
    };
  }

  const membership = matches[0];
  return membership === undefined
    ? { kind: "selector", reason: "unknown", memberships: owners }
    : { kind: "active", membership, view, field, source: "url" };
}

export function buildWorkspaceUrl(
  pathname: string,
  membership: MembershipSummary,
  view: WorkspaceView,
  field: WorkspaceFieldLocator = { kind: "none" },
): string {
  const params = new URLSearchParams({
    org: formatShortId(membership.organizationId),
    view,
  });
  if (view === "fields" && field.kind === "requested") {
    params.set("field", field.prefix);
  }
  return `${pathname}?${params.toString()}`;
}

function viewLabel(view: WorkspaceView): string {
  switch (view) {
    case "fields":
      return "Campos";
    case "catalog":
      return "Catálogo";
    case "team":
      return "Equipo";
    case "territory":
      return "Territorio";
    case "account":
      return "Cuenta";
  }
}

function selectorMessage(
  reason: Extract<WorkspaceLocation, { kind: "selector" }>["reason"],
): string {
  if (reason === "ambiguous") {
    return "Ese código corto coincide con más de una organización. Elegí una de la lista para continuar.";
  }
  if (reason === "unknown") {
    return "Esa organización ya no está disponible en tu sesión. Elegí un contexto vigente.";
  }
  return "Elegí una organización antes de consultar sus datos.";
}

export function OwnerWorkspaceShell({
  memberships,
  guard,
  onActiveOrganizationChange,
  onDiscardDraft,
  onboarding,
  children,
}: OwnerWorkspaceShellProps) {
  const [location, setLocation] = useState<WorkspaceLocation | null>(null);
  const [announcement, setAnnouncement] = useState("");
  const headingRef = useRef<HTMLHeadingElement>(null);
  const previousOrganizationId = useRef<string | null>(null);
  const historyPositionRef = useRef<number | null>(null);
  const suppressedPopPositionRef = useRef<number | null>(null);
  const previousGuardRef = useRef(guard);

  const resolveCurrentLocation = useCallback(
    () => resolveWorkspaceLocation(memberships, globalThis.location.search),
    [memberships],
  );

  const commitLocation = useCallback(
    (
      next: WorkspaceLocation,
      mode: "push" | "replace" | "none",
      nextAnnouncement?: string,
    ) => {
      if (mode !== "none") {
        const url =
          next.kind === "active"
            ? buildWorkspaceUrl(
                globalThis.location.pathname,
                next.membership,
                next.view,
                next.field ?? NO_WORKSPACE_FIELD,
              )
            : globalThis.location.pathname;
        const currentPosition =
          historyPositionRef.current ??
          historyPosition(globalThis.history.state) ??
          0;
        const nextPosition =
          mode === "push" ? currentPosition + 1 : currentPosition;
        const state = workspaceHistoryState(nextPosition);
        if (mode === "push") {
          globalThis.history.pushState(state, "", url);
        } else {
          globalThis.history.replaceState(state, "", url);
        }
        historyPositionRef.current = nextPosition;
      }
      if (nextAnnouncement !== undefined) {
        setAnnouncement(nextAnnouncement);
      }
      setLocation(next);
    },
    [],
  );

  const canLeaveCurrentContext = useCallback(
    (change: "context" | "view" | "field"): boolean => {
      if (
        guard === "pending" ||
        (guard === "context-pending" && change === "context")
      ) {
        setAnnouncement(
          "Hay una operación pendiente. Esperá su confirmación antes de cambiar de contexto.",
        );
        return false;
      }
      if (
        guard === "dirty" &&
        !globalThis.confirm(
          "Hay cambios sin enviar. ¿Querés descartarlos y cambiar de contexto?",
        )
      ) {
        setAnnouncement("Conservamos el borrador en la organización actual.");
        return false;
      }
      if (guard === "dirty") onDiscardDraft?.();
      return true;
    },
    [guard, onDiscardDraft],
  );

  useEffect(() => {
    let cancelled = false;
    queueMicrotask(() => {
      if (cancelled) return;
      const next = resolveCurrentLocation();
      commitLocation(
        next,
        next.kind === "active" ||
          (next.kind === "selector" && next.reason !== "choose")
          ? "replace"
          : "none",
        next.kind === "active"
          ? invalidFieldAnnouncement(next.field ?? NO_WORKSPACE_FIELD) ||
              undefined
          : undefined,
      );
    });
    return () => {
      cancelled = true;
    };
  }, [commitLocation, resolveCurrentLocation]);

  useEffect(() => {
    const previousGuard = previousGuardRef.current;
    previousGuardRef.current = guard;
    if (previousGuard === "clear" || guard !== "clear") return;
    let cancelled = false;
    queueMicrotask(() => {
      if (!cancelled) setAnnouncement("");
    });
    return () => {
      cancelled = true;
    };
  }, [guard]);

  const handlePopState = useEffectEvent((event: PopStateEvent) => {
    const targetPosition = historyPosition(event.state);
    const suppressedPosition = suppressedPopPositionRef.current;
    if (suppressedPosition !== null && targetPosition === suppressedPosition) {
      suppressedPopPositionRef.current = null;
      historyPositionRef.current = suppressedPosition;
      return;
    }
    suppressedPopPositionRef.current = null;
    const next = resolveCurrentLocation();
    const currentOrganizationId =
      location?.kind === "active" ? location.membership.organizationId : null;
    const nextOrganizationId =
      next.kind === "active" ? next.membership.organizationId : null;
    const contextChanges = currentOrganizationId !== nextOrganizationId;
    const viewChanges =
      location?.kind === "active" &&
      next.kind === "active" &&
      location.view !== next.view;
    const fieldChanges =
      location?.kind === "active" &&
      next.kind === "active" &&
      fieldLocatorKey(location.field ?? NO_WORKSPACE_FIELD) !==
        fieldLocatorKey(next.field ?? NO_WORKSPACE_FIELD);
    const change = contextChanges ? "context" : viewChanges ? "view" : "field";
    if (
      (contextChanges || viewChanges || fieldChanges) &&
      !canLeaveCurrentContext(change)
    ) {
      const currentPosition = historyPositionRef.current;
      if (
        currentPosition !== null &&
        targetPosition !== null &&
        currentPosition !== targetPosition
      ) {
        suppressedPopPositionRef.current = currentPosition;
        globalThis.history.go(currentPosition - targetPosition);
      } else if (currentPosition !== null && targetPosition === null) {
        suppressedPopPositionRef.current = currentPosition;
        globalThis.history.go(1);
      }
      return;
    }
    if (targetPosition !== null) {
      historyPositionRef.current = targetPosition;
    }
    commitLocation(
      next,
      next.kind === "active" ||
        (next.kind === "selector" && next.reason !== "choose")
        ? "replace"
        : "none",
      next.kind === "active"
        ? invalidFieldAnnouncement(next.field ?? NO_WORKSPACE_FIELD)
        : "",
    );
  });

  useEffect(() => {
    // Keep the subscription installed during router/parent updates. Removing a
    // listener while another popstate listener commits a render can lose Back.
    const onPopState = (event: PopStateEvent) => handlePopState(event);
    globalThis.addEventListener("popstate", onPopState);
    return () => globalThis.removeEventListener("popstate", onPopState);
  }, []);

  const activeOrganizationId =
    location?.kind === "active" ? location.membership.organizationId : null;

  useEffect(() => {
    onActiveOrganizationChange(activeOrganizationId);
    if (activeOrganizationId === null) {
      previousOrganizationId.current = null;
      return;
    }
    if (previousOrganizationId.current !== activeOrganizationId) {
      previousOrganizationId.current = activeOrganizationId;
      headingRef.current?.focus();
    }
  }, [activeOrganizationId, onActiveOrganizationChange]);

  const activeOwners = useMemo(
    () => ownerMemberships(memberships),
    [memberships],
  );

  const handleFieldIntent = useCallback(
    (intent: WorkspaceFieldIntent): boolean => {
      if (location?.kind !== "active") return false;
      const currentField = location.field ?? NO_WORKSPACE_FIELD;
      switch (intent.kind) {
        case "select": {
          const prefix = intent.prefix.toUpperCase();
          if (!SHORT_ID_PATTERN.test(prefix)) return false;
          if (
            location.view === "fields" &&
            currentField.kind === "requested" &&
            currentField.prefix === prefix
          ) {
            return true;
          }
          if (!canLeaveCurrentContext("field")) return false;
          commitLocation(
            {
              ...location,
              view: "fields",
              field: { kind: "requested", prefix },
              source: "url",
            },
            "push",
            "",
          );
          return true;
        }
        case "clear":
          if (currentField.kind === "none") return true;
          if (!canLeaveCurrentContext("field")) return false;
          commitLocation(
            { ...location, field: { kind: "none" }, source: "url" },
            "push",
            "",
          );
          return true;
        case "reject":
          if (
            currentField.kind !== "requested" ||
            currentField.prefix !== intent.prefix
          ) {
            return false;
          }
          commitLocation(
            { ...location, field: { kind: "none" }, source: "url" },
            "replace",
            intent.reason === "ambiguous"
              ? "Ese código corto coincide con más de un campo. Mostramos la lista sin abrir una ficha."
              : intent.reason === "signed-out"
                ? "La sesión ya no está disponible. Cerramos la ficha solicitada."
                : intent.reason === "unavailable"
                  ? "Ese campo ya no está disponible. Mostramos la lista vigente."
                  : "Ese campo no está disponible en la organización activa. Mostramos la lista vigente.",
          );
          return true;
        case "restore": {
          const prefix = intent.prefix.toUpperCase();
          if (!SHORT_ID_PATTERN.test(prefix)) return false;
          commitLocation(
            {
              ...location,
              view: "fields",
              field: { kind: "requested", prefix },
              source: "url",
            },
            "replace",
            "",
          );
          return true;
        }
      }
    },
    [canLeaveCurrentContext, commitLocation, location],
  );

  if (location === null) {
    return (
      <section
        className="owner-workspace owner-workspace--loading"
        role="status"
      >
        Preparando tu espacio de trabajo…
      </section>
    );
  }

  if (location.kind === "onboarding") {
    return (
      <section
        className="owner-workspace"
        aria-labelledby="workspace-onboarding-title"
      >
        <header className="owner-workspace__heading">
          <div>
            <p className="section-kicker">Primer paso</p>
            <h2 id="workspace-onboarding-title">Creá tu organización</h2>
          </div>
        </header>
        {onboarding}
      </section>
    );
  }

  const chooseMembership = (membership: MembershipSummary) => {
    if (!canLeaveCurrentContext("context")) return;
    commitLocation(
      {
        kind: "active",
        membership,
        view: "fields",
        field: { kind: "none" },
        source: "url",
      },
      "push",
      "",
    );
  };

  if (location.kind === "selector") {
    return (
      <section
        className="owner-workspace owner-workspace--selector"
        aria-labelledby="organization-selector-title"
      >
        <header className="owner-workspace__heading">
          <div>
            <p className="section-kicker">Contexto de trabajo</p>
            <h2 id="organization-selector-title">Elegir organización</h2>
            <p>{selectorMessage(location.reason)}</p>
          </div>
          <span
            className="count-chip"
            aria-label={`${activeOwners.length} organizaciones`}
          >
            {activeOwners.length}
          </span>
        </header>
        <ul className="owner-workspace__organizations">
          {location.memberships.map((membership) => (
            <li key={membership.organizationId}>
              <button
                aria-label={`Abrir ${membership.organizationName}, organización ${formatShortId(membership.organizationId)}`}
                onClick={() => chooseMembership(membership)}
                type="button"
              >
                <span>{membership.organizationName}</span>
                <small>
                  Organización {formatShortId(membership.organizationId)}
                </small>
              </button>
            </li>
          ))}
        </ul>
        <p className="owner-workspace__privacy">
          No consultamos datos de ninguna organización hasta que elijas una.
        </p>
      </section>
    );
  }

  const navigate = (view: WorkspaceView) => {
    if (view === location.view || !canLeaveCurrentContext("view")) return;
    commitLocation(
      { ...location, view, field: { kind: "none" }, source: "url" },
      "push",
      "",
    );
  };

  return (
    <section
      className="owner-workspace"
      aria-labelledby="active-workspace-title"
    >
      <a className="skip-link" href="#workspace-content">
        Saltar al contenido del espacio
      </a>
      <header className="owner-workspace__heading owner-workspace__heading--active">
        <div>
          <p className="section-kicker">Organización activa</p>
          <h2 id="active-workspace-title" ref={headingRef} tabIndex={-1}>
            Espacio de {location.membership.organizationName}
          </h2>
          <p>
            Organización {formatShortId(location.membership.organizationId)} ·{" "}
            {viewLabel(location.view)}
          </p>
        </div>
        {activeOwners.length > 1 ? (
          <button
            className="button button--quiet"
            onClick={() => {
              if (!canLeaveCurrentContext("context")) return;
              commitLocation(
                {
                  kind: "selector",
                  reason: "choose",
                  memberships: activeOwners,
                },
                "push",
                "",
              );
            }}
            type="button"
          >
            Cambiar organización
          </button>
        ) : null}
      </header>

      <nav
        aria-label="Navegación del espacio de trabajo"
        className="owner-workspace__nav"
      >
        {WORKSPACE_VIEWS.map((view) => (
          <button
            aria-current={location.view === view ? "page" : undefined}
            key={view}
            onClick={() => navigate(view)}
            type="button"
          >
            {viewLabel(view)}
          </button>
        ))}
      </nav>

      <p aria-live="polite" className="sr-only">
        {announcement ||
          `Contexto activo: ${location.membership.organizationName}. Vista ${viewLabel(location.view)}.`}
      </p>
      <div
        className="owner-workspace__content"
        id="workspace-content"
        tabIndex={-1}
      >
        <WorkspaceFieldNavigationContext.Provider value={handleFieldIntent}>
          {children(location)}
        </WorkspaceFieldNavigationContext.Provider>
      </div>
    </section>
  );
}
