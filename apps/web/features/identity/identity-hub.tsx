"use client";

import { useCallback, useEffect, useRef, useState } from "react";

import {
  completeDevelopmentStepUp,
  completeDevelopmentLink,
  createOrganization,
  developmentSignIn,
  identityLoginUrl,
  IdentityApiError,
  loadCapabilities,
  loadSession,
  resolveAuthorizationUrl,
  revokeSession,
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
  OrganizationCreationState,
} from "./identity-types";
import { NO_IDENTITY_NOTICE } from "./identity-types";
import { IdentityView } from "./identity-view";

const ORGANIZATION_REAUTH_STATE_KEY =
  "agropecuaria.identity.organization-reauth.v1";
const ORGANIZATION_ATTEMPT_KEY_PATTERN = /^[0-9a-f]{32}$/;

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
  const organizationAttemptKey = useRef<string | null>(null);
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

  return (
    <IdentityView
      notice={notice}
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
      onReauthenticateOrganization={handleReauthenticateOrganization}
      organizationCreation={organizationCreation}
      organizationDraft={organizationDraft}
      organizationFormOpen={organizationFormOpen}
      pendingAction={pendingAction}
      resource={resource}
    />
  );
}
