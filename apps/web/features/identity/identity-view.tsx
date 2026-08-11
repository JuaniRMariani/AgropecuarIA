import { useEffect, useId, useRef } from "react";

import { formatShortId } from "../../lib/format-id";

import type {
  IdentityAction,
  IdentityCapabilities,
  IdentityConnection,
  IdentityNotice,
  IdentityResourceState,
  IdentitySession,
  LinkedIdentity,
  OrganizationCreationState,
} from "./identity-types";

type IdentityViewProps = Readonly<{
  resource: IdentityResourceState;
  notice: IdentityNotice;
  pendingAction: IdentityAction | null;
  onRetry: () => void;
  onLogin: (connection: IdentityConnection) => void;
  onLink: (connection: IdentityConnection) => void;
  onStepUp: () => void;
  onUnlink: (identityId: string) => void;
  onRevoke: () => void;
  onSyntheticSignIn: () => void;
  organizationDraft: string;
  organizationFormOpen: boolean;
  organizationCreation: OrganizationCreationState;
  onOrganizationDraftChange: (value: string) => void;
  onStartOrganization: () => void;
  onCancelOrganization: () => void;
  onCreateOrganization: () => void;
  onReauthenticateOrganization: () => void;
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

function formatAssuranceTime(value: string): string {
  return new Intl.DateTimeFormat("es-AR", {
    dateStyle: "short",
    timeStyle: "short",
    timeZone: "America/Argentina/Buenos_Aires",
  }).format(new Date(value));
}

function AuthenticationCard({
  capabilities,
  session,
  pendingAction,
  onStepUp,
}: Readonly<{
  capabilities: IdentityCapabilities;
  session: IdentitySession;
  pendingAction: IdentityAction | null;
  onStepUp: IdentityViewProps["onStepUp"];
}>) {
  const assurance = session.authentication;
  const isStrong = assurance.level === "strong";

  return (
    <section
      className={`assurance-card assurance-card--${assurance.level}`}
      aria-labelledby="assurance-title"
      aria-busy={pendingAction === "step-up"}
    >
      <div className="assurance-card__heading">
        <div>
          <p className="section-kicker">Protección reforzada</p>
          <h2 id="assurance-title">
            {isStrong
              ? "Identidad verificada con MFA"
              : "Verificación principal"}
          </h2>
        </div>
        <span className={`assurance-chip assurance-chip--${assurance.level}`}>
          {isStrong ? "MFA vigente" : "Nivel básico"}
        </span>
      </div>

      <p className="assurance-card__detail">
        {isStrong
          ? `La verificación reforzada reserva un contexto seguro para la futura gestión de métodos de acceso${
              assurance.expiresAtUtc === null
                ? "."
                : ` hasta ${formatAssuranceTime(assurance.expiresAtUtc)}.`
            }`
          : "Verificá nuevamente con un segundo factor antes de administrar tus métodos de acceso."}
      </p>

      <div className="assurance-card__provider-note">
        <span aria-hidden="true">◇</span>
        <p>
          Passkeys, TOTP y códigos de recuperación quedan cifrados y custodiados
          por el proveedor de identidad. AgropecuarIA no recibe esos secretos.
        </p>
      </div>

      {!isStrong ? (
        capabilities.strongAuthenticationAvailable ? (
          <button
            className="button button--primary assurance-card__action"
            disabled={pendingAction !== null}
            onClick={onStepUp}
            type="button"
          >
            {pendingAction === "step-up" ? <Spinner /> : null}
            {pendingAction === "step-up"
              ? "Verificando con MFA"
              : "Verificar con MFA"}
          </button>
        ) : (
          <div className="empty-state" role="status">
            <strong>Verificación reforzada no disponible</strong>
            <p>
              El proveedor todavía no está configurado. No habilitamos cambios
              sensibles sin esa comprobación.
            </p>
          </div>
        )
      ) : null}
    </section>
  );
}

function OrganizationOnboarding({
  session,
  formOpen,
  draft,
  creation,
  onDraftChange,
  onStart,
  onCancel,
  onCreate,
  onReauthenticate,
}: Readonly<{
  session: IdentitySession;
  formOpen: boolean;
  draft: string;
  creation: OrganizationCreationState;
  onDraftChange: IdentityViewProps["onOrganizationDraftChange"];
  onStart: IdentityViewProps["onStartOrganization"];
  onCancel: IdentityViewProps["onCancelOrganization"];
  onCreate: IdentityViewProps["onCreateOrganization"];
  onReauthenticate: IdentityViewProps["onReauthenticateOrganization"];
}>) {
  const inputId = useId();
  const helpId = useId();
  const stateId = useId();
  const inputRef = useRef<HTMLInputElement>(null);
  const locked =
    creation.kind === "submitting" ||
    creation.kind === "in-progress" ||
    creation.kind === "reconciliation-required";
  const hasStateMessage =
    creation.kind !== "idle" && creation.kind !== "submitting";

  useEffect(() => {
    if (formOpen) {
      inputRef.current?.focus();
    }
  }, [formOpen]);

  useEffect(() => {
    if (creation.kind === "validation") {
      inputRef.current?.focus();
    }
  }, [creation.kind]);

  const submitLabel =
    creation.kind === "submitting"
      ? "Creando organización"
      : creation.kind === "in-progress"
        ? "Consultar nuevamente"
        : creation.kind === "reconciliation-required"
          ? "Consultar resultado"
          : creation.kind === "conflict"
            ? "Iniciar nuevo intento"
            : creation.kind === "rate-limited" ||
                creation.kind === "offline" ||
                creation.kind === "error"
              ? "Reintentar"
              : "Crear organización";

  return (
    <section
      className="membership-card organization-onboarding"
      aria-labelledby="memberships-title"
      aria-busy={creation.kind === "submitting"}
    >
      <div className="section-heading">
        <div>
          <p className="section-kicker">Organizaciones privadas</p>
          <h2 id="memberships-title">Tus organizaciones</h2>
        </div>
        <span
          className="count-chip"
          aria-label={`${session.memberships.length} membresías`}
        >
          {session.memberships.length}
        </span>
      </div>

      {session.memberships.length === 0 && !formOpen ? (
        <div className="organization-empty" role="status">
          <div>
            <strong>Creá tu primera organización</strong>
            <p>
              Va a ser privada. Quedarás como owner y después podrás sumar otras
              sin mezclar sus datos.
            </p>
          </div>
          <button
            className="button button--primary"
            onClick={onStart}
            type="button"
          >
            Crear mi organización
          </button>
        </div>
      ) : null}

      {session.memberships.length > 0 ? (
        <>
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
          {!formOpen ? (
            <button
              className="button button--secondary organization-onboarding__new"
              onClick={onStart}
              type="button"
            >
              Crear otra
            </button>
          ) : null}
        </>
      ) : null}

      {formOpen ? (
        <form
          className="organization-form"
          onSubmit={(event) => {
            event.preventDefault();
            onCreate();
          }}
        >
          <div className="organization-form__heading">
            <div>
              <p className="section-kicker">Nueva organización</p>
              <h3>¿Cómo querés identificarla?</h3>
            </div>
            <span className="privacy-chip">Solo miembros</span>
          </div>
          <div className="form-field">
            <label htmlFor={inputId}>Nombre de la organización</label>
            <input
              ref={inputRef}
              id={inputId}
              aria-describedby={`${helpId} ${hasStateMessage ? stateId : ""}`.trim()}
              aria-invalid={creation.kind === "validation"}
              autoComplete="organization"
              disabled={locked}
              maxLength={160}
              minLength={2}
              name="displayName"
              onChange={(event) => onDraftChange(event.currentTarget.value)}
              required
              type="text"
              value={draft}
            />
            <small id={helpId}>
              Entre 2 y 160 caracteres. No tiene que coincidir con un CUIT.
            </small>
          </div>

          {hasStateMessage ? (
            <div
              id={stateId}
              className={`organization-state organization-state--${creation.kind}`}
              role={
                creation.kind === "in-progress" ||
                creation.kind === "reconciliation-required"
                  ? "status"
                  : "alert"
              }
              aria-live={
                creation.kind === "in-progress" ||
                creation.kind === "reconciliation-required"
                  ? "polite"
                  : "assertive"
              }
            >
              <strong>
                {creation.kind === "reauthentication-required"
                  ? "Necesitamos verificarte de nuevo"
                  : creation.kind === "reconciliation-required"
                    ? "Resultado pendiente"
                    : creation.kind === "in-progress"
                      ? "Creación en curso"
                      : creation.kind === "offline"
                        ? "Sin conexión"
                        : "No pudimos confirmar la creación"}
              </strong>
              <p>{creation.message}</p>
            </div>
          ) : null}

          <div className="organization-form__actions">
            {creation.kind === "reauthentication-required" ? (
              <button
                className="button button--primary"
                onClick={onReauthenticate}
                type="button"
              >
                Volver a verificar
              </button>
            ) : (
              <button
                className="button button--primary"
                disabled={creation.kind === "submitting"}
                type="submit"
              >
                {creation.kind === "submitting" ? <Spinner /> : null}
                {submitLabel}
              </button>
            )}
            {!locked ? (
              <button
                className="button button--quiet"
                onClick={onCancel}
                type="button"
              >
                Cancelar
              </button>
            ) : null}
          </div>
        </form>
      ) : null}
    </section>
  );
}

function CurrentSession({
  capabilities,
  session,
  pendingAction,
  onLink,
  onStepUp,
  onUnlink,
  onRevoke,
  organizationDraft,
  organizationFormOpen,
  organizationCreation,
  onOrganizationDraftChange,
  onStartOrganization,
  onCancelOrganization,
  onCreateOrganization,
  onReauthenticateOrganization,
}: Readonly<{
  capabilities: IdentityCapabilities;
  session: IdentitySession;
  pendingAction: IdentityAction | null;
  onLink: IdentityViewProps["onLink"];
  onStepUp: IdentityViewProps["onStepUp"];
  onUnlink: IdentityViewProps["onUnlink"];
  onRevoke: IdentityViewProps["onRevoke"];
  organizationDraft: IdentityViewProps["organizationDraft"];
  organizationFormOpen: IdentityViewProps["organizationFormOpen"];
  organizationCreation: IdentityViewProps["organizationCreation"];
  onOrganizationDraftChange: IdentityViewProps["onOrganizationDraftChange"];
  onStartOrganization: IdentityViewProps["onStartOrganization"];
  onCancelOrganization: IdentityViewProps["onCancelOrganization"];
  onCreateOrganization: IdentityViewProps["onCreateOrganization"];
  onReauthenticateOrganization: IdentityViewProps["onReauthenticateOrganization"];
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

      <AuthenticationCard
        capabilities={capabilities}
        onStepUp={onStepUp}
        pendingAction={pendingAction}
        session={session}
      />

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

      <OrganizationOnboarding
        creation={organizationCreation}
        draft={organizationDraft}
        formOpen={organizationFormOpen}
        onCancel={onCancelOrganization}
        onCreate={onCreateOrganization}
        onDraftChange={onOrganizationDraftChange}
        onStart={onStartOrganization}
        onReauthenticate={onReauthenticateOrganization}
        session={session}
      />

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
  onStepUp,
  onUnlink,
  onRevoke,
  onSyntheticSignIn,
  organizationDraft,
  organizationFormOpen,
  organizationCreation,
  onOrganizationDraftChange,
  onStartOrganization,
  onCancelOrganization,
  onCreateOrganization,
  onReauthenticateOrganization,
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
          aria-busy={resource.kind === "loading"}
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
              onStepUp={onStepUp}
              onRevoke={onRevoke}
              onUnlink={onUnlink}
              onCancelOrganization={onCancelOrganization}
              onCreateOrganization={onCreateOrganization}
              onOrganizationDraftChange={onOrganizationDraftChange}
              onReauthenticateOrganization={onReauthenticateOrganization}
              onStartOrganization={onStartOrganization}
              organizationCreation={organizationCreation}
              organizationDraft={organizationDraft}
              organizationFormOpen={organizationFormOpen}
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
