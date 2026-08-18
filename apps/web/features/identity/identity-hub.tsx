"use client";

import { useCallback, useEffect, useRef, useState } from "react";

import {
  acceptOwnerInvitation,
  completeDevelopmentStepUp,
  completeDevelopmentLink,
  createOwnerInvitation,
  createOrganization,
  developmentSignIn,
  identityLoginUrl,
  IdentityApiError,
  loadCapabilities,
  listOwnSessions,
  listOrganizationOwnerMemberships,
  listOwnerInvitations,
  loadSession,
  resolveAuthorizationUrl,
  removeOrganizationOwnerMembership,
  revokeAllOwnSessionsAndLogout,
  revokeAllOtherOwnSessions,
  revokeSession,
  revokeOwnSession,
  revokeOwnerInvitation,
  startLink,
  startStepUp,
  unlinkIdentity,
} from "./identity-api";
import type {
  IdentityAction,
  IdentityCapabilities,
  IdentityConnection,
  IdentityNotice,
  IdentityResourceState,
  IdentitySession,
  OrganizationCreationState,
  OrganizationOwnerMembershipSummary,
  OwnerMembershipActionState,
  OwnerMembershipResourceState,
  OwnSessionActionState,
  OwnSessionRevocationIntent,
  OwnSessionResourceState,
  OwnSessionSummary,
  OwnerInvitationAcceptanceState,
  OwnerInvitationActionState,
  OwnerInvitationResourceState,
  OwnerInvitationSummary,
} from "./identity-types";
import { NO_IDENTITY_NOTICE } from "./identity-types";
import { IdentityView } from "./identity-view";

const ORGANIZATION_REAUTH_STATE_KEY =
  "agropecuaria.identity.organization-reauth.v1";
const ORGANIZATION_ATTEMPT_KEY_PATTERN = /^[0-9a-f]{32}$/;
const OWNER_INVITATION_TOKEN_KEY =
  "agropecuaria.identity.owner-invitation-token.v1";
const OWNER_INVITATION_ACTION_KEY =
  "agropecuaria.identity.owner-invitation-action.v1";
const OWNER_REMOVAL_ACTION_KEY =
  "agropecuaria.identity.owner-removal-action.v1";
const OWN_SESSION_REVOCATION_KEY =
  "agropecuaria.identity.session-revocation.v1";
const OWNER_INVITATION_TOKEN_PATTERN = /^[A-Za-z0-9_-]{43}$/;
const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

type StoredOwnerInvitationAction =
  | Readonly<{
      kind: "create";
      organizationId: string;
      idempotencyKey: string;
    }>
  | Readonly<{
      kind: "revoke";
      organizationId: string;
      invitationId: string;
      version: string;
    }>;

type StoredOrganizationAttempt = Readonly<{
  draft: string;
  idempotencyKey: string;
}>;

type StoredOwnerRemovalAction = Readonly<{
  organizationId: string;
  membershipId: string;
  displayName: string;
  version: string;
  idempotencyKey: string;
}>;

type StoredOwnSessionRevocation = OwnSessionRevocationIntent;

export function createOrganizationIdempotencyKey(): string {
  const bytes = globalThis.crypto.getRandomValues(new Uint8Array(16));
  return Array.from(bytes, (byte) => byte.toString(16).padStart(2, "0")).join(
    "",
  );
}

function isStoredOrganizationAttempt(
  value: unknown,
): value is StoredOrganizationAttempt {
  return (
    typeof value === "object" &&
    value !== null &&
    "draft" in value &&
    typeof value.draft === "string" &&
    value.draft.length <= 320 &&
    normalizedOrganizationName(value.draft) !== null &&
    "idempotencyKey" in value &&
    typeof value.idempotencyKey === "string" &&
    ORGANIZATION_ATTEMPT_KEY_PATTERN.test(value.idempotencyKey)
  );
}

function takeStoredOrganizationAttempt(): StoredOrganizationAttempt | null {
  try {
    const serialized = globalThis.sessionStorage.getItem(
      ORGANIZATION_REAUTH_STATE_KEY,
    );
    globalThis.sessionStorage.removeItem(ORGANIZATION_REAUTH_STATE_KEY);
    if (serialized === null) {
      return null;
    }
    const value: unknown = JSON.parse(serialized);
    return isStoredOrganizationAttempt(value) ? value : null;
  } catch {
    return null;
  }
}

function storeOrganizationAttemptForReauthentication(
  attempt: StoredOrganizationAttempt,
): void {
  try {
    globalThis.sessionStorage.setItem(
      ORGANIZATION_REAUTH_STATE_KEY,
      JSON.stringify(attempt),
    );
  } catch {
    // Reauthentication remains available when browser storage is disabled.
  }
}

function clearStoredOrganizationAttempt(): void {
  try {
    globalThis.sessionStorage.removeItem(ORGANIZATION_REAUTH_STATE_KEY);
  } catch {
    // Browser storage is optional; the in-memory attempt remains authoritative.
  }
}

function isStoredOwnerInvitationAction(
  value: unknown,
): value is StoredOwnerInvitationAction {
  if (
    typeof value !== "object" ||
    value === null ||
    !("kind" in value) ||
    !("organizationId" in value) ||
    typeof value.organizationId !== "string" ||
    !UUID_PATTERN.test(value.organizationId)
  ) {
    return false;
  }
  if (value.kind === "create") {
    return (
      "idempotencyKey" in value &&
      typeof value.idempotencyKey === "string" &&
      ORGANIZATION_ATTEMPT_KEY_PATTERN.test(value.idempotencyKey)
    );
  }
  return (
    value.kind === "revoke" &&
    "invitationId" in value &&
    typeof value.invitationId === "string" &&
    UUID_PATTERN.test(value.invitationId) &&
    "version" in value &&
    typeof value.version === "string" &&
    UUID_PATTERN.test(value.version)
  );
}

function storeOwnerInvitationAction(action: StoredOwnerInvitationAction): void {
  try {
    globalThis.sessionStorage.setItem(
      OWNER_INVITATION_ACTION_KEY,
      JSON.stringify(action),
    );
  } catch {
    // The action remains in memory when browser storage is unavailable.
  }
}

function takeOwnerInvitationAction(): StoredOwnerInvitationAction | null {
  try {
    const serialized = globalThis.sessionStorage.getItem(
      OWNER_INVITATION_ACTION_KEY,
    );
    globalThis.sessionStorage.removeItem(OWNER_INVITATION_ACTION_KEY);
    if (serialized === null) {
      return null;
    }
    const value: unknown = JSON.parse(serialized);
    return isStoredOwnerInvitationAction(value) ? value : null;
  } catch {
    return null;
  }
}

function isStoredOwnerRemovalAction(
  value: unknown,
): value is StoredOwnerRemovalAction {
  return (
    typeof value === "object" &&
    value !== null &&
    "organizationId" in value &&
    typeof value.organizationId === "string" &&
    UUID_PATTERN.test(value.organizationId) &&
    "membershipId" in value &&
    typeof value.membershipId === "string" &&
    UUID_PATTERN.test(value.membershipId) &&
    "displayName" in value &&
    typeof value.displayName === "string" &&
    value.displayName.trim().length > 0 &&
    value.displayName.length <= 160 &&
    "version" in value &&
    typeof value.version === "string" &&
    UUID_PATTERN.test(value.version) &&
    "idempotencyKey" in value &&
    typeof value.idempotencyKey === "string" &&
    ORGANIZATION_ATTEMPT_KEY_PATTERN.test(value.idempotencyKey)
  );
}

function storeOwnerRemovalAction(action: StoredOwnerRemovalAction): void {
  try {
    globalThis.sessionStorage.setItem(
      OWNER_REMOVAL_ACTION_KEY,
      JSON.stringify(action),
    );
  } catch {
    // The action remains in memory when browser storage is unavailable.
  }
}

function takeOwnerRemovalAction(): StoredOwnerRemovalAction | null {
  try {
    const serialized = globalThis.sessionStorage.getItem(
      OWNER_REMOVAL_ACTION_KEY,
    );
    globalThis.sessionStorage.removeItem(OWNER_REMOVAL_ACTION_KEY);
    if (serialized === null) {
      return null;
    }
    const value: unknown = JSON.parse(serialized);
    return isStoredOwnerRemovalAction(value) ? value : null;
  } catch {
    return null;
  }
}

function clearOwnerRemovalAction(): void {
  try {
    globalThis.sessionStorage.removeItem(OWNER_REMOVAL_ACTION_KEY);
  } catch {
    // Browser storage is optional; the in-memory action is authoritative.
  }
}

