import { formatShortId } from "../../lib/format-id";

import type {
  IdentityAction,
  IdentityCapabilities,
  IdentityConnection,
  IdentityNotice,
  IdentityResourceState,
  IdentitySession,
  LinkedIdentity,
} from "./identity-types";

type IdentityViewProps = Readonly<{
  resource: IdentityResourceState;
  notice: IdentityNotice;
  pendingAction: IdentityAction | null;
  onRetry: () => void;
  onLogin: (connection: IdentityConnection) => void;
  onLink: (connection: IdentityConnection) => void;
  onUnlink: (identityId: string) => void;
  onRevoke: () => void;
  onSyntheticSignIn: () => void;
}>;

const CONNECTION_LABELS = {
  email: "Correo verificado",
  google: "Google",
} satisfies Record<IdentityConnection, string>;

function isConnectionAvailable(
  capabilities: IdentityCapabilities,
  connection: IdentityConnection,
): boolean {
  return capabilities.connections.some(
    (item) => item.id === connection && item.available,
  );
}

function Spinner() {
  return <span aria-hidden="true" className="spinner" />;
}

function RegionLoading({ label }: Readonly<{ label: string }>) {
  return (
    <div className="region-state region-state--loading" role="status">
      <Spinner />
      <div>
        <strong>{label}</strong>
        <p>Estamos comprobando el acceso sin mover el resto de la pantalla.</p>
      </div>
    </div>
  );
}

function Notice({ notice }: Readonly<{ notice: IdentityNotice }>) {
  if (notice.kind === "none") {
    return null;
  }

  return (
    <div
      className={`notice notice--${notice.kind}`}
      role={notice.kind === "success" ? "status" : "alert"}
    >
      <span aria-hidden="true" className="notice__mark">
        {notice.kind === "success" ? "✓" : "!"}
      </span>
      <p>{notice.message}</p>
    </div>
  );
}

function AccessPanel({
  capabilities,
  pendingAction,
  onLogin,
  onSyntheticSignIn,
}: Readonly<{
  capabilities: IdentityCapabilities;
  pendingAction: IdentityAction | null;
  onLogin: IdentityViewProps["onLogin"];
  onSyntheticSignIn: IdentityViewProps["onSyntheticSignIn"];
}>) {
  const emailEnabled = isConnectionAvailable(capabilities, "email");
  const googleEnabled = isConnectionAvailable(capabilities, "google");
  const syntheticVisible =
    capabilities.developmentProviderEnabled &&
    (capabilities.environment === "Development" ||
      capabilities.environment === "Test");

  return (
    <div className="access-stack">
      {emailEnabled ? (
        <button
          className="button button--primary"
          disabled={pendingAction !== null}
          onClick={() => onLogin("email")}
          type="button"
        >
          Continuar con correo
        </button>
      ) : null}

      {emailEnabled && googleEnabled ? (
        <div className="divider" aria-hidden="true">
          <span>o</span>
        </div>
      ) : null}

      {googleEnabled ? (
        <button
          className="button button--google"
          disabled={pendingAction !== null}
          onClick={() => onLogin("google")}
          type="button"
        >
          <span aria-hidden="true">G</span>
          Continuar con Google
        </button>
      ) : null}

      {!emailEnabled && !googleEnabled ? (
        <div className="empty-state" role="status">
          <strong>No hay métodos de acceso disponibles</strong>
          <p>El equipo ya fue avisado. Intentá nuevamente más tarde.</p>
        </div>
      ) : null}

      {syntheticVisible ? (
        <div className="development-access">
          <p>
            <strong>Entorno {capabilities.environment}</strong> · datos
            sintéticos, sin proveedor real.
          </p>
          <button
            className="button button--quiet"
            disabled={pendingAction !== null}
            onClick={onSyntheticSignIn}
            type="button"
          >
            {pendingAction === "synthetic" ? <Spinner /> : null}
            Entrar con fixture seguro
          </button>
        </div>
      ) : null}
    </div>
  );
}

function IdentityRow({
  identity,
  canUnlink,
  pendingAction,
  onUnlink,
}: Readonly<{
  identity: LinkedIdentity;
  canUnlink: boolean;
  pendingAction: IdentityAction | null;
  onUnlink: IdentityViewProps["onUnlink"];
}>) {
  return (
    <li className="identity-row">
      <div className="identity-row__icon" aria-hidden="true">
        {identity.connection === "google" ? "G" : "@"}
      </div>
      <div className="identity-row__copy">
        <strong>{CONNECTION_LABELS[identity.connection]}</strong>
        <span>{identity.label}</span>
        <small>Verificada · ID {formatShortId(identity.identityId)}</small>
      </div>
      {canUnlink ? (
        <button
          aria-label={`Desvincular ${CONNECTION_LABELS[identity.connection]}`}
          className="button button--danger-quiet"
          disabled={pendingAction !== null}
          onClick={() => onUnlink(identity.identityId)}
          type="button"
        >
          {pendingAction === "unlink" ? <Spinner /> : null}
          Desvincular
        </button>
      ) : null}
    </li>
  );
}

