import { useEffect, useId, useRef } from "react";

import { formatShortId } from "../../lib/format-id";
import { FieldManagement } from "../fields/field-management";
import { TerritoryExplorer } from "../territory/territory-explorer";

import type {
  IdentityAction,
  IdentityCapabilities,
  IdentityConnection,
  IdentityNotice,
  IdentityResourceState,
  IdentitySession,
  LinkedIdentity,
  OrganizationCreationState,
  OrganizationOwnerMembershipSummary,
  OwnerMembershipActionState,
  OwnerMembershipResourceState,
  OwnerInvitationAcceptanceState,
  OwnerInvitationActionState,
  OwnerInvitationResourceState,
  OwnerInvitationSummary,
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
  ownerInvitations: Readonly<Record<string, OwnerInvitationResourceState>>;
  ownerInvitationAction: OwnerInvitationActionState;
  ownerInvitationAcceptance: OwnerInvitationAcceptanceState;
  onCreateOwnerInvitation: (organizationId: string) => void;
  onRevokeOwnerInvitation: (invitation: OwnerInvitationSummary) => void;
  onRefreshOwnerInvitations: (organizationId: string) => void;
  onCopyOwnerInvitation: () => void;
  onResumeOwnerManagement: () => void;
  onAcceptOwnerInvitation: () => void;
  onReauthenticateOwnerInvitation: () => void;
  ownerMemberships: Readonly<Record<string, OwnerMembershipResourceState>>;
  ownerMembershipAction: OwnerMembershipActionState;
  onBeginOwnerRemoval: (membership: OrganizationOwnerMembershipSummary) => void;
  onCancelOwnerRemoval: () => void;
  onConfirmOwnerRemoval: () => void;
  onRefreshOwnerMemberships: (organizationId: string) => void;
  onRetryOwnerRemoval: () => void;
  onResumeOwnerRemoval: () => void;
  onDismissOwnerRemoval: () => void;
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
  const strongPurposeLabel =
    assurance.purpose === "manage_organization_owners"
      ? "administrar co-owners"
      : "administrar métodos de acceso";

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
          ? `La verificación reforzada reserva un contexto seguro para ${strongPurposeLabel}${
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

const INVITATION_STATUS_LABELS = {
  pending: "Pendiente",
  accepted: "Aceptada",
  revoked: "Revocada",
  expired: "Vencida",
} satisfies Record<OwnerInvitationSummary["status"], string>;

function formatInvitationDate(value: string): string {
  return new Intl.DateTimeFormat("es-AR", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "America/Argentina/Buenos_Aires",
  }).format(new Date(value));
}

function OwnerMembershipManagement({
  session,
  resources,
  action,
  onBeginRemoval,
  onCancelRemoval,
  onConfirmRemoval,
  onRefresh,
  onRetryRemoval,
  onResumeRemoval,
  onDismissRemoval,
}: Readonly<{
  session: IdentitySession;
  resources: IdentityViewProps["ownerMemberships"];
  action: IdentityViewProps["ownerMembershipAction"];
  onBeginRemoval: IdentityViewProps["onBeginOwnerRemoval"];
  onCancelRemoval: IdentityViewProps["onCancelOwnerRemoval"];
  onConfirmRemoval: IdentityViewProps["onConfirmOwnerRemoval"];
  onRefresh: IdentityViewProps["onRefreshOwnerMemberships"];
  onRetryRemoval: IdentityViewProps["onRetryOwnerRemoval"];
  onResumeRemoval: IdentityViewProps["onResumeOwnerRemoval"];
  onDismissRemoval: IdentityViewProps["onDismissOwnerRemoval"];
}>) {
  const titleId = useId();
  const confirmationTitleId = useId();
  const confirmationDescriptionId = useId();
  const cancelRef = useRef<HTMLButtonElement>(null);
  const titleRef = useRef<HTMLHeadingElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const previousAction = useRef(action.kind);
  const ownerOrganizations = session.memberships.filter(
    (membership) => membership.role.toLowerCase() === "owner",
  );

  useEffect(() => {
    const previous = previousAction.current;
    previousAction.current = action.kind;
    if (action.kind === "confirming") {
      cancelRef.current?.focus();
    } else if (action.kind === "removed") {
      titleRef.current?.focus();
    } else if (action.kind === "idle" && previous === "confirming") {
      triggerRef.current?.focus();
    }
  }, [action.kind]);

  if (ownerOrganizations.length === 0) {
    return null;
  }

  const retryable =
    action.kind === "offline" ||
    action.kind === "service-unavailable" ||
    action.kind === "rate-limited" ||
    action.kind === "error";
  const dismissible =
    action.kind === "last-owner" ||
    action.kind === "stale" ||
    action.kind === "unavailable" ||
    action.kind === "conflict" ||
    action.kind === "removed";

  return (
    <section
      aria-labelledby={titleId}
      className="owner-membership-management identity-card"
    >
      <div className="section-heading">
        <div>
          <p className="section-kicker">Acceso a organizaciones</p>
          <h2 id={titleId} ref={titleRef} tabIndex={-1}>
            Co-owners
          </h2>
          <p>
            Revisá quién puede administrar cada organización. Los cambios de
            acceso requieren una verificación reforzada.
          </p>
        </div>
      </div>

      <div aria-live="polite" className="owner-membership-action">
        {action.kind === "removing" ? (
          <div className="inline-notice" role="status">
            <Spinner />
            <p>Quitando acceso de forma segura…</p>
          </div>
        ) : null}
        {action.kind === "reauthentication-required" ? (
          <div className="inline-notice inline-notice--warning" role="status">
            <p>{action.message}</p>
            <button
              className="button button--secondary"
              onClick={onResumeRemoval}
              type="button"
            >
              Verificar con MFA y continuar
            </button>
          </div>
        ) : null}
        {retryable ? (
          <div className="inline-notice inline-notice--warning" role="alert">
            <p>{action.message}</p>
            <button
              className="button button--secondary"
              onClick={onRetryRemoval}
              type="button"
            >
              Reintentar
            </button>
          </div>
        ) : null}
        {dismissible ? (
          <div
            className={`inline-notice ${action.kind === "removed" ? "inline-notice--success" : "inline-notice--warning"}`}
            role={action.kind === "removed" ? "status" : "alert"}
          >
            <p>{action.message}</p>
            <button
              className="button button--quiet"
              onClick={onDismissRemoval}
              type="button"
            >
              Cerrar aviso
            </button>
          </div>
        ) : null}
      </div>

      <div className="owner-membership-organizations">
        {ownerOrganizations.map((organization) => {
          const state = resources[organization.organizationId] ?? {
            kind: "idle",
          };
          const busy =
            state.kind === "loading" ||
            (action.kind === "removing" &&
              action.organizationId === organization.organizationId);
          return (
            <article
              aria-busy={busy}
              className="owner-membership-organization"
              key={organization.organizationId}
            >
              <div className="owner-membership-organization__heading">
                <div>
                  <h3>{organization.organizationName}</h3>
                  <p>Owners activos con acceso a esta organización.</p>
                </div>
                <button
                  aria-label={`Actualizar co-owners de ${organization.organizationName}`}
                  className="button button--quiet"
                  disabled={busy}
                  onClick={() => onRefresh(organization.organizationId)}
                  type="button"
                >
                  Actualizar
                </button>
              </div>

              {state.kind === "idle" || state.kind === "loading" ? (
                <div className="owner-membership-state" role="status">
                  <Spinner /> Cargando co-owners
                </div>
              ) : null}
              {state.kind !== "idle" &&
              state.kind !== "loading" &&
              state.kind !== "ready" ? (
                <div className="owner-membership-state" role="alert">
                  <p>
                    {state.kind === "offline"
                      ? "Estás sin conexión. No pudimos cargar los co-owners."
                      : state.kind === "unavailable"
                        ? "La organización o tu acceso ya no están disponibles."
                        : state.kind === "service-unavailable"
                          ? "El servicio de co-owners no está disponible temporalmente."
                          : "No pudimos cargar los co-owners."}
                  </p>
                  <button
                    className="button button--secondary"
                    onClick={() => onRefresh(organization.organizationId)}
                    type="button"
                  >
                    Reintentar
                  </button>
                </div>
              ) : null}
              {state.kind === "ready" ? (
                <>
                  <ul className="owner-membership-list">
                    {state.items.map((membership) => (
                      <li
                        className="owner-membership-row"
                        key={membership.membershipId}
                      >
                        <div className="owner-membership-meta">
                          <strong>{membership.displayName}</strong>
                          <span>
                            Membresía {formatShortId(membership.membershipId)}
                          </span>
                          <div className="owner-membership-chips">
                            <span className="role-chip">Owner</span>
                            {membership.isCurrentUser ? (
                              <span className="current-user-chip">Vos</span>
                            ) : null}
                          </div>
                        </div>
                        {!membership.isCurrentUser ? (
                          <button
                            aria-label={`Quitar acceso a ${membership.displayName}`}
                            className="button button--danger-quiet"
                            disabled={busy}
                            onClick={(event) => {
                              triggerRef.current = event.currentTarget;
                              onBeginRemoval(membership);
                            }}
                            type="button"
                          >
                            Quitar acceso
                          </button>
                        ) : null}
                      </li>
                    ))}
                  </ul>
                  {state.items.length === 1 ? (
                    <p className="owner-membership-guidance">
                      Sos el único owner activo. Invitá a otra persona antes de
                      modificar el acceso de owners.
                    </p>
                  ) : null}
                </>
              ) : null}

              {action.kind === "confirming" &&
              action.membership.organizationId ===
                organization.organizationId ? (
                <div
                  aria-describedby={confirmationDescriptionId}
                  aria-labelledby={confirmationTitleId}
                  className="owner-removal-confirmation"
                  onKeyDown={(event) => {
                    if (event.key === "Escape") {
                      event.preventDefault();
                      onCancelRemoval();
                    }
                  }}
                  role="alertdialog"
                >
                  <h4 id={confirmationTitleId}>
                    ¿Quitar acceso a {action.membership.displayName}?
                  </h4>
                  <p id={confirmationDescriptionId}>
                    Perderá el acceso a {organization.organizationName} y sus
                    invitaciones pendientes se revocarán. Esta acción no se
                    puede deshacer desde esta pantalla.
                  </p>
                  <div className="owner-membership-actions">
                    <button
                      className="button button--secondary"
                      onClick={onCancelRemoval}
                      ref={cancelRef}
                      type="button"
                    >
                      Cancelar
                    </button>
                    <button
                      className="button button--danger-quiet"
                      onClick={onConfirmRemoval}
                      type="button"
                    >
                      Quitar acceso a {action.membership.displayName}
                    </button>
                  </div>
                </div>
              ) : null}
            </article>
          );
        })}
      </div>
    </section>
  );
}

function OwnerInvitationManagement({
  session,
  resources,
  action,
  onCreate,
  onRevoke,
  onRefresh,
  onCopy,
  onResume,
}: Readonly<{
  session: IdentitySession;
  resources: IdentityViewProps["ownerInvitations"];
  action: IdentityViewProps["ownerInvitationAction"];
  onCreate: IdentityViewProps["onCreateOwnerInvitation"];
  onRevoke: IdentityViewProps["onRevokeOwnerInvitation"];
  onRefresh: IdentityViewProps["onRefreshOwnerInvitations"];
  onCopy: IdentityViewProps["onCopyOwnerInvitation"];
  onResume: IdentityViewProps["onResumeOwnerManagement"];
}>) {
  const ownerMemberships = session.memberships.filter(
    (membership) => membership.role.toLowerCase() === "owner",
  );
  const linkRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (action.kind === "revealed") {
      linkRef.current?.focus();
      linkRef.current?.select();
    }
  }, [action.kind]);

  if (ownerMemberships.length === 0) {
    return null;
  }

  const actionHasMessage =
    action.kind === "reauthentication-required" ||
    action.kind === "conflict" ||
    action.kind === "offline" ||
    action.kind === "error";

  return (
    <section
      className="identity-card invitation-management"
      aria-labelledby="owner-invitations-title"
    >
      <div className="section-heading">
        <div>
          <p className="section-kicker">Acceso compartido</p>
          <h2 id="owner-invitations-title">Invitaciones de co-owner</h2>
        </div>
      </div>
      <p className="invitation-management__intro">
        Cada enlace concede permisos de owner a quien lo controle y acepte con
        una cuenta verificada. Compartilo por un canal seguro.
      </p>

      {action.kind === "revealed" ? (
        <div
          className="invitation-secret"
          role="alert"
          aria-labelledby="invitation-secret-title"
        >
          <div>
            <p className="section-kicker">Visible una sola vez</p>
            <h3 id="invitation-secret-title">Guardá el enlace ahora</h3>
            <p>
              No podremos recuperarlo. Quien tenga este enlace podrá convertirse
              en owner de la organización.
            </p>
          </div>
          <label htmlFor="owner-invitation-link">Enlace de invitación</label>
          <input
            ref={linkRef}
            id="owner-invitation-link"
            onFocus={(event) => event.currentTarget.select()}
            readOnly
            spellCheck={false}
            type="text"
            value={action.shareLink}
          />
          <button
            className="button button--primary"
            onClick={onCopy}
            type="button"
          >
            Copiar y ocultar
          </button>
        </div>
      ) : null}

      {actionHasMessage ? (
        <div
          className={`organization-state organization-state--${action.kind}`}
          role="alert"
        >
          <strong>
            {action.kind === "reauthentication-required"
              ? "Verificación reforzada necesaria"
              : action.kind === "offline"
                ? "Sin conexión"
                : action.kind === "conflict"
                  ? "La invitación cambió"
                  : "No pudimos completar la operación"}
          </strong>
          <p>{action.message}</p>
          {action.kind === "reauthentication-required" ? (
            <button
              className="button button--primary"
              onClick={onResume}
              type="button"
            >
              Verificar con MFA y continuar
            </button>
          ) : null}
        </div>
      ) : null}

      <div className="invitation-organizations">
        {ownerMemberships.map((membership) => {
          const state = resources[membership.organizationId] ?? {
            kind: "idle",
          };
          const creating =
            action.kind === "creating" &&
            action.organizationId === membership.organizationId;
          return (
            <article
              className="invitation-organization"
              key={membership.organizationId}
            >
              <header>
                <div>
                  <h3>{membership.organizationName}</h3>
                  <p>Organización {formatShortId(membership.organizationId)}</p>
                </div>
                <button
                  className="button button--secondary"
                  disabled={creating || action.kind === "revoking"}
                  onClick={() => onCreate(membership.organizationId)}
                  type="button"
                >
                  {creating ? <Spinner /> : null}
                  {creating ? "Creando enlace" : "Verificar y crear enlace"}
                </button>
              </header>

              <div aria-busy={state.kind === "loading"} aria-live="polite">
                {state.kind === "idle" || state.kind === "loading" ? (
                  <div className="invitation-region-state" role="status">
                    <Spinner /> Cargando invitaciones
                  </div>
                ) : null}
                {state.kind === "offline" || state.kind === "error" ? (
                  <div
                    className="invitation-region-state invitation-region-state--error"
                    role="alert"
                  >
                    <span>
                      {state.kind === "offline"
                        ? "Sin conexión."
                        : "No pudimos cargar las invitaciones."}
                    </span>
                    <button
                      className="button button--quiet"
                      onClick={() => onRefresh(membership.organizationId)}
                      type="button"
                    >
                      Reintentar
                    </button>
                  </div>
                ) : null}
                {state.kind === "ready" && state.items.length === 0 ? (
                  <div className="empty-state" role="status">
                    <strong>No hay invitaciones</strong>
                    <p>Creá un enlace cuando necesites sumar otro owner.</p>
                  </div>
                ) : null}
                {state.kind === "ready" && state.items.length > 0 ? (
                  <ul className="invitation-list">
                    {state.items.map((invitation) => {
                      const revoking =
                        action.kind === "revoking" &&
                        action.invitationId === invitation.invitationId;
                      return (
                        <li key={invitation.invitationId}>
                          <div>
                            <strong>
                              Invitación{" "}
                              {formatShortId(invitation.invitationId)}
                            </strong>
                            <span>
                              Vence{" "}
                              {formatInvitationDate(invitation.expiresAtUtc)}
                            </span>
                          </div>
                          <span
                            className={`invitation-status invitation-status--${invitation.status}`}
                          >
                            {INVITATION_STATUS_LABELS[invitation.status]}
                          </span>
                          {invitation.status === "pending" ? (
                            <button
                              className="button button--danger-quiet"
                              disabled={revoking || action.kind === "creating"}
                              onClick={() => onRevoke(invitation)}
                              type="button"
                            >
                              {revoking ? <Spinner /> : null}
                              {revoking ? "Revocando" : "Revocar"}
                            </button>
                          ) : null}
                        </li>
                      );
                    })}
                  </ul>
                ) : null}
              </div>
            </article>
          );
        })}
      </div>
    </section>
  );
}

function OwnerInvitationAcceptance({
  state,
  onAccept,
  onReauthenticate,
}: Readonly<{
  state: IdentityViewProps["ownerInvitationAcceptance"];
  onAccept: IdentityViewProps["onAcceptOwnerInvitation"];
  onReauthenticate: IdentityViewProps["onReauthenticateOwnerInvitation"];
}>) {
  if (state.kind === "idle") {
    return null;
  }
  const needsAuthentication = state.kind === "reauthentication-required";
  return (
    <section
      className={`invitation-acceptance invitation-acceptance--${state.kind}`}
      aria-live="polite"
    >
      <div>
        <p className="section-kicker">Invitación de co-owner</p>
        <h3>
          {state.kind === "accepting"
            ? "Aceptando invitación"
            : state.kind === "unavailable"
              ? "Enlace no disponible"
              : needsAuthentication
                ? "Protegemos este acceso"
                : "No pudimos aceptar el enlace"}
        </h3>
        {state.kind === "accepting" ? (
          <p>
            Estamos validando el enlace sin recargar el resto de la pantalla.
          </p>
        ) : (
          <p>{state.message}</p>
        )}
      </div>
      {state.kind === "accepting" ? <Spinner /> : null}
      {needsAuthentication ? (
        <button
          className="button button--primary"
          onClick={onReauthenticate}
          type="button"
        >
          Ingresar y aceptar
        </button>
      ) : state.kind === "offline" || state.kind === "error" ? (
        <button
          className="button button--primary"
          onClick={onAccept}
          type="button"
        >
          Reintentar
        </button>
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
  ownerInvitations,
  ownerInvitationAction,
  onCreateOwnerInvitation,
  onRevokeOwnerInvitation,
  onRefreshOwnerInvitations,
  onCopyOwnerInvitation,
  onResumeOwnerManagement,
  ownerMemberships,
  ownerMembershipAction,
  onBeginOwnerRemoval,
  onCancelOwnerRemoval,
  onConfirmOwnerRemoval,
  onRefreshOwnerMemberships,
  onRetryOwnerRemoval,
  onResumeOwnerRemoval,
  onDismissOwnerRemoval,
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
  ownerInvitations: IdentityViewProps["ownerInvitations"];
  ownerInvitationAction: IdentityViewProps["ownerInvitationAction"];
  onCreateOwnerInvitation: IdentityViewProps["onCreateOwnerInvitation"];
  onRevokeOwnerInvitation: IdentityViewProps["onRevokeOwnerInvitation"];
  onRefreshOwnerInvitations: IdentityViewProps["onRefreshOwnerInvitations"];
  onCopyOwnerInvitation: IdentityViewProps["onCopyOwnerInvitation"];
  onResumeOwnerManagement: IdentityViewProps["onResumeOwnerManagement"];
  ownerMemberships: IdentityViewProps["ownerMemberships"];
  ownerMembershipAction: IdentityViewProps["ownerMembershipAction"];
  onBeginOwnerRemoval: IdentityViewProps["onBeginOwnerRemoval"];
  onCancelOwnerRemoval: IdentityViewProps["onCancelOwnerRemoval"];
  onConfirmOwnerRemoval: IdentityViewProps["onConfirmOwnerRemoval"];
  onRefreshOwnerMemberships: IdentityViewProps["onRefreshOwnerMemberships"];
  onRetryOwnerRemoval: IdentityViewProps["onRetryOwnerRemoval"];
  onResumeOwnerRemoval: IdentityViewProps["onResumeOwnerRemoval"];
  onDismissOwnerRemoval: IdentityViewProps["onDismissOwnerRemoval"];
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

      <FieldManagement organizations={session.memberships} />

      <OwnerMembershipManagement
        action={ownerMembershipAction}
        onBeginRemoval={onBeginOwnerRemoval}
        onCancelRemoval={onCancelOwnerRemoval}
        onConfirmRemoval={onConfirmOwnerRemoval}
        onDismissRemoval={onDismissOwnerRemoval}
        onRefresh={onRefreshOwnerMemberships}
        onResumeRemoval={onResumeOwnerRemoval}
        onRetryRemoval={onRetryOwnerRemoval}
        resources={ownerMemberships}
        session={session}
      />

      {capabilities.ownerInvitationsAvailable ? (
        <OwnerInvitationManagement
          action={ownerInvitationAction}
          onCopy={onCopyOwnerInvitation}
          onCreate={onCreateOwnerInvitation}
          onRefresh={onRefreshOwnerInvitations}
          onResume={onResumeOwnerManagement}
          onRevoke={onRevokeOwnerInvitation}
          resources={ownerInvitations}
          session={session}
        />
      ) : null}

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
  ownerInvitations,
  ownerInvitationAction,
  ownerInvitationAcceptance,
  onCreateOwnerInvitation,
  onRevokeOwnerInvitation,
  onRefreshOwnerInvitations,
  onCopyOwnerInvitation,
  onResumeOwnerManagement,
  onAcceptOwnerInvitation,
  onReauthenticateOwnerInvitation,
  ownerMemberships,
  ownerMembershipAction,
  onBeginOwnerRemoval,
  onCancelOwnerRemoval,
  onConfirmOwnerRemoval,
  onRefreshOwnerMemberships,
  onRetryOwnerRemoval,
  onResumeOwnerRemoval,
  onDismissOwnerRemoval,
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
        <OwnerInvitationAcceptance
          onAccept={onAcceptOwnerInvitation}
          onReauthenticate={onReauthenticateOwnerInvitation}
          state={ownerInvitationAcceptance}
        />

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
            <>
              <CurrentSession
                capabilities={resource.capabilities}
                onLink={onLink}
                onStepUp={onStepUp}
                onRevoke={onRevoke}
                onUnlink={onUnlink}
                onCancelOrganization={onCancelOrganization}
                onBeginOwnerRemoval={onBeginOwnerRemoval}
                onCancelOwnerRemoval={onCancelOwnerRemoval}
                onConfirmOwnerRemoval={onConfirmOwnerRemoval}
                onCreateOrganization={onCreateOrganization}
                onOrganizationDraftChange={onOrganizationDraftChange}
                onReauthenticateOrganization={onReauthenticateOrganization}
                onStartOrganization={onStartOrganization}
                onCopyOwnerInvitation={onCopyOwnerInvitation}
                onCreateOwnerInvitation={onCreateOwnerInvitation}
                onRefreshOwnerInvitations={onRefreshOwnerInvitations}
                onRefreshOwnerMemberships={onRefreshOwnerMemberships}
                onRetryOwnerRemoval={onRetryOwnerRemoval}
                onResumeOwnerRemoval={onResumeOwnerRemoval}
                onDismissOwnerRemoval={onDismissOwnerRemoval}
                onResumeOwnerManagement={onResumeOwnerManagement}
                onRevokeOwnerInvitation={onRevokeOwnerInvitation}
                organizationCreation={organizationCreation}
                organizationDraft={organizationDraft}
                organizationFormOpen={organizationFormOpen}
                ownerInvitationAction={ownerInvitationAction}
                ownerInvitations={ownerInvitations}
                ownerMembershipAction={ownerMembershipAction}
                ownerMemberships={ownerMemberships}
                pendingAction={pendingAction}
                session={resource.session}
              />
              <TerritoryExplorer />
            </>
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