function isStoredOwnSessionSummary(value: unknown): value is OwnSessionSummary {
  return (
    typeof value === "object" &&
    value !== null &&
    "sessionId" in value &&
    typeof value.sessionId === "string" &&
    UUID_PATTERN.test(value.sessionId) &&
    "version" in value &&
    typeof value.version === "string" &&
    UUID_PATTERN.test(value.version) &&
    "authenticatedAtUtc" in value &&
    typeof value.authenticatedAtUtc === "string" &&
    !Number.isNaN(Date.parse(value.authenticatedAtUtc)) &&
    "expiresAtUtc" in value &&
    typeof value.expiresAtUtc === "string" &&
    !Number.isNaN(Date.parse(value.expiresAtUtc)) &&
    "isCurrent" in value &&
    value.isCurrent === false
  );
}

function parseStoredOwnSessionRevocation(
  value: unknown,
): StoredOwnSessionRevocation | null {
  if (isStoredOwnSessionSummary(value)) {
    return { kind: "single", session: value };
  }
  if (typeof value !== "object" || value === null || !("kind" in value)) {
    return null;
  }
  if (value.kind === "all-others") {
    return { kind: "all-others" };
  }
  if (value.kind === "all-sessions") {
    return { kind: "all-sessions" };
  }
  if (
    value.kind === "single" &&
    "session" in value &&
    isStoredOwnSessionSummary(value.session)
  ) {
    return { kind: "single", session: value.session };
  }
  return null;
}

function storeOwnSessionRevocation(action: StoredOwnSessionRevocation): void {
  try {
    globalThis.sessionStorage.setItem(
      OWN_SESSION_REVOCATION_KEY,
      JSON.stringify(action),
    );
  } catch {
    // The action remains in memory when browser storage is unavailable.
  }
}

function takeOwnSessionRevocation(): StoredOwnSessionRevocation | null {
  try {
    const serialized = globalThis.sessionStorage.getItem(
      OWN_SESSION_REVOCATION_KEY,
    );
    globalThis.sessionStorage.removeItem(OWN_SESSION_REVOCATION_KEY);
    if (serialized === null) {
      return null;
    }
    const value: unknown = JSON.parse(serialized);
    return parseStoredOwnSessionRevocation(value);
  } catch {
    return null;
  }
}

function clearOwnSessionRevocation(): void {
  try {
    globalThis.sessionStorage.removeItem(OWN_SESSION_REVOCATION_KEY);
  } catch {
    // Browser storage is optional; the in-memory action remains authoritative.
  }
}

function clearOwnerInvitationToken(): void {
  try {
    globalThis.sessionStorage.removeItem(OWNER_INVITATION_TOKEN_KEY);
  } catch {
    // The token is also cleared from memory by the caller.
  }
}

function readOwnerInvitationToken(): string | null {
  try {
    const token = globalThis.sessionStorage.getItem(OWNER_INVITATION_TOKEN_KEY);
    return token !== null && OWNER_INVITATION_TOKEN_PATTERN.test(token)
      ? token
      : null;
  } catch {
    return null;
  }
}

export function captureOwnerInvitationTokenFromHash(): string | null {
  const hash = globalThis.location.hash;
  if (!hash.startsWith("#owner-invitation=")) {
    return null;
  }
  const cleanUrl = `${globalThis.location.pathname}${globalThis.location.search}`;
  globalThis.history.replaceState(globalThis.history.state, "", cleanUrl);
  let token: string;
  try {
    token = decodeURIComponent(hash.slice("#owner-invitation=".length));
  } catch {
    return null;
  }
  if (!OWNER_INVITATION_TOKEN_PATTERN.test(token)) {
    return null;
  }
  try {
    globalThis.sessionStorage.setItem(OWNER_INVITATION_TOKEN_KEY, token);
  } catch {
    // The caller keeps the token in memory for this page lifetime.
  }
  return token;
}

function isAbort(error: unknown): boolean {
  return error instanceof DOMException && error.name === "AbortError";
}

function actionNotice(error: unknown): IdentityNotice {
  if (error instanceof IdentityApiError) {
    if (error.kind === "conflict") {
      return {
        kind: "conflict",
        message:
          "No aplicamos el cambio porque la identidad ya está vinculada o cambió en paralelo.",
      };
    }
    if (error.kind === "replay") {
      return {
        kind: "replay",
        message:
          "Ese intento ya fue utilizado o venció. Iniciá una vinculación nueva.",
      };
    }
    if (error.kind === "provider-down") {
      return {
        kind: "provider-down",
        message:
          "El proveedor de acceso no responde. No confirmamos ningún cambio.",
      };
    }
  }
  return {
    kind: "error",
    message:
      "No pudimos completar la operación. Tu acceso anterior se mantiene sin cambios.",
  };
}

function developmentProviderAvailable(
  capabilities: IdentityCapabilities,
): boolean {
  return (
    capabilities.developmentProviderEnabled &&
    (capabilities.environment === "Development" ||
      capabilities.environment === "Test")
  );
}

function organizationFailureState(error: unknown): OrganizationCreationState {
  if (error instanceof IdentityApiError) {
    switch (error.kind) {
      case "validation":
        return {
          kind: "validation",
          message: "Revisá el nombre. Debe tener entre 2 y 160 caracteres.",
        };
      case "signed-out":
      case "revoked":
      case "reauthentication-required":
        return {
          kind: "reauthentication-required",
          message:
            "Volvé a verificar tu identidad. Conservamos el nombre para que puedas continuar.",
        };
      case "in-progress":
        return {
          kind: "in-progress",
          message:
            "La creación sigue en proceso. Consultá nuevamente con este mismo intento.",
        };
      case "conflict":
      case "replay":
        return {
          kind: "conflict",
          message:
            "Ese intento ya no coincide con el nombre. Iniciá uno nuevo sin perder lo escrito.",
        };
      case "reconciliation-required":
        return {
          kind: "reconciliation-required",
          message:
            "No podemos confirmar todavía el resultado. Consultá nuevamente; no crees otra organización.",
        };
      case "rate-limited":
        return {
          kind: "rate-limited",
          message:
            "Hay demasiados intentos. Esperá un momento y reintentá sin cambiar el nombre.",
        };
      case "provider-down":
      case "error":
        return {
          kind: "error",
          message:
            "No pudimos crear la organización. El intento se conserva para reintentar de forma segura.",
        };
    }
  }

  if (error instanceof TypeError && !window.navigator.onLine) {
    return {
      kind: "offline",
      message:
        "Estás sin conexión. No confirmamos cambios; reintentá con el mismo nombre cuando vuelva la red.",
    };
  }

  return {
    kind: "error",
    message:
      "No pudimos crear la organización. El intento se conserva para reintentar de forma segura.",
  };
}

function normalizedOrganizationName(value: string): string | null {
  const normalized = value.normalize("NFC").trim();
  return normalized.length >= 2 && normalized.length <= 160 ? normalized : null;
}

function hasOwnerManagementAssurance(session: IdentitySession): boolean {
  const { authentication } = session;
  return (
    authentication.level === "strong" &&
    authentication.purpose === "manage_organization_owners" &&
    authentication.expiresAtUtc !== null &&
    Date.parse(authentication.expiresAtUtc) > Date.now()
  );
}

function hasSessionManagementAssurance(session: IdentitySession): boolean {
  const { authentication } = session;
  return (
    authentication.level === "strong" &&
    authentication.purpose === "manage_sessions" &&
    authentication.expiresAtUtc !== null &&
    Date.parse(authentication.expiresAtUtc) > Date.now()
  );
}

function ownSessionInventoryContext(session: IdentitySession): string {
  const { authentication } = session;
  return [
    session.userId,
    authentication.level,
    authentication.authenticatedAtUtc,
    authentication.purpose ?? "",
    authentication.strongAuthenticatedAtUtc ?? "",
    authentication.expiresAtUtc ?? "",
  ].join("|");
}

function ownSessionResourceFailure(error: unknown): OwnSessionResourceState {
  if (error instanceof TypeError && !window.navigator.onLine) {
    return { kind: "offline" };
  }
  if (error instanceof IdentityApiError && error.status === 503) {
    return { kind: "unavailable" };
  }
  return { kind: "error" };
}

function ownSessionReauthenticationMessage(
  intent: OwnSessionRevocationIntent,
): string {
  switch (intent.kind) {
    case "single":
      return "Verificá tu identidad con MFA para cerrar esta sesión.";
    case "all-others":
      return "Verificá tu identidad con MFA para cerrar las otras sesiones.";
    case "all-sessions":
      return "Verificá tu identidad con MFA para cerrar todas las sesiones, incluida esta.";
  }
}

function ownSessionGenericFailureMessage(
  intent: OwnSessionRevocationIntent,
): string {
  switch (intent.kind) {
    case "single":
      return "No pudimos cerrar la sesión. No confirmamos ningún cambio.";
    case "all-others":
      return "No pudimos cerrar las otras sesiones. No confirmamos ningún cambio.";
    case "all-sessions":
      return "No pudimos confirmar el cierre de todas las sesiones.";
  }
}