function CurrentSession({
  capabilities,
  session,
  pendingAction,
  onLink,
  onUnlink,
  onRevoke,
}: Readonly<{
  capabilities: IdentityCapabilities;
  session: IdentitySession;
  pendingAction: IdentityAction | null;
  onLink: IdentityViewProps["onLink"];
  onUnlink: IdentityViewProps["onUnlink"];
  onRevoke: IdentityViewProps["onRevoke"];
}>) {
  const connections = new Set(
    session.identities.map((identity) => identity.connection),
  );
  const canLinkEmail =
    isConnectionAvailable(capabilities, "email") && !connections.has("email");
  const canLinkGoogle =
    isConnectionAvailable(capabilities, "google") && !connections.has("google");

  return (
    <div className="session-stack">
      <section className="session-card" aria-labelledby="session-title">
        <div>
          <p className="section-kicker">Sesión actual</p>
          <h2 id="session-title">{session.displayName}</h2>
          <p className="session-email">
            Usuario {formatShortId(session.userId)}
          </p>
        </div>
        <div className="session-chip">
          <span aria-hidden="true" />
          Activa
        </div>
      </section>

      <section className="identity-card" aria-labelledby="identities-title">
        <div className="section-heading">
          <div>
            <p className="section-kicker">Seguridad de cuenta</p>
            <h2 id="identities-title">Identidades vinculadas</h2>
          </div>
          <span
            className="count-chip"
            aria-label={`${session.identities.length} identidades`}
          >
            {session.identities.length}
          </span>
        </div>

        {session.identities.length > 0 ? (
          <ul className="identity-list">
            {session.identities.map((identity) => (
              <IdentityRow
                canUnlink={session.identities.length > 1}
                identity={identity}
                key={identity.identityId}
                onUnlink={onUnlink}
                pendingAction={pendingAction}
              />
            ))}
          </ul>
        ) : (
          <div className="empty-state" role="status">
            <strong>No hay identidades disponibles</strong>
            <p>
              La sesión no puede operar hasta recuperar una credencial
              verificada.
            </p>
          </div>
        )}

        {canLinkEmail || canLinkGoogle ? (
          <div className="link-panel">
            <div>
              <p className="section-kicker">Segundo acceso</p>
              <h3>Vinculá otra identidad</h3>
              <p>
                Vas a reautenticar ambas credenciales. Coincidir por correo
                nunca alcanza.
              </p>
            </div>
            <div className="link-actions">
              {canLinkEmail ? (
                <button
                  className="button button--secondary"
                  disabled={pendingAction !== null}
                  onClick={() => onLink("email")}
                  type="button"
                >
                  {pendingAction === "link-email" ? <Spinner /> : null}
                  Vincular correo
                </button>
              ) : null}
              {canLinkGoogle ? (
                <button
                  className="button button--secondary"
                  disabled={pendingAction !== null}
                  onClick={() => onLink("google")}
                  type="button"
                >
                  {pendingAction === "link-google" ? (
                    <Spinner />
                  ) : (
                    <span aria-hidden="true">G</span>
                  )}
                  Vincular Google
                </button>
              ) : null}
            </div>
          </div>
        ) : null}
      </section>

      <section className="membership-card" aria-labelledby="memberships-title">
        <div className="section-heading">
          <div>
            <p className="section-kicker">Organizaciones</p>
            <h2 id="memberships-title">Membresías vigentes</h2>
          </div>
          <span
            className="count-chip"
            aria-label={`${session.memberships.length} membresías`}
          >
            {session.memberships.length}
          </span>
        </div>
        {session.memberships.length === 0 ? (
          <div className="empty-state" role="status">
            <strong>Todavía no tenés una organización asignada</strong>
            <p>
              Tu acceso está activo, pero no habilita datos de ninguna empresa.
            </p>
          </div>
        ) : (
          <ul className="membership-list">
            {session.memberships.map((membership) => (
              <li key={membership.organizationId}>
                <div>
                  <strong>{membership.organizationName}</strong>
                  <span>
                    Organización {formatShortId(membership.organizationId)}
                  </span>
                </div>
                <span className="role-chip">{membership.role}</span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="revoke-card" aria-labelledby="revoke-title">
        <div>
          <h2 id="revoke-title">Cerrar esta sesión</h2>
          <p>
            Revoca el acceso de este navegador sin afectar tus identidades
            vinculadas.
          </p>
        </div>
        <button
          className="button button--danger-quiet"
          disabled={pendingAction !== null}
          onClick={onRevoke}
          type="button"
        >
          {pendingAction === "revoke" ? <Spinner /> : null}
          Cerrar sesión
        </button>
      </section>
    </div>
  );
}

function StatePanel({
  kind,
  onRetry,
}: Readonly<{
  kind: "revoked" | "provider-down" | "error";
  onRetry: IdentityViewProps["onRetry"];
}>) {
  const content = {
    revoked: {
      eyebrow: "Sesión protegida",
      title: "Esta sesión fue revocada",
      detail:
        "Volvé a ingresar para continuar. No se reutilizó ninguna credencial anterior.",
    },
    "provider-down": {
      eyebrow: "Servicio de acceso",
      title: "El proveedor no responde",
      detail:
        "No confirmamos ningún acceso. Esperá un momento y volvé a intentar.",
    },
    error: {
      eyebrow: "Acceso seguro",
      title: "No pudimos comprobar la sesión",
      detail:
        "Tu información no cambió. El mensaje es deliberadamente neutral para proteger tu cuenta.",
    },
  } satisfies Record<
    typeof kind,
    { eyebrow: string; title: string; detail: string }
  >;

  return (
    <div className="region-state region-state--message" role="alert">
      <p className="section-kicker">{content[kind].eyebrow}</p>
      <h2>{content[kind].title}</h2>
      <p>{content[kind].detail}</p>
      <button
        className="button button--primary"
        onClick={onRetry}
        type="button"
      >
        Reintentar
      </button>
    </div>
  );
}

export function IdentityView({
  resource,
  notice,
  pendingAction,
  onRetry,
  onLogin,
  onLink,
  onUnlink,
  onRevoke,
  onSyntheticSignIn,
}: IdentityViewProps) {
  return (
    <main className="identity-page">
      <section className="identity-intro" aria-labelledby="page-title">
        <a
          className="brand"
          href="#page-title"
          aria-label="AgropecuarIA, ir al título"
        >
          <span className="brand__mark" aria-hidden="true">
            A
          </span>
          <span>AgropecuarIA</span>
        </a>
        <div className="identity-intro__copy">
          <p className="eyebrow">Tu campo, bajo tu control</p>
          <h1 id="page-title">Entrá al cuaderno que trabaja con vos.</h1>
          <p>
            Un único acceso para registrar, revisar y decidir con la historia de
            tu organización siempre separada y protegida.
          </p>
        </div>
        <ul className="trust-list" aria-label="Principios del acceso">
          <li>
            <span aria-hidden="true">01</span>El servidor decide cada permiso
          </li>
          <li>
            <span aria-hidden="true">02</span>Nunca vinculamos cuentas solo por
            email
          </li>
          <li>
            <span aria-hidden="true">03</span>Sin conexión no confirmamos
            cambios
          </li>
        </ul>
      </section>

      <section className="identity-workspace" aria-labelledby="workspace-title">
        <header className="workspace-heading">
          <div>
            <p className="eyebrow">Identidad</p>
            <h2 id="workspace-title">
              {resource.kind === "authenticated"
                ? "Tu acceso"
                : "Ingresá de forma segura"}
            </h2>
          </div>
          <span className="secure-chip">
            <span aria-hidden="true">●</span>Conexión protegida
          </span>
        </header>

        <Notice notice={notice} />

        <div
          aria-busy={resource.kind === "loading" || pendingAction !== null}
          aria-live="polite"
          className="identity-region"
        >
          {resource.kind === "loading" ? (
            <RegionLoading label="Comprobando tu sesión" />
          ) : null}
          {resource.kind === "signed-out" ? (
            <AccessPanel
              capabilities={resource.capabilities}
              onLogin={onLogin}
              onSyntheticSignIn={onSyntheticSignIn}
              pendingAction={pendingAction}
            />
          ) : null}
          {resource.kind === "authenticated" ? (
            <CurrentSession
              capabilities={resource.capabilities}
              onLink={onLink}
              onRevoke={onRevoke}
              onUnlink={onUnlink}
              pendingAction={pendingAction}
              session={resource.session}
            />
          ) : null}
          {resource.kind === "revoked" ||
          resource.kind === "provider-down" ||
          resource.kind === "error" ? (
            <StatePanel kind={resource.kind} onRetry={onRetry} />
          ) : null}
        </div>

        <footer className="workspace-footer">
          <span>Argentina · MVP online</span>
          <span>Privacidad y trazabilidad desde el inicio</span>
        </footer>
      </section>
    </main>
  );
}
