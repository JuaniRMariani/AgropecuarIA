"use client";

import { useCallback, useEffect, useRef, useState } from "react";

import {
  completeDevelopmentStepUp,
  completeDevelopmentLink,
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
} from "./identity-types";
import { NO_IDENTITY_NOTICE } from "./identity-types";
import { IdentityView } from "./identity-view";

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

  return (
    <IdentityView
      notice={notice}
      onLink={(connection) => void handleLink(connection)}
      onLogin={handleLogin}
      onStepUp={() => void handleStepUp()}
      onRetry={() => {
        setResource({ kind: "loading" });
        setNotice(NO_IDENTITY_NOTICE);
        void refresh();
      }}
      onRevoke={() => void handleRevoke()}
      onSyntheticSignIn={() => void handleSyntheticSignIn()}
      onUnlink={(identityId) => void handleUnlink(identityId)}
      pendingAction={pendingAction}
      resource={resource}
    />
  );
}