function requiresOwnSessionReconciliation(
  error: unknown,
  intent: OwnSessionRevocationIntent,
): boolean {
  if (intent.kind !== "all-sessions") return false;
  return (
    error instanceof TypeError ||
    (error instanceof IdentityApiError && error.status === 503)
  );
}

function ownSessionActionFailure(
  error: unknown,
  intent: OwnSessionRevocationIntent,
): OwnSessionActionState {
  if (error instanceof IdentityApiError) {
    if (error.kind === "reauthentication-required") {
      return {
        kind: "reauthentication-required",
        intent,
        message: ownSessionReauthenticationMessage(intent),
      };
    }
    if (intent.kind === "single" && error.status === 412) {
      return {
        kind: "stale",
        message:
          "La sesión cambió en paralelo. Actualizamos la lista para que revises su estado.",
      };
    }
    if (intent.kind === "single" && error.status === 404) {
      return {
        kind: "unavailable",
        message:
          "La sesión ya no está disponible. Actualizamos la lista sin revelar más datos.",
      };
    }
    if (
      error.status === 409 &&
      error.code === "identity.current_session_requires_logout"
    ) {
      return {
        kind: "unavailable",
        message: "La sesión actual se cierra desde el control Cerrar sesión.",
      };
    }
    if (error.kind === "rate-limited") {
      return {
        kind: "rate-limited",
        message:
          "Alcanzaste el límite temporal de seguridad. Esperá un momento antes de reintentar.",
      };
    }
    if (error.status === 503) {
      return {
        kind: "service-unavailable",
        message:
          "La gestión de sesiones no está disponible temporalmente. No confirmamos ningún cambio.",
      };
    }
  }
  if (error instanceof TypeError && !window.navigator.onLine) {
    return {
      kind: "offline",
      message: "Estás sin conexión. No confirmamos ningún cambio.",
    };
  }
  return {
    kind: "error",
    message: ownSessionGenericFailureMessage(intent),
  };
}

function replaceInvitation(
  items: readonly OwnerInvitationSummary[],
  next: OwnerInvitationSummary,
): readonly OwnerInvitationSummary[] {
  const index = items.findIndex(
    (item) => item.invitationId === next.invitationId,
  );
  if (index < 0) {
    return [next, ...items];
  }
  return items.map((item, itemIndex) => (itemIndex === index ? next : item));
}

function invitationActionFailure(error: unknown): OwnerInvitationActionState {
  if (error instanceof IdentityApiError) {
    if (
      error.kind === "signed-out" ||
      error.kind === "revoked" ||
      error.kind === "reauthentication-required"
    ) {
      return {
        kind: "reauthentication-required",
        message: "Verificá tu identidad con MFA para administrar co-owners.",
      };
    }
    if (error.kind === "conflict" || error.kind === "replay") {
      return {
        kind: "conflict",
        message:
          "La invitación cambió en paralelo. Actualizamos la lista para que puedas revisar su estado.",
      };
    }
  }
  if (error instanceof TypeError && !window.navigator.onLine) {
    return {
      kind: "offline",
      message: "Estás sin conexión. No confirmamos ningún cambio.",
    };
  }
  return {
    kind: "error",
    message:
      "No pudimos administrar la invitación. Ningún permiso se confirmó.",
  };
}

function ownerMembershipActionFailure(
  error: unknown,
): OwnerMembershipActionState {
  if (error instanceof IdentityApiError) {
    if (
      error.kind === "signed-out" ||
      error.kind === "revoked" ||
      error.kind === "reauthentication-required"
    ) {
      return {
        kind: "reauthentication-required",
        message: "Verificá tu identidad con MFA para quitar un co-owner.",
      };
    }
    if (error.status === 404) {
      return {
        kind: "unavailable",
        message:
          "La membresía ya no está disponible. Actualizá la lista antes de continuar.",
      };
    }
    if (error.status === 409 && /last[_-]?owner/.test(error.code)) {
      return {
        kind: "last-owner",
        message:
          "No se puede quitar al último owner activo de la organización.",
      };
    }
    if (error.status === 412) {
      return {
        kind: "stale",
        message:
          "La membresía cambió en paralelo. Actualizamos la lista para que revises su estado.",
      };
    }
    if (error.status === 503) {
      return {
        kind: "service-unavailable",
        message:
          "El servicio no está disponible temporalmente. Conservamos el intento para reintentarlo.",
      };
    }
    if (error.status === 429) {
      return {
        kind: "rate-limited",
        message:
          "Hay demasiados intentos. Esperá un momento antes de reintentar.",
      };
    }
    if (error.status === 409 || error.kind === "conflict") {
      return {
        kind: "conflict",
        message:
          "No se pudo confirmar el cambio porque la membresía cambió en paralelo.",
      };
    }
  }
  if (error instanceof TypeError && !window.navigator.onLine) {
    return {
      kind: "offline",
      message: "Estás sin conexión. No confirmamos ningún cambio.",
    };
  }
  return {
    kind: "error",
    message: "No pudimos quitar al co-owner. Ningún permiso se confirmó.",
  };
}

function invitationAcceptanceFailure(
  error: unknown,
): OwnerInvitationAcceptanceState {
  if (error instanceof IdentityApiError) {
    if (
      error.kind === "signed-out" ||
      error.kind === "revoked" ||
      error.kind === "reauthentication-required"
    ) {
      return {
        kind: "reauthentication-required",
        message:
          "Ingresá o verificá nuevamente tu identidad para aceptar la invitación.",
      };
    }
    if (error.kind === "unavailable") {
      return {
        kind: "unavailable",
        message:
          "El enlace es inválido, venció, fue revocado o ya no está disponible.",
      };
    }
    if (error.kind === "conflict" || error.kind === "replay") {
      return {
        kind: "conflict",
        message:
          "La invitación cambió mientras la aceptabas. Pedile al owner que revise su estado.",
      };
    }
  }
  if (error instanceof TypeError && !window.navigator.onLine) {
    return {
      kind: "offline",
      message:
        "Estás sin conexión. Conservamos el enlace solo en este navegador para reintentar.",
    };
  }
  return {
    kind: "error",
    message: "No pudimos aceptar la invitación. No confirmamos ningún permiso.",
  };
}

async function fetchIdentityResource(
  signal?: AbortSignal,
): Promise<IdentityResourceState> {
  const [capabilitiesResult, sessionResult] = await Promise.allSettled([
    loadCapabilities(signal),
    loadSession(signal),
  ]);

  if (capabilitiesResult.status === "rejected") {
    if (isAbort(capabilitiesResult.reason)) {
      throw capabilitiesResult.reason;
    }
    const kind =
      capabilitiesResult.reason instanceof IdentityApiError &&
      capabilitiesResult.reason.kind === "provider-down"
        ? "provider-down"
        : "error";
    return { kind, capabilities: null };
  }

  const capabilities = capabilitiesResult.value;
  if (sessionResult.status === "fulfilled") {
    return {
      kind: "authenticated",
      capabilities,
      session: sessionResult.value,
    };
  }
  if (isAbort(sessionResult.reason)) {
    throw sessionResult.reason;
  }
  if (sessionResult.reason instanceof IdentityApiError) {
    if (sessionResult.reason.kind === "signed-out") {
      return { kind: "signed-out", capabilities };
    }
    if (sessionResult.reason.kind === "revoked") {
      return { kind: "revoked", capabilities };
    }
    if (sessionResult.reason.kind === "provider-down") {
      return { kind: "provider-down", capabilities };
    }
  }
  return { kind: "error", capabilities };
}

