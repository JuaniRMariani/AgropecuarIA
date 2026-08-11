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
  listOwnerInvitations,
  loadSession,
  resolveAuthorizationUrl,
  revokeSession,
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
  const organizationAttemptKey = useRef<string | null>(null);
  const invitationToken = useRef<string | null>(null);
  const invitationAction = useRef<StoredOwnerInvitationAction | null>(null);
  const invitationCreateKeys = useRef<Record<string, string>>({});
  const mounted = useRef(true);

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
      await Promise.resolve();
      if (signal?.aborted === true) {
        return;
      }
      setOwnerInvitations((current) => ({
        ...current,
        [organizationId]: { kind: "loading" },
      }));
      try {
        const items = await listOwnerInvitations(organizationId, signal);
        if (mounted.current) {
          setOwnerInvitations((current) => ({
            ...current,
            [organizationId]: { kind: "ready", items },
          }));
        }
      } catch (error) {
        if (!isAbort(error) && mounted.current) {
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
      if (invitationToken.current !== null && resource.kind === "signed-out") {
        setOwnerInvitationAcceptance({
          kind: "reauthentication-required",
          message: "Ingresá para aceptar la invitación como co-owner.",
        });
      }
      return;
    }
    if (!resource.capabilities.ownerInvitationsAvailable) {
      if (invitationToken.current !== null) {
        setOwnerInvitationAcceptance({
          kind: "unavailable",
          message:
            "Las invitaciones de co-owner no están disponibles en este entorno.",
        });
      }
      return;
    }
    const controller = new AbortController();
    queueMicrotask(() => {
      if (controller.signal.aborted) {
        return;
      }
      for (const membership of resource.session.memberships) {
        if (membership.role.toLowerCase() === "owner") {
          void refreshOwnerInvitations(
            membership.organizationId,
            controller.signal,
          );
        }
      }
      const storedAction = takeOwnerInvitationAction();
      if (storedAction !== null) {
        invitationAction.current = storedAction;
        void executeOwnerInvitationAction(storedAction);
      }
      if (invitationToken.current !== null) {
        void handleAcceptOwnerInvitation();
      }
    });
    return () => controller.abort();
  }, [
    executeOwnerInvitationAction,
    handleAcceptOwnerInvitation,
    refreshOwnerInvitations,
    resource,
  ]);

  return (
    <IdentityView
      notice={notice}
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
      onCopyOwnerInvitation={() => void handleCopyOwnerInvitation()}
      onCreateOwnerInvitation={handleCreateOwnerInvitation}
      onReauthenticateOwnerInvitation={handleReauthenticateOwnerInvitation}
      onRefreshOwnerInvitations={(organizationId) =>
        void refreshOwnerInvitations(organizationId)
      }
      onResumeOwnerManagement={handleResumeOwnerManagement}
      onRevokeOwnerInvitation={handleRevokeOwnerInvitation}
      onReauthenticateOrganization={handleReauthenticateOrganization}
      organizationCreation={organizationCreation}
      organizationDraft={organizationDraft}
      organizationFormOpen={organizationFormOpen}
      ownerInvitationAcceptance={ownerInvitationAcceptance}
      ownerInvitationAction={ownerInvitationAction}
      ownerInvitations={ownerInvitations}
      pendingAction={pendingAction}
      resource={resource}
    />
  );
}
