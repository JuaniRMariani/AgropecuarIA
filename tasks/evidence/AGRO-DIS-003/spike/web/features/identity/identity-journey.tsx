"use client";

import { useEffect, useReducer, useRef, type ReactNode } from "react";
import styles from "../../app/page.module.css";
import {
  evaluateLinkPolicy,
  identityReducer,
  shortUuid,
  type IdentityState,
  type OrganizationSummary,
  type PrototypeUser,
} from "../../lib/identity-machine";

type IdentityJourneyProps = Readonly<{
  user: PrototypeUser;
  organizations: readonly OrganizationSummary[];
}>;

const initialState: IdentityState = { status: "signed-out" };

function roleLabel(role: OrganizationSummary["role"]): string {
  return role === "OWNER" ? "Titular" : "Asesora";
}

function organizationFor(state: IdentityState): OrganizationSummary | undefined {
  switch (state.status) {
    case "one-organization":
    case "active":
    case "linking":
    case "linking-conflict":
      return state.organization;
    case "signed-out":
    case "loading":
    case "zero-organizations":
    case "many-organizations":
    case "provider-down":
    case "recovery-accepted":
    case "rate-limited":
    case "revoked":
      return undefined;
  }
}

function StateHeader({ eyebrow, title, description }: Readonly<{
  eyebrow: string;
  title: string;
  description: string;
}>) {
  return (
    <header className={styles.stateHeader}>
      <p className={styles.eyebrow}>{eyebrow}</p>
      <h1 tabIndex={-1} data-state-heading>{title}</h1>
      <p className={styles.lead}>{description}</p>
    </header>
  );
}

function OrganizationCard({ organization, action }: Readonly<{
  organization: OrganizationSummary;
  action: ReactNode;
}>) {
  return (
    <article className={styles.organizationCard}>
      <div>
        <p className={styles.cardMeta}>{roleLabel(organization.role)} · {organization.locality}</p>
        <h2>{organization.name}</h2>
        <p className={styles.shortId}>Organización {shortUuid(organization.id)}</p>
      </div>
      {action}
    </article>
  );
}