export function IdentityHub() {
  const [resource, setResource] = useState<IdentityResourceState>({
    kind: "loading",
  });
  const [notice, setNotice] = useState<IdentityNotice>(NO_IDENTITY_NOTICE);
  const [pendingAction, setPendingAction] = useState<IdentityAction | null>(
    null,
  );
  const [organizationDraft, setOrganizationDraft] = useState("");
  const [organizationFormOpen, setOrganizationFormOpen] = useState(false);
  const [organizationCreation, setOrganizationCreation] =
    useState<OrganizationCreationState>({ kind: "idle" });
  const [ownerInvitations, setOwnerInvitations] = useState<
    Readonly<Record<string, OwnerInvitationResourceState>>
  >({});
  const [ownerInvitationAction, setOwnerInvitationAction] =
    useState<OwnerInvitationActionState>({ kind: "idle" });
  const [ownerInvitationAcceptance, setOwnerInvitationAcceptance] =
    useState<OwnerInvitationAcceptanceState>({ kind: "idle" });
  const [ownerMemberships, setOwnerMemberships] = useState<
    Readonly<Record<string, OwnerMembershipResourceState>>
  >({});
  const [ownerMembershipAction, setOwnerMembershipAction] =
    useState<OwnerMembershipActionState>({ kind: "idle" });
  const [ownSessions, setOwnSessions] = useState<OwnSessionResourceState>({
    kind: "idle",
  });
  const [ownSessionAction, setOwnSessionAction] =
    useState<OwnSessionActionState>({ kind: "idle" });
  const [activeOrganizationId, setActiveOrganizationId] = useState<
    string | null
  >(null);
  const organizationAttemptKey = useRef<string | null>(null);
  const invitationToken = useRef<string | null>(null);
  const invitationAction = useRef<StoredOwnerInvitationAction | null>(null);
  const ownerRemovalAction = useRef<StoredOwnerRemovalAction | null>(null);
  const invitationCreateKeys = useRef<Record<string, string>>({});
  const ownerInvitationsAvailable = useRef(false);
  const ownerMembershipRequestVersions = useRef<Record<string, number>>({});
  const ownerInvitationRequestVersions = useRef<Record<string, number>>({});
  const ownSessionRequestVersion = useRef(0);
  const ownSessionRevocation = useRef<StoredOwnSessionRevocation | null>(null);
  const ownSessionContext = useRef<string | null>(null);
  const activeOrganizationIdRef = useRef<string | null>(null);
  const mounted = useRef(true);

  const handleActiveOrganizationChange = useCallback(
    (organizationId: string | null) => {
      const previousOrganizationId = activeOrganizationIdRef.current;
      if (previousOrganizationId === organizationId) return;
      if (previousOrganizationId !== null) {
        ownerMembershipRequestVersions.current[previousOrganizationId] =
          (ownerMembershipRequestVersions.current[previousOrganizationId] ??
            0) + 1;
        ownerInvitationRequestVersions.current[previousOrganizationId] =
          (ownerInvitationRequestVersions.current[previousOrganizationId] ??
            0) + 1;
      }
      activeOrganizationIdRef.current = organizationId;
      setActiveOrganizationId(organizationId);
      setOwnerMemberships({});
      setOwnerInvitations({});
      setOwnerMembershipAction({ kind: "idle" });
      setOwnerInvitationAction({ kind: "idle" });
    },
    [],
  );

  useEffect(() => {
    ownerInvitationsAvailable.current =
      resource.kind === "authenticated" &&
      resource.capabilities.ownerInvitationsAvailable;
  }, [resource]);

  const refresh = useCallback(async (signal?: AbortSignal) => {
    try {
      const nextResource = await fetchIdentityResource(signal);
      if (mounted.current) {
        setResource(nextResource);
      }
    } catch (error) {
      if (!isAbort(error) && mounted.current) {
        setResource({ kind: "error", capabilities: null });
      }
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    mounted.current = true;
    const invitationHashWasPresent =
      globalThis.location.hash.startsWith("#owner-invitation=");
    const capturedToken = captureOwnerInvitationTokenFromHash();
    invitationToken.current = capturedToken ?? readOwnerInvitationToken();
    if (invitationHashWasPresent && capturedToken === null) {
      queueMicrotask(() => {
        if (mounted.current) {
          setOwnerInvitationAcceptance({
            kind: "unavailable",
            message:
              "El enlace es inválido, venció, fue revocado o ya no está disponible.",
          });
        }
      });
    }
    void fetchIdentityResource(controller.signal)
      .then((nextResource) => {
        if (mounted.current) {
          if (nextResource.kind === "authenticated") {
            const storedAttempt = takeStoredOrganizationAttempt();
            if (storedAttempt !== null) {
              organizationAttemptKey.current = storedAttempt.idempotencyKey;
              setOrganizationDraft(storedAttempt.draft);
              setOrganizationFormOpen(true);
              setOrganizationCreation({ kind: "idle" });
            }
          }
          setResource(nextResource);
        }
      })
      .catch((error: unknown) => {
        if (!isAbort(error) && mounted.current) {
          setResource({ kind: "error", capabilities: null });
        }
      });
    return () => {
      controller.abort();
      mounted.current = false;
    };
  }, [refresh]);

  const handleLogin = useCallback((connection: IdentityConnection) => {
    window.location.assign(identityLoginUrl(connection));
  }, []);

  const handleLink = useCallback(
    async (connection: IdentityConnection) => {
      if (resource.kind !== "authenticated") {
        return;
      }
      setPendingAction(connection === "email" ? "link-email" : "link-google");
      setNotice(NO_IDENTITY_NOTICE);
      try {
        const attempt = await startLink(connection);
        if (developmentProviderAvailable(resource.capabilities)) {
          const session = await completeDevelopmentLink(attempt);
          if (mounted.current) {
            setResource({
              kind: "authenticated",
              capabilities: resource.capabilities,
              session,
            });
            setNotice({
              kind: "success",
              message: "La identidad quedó vinculada y verificada.",
            });
          }
          return;
        }

        const authorizationUrl = resolveAuthorizationUrl(
          attempt.authorizationUrl,
        );
        if (authorizationUrl === null) {
          throw new IdentityApiError("error", 502);
        }
        window.location.assign(authorizationUrl);
      } catch (error) {
        if (!isAbort(error) && mounted.current) {
          setNotice(actionNotice(error));
        }
      } finally {
        if (mounted.current) {
          setPendingAction(null);
        }
      }
    },
    [resource],
  );

  const handleUnlink = useCallback(
    async (identityId: string) => {
      if (resource.kind !== "authenticated") {
        return;
      }
      if (!window.confirm("¿Desvincular esta identidad verificada?")) {
        return;
      }
      setPendingAction("unlink");
      setNotice(NO_IDENTITY_NOTICE);
      try {
        await unlinkIdentity(identityId);
        const session = await loadSession();
        if (mounted.current) {
          setResource({
            kind: "authenticated",
            capabilities: resource.capabilities,
            session,
          });
          setNotice({
            kind: "success",
            message: "La identidad fue desvinculada.",
          });
        }
      } catch (error) {
        if (!isAbort(error) && mounted.current) {
          setNotice(actionNotice(error));
        }
      } finally {
        if (mounted.current) {
          setPendingAction(null);
        }
      }
    },
    [resource],
  );

  const handleStepUp = useCallback(async () => {
    if (resource.kind !== "authenticated") {
      return;
    }
    setPendingAction("step-up");
    setNotice(NO_IDENTITY_NOTICE);
    try {
      const attempt = await startStepUp("manage_authentication_methods");
      if (developmentProviderAvailable(resource.capabilities)) {
        const session = await completeDevelopmentStepUp(attempt);
        if (mounted.current) {
          setResource({
            kind: "authenticated",
            capabilities: resource.capabilities,
            session,
          });
          setNotice({
            kind: "success",
            message:
              "Verificación reforzada confirmada para proteger la futura gestión de métodos de acceso.",
          });
        }
        return;
      }

      const authorizationUrl = resolveAuthorizationUrl(
        attempt.authorizationUrl,
      );
      if (authorizationUrl === null) {
        throw new IdentityApiError("error", 502);
      }
      window.location.assign(authorizationUrl);
    } catch (error) {
      if (!isAbort(error) && mounted.current) {
        setNotice(actionNotice(error));
      }
    } finally {
      if (mounted.current) {
        setPendingAction(null);
      }
    }
  }, [resource]);

  const handleRevoke = useCallback(async () => {
    if (!window.confirm("¿Cerrar y revocar esta sesión en este navegador?")) {
      return;
    }
    setPendingAction("revoke");
    setNotice(NO_IDENTITY_NOTICE);
    try {
      await revokeSession();
      if (mounted.current) {
        setResource({
          kind: "revoked",
          capabilities:
            resource.kind === "loading" ? null : resource.capabilities,
        });
      }
    } catch (error) {
      if (!isAbort(error) && mounted.current) {
        setNotice(actionNotice(error));
      }
    } finally {
      if (mounted.current) {
        setPendingAction(null);
      }
    }
  }, [resource]);

  const handleOwnSessionUnauthorized = useCallback(
    (error: unknown): boolean => {
      if (!(error instanceof IdentityApiError) || error.status !== 401) {
        return false;
      }
      ownSessionRequestVersion.current += 1;
      ownSessionRevocation.current = null;
      clearOwnSessionRevocation();
      setOwnSessions({ kind: "idle" });
      setOwnSessionAction({ kind: "idle" });
      setResource((current) => {
        const capabilities =
          current.kind === "loading" ? null : current.capabilities;
        if (error.kind === "revoked" || capabilities === null) {
          return { kind: "revoked", capabilities };
        }
        return { kind: "signed-out", capabilities };
      });
      return true;
    },
    [],
  );

  const refreshOwnSessions = useCallback(
    async (offset = 0, signal?: AbortSignal) => {
      const requestVersion = ownSessionRequestVersion.current + 1;
      ownSessionRequestVersion.current = requestVersion;
      setOwnSessions({ kind: "loading" });
      try {
        const page = await listOwnSessions(offset, 20, signal);
        if (
          mounted.current &&
          ownSessionRequestVersion.current === requestVersion
        ) {
          setOwnSessions({ kind: "ready", page });
        }
      } catch (error) {
        if (
          !isAbort(error) &&
          mounted.current &&
          ownSessionRequestVersion.current === requestVersion
        ) {
          if (!handleOwnSessionUnauthorized(error)) {
            setOwnSessions(ownSessionResourceFailure(error));
          }
        }
      }
    },
    [handleOwnSessionUnauthorized],
  );

  const executeOwnSessionRevocation = useCallback(
    async (intent: StoredOwnSessionRevocation) => {
      ownSessionRevocation.current = intent;
      if (intent.kind === "all-sessions") {
        storeOwnSessionRevocation(intent);
      }
      setOwnSessionAction({
        kind: "revoking",
        intent,
      });
      try {
        if (intent.kind === "single") {
          await revokeOwnSession(
            intent.session.sessionId,
            intent.session.version,
          );
        } else if (intent.kind === "all-others") {
          await revokeAllOtherOwnSessions();
        } else {
          await revokeAllOwnSessionsAndLogout();
        }
        if (!mounted.current) return;
        ownSessionRevocation.current = null;
        clearOwnSessionRevocation();
        if (intent.kind === "all-sessions") {
          ownSessionRequestVersion.current += 1;
          ownSessionContext.current = null;
          setOwnSessions({ kind: "idle" });
          setOwnSessionAction({ kind: "idle" });
          setResource((current) =>
            current.kind === "authenticated"
              ? { kind: "signed-out", capabilities: current.capabilities }
              : current,
          );
          return;
        }
        setOwnSessionAction({
          kind: "revoked",
          message:
            intent.kind === "all-others"
              ? "Las otras sesiones fueron cerradas. Esta sesión sigue abierta."
              : "La otra sesión fue cerrada.",
        });
        const currentOffset =
          intent.kind === "single" && ownSessions.kind === "ready"
            ? ownSessions.page.items.length === 1 && ownSessions.page.offset > 0
              ? Math.max(0, ownSessions.page.offset - ownSessions.page.limit)
              : ownSessions.page.offset
            : 0;
        await refreshOwnSessions(currentOffset);
      } catch (error) {
        if (isAbort(error) || !mounted.current) return;
        if (handleOwnSessionUnauthorized(error)) return;
        const failure = ownSessionActionFailure(error, intent);
        if (requiresOwnSessionReconciliation(error, intent)) {
          setOwnSessionAction({
            kind: "reconciling",
            intent,
            message:
              "Comprobando si este navegador sigue autenticado antes de ofrecer un reintento…",
          });
          try {
            await loadSession();
            if (!mounted.current) return;
            clearOwnSessionRevocation();
            setOwnSessionAction(failure);
          } catch (reconciliationError) {
            if (isAbort(reconciliationError) || !mounted.current) return;
            if (!handleOwnSessionUnauthorized(reconciliationError)) {
              clearOwnSessionRevocation();
              setOwnSessionAction(failure);
            }
          }
          return;
        }
        if (failure.kind === "reauthentication-required") {
          storeOwnSessionRevocation(intent);
        } else if (failure.kind === "stale" || failure.kind === "unavailable") {
          ownSessionRevocation.current = null;
          clearOwnSessionRevocation();
          void refreshOwnSessions(
            ownSessions.kind === "ready" ? ownSessions.page.offset : 0,
          );
        } else if (intent.kind === "all-sessions") {
          clearOwnSessionRevocation();
        }
        setOwnSessionAction(failure);
      }
    },
    [handleOwnSessionUnauthorized, ownSessions, refreshOwnSessions],
  );

  const requestOwnSessionStepUp = useCallback(
    async (intent: StoredOwnSessionRevocation) => {
      if (resource.kind !== "authenticated") return;
      ownSessionRevocation.current = intent;
      storeOwnSessionRevocation(intent);
      setOwnSessionAction({
        kind: "reauthentication-required",
        intent,
        message: ownSessionReauthenticationMessage(intent),
      });
      try {
        const attempt = await startStepUp("manage_sessions");
        if (developmentProviderAvailable(resource.capabilities)) {
          const nextSession = await completeDevelopmentStepUp(attempt);
          if (mounted.current) {
            setResource({
              kind: "authenticated",
              capabilities: resource.capabilities,
              session: nextSession,
            });
            takeOwnSessionRevocation();
            await executeOwnSessionRevocation(intent);
          }
          return;
        }
        const authorizationUrl = resolveAuthorizationUrl(
          attempt.authorizationUrl,
        );
        if (authorizationUrl === null) {
          throw new IdentityApiError("error", 502);
        }
        window.location.assign(authorizationUrl);
      } catch (error) {
        if (
          !isAbort(error) &&
          mounted.current &&
          !handleOwnSessionUnauthorized(error)
        ) {
          setOwnSessionAction(ownSessionActionFailure(error, intent));
        }
      }
    },
    [executeOwnSessionRevocation, handleOwnSessionUnauthorized, resource],
  );

  const handleBeginOwnSessionRevocation = useCallback(
    (session: OwnSessionSummary) => {
      if (!session.isCurrent) {
        setOwnSessionAction({
          kind: "confirming",
          intent: { kind: "single", session },
        });
      }
    },
    [],
  );

  const handleBeginAllOtherOwnSessionRevocation = useCallback(() => {
    setOwnSessionAction({
      kind: "confirming",
      intent: { kind: "all-others" },
    });
  }, []);

  const handleBeginAllOwnSessionRevocation = useCallback(() => {
    setOwnSessionAction({
      kind: "confirming",
      intent: { kind: "all-sessions" },
    });
  }, []);

  const handleConfirmOwnSessionRevocation = useCallback(() => {
    if (
      resource.kind !== "authenticated" ||
      ownSessionAction.kind !== "confirming"
    ) {
      return;
    }
    const { intent } = ownSessionAction;
    if (hasSessionManagementAssurance(resource.session)) {
      void executeOwnSessionRevocation(intent);
    } else {
      void requestOwnSessionStepUp(intent);
    }
  }, [
    executeOwnSessionRevocation,
    ownSessionAction,
    requestOwnSessionStepUp,
    resource,
  ]);

  const handleResumeOwnSessionRevocation = useCallback(() => {
    const intent = ownSessionRevocation.current;
    if (intent !== null) {
      void requestOwnSessionStepUp(intent);
    }
  }, [requestOwnSessionStepUp]);

  const handleSyntheticSignIn = useCallback(async () => {
    if (
      resource.kind !== "signed-out" ||
      !developmentProviderAvailable(resource.capabilities)
    ) {
      return;
    }
    setPendingAction("synthetic");
    setNotice(NO_IDENTITY_NOTICE);
    try {
      const session = await developmentSignIn();
      if (mounted.current) {
        setResource({
          kind: "authenticated",
          capabilities: resource.capabilities,
          session,
        });
        setNotice({
          kind: "success",
          message: "Ingresaste con una identidad sintética segura.",
        });
      }
    } catch (error) {
      if (!isAbort(error) && mounted.current) {
        if (
          error instanceof IdentityApiError &&
          error.kind === "provider-down"
        ) {
          setResource({
            kind: "provider-down",
            capabilities: resource.capabilities,
          });
        } else {
          setNotice(actionNotice(error));
        }
      }
    } finally {
      if (mounted.current) {
        setPendingAction(null);
      }
    }
  }, [resource]);

  const handleOrganizationDraftChange = useCallback(
    (value: string) => {
      if (
        organizationCreation.kind === "submitting" ||
        organizationCreation.kind === "in-progress" ||
        organizationCreation.kind === "reconciliation-required"
      ) {
        return;
      }
      clearStoredOrganizationAttempt();
      organizationAttemptKey.current = null;
      setOrganizationDraft(value);
      setOrganizationCreation({ kind: "idle" });
    },
    [organizationCreation.kind],
  );

  const handleCreateOrganization = useCallback(async () => {
    if (
      resource.kind !== "authenticated" ||
      organizationCreation.kind === "submitting"
    ) {
      return;
    }

    const displayName = normalizedOrganizationName(organizationDraft);
    if (displayName === null) {
      setOrganizationCreation({
        kind: "validation",
        message: "Ingresá un nombre de entre 2 y 160 caracteres.",
      });
      return;
    }

    const idempotencyKey =
      organizationAttemptKey.current ?? createOrganizationIdempotencyKey();
    organizationAttemptKey.current = idempotencyKey;
    setOrganizationCreation({ kind: "submitting" });

    try {
      const created = await createOrganization(displayName, idempotencyKey);
      if (!mounted.current) {
        return;
      }
      setResource((current) => {
        if (current.kind !== "authenticated") {
          return current;
        }
        const organizationId = created.organization.organizationId;
        const alreadyListed = current.session.memberships.some(
          (membership) => membership.organizationId === organizationId,
        );
        const memberships = alreadyListed
          ? current.session.memberships
          : [
              ...current.session.memberships,
              {
                organizationId,
                organizationName: created.organization.displayName,
                role: created.membership.role,
              },
            ];
        return {
          kind: "authenticated",
          capabilities: current.capabilities,
          session: { ...current.session, memberships },
        };
      });
      organizationAttemptKey.current = null;
      clearStoredOrganizationAttempt();
      setOrganizationDraft("");
      setOrganizationFormOpen(false);
      setOrganizationCreation({ kind: "idle" });
      setNotice({
        kind: "success",
        message: `${created.organization.displayName} ya está lista y sos owner.`,
      });
    } catch (error) {
      if (!isAbort(error) && mounted.current) {
        const failure = organizationFailureState(error);
        if (failure.kind === "conflict") {
          organizationAttemptKey.current = null;
        }
        setOrganizationCreation(failure);
      }
    }
  }, [organizationCreation.kind, organizationDraft, resource.kind]);

  const handleStartOrganization = useCallback(() => {
    clearStoredOrganizationAttempt();
    organizationAttemptKey.current = null;
    setOrganizationDraft("");
    setOrganizationCreation({ kind: "idle" });
    setOrganizationFormOpen(true);
  }, []);

  const handleCancelOrganization = useCallback(() => {
    if (
      organizationCreation.kind === "submitting" ||
      organizationCreation.kind === "in-progress" ||
      organizationCreation.kind === "reconciliation-required"
    ) {
      return;
    }
    clearStoredOrganizationAttempt();
    organizationAttemptKey.current = null;
    setOrganizationDraft("");
    setOrganizationCreation({ kind: "idle" });
    setOrganizationFormOpen(false);
  }, [organizationCreation.kind]);

  const handleReauthenticateOrganization = useCallback(() => {
    const idempotencyKey = organizationAttemptKey.current;
    if (idempotencyKey !== null) {
      storeOrganizationAttemptForReauthentication({
        draft: organizationDraft,
        idempotencyKey,
      });
    }
    handleLogin("email");
  }, [handleLogin, organizationDraft]);

  const refreshOwnerInvitations = useCallback(
    async (organizationId: string, signal?: AbortSignal) => {
      const requestVersion =
        (ownerInvitationRequestVersions.current[organizationId] ?? 0) + 1;
      ownerInvitationRequestVersions.current[organizationId] = requestVersion;
      await Promise.resolve();
      if (
        signal?.aborted === true ||
        activeOrganizationIdRef.current !== organizationId
      ) {
        return;
      }
      setOwnerInvitations((current) => ({
        ...current,
        [organizationId]: { kind: "loading" },
      }));
      try {
        const items = await listOwnerInvitations(organizationId, signal);
        if (
          mounted.current &&
          ownerInvitationRequestVersions.current[organizationId] ===
            requestVersion &&
          activeOrganizationIdRef.current === organizationId
        ) {
          setOwnerInvitations((current) => ({
            ...current,
            [organizationId]: { kind: "ready", items },
          }));
        }
      } catch (error) {
        if (
          !isAbort(error) &&
          mounted.current &&
          ownerInvitationRequestVersions.current[organizationId] ===
            requestVersion &&
          activeOrganizationIdRef.current === organizationId
        ) {
          const kind =
            error instanceof TypeError && !window.navigator.onLine
              ? "offline"
              : "error";
          setOwnerInvitations((current) => ({
            ...current,
            [organizationId]: { kind },
          }));
        }
      }
    },
    [],
  );

  const refreshOwnerMemberships = useCallback(
    async (organizationId: string, signal?: AbortSignal) => {
      const requestVersion =
        (ownerMembershipRequestVersions.current[organizationId] ?? 0) + 1;
      ownerMembershipRequestVersions.current[organizationId] = requestVersion;
      await Promise.resolve();
      if (
        signal?.aborted === true ||
        activeOrganizationIdRef.current !== organizationId
      ) {
        return;
      }
      setOwnerMemberships((current) => ({
        ...current,
        [organizationId]: { kind: "loading" },
      }));
      try {
        const items = await listOrganizationOwnerMemberships(
          organizationId,
          signal,
        );
        if (
          mounted.current &&
          ownerMembershipRequestVersions.current[organizationId] ===
            requestVersion &&
          activeOrganizationIdRef.current === organizationId
        ) {
          setOwnerMemberships((current) => ({
            ...current,
            [organizationId]: { kind: "ready", items },
          }));
        }
      } catch (error) {
        if (
          !isAbort(error) &&
          mounted.current &&
          ownerMembershipRequestVersions.current[organizationId] ===
            requestVersion &&
          activeOrganizationIdRef.current === organizationId
        ) {
          let kind: OwnerMembershipResourceState["kind"] = "error";
          if (error instanceof TypeError && !window.navigator.onLine) {
            kind = "offline";
          } else if (error instanceof IdentityApiError) {
            if (error.status === 404) {
              kind = "unavailable";
            } else if (error.status === 503) {
              kind = "service-unavailable";
            }
          }
          setOwnerMemberships((current) => ({
            ...current,
            [organizationId]: { kind },
          }));
        }
      }
    },
    [],
  );

  const executeOwnerRemoval = useCallback(
    async (action: StoredOwnerRemovalAction) => {
      ownerRemovalAction.current = action;
      storeOwnerRemovalAction(action);
      setOwnerMembershipAction({
        kind: "removing",
        organizationId: action.organizationId,
        membershipId: action.membershipId,
      });
      try {
        await removeOrganizationOwnerMembership(
          action.organizationId,
          action.membershipId,
          action.version,
          action.idempotencyKey,
        );
        if (!mounted.current) {
          return;
        }
        setOwnerMemberships((current) => {
          const previous = current[action.organizationId];
          if (previous?.kind !== "ready") {
            return current;
          }
          return {
            ...current,
            [action.organizationId]: {
              kind: "ready",
              items: previous.items.filter(
                (item) => item.membershipId !== action.membershipId,
              ),
            },
          };
        });
        ownerRemovalAction.current = null;
        clearOwnerRemovalAction();
        await refreshOwnerMemberships(action.organizationId);
        setOwnerMembershipAction({
          kind: "removed",
          organizationId: action.organizationId,
          membershipId: action.membershipId,
          message: `${action.displayName} ya no tiene acceso a la organización.`,
        });
        if (ownerInvitationsAvailable.current) {
          void refreshOwnerInvitations(action.organizationId);
        }
      } catch (error) {
        if (isAbort(error) || !mounted.current) {
          return;
        }
        const failure = ownerMembershipActionFailure(error);
        if (failure.kind === "reauthentication-required") {
          storeOwnerRemovalAction(action);
        }
        if (
          failure.kind === "last-owner" ||
          failure.kind === "stale" ||
          failure.kind === "unavailable" ||
          failure.kind === "conflict" ||
          failure.kind === "rate-limited"
        ) {
          ownerRemovalAction.current = null;
          clearOwnerRemovalAction();
          void refreshOwnerMemberships(action.organizationId);
        }
        setOwnerMembershipAction(failure);
      }
    },
    [refreshOwnerInvitations, refreshOwnerMemberships],
  );

  const requestOwnerRemovalStepUp = useCallback(
    async (action: StoredOwnerRemovalAction) => {
      if (resource.kind !== "authenticated") {
        return;
      }
      ownerRemovalAction.current = action;
      storeOwnerRemovalAction(action);
      setOwnerMembershipAction({
        kind: "reauthentication-required",
        message: "Verificá tu identidad con MFA para quitar un co-owner.",
      });
      try {
        const attempt = await startStepUp("manage_organization_owners");
        if (developmentProviderAvailable(resource.capabilities)) {
          const session = await completeDevelopmentStepUp(attempt);
          if (mounted.current) {
            setResource({
              kind: "authenticated",
              capabilities: resource.capabilities,
              session,
            });
            takeOwnerRemovalAction();
            await executeOwnerRemoval(action);
          }
          return;
        }
        const authorizationUrl = resolveAuthorizationUrl(
          attempt.authorizationUrl,
        );
        if (authorizationUrl === null) {
          throw new IdentityApiError("error", 502);
        }
        window.location.assign(authorizationUrl);
      } catch (error) {
        if (!isAbort(error) && mounted.current) {
          setOwnerMembershipAction(ownerMembershipActionFailure(error));
        }
      }
    },
    [executeOwnerRemoval, resource],
  );

  const handleBeginOwnerRemoval = useCallback(
    (membership: OrganizationOwnerMembershipSummary) => {
      if (membership.isCurrentUser) {
        return;
      }
      setOwnerMembershipAction({ kind: "confirming", membership });
    },
    [],
  );

  const handleConfirmOwnerRemoval = useCallback(() => {
    if (
      resource.kind !== "authenticated" ||
      ownerMembershipAction.kind !== "confirming"
    ) {
      return;
    }
    const { membership } = ownerMembershipAction;
    const action: StoredOwnerRemovalAction = {
      organizationId: membership.organizationId,
      membershipId: membership.membershipId,
      displayName: membership.displayName,
      version: membership.version,
      idempotencyKey: createOrganizationIdempotencyKey(),
    };
    if (hasOwnerManagementAssurance(resource.session)) {
      void executeOwnerRemoval(action);
    } else {
      void requestOwnerRemovalStepUp(action);
    }
  }, [
    executeOwnerRemoval,
    ownerMembershipAction,
    requestOwnerRemovalStepUp,
    resource,
  ]);

  const handleRetryOwnerRemoval = useCallback(() => {
    const action = ownerRemovalAction.current;
    if (action !== null) {
      if (
        resource.kind === "authenticated" &&
        hasOwnerManagementAssurance(resource.session)
      ) {
        void executeOwnerRemoval(action);
      } else {
        void requestOwnerRemovalStepUp(action);
      }
    }
  }, [executeOwnerRemoval, requestOwnerRemovalStepUp, resource]);

  const handleResumeOwnerRemoval = useCallback(() => {
    const action = ownerRemovalAction.current;
    if (action !== null) {
      void requestOwnerRemovalStepUp(action);
    }
  }, [requestOwnerRemovalStepUp]);

  const executeOwnerInvitationAction = useCallback(
    async (action: StoredOwnerInvitationAction) => {
      invitationAction.current = action;
      setOwnerInvitationAction(
        action.kind === "create"
          ? { kind: "creating", organizationId: action.organizationId }
          : {
              kind: "revoking",
              organizationId: action.organizationId,
              invitationId: action.invitationId,
            },
      );
      try {
        if (action.kind === "create") {
          const created = await createOwnerInvitation(
            action.organizationId,
            action.idempotencyKey,
          );
          invitationCreateKeys.current[action.organizationId] = "";
          setOwnerInvitations((current) => {
            const previous = current[action.organizationId];
            const items = previous?.kind === "ready" ? previous.items : [];
            return {
              ...current,
              [action.organizationId]: {
                kind: "ready",
                items: replaceInvitation(items, created.invitation),
              },
            };
          });
          if (created.token === null) {
            setOwnerInvitationAction({
              kind: "conflict",
              message:
                "La invitación ya existía y su enlace no puede volver a mostrarse. Revocala y creá otra si no lo guardaste.",
            });
          } else {
            setOwnerInvitationAction({
              kind: "revealed",
              organizationId: action.organizationId,
              invitationId: created.invitation.invitationId,
              shareLink: `${window.location.origin}/#owner-invitation=${encodeURIComponent(created.token)}`,
            });
          }
        } else {
          const revoked = await revokeOwnerInvitation(
            action.organizationId,
            action.invitationId,
            action.version,
          );
          setOwnerInvitations((current) => {
            const previous = current[action.organizationId];
            const items = previous?.kind === "ready" ? previous.items : [];
            return {
              ...current,
              [action.organizationId]: {
                kind: "ready",
                items: replaceInvitation(items, revoked),
              },
            };
          });
          setOwnerInvitationAction({ kind: "idle" });
          setNotice({
            kind: "success",
            message: "La invitación fue revocada.",
          });
        }
        invitationAction.current = null;
      } catch (error) {
        if (!isAbort(error) && mounted.current) {
          const failure = invitationActionFailure(error);
          if (failure.kind === "reauthentication-required") {
            storeOwnerInvitationAction(action);
          } else if (failure.kind === "conflict") {
            void refreshOwnerInvitations(action.organizationId);
          }
          setOwnerInvitationAction(failure);
        }
      }
    },
    [refreshOwnerInvitations],
  );

  const requestOwnerManagementStepUp = useCallback(
    async (action: StoredOwnerInvitationAction) => {
      if (resource.kind !== "authenticated") {
        return;
      }
      invitationAction.current = action;
      storeOwnerInvitationAction(action);
      setOwnerInvitationAction({
        kind: "reauthentication-required",
        message: "Verificá tu identidad con MFA para administrar co-owners.",
      });
      try {
        const attempt = await startStepUp("manage_organization_owners");
        if (developmentProviderAvailable(resource.capabilities)) {
          const session = await completeDevelopmentStepUp(attempt);
          if (mounted.current) {
            setResource({
              kind: "authenticated",
              capabilities: resource.capabilities,
              session,
            });
            takeOwnerInvitationAction();
            await executeOwnerInvitationAction(action);
          }
          return;
        }
        const authorizationUrl = resolveAuthorizationUrl(
          attempt.authorizationUrl,
        );
        if (authorizationUrl === null) {
          throw new IdentityApiError("error", 502);
        }
        window.location.assign(authorizationUrl);
      } catch (error) {
        if (!isAbort(error) && mounted.current) {
          setOwnerInvitationAction(invitationActionFailure(error));
        }
      }
    },
    [executeOwnerInvitationAction, resource],
  );

  const handleCreateOwnerInvitation = useCallback(
    (organizationId: string) => {
      if (resource.kind !== "authenticated") {
        return;
      }
      const existingKey = invitationCreateKeys.current[organizationId];
      const idempotencyKey =
        existingKey && ORGANIZATION_ATTEMPT_KEY_PATTERN.test(existingKey)
          ? existingKey
          : createOrganizationIdempotencyKey();
      invitationCreateKeys.current[organizationId] = idempotencyKey;
      const action: StoredOwnerInvitationAction = {
        kind: "create",
        organizationId,
        idempotencyKey,
      };
      if (hasOwnerManagementAssurance(resource.session)) {
        void executeOwnerInvitationAction(action);
      } else {
        void requestOwnerManagementStepUp(action);
      }
    },
    [executeOwnerInvitationAction, requestOwnerManagementStepUp, resource],
  );

  const handleRevokeOwnerInvitation = useCallback(
    (invitation: OwnerInvitationSummary) => {
      if (
        resource.kind !== "authenticated" ||
        !window.confirm("¿Revocar este enlace de invitación?")
      ) {
        return;
      }
      const action: StoredOwnerInvitationAction = {
        kind: "revoke",
        organizationId: invitation.organizationId,
        invitationId: invitation.invitationId,
        version: invitation.version,
      };
      if (hasOwnerManagementAssurance(resource.session)) {
        void executeOwnerInvitationAction(action);
      } else {
        void requestOwnerManagementStepUp(action);
      }
    },
    [executeOwnerInvitationAction, requestOwnerManagementStepUp, resource],
  );

  const handleCopyOwnerInvitation = useCallback(async () => {
    if (ownerInvitationAction.kind !== "revealed") {
      return;
    }
    try {
      await window.navigator.clipboard.writeText(
        ownerInvitationAction.shareLink,
      );
      setOwnerInvitationAction({ kind: "idle" });
      setNotice({
        kind: "success",
        message: "Enlace copiado. Por seguridad ya no volveremos a mostrarlo.",
      });
    } catch {
      setNotice({
        kind: "error",
        message:
          "No pudimos copiar el enlace. Seleccionalo y copialo manualmente antes de cerrar este aviso.",
      });
    }
  }, [ownerInvitationAction]);

  const handleResumeOwnerManagement = useCallback(() => {
    const action = invitationAction.current;
    if (action !== null) {
      void requestOwnerManagementStepUp(action);
    }
  }, [requestOwnerManagementStepUp]);

  const handleAcceptOwnerInvitation = useCallback(async () => {
    const token = invitationToken.current ?? readOwnerInvitationToken();
    if (token === null) {
      return;
    }
    if (resource.kind !== "authenticated") {
      setOwnerInvitationAcceptance({
        kind: "reauthentication-required",
        message: "Ingresá para aceptar la invitación como co-owner.",
      });
      return;
    }
    setOwnerInvitationAcceptance({ kind: "accepting" });
    try {
      const accepted = await acceptOwnerInvitation(token);
      if (!mounted.current) {
        return;
      }
      clearOwnerInvitationToken();
      invitationToken.current = null;
      setResource((current) => {
        if (current.kind !== "authenticated") {
          return current;
        }
        const membership = {
          organizationId: accepted.organization.organizationId,
          organizationName: accepted.organization.displayName,
          role: accepted.membership.role,
        };
        const memberships = current.session.memberships.some(
          (item) => item.organizationId === membership.organizationId,
        )
          ? current.session.memberships
          : [...current.session.memberships, membership];
        return {
          ...current,
          session: { ...current.session, memberships },
        };
      });
      setOwnerInvitationAcceptance({ kind: "idle" });
      setNotice({
        kind: "success",
        message: `Ya sos co-owner de ${accepted.organization.displayName}.`,
      });
    } catch (error) {
      if (!isAbort(error) && mounted.current) {
        const failure = invitationAcceptanceFailure(error);
        if (failure.kind === "unavailable" || failure.kind === "conflict") {
          clearOwnerInvitationToken();
          invitationToken.current = null;
        }
        setOwnerInvitationAcceptance(failure);
      }
    }
  }, [resource.kind]);

  const handleReauthenticateOwnerInvitation = useCallback(() => {
    handleLogin("email");
  }, [handleLogin]);

  useEffect(() => {
    if (resource.kind !== "authenticated") {
      if (resource.kind === "signed-out" || resource.kind === "revoked") {
        clearOwnSessionRevocation();
      }
      if (ownSessionContext.current !== null) {
        ownSessionRequestVersion.current += 1;
        ownSessionContext.current = null;
        ownSessionRevocation.current = null;
        setOwnSessions({ kind: "idle" });
        setOwnSessionAction({ kind: "idle" });
      }
      return;
    }

    const inventoryContext = ownSessionInventoryContext(resource.session);
    if (ownSessionContext.current !== inventoryContext) {
      ownSessionContext.current = inventoryContext;
      void refreshOwnSessions(0);
    }

    const storedRevocation = takeOwnSessionRevocation();
    if (storedRevocation === null) return;
    ownSessionRevocation.current = storedRevocation;
    queueMicrotask(() => {
      if (!mounted.current) return;
      if (hasSessionManagementAssurance(resource.session)) {
        void executeOwnSessionRevocation(storedRevocation);
      } else {
        storeOwnSessionRevocation(storedRevocation);
        setOwnSessionAction({
          kind: "reauthentication-required",
          intent: storedRevocation,
          message: ownSessionReauthenticationMessage(storedRevocation),
        });
      }
    });
  }, [executeOwnSessionRevocation, refreshOwnSessions, resource]);

  useEffect(() => {
    if (resource.kind !== "authenticated") {
      handleActiveOrganizationChange(null);
      if (invitationToken.current !== null && resource.kind === "signed-out") {
        setOwnerInvitationAcceptance({
          kind: "reauthentication-required",
          message: "Ingresá para aceptar la invitación como co-owner.",
        });
      }
      return;
    }
    if (
      !resource.capabilities.ownerInvitationsAvailable &&
      invitationToken.current !== null
    ) {
      setOwnerInvitationAcceptance({
        kind: "unavailable",
        message:
          "Las invitaciones de co-owner no están disponibles en este entorno.",
      });
    }
    const controller = new AbortController();
    queueMicrotask(() => {
      if (controller.signal.aborted) {
        return;
      }
      const activeMembership = resource.session.memberships.find(
        (membership) =>
          membership.organizationId === activeOrganizationId &&
          membership.role.toLowerCase() === "owner",
      );
      if (activeMembership !== undefined) {
        void refreshOwnerMemberships(
          activeMembership.organizationId,
          controller.signal,
        );
        if (resource.capabilities.ownerInvitationsAvailable) {
          void refreshOwnerInvitations(
            activeMembership.organizationId,
            controller.signal,
          );
        }
      }
      if (resource.capabilities.ownerInvitationsAvailable) {
        const storedAction = takeOwnerInvitationAction();
        if (storedAction !== null) {
          if (storedAction.organizationId === activeOrganizationId) {
            invitationAction.current = storedAction;
            void executeOwnerInvitationAction(storedAction);
          } else {
            storeOwnerInvitationAction(storedAction);
          }
        }
        if (invitationToken.current !== null) {
          void handleAcceptOwnerInvitation();
        }
      }
      const storedRemoval = takeOwnerRemovalAction();
      if (storedRemoval !== null) {
        if (storedRemoval.organizationId === activeOrganizationId) {
          ownerRemovalAction.current = storedRemoval;
          void executeOwnerRemoval(storedRemoval);
        } else {
          storeOwnerRemovalAction(storedRemoval);
        }
      }
    });
    return () => controller.abort();
  }, [
    executeOwnerInvitationAction,
    executeOwnerRemoval,
    handleAcceptOwnerInvitation,
    handleActiveOrganizationChange,
    refreshOwnerInvitations,
    refreshOwnerMemberships,
    activeOrganizationId,
    resource,
  ]);

  return (
    <IdentityView
      notice={notice}
      ownSessionAction={ownSessionAction}
      ownSessions={ownSessions}
      onBeginAllOwnSessionRevocation={handleBeginAllOwnSessionRevocation}
      onBeginAllOtherOwnSessionRevocation={
        handleBeginAllOtherOwnSessionRevocation
      }
      onBeginOwnSessionRevocation={handleBeginOwnSessionRevocation}
      onCancelOwnSessionRevocation={() => setOwnSessionAction({ kind: "idle" })}
      onConfirmOwnSessionRevocation={handleConfirmOwnSessionRevocation}
      onDismissOwnSessionNotice={() => setOwnSessionAction({ kind: "idle" })}
      onRefreshOwnSessions={() =>
        void refreshOwnSessions(
          ownSessions.kind === "ready" ? ownSessions.page.offset : 0,
        )
      }
      onNextOwnSessions={() => {
        if (ownSessions.kind === "ready") {
          void refreshOwnSessions(
            ownSessions.page.offset + ownSessions.page.limit,
          );
        }
      }}
      onPreviousOwnSessions={() => {
        if (ownSessions.kind === "ready") {
          void refreshOwnSessions(
            Math.max(0, ownSessions.page.offset - ownSessions.page.limit),
          );
        }
      }}
      onRetryOwnSessionRevocation={() => {
        const intent = ownSessionRevocation.current;
        if (intent === null || resource.kind !== "authenticated") return;
        if (hasSessionManagementAssurance(resource.session)) {
          void executeOwnSessionRevocation(intent);
        } else {
          void requestOwnSessionStepUp(intent);
        }
      }}
      onResumeOwnSessionRevocation={handleResumeOwnSessionRevocation}
      onActiveOrganizationChange={handleActiveOrganizationChange}
      onAcceptOwnerInvitation={() => void handleAcceptOwnerInvitation()}
      onCancelOrganization={handleCancelOrganization}
      onCreateOrganization={() => void handleCreateOrganization()}
      onLink={(connection) => void handleLink(connection)}
      onLogin={handleLogin}
      onStepUp={() => void handleStepUp()}
      onRetry={() => {
        setResource({ kind: "loading" });
        setNotice(NO_IDENTITY_NOTICE);
        void refresh();
      }}
      onRevoke={() => void handleRevoke()}
      onStartOrganization={handleStartOrganization}
      onSyntheticSignIn={() => void handleSyntheticSignIn()}
      onUnlink={(identityId) => void handleUnlink(identityId)}
      onOrganizationDraftChange={handleOrganizationDraftChange}
      onBeginOwnerRemoval={handleBeginOwnerRemoval}
      onCancelOwnerRemoval={() => setOwnerMembershipAction({ kind: "idle" })}
      onConfirmOwnerRemoval={handleConfirmOwnerRemoval}
      onCopyOwnerInvitation={() => void handleCopyOwnerInvitation()}
      onCreateOwnerInvitation={handleCreateOwnerInvitation}
      onReauthenticateOwnerInvitation={handleReauthenticateOwnerInvitation}
      onRefreshOwnerInvitations={(organizationId) =>
        void refreshOwnerInvitations(organizationId)
      }
      onRefreshOwnerMemberships={(organizationId) =>
        void refreshOwnerMemberships(organizationId)
      }
      onRetryOwnerRemoval={handleRetryOwnerRemoval}
      onResumeOwnerRemoval={handleResumeOwnerRemoval}
      onDismissOwnerRemoval={() => {
        ownerRemovalAction.current = null;
        clearOwnerRemovalAction();
        setOwnerMembershipAction({ kind: "idle" });
      }}
      onResumeOwnerManagement={handleResumeOwnerManagement}
      onRevokeOwnerInvitation={handleRevokeOwnerInvitation}
      onReauthenticateOrganization={handleReauthenticateOrganization}
      organizationCreation={organizationCreation}
      organizationDraft={organizationDraft}
      organizationFormOpen={organizationFormOpen}
      ownerInvitationAcceptance={ownerInvitationAcceptance}
      ownerInvitationAction={ownerInvitationAction}
      ownerInvitations={ownerInvitations}
      ownerMembershipAction={ownerMembershipAction}
      ownerMemberships={ownerMemberships}
      pendingAction={pendingAction}
      resource={resource}
    />
  );
}