export function IdentityJourney({ user, organizations }: IdentityJourneyProps) {
  const [state, dispatch] = useReducer(identityReducer, initialState);
  const regionRef = useRef<HTMLDivElement>(null);
  const activeOrganization = organizationFor(state);

  useEffect(() => {
    const heading = regionRef.current?.querySelector<HTMLElement>("[data-state-heading]");
    heading?.focus();
  }, [state.status]);

  function authenticatedWith(count: number): void {
    dispatch({ type: "AUTHENTICATED", user, organizations: organizations.slice(0, count) });
  }

  function renderState(): ReactNode {
    switch (state.status) {
      case "signed-out":
        return (
          <>
            <StateHeader
              eyebrow="Acceso seguro"
              title="Volvé al campo con tu contexto a la vista."
              description="Este prototipo prueba cómo entrar sin confundir identidad personal con empresa, establecimiento o asesoría."
            />
            <div className={styles.actionStack}>
              <button className={styles.primaryButton} onClick={() => dispatch({ type: "START_LOGIN" })}>
                Continuar con identidad digital
              </button>
              <button className={styles.textButton} onClick={() => dispatch({ type: "RECOVERY_REQUESTED" })}>
                No puedo acceder a mi cuenta
              </button>
              <button className={styles.demoLink} onClick={() => dispatch({ type: "RATE_LIMIT_REACHED", retryAfterMinutes: 12 })}>
                Ver límite de intentos
              </button>
            </div>
            <p className={styles.privacyNote}>La dirección de email orienta el acceso, pero nunca vincula cuentas automáticamente.</p>
          </>
        );
      case "loading":
        return (
          <>
            <StateHeader
              eyebrow="Verificando acceso"
              title="Estamos consultando tu identidad."
              description="En el producto real, este paso espera al proveedor. Elegí un resultado para recorrer el prototipo."
            />
            <div className={styles.progress} role="status"><span /> Validación en curso</div>
            <fieldset className={styles.scenarioPicker}>
              <legend>Resultado simulado</legend>
              <button onClick={() => authenticatedWith(0)}>Sin organizaciones</button>
              <button onClick={() => authenticatedWith(1)}>Una organización</button>
              <button onClick={() => authenticatedWith(organizations.length)}>Varias organizaciones</button>
              <button onClick={() => dispatch({ type: "PROVIDER_FAILED" })}>Proveedor no disponible</button>
            </fieldset>
          </>
        );
      case "zero-organizations":
        return (
          <>
            <StateHeader
              eyebrow="Identidad confirmada"
              title="Todavía no tenés una organización habilitada."
              description="Tu cuenta existe, pero no puede ver información productiva hasta recibir una invitación o alta autorizada."
            />
            <div className={styles.notice}><strong>{state.user.name}</strong><span>Acceso a datos: no habilitado</span></div>
            <button className={styles.secondaryButton} onClick={() => dispatch({ type: "RESET" })}>Volver al acceso</button>
          </>
        );
      case "one-organization":
        return (
          <>
            <StateHeader
              eyebrow="Confirmá el contexto"
              title="Encontramos una organización."
              description="Antes de mostrar información, confirmá dónde vas a trabajar."
            />
            <OrganizationCard
              organization={state.organization}
              action={<button className={styles.primaryButton} onClick={() => dispatch({ type: "ACTIVATE_ORGANIZATION", organizationId: state.organization.id })}>Entrar a esta organización</button>}
            />
          </>
        );
      case "many-organizations":
        return (
          <>
            <StateHeader
              eyebrow="Elegí una organización"
              title="¿En qué contexto vas a trabajar?"
              description="Cada empresa se abre por separado. El rol de asesora no mezcla datos entre clientes."
            />
            <div className={styles.organizationList}>
              {state.organizations.map((organization) => (
                <OrganizationCard
                  key={organization.id}
                  organization={organization}
                  action={<button className={styles.secondaryButton} onClick={() => dispatch({ type: "ACTIVATE_ORGANIZATION", organizationId: organization.id })}>Elegir {organization.name}</button>}
                />
              ))}
            </div>
          </>
        );
      case "active":
        return (
          <>
            <StateHeader
              eyebrow="Sesión activa"
              title={`Estás trabajando en ${state.organization.name}.`}
              description="El contexto activo acompaña cada acción y debe volver a validarse del lado del servidor."
            />
            <div className={styles.contextCard}>
              <span className={styles.signal}>Contexto confirmado</span>
              <dl>
                <div><dt>Organización</dt><dd>{state.organization.name}</dd></div>
                <div><dt>Rol</dt><dd>{roleLabel(state.organization.role)}</dd></div>
                <div><dt>ID visible</dt><dd>{shortUuid(state.organization.id)}</dd></div>
              </dl>
            </div>
            <div className={styles.buttonRow}>
              {state.organizations.length > 1 ? <button className={styles.secondaryButton} onClick={() => dispatch({ type: "OPEN_ORGANIZATION_PICKER" })}>Cambiar organización</button> : null}
              <button className={styles.secondaryButton} onClick={() => dispatch({ type: "START_LINKING" })}>Vincular otro acceso</button>
              <button className={styles.dangerButton} onClick={() => dispatch({ type: "SESSION_REVOKED" })}>Simular acceso revocado</button>
            </div>
          </>
        );
      case "provider-down":
        return (
          <>
            <StateHeader
              eyebrow="Servicio temporalmente no disponible"
              title="No pudimos verificar tu identidad."
              description="No abrimos una sesión degradada ni exponemos datos. Tus cambios previos no se ven afectados."
            />
            <div className={styles.alert} role="alert">El proveedor de identidad no respondió. Reintentá en unos minutos.</div>
            <button className={styles.primaryButton} onClick={() => dispatch({ type: "START_LOGIN" })}>Reintentar verificación</button>
          </>
        );
      case "linking": {
        const decision = evaluateLinkPolicy({
          accountOwnedByAnotherIdentity: false,
          primaryReauthenticated: state.primaryReauthenticated,
          secondaryReauthenticated: state.secondaryReauthenticated,
          emailsMatch: true,
        });
        const canConfirm = decision === "requires-explicit-confirmation";
        return (
          <>
            <StateHeader
              eyebrow="Vinculación protegida"
              title="Confirmá ambos accesos."
              description="Aunque los emails coincidan, la unión requiere autenticación reciente de cada identidad y una confirmación explícita."
            />
            <ol className={styles.checklist}>
              <li data-complete={state.primaryReauthenticated}><span>1</span><div><strong>Acceso actual</strong><p>{state.primaryReauthenticated ? "Reautenticado" : "Pendiente"}</p></div><button disabled={state.primaryReauthenticated} onClick={() => dispatch({ type: "PRIMARY_REAUTHENTICATED" })}>Verificar</button></li>
              <li data-complete={state.secondaryReauthenticated}><span>2</span><div><strong>Segundo acceso</strong><p>{state.secondaryReauthenticated ? "Reautenticado" : "Pendiente"}</p></div><button disabled={state.secondaryReauthenticated} onClick={() => dispatch({ type: "SECONDARY_REAUTHENTICATED" })}>Verificar</button></li>
            </ol>
            <div className={styles.buttonRow}>
              <button className={styles.primaryButton} disabled={!canConfirm} onClick={() => dispatch({ type: "LINKING_CONFIRMED" })}>Confirmar vinculación</button>
              <button className={styles.secondaryButton} onClick={() => dispatch({ type: "LINKING_CONFLICT" })}>Simular conflicto</button>
              <button className={styles.textButton} onClick={() => dispatch({ type: "CANCEL_LINKING" })}>Cancelar</button>
            </div>
          </>
        );
      }
      case "linking-conflict":
        return (
          <>
            <StateHeader
              eyebrow="Revisión necesaria"
              title="Ese acceso ya pertenece a otra identidad."
              description="La vinculación se detuvo para no mezclar personas ni organizaciones. Ninguna cuenta fue modificada."
            />
            <div className={styles.alert} role="alert">Un equipo autorizado deberá revisar titularidad y trazabilidad.</div>
            <button className={styles.primaryButton} onClick={() => dispatch({ type: "CANCEL_LINKING" })}>Volver a mi organización</button>
          </>
        );
      case "recovery-accepted":
        return (
          <>
            <StateHeader
              eyebrow="Solicitud recibida"
              title="Revisá tus medios de contacto."
              description="Si existe una cuenta elegible, enviaremos los próximos pasos. Esta respuesta no confirma si el usuario está registrado."
            />
            <button className={styles.primaryButton} onClick={() => dispatch({ type: "RESET" })}>Volver al acceso</button>
          </>
        );
      case "rate-limited":
        return (
          <>
            <StateHeader
              eyebrow="Protección de acceso"
              title="Pausamos los intentos por unos minutos."
              description={`Podés volver a intentar en ${state.retryAfterMinutes} minutos. No compartas códigos ni contraseñas con nadie.`}
            />
            <button className={styles.secondaryButton} onClick={() => dispatch({ type: "RESET" })}>Entendido</button>
          </>
        );
      case "revoked":
        return (
          <>
            <StateHeader
              eyebrow="Acceso finalizado"
              title="Tu permiso cambió y cerramos la sesión."
              description="Para proteger la información de la organización no conservamos un contexto anterior."
            />
            <div className={styles.alert} role="alert">{state.reason === "invalid-organization" ? "La organización solicitada no pertenece a tus accesos." : "Tu acceso fue revocado por un administrador."}</div>
            <button className={styles.primaryButton} onClick={() => dispatch({ type: "RESET" })}>Volver al acceso</button>
          </>
        );
    }
  }

  return (
    <div className={styles.prototypeShell}>
      <main className={styles.journeyPanel} ref={regionRef} aria-live="polite">
        {renderState()}
      </main>
      <aside className={styles.contextRail} aria-label="Contexto de acceso actual">
        <div className={styles.brandMark} aria-hidden="true">A</div>
        <div className={styles.railHeading}>
          <p>AgropecuarIA</p>
          <span>Prototipo de identidad · sin datos reales</span>
        </div>
        <dl className={styles.railFacts}>
          <div><dt>Estado</dt><dd>{state.status}</dd></div>
          <div><dt>Organización activa</dt><dd>{activeOrganization?.name ?? "Sin contexto"}</dd></div>
          <div><dt>ID</dt><dd>{activeOrganization === undefined ? "—" : shortUuid(activeOrganization.id)}</dd></div>
          <div><dt>Acceso justo a tiempo</dt><dd>Desactivado</dd></div>
        </dl>
        <p className={styles.railFootnote}>El contexto se muestra completo para la persona; los identificadores técnicos se abrevian.</p>
      </aside>
    </div>
  );
}
