"use client";

import {
  useCallback,
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
} from "react";

import { formatShortId } from "../../lib/format-id";
import {
  createField,
  createFieldIdempotencyKey,
  FieldApiError,
  getField,
  listFields,
  normalizeFieldDisplayName,
} from "./field-api";

import type { FieldOrganization, FieldSummary } from "./field-types";

type FieldResourceState =
  | Readonly<{ kind: "loading" }>
  | Readonly<{ kind: "ready"; items: readonly FieldSummary[] }>
  | Readonly<{
      kind:
        | "offline"
        | "signed-out"
        | "forbidden"
        | "unavailable"
        | "rate-limited"
        | "service-unavailable"
        | "error";
      message: string;
    }>;

type FieldCreationState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{ kind: "submitting"; organizationId: string }>
  | Readonly<{
      kind:
        | "validation"
        | "signed-out"
        | "forbidden"
        | "unavailable"
        | "in-progress"
        | "capacity-reached"
        | "conflict"
        | "rate-limited"
        | "reconciliation-required"
        | "service-unavailable"
        | "offline"
        | "error";
      organizationId: string;
      message: string;
    }>
  | Readonly<{
      kind: "success";
      organizationId: string;
      field: FieldSummary;
      isReplay: boolean;
    }>;

type FieldDetailState =
  | Readonly<{ kind: "closed" }>
  | Readonly<{ kind: "loading"; fieldId: string }>
  | Readonly<{ kind: "ready"; field: FieldSummary }>
  | Readonly<{
      kind: "error";
      fieldId: string;
      message: string;
    }>;

type StoredFieldAttempt = Readonly<{
  organizationId: string;
  displayName: string;
  idempotencyKey: string;
  reason: "pending" | "reauthentication" | "unavailable";
}>;

const FIELD_ATTEMPT_STORAGE_KEY = "agropecuaria.fields.create-attempt.v1";
const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function isStoredFieldAttempt(value: unknown): value is StoredFieldAttempt {
  return (
    typeof value === "object" &&
    value !== null &&
    "organizationId" in value &&
    typeof value.organizationId === "string" &&
    UUID_PATTERN.test(value.organizationId) &&
    "displayName" in value &&
    typeof value.displayName === "string" &&
    normalizeFieldDisplayName(value.displayName) !== null &&
    "idempotencyKey" in value &&
    typeof value.idempotencyKey === "string" &&
    /^[A-Za-z0-9_-]{32,128}$/.test(value.idempotencyKey) &&
    "reason" in value &&
    (value.reason === "pending" ||
      value.reason === "reauthentication" ||
      value.reason === "unavailable")
  );
}

function readStoredAttempt(): StoredFieldAttempt | null {
  try {
    const serialized = globalThis.sessionStorage.getItem(
      FIELD_ATTEMPT_STORAGE_KEY,
    );
    if (serialized === null) return null;
    const value: unknown = JSON.parse(serialized);
    if (isStoredFieldAttempt(value)) return value;
    globalThis.sessionStorage.removeItem(FIELD_ATTEMPT_STORAGE_KEY);
  } catch {
    // Storage is an enhancement. A blocked or malformed store must not crash the hub.
  }
  return null;
}

function storeAttempt(attempt: StoredFieldAttempt): void {
  try {
    globalThis.sessionStorage.setItem(
      FIELD_ATTEMPT_STORAGE_KEY,
      JSON.stringify(attempt),
    );
  } catch {
    // The in-memory attempt still preserves the key for this page lifetime.
  }
}

function clearStoredAttempt(): void {
  try {
    globalThis.sessionStorage.removeItem(FIELD_ATTEMPT_STORAGE_KEY);
  } catch {
    // Nothing else is required when storage is unavailable.
  }
}

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === "AbortError";
}

function resourceFailure(error: unknown): FieldResourceState {
  if (error instanceof FieldApiError) {
    switch (error.kind) {
      case "signed-out":
        return {
          kind: "signed-out",
          message: "Tu sesión ya no está disponible. Volvé a ingresar.",
        };
      case "reauthentication-required":
        return {
          kind: "forbidden",
          message: "No pudimos confirmar tu permiso para esta organización.",
        };
      case "unavailable":
        return {
          kind: "unavailable",
          message: "La organización o sus campos no están disponibles.",
        };
      case "rate-limited":
        return {
          kind: "rate-limited",
          message: "Hay demasiadas solicitudes. Esperá un momento.",
        };
      case "service-unavailable":
      case "reconciliation-required":
        return {
          kind: "service-unavailable",
          message: "Campos no está disponible por el momento.",
        };
      case "validation":
      case "in-progress":
      case "capacity-reached":
      case "conflict":
      case "error":
        return {
          kind: "error",
          message: "No pudimos cargar los campos. Ningún dato se modificó.",
        };
      default: {
        const exhaustive: never = error.kind;
        return exhaustive;
      }
    }
  }
  if (error instanceof TypeError) {
    return {
      kind: "offline",
      message: "La red no respondió. Revisá tu conexión y reintentá.",
    };
  }
  return {
    kind: "error",
    message: "No pudimos cargar los campos. Ningún dato se modificó.",
  };
}

function creationFailure(
  error: unknown,
  organizationId: string,
): FieldCreationState {
  if (error instanceof FieldApiError) {
    switch (error.kind) {
      case "validation":
        return {
          kind: "validation",
          organizationId,
          message: "Revisá el nombre del campo antes de continuar.",
        };
      case "signed-out":
        return {
          kind: "signed-out",
          organizationId,
          message:
            "Tu sesión venció. El intento quedó guardado para retomarlo al ingresar.",
        };
      case "reauthentication-required":
        return {
          kind: "forbidden",
          organizationId,
          message:
            "Verificá nuevamente tu acceso. Conservamos este intento sin cambiar la clave.",
        };
      case "unavailable":
        return {
          kind: "unavailable",
          organizationId,
          message: "La organización no está disponible o ya no tenés acceso.",
        };
      case "in-progress":
        return {
          kind: "in-progress",
          organizationId,
          message:
            "El intento sigue en curso. Reintentá con el mismo nombre y clave.",
        };
      case "capacity-reached":
        return {
          kind: "capacity-reached",
          organizationId,
          message:
            "La organización alcanzó el máximo local de 100 campos para este incremento.",
        };
      case "conflict":
        return {
          kind: "conflict",
          organizationId,
          message:
            "La clave ya corresponde a otro contenido. Iniciá un intento nuevo.",
        };
      case "rate-limited":
        return {
          kind: "rate-limited",
          organizationId,
          message:
            "Hay demasiadas solicitudes. Esperá y reintentá el mismo intento.",
        };
      case "reconciliation-required":
        return {
          kind: "reconciliation-required",
          organizationId,
          message:
            "Todavía no podemos confirmar el resultado. Guardamos la clave para reconciliarlo.",
        };
      case "service-unavailable":
        return {
          kind: "service-unavailable",
          organizationId,
          message:
            "Campos no está disponible. El intento quedó guardado para reintentar sin duplicar.",
        };
      case "error":
        return {
          kind: "error",
          organizationId,
          message:
            "No pudimos confirmar la creación. Reintentá para comprobar el resultado.",
        };
      default: {
        const exhaustive: never = error.kind;
        return exhaustive;
      }
    }
  }
  if (error instanceof TypeError) {
    return {
      kind: "offline",
      organizationId,
      message:
        "La red no respondió. No confirmamos cambios; reintentá con el mismo nombre.",
    };
  }
  return {
    kind: "error",
    organizationId,
    message:
      "No pudimos confirmar la creación. Reintentá para comprobar el resultado.",
  };
}

function detailFailure(error: unknown): string {
  if (error instanceof FieldApiError) {
    if (error.kind === "unavailable") {
      return "El campo ya no está disponible para esta organización.";
    }
    if (error.kind === "signed-out") {
      return "Tu sesión venció. Volvé a ingresar para abrir la ficha.";
    }
  }
  if (error instanceof TypeError) {
    return "La red no respondió. Revisá tu conexión y reintentá.";
  }
  return "No pudimos abrir la ficha del campo.";
}

function FieldSpinner() {
  return <span aria-hidden="true" className="spinner" />;
}

export function FieldManagement({
  organizations,
}: Readonly<{ organizations: readonly FieldOrganization[] }>) {
  const titleId = useId();
  const formHelpId = useId();
  const ownerOrganizations = useMemo(
    () =>
      organizations.filter(
        (organization) => organization.role.toLowerCase() === "owner",
      ),
    [organizations],
  );
  const [resources, setResources] = useState<
    Readonly<Record<string, FieldResourceState>>
  >({});
  const [details, setDetails] = useState<
    Readonly<Record<string, FieldDetailState>>
  >({});
  const [formOrganizationId, setFormOrganizationId] = useState<string | null>(
    null,
  );
  const [draft, setDraft] = useState("");
  const [creation, setCreation] = useState<FieldCreationState>({
    kind: "idle",
  });
  const attemptKey = useRef<string | null>(null);
  const restoredAttempt = useRef(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const refreshFields = useCallback(
    async (organizationId: string, signal?: AbortSignal) => {
      setResources((current) => ({
        ...current,
        [organizationId]: { kind: "loading" },
      }));
      try {
        const items = await listFields(organizationId, signal);
        setResources((current) => ({
          ...current,
          [organizationId]: { kind: "ready", items },
        }));
      } catch (error) {
        if (isAbortError(error)) return;
        setResources((current) => ({
          ...current,
          [organizationId]: resourceFailure(error),
        }));
      }
    },
    [],
  );

  useEffect(() => {
    const controller = new AbortController();
    for (const organization of ownerOrganizations) {
      void refreshFields(organization.organizationId, controller.signal);
    }
    return () => controller.abort();
  }, [ownerOrganizations, refreshFields]);

  useEffect(() => {
    if (restoredAttempt.current || ownerOrganizations.length === 0) return;
    restoredAttempt.current = true;
    const stored = readStoredAttempt();
    if (
      stored === null ||
      !ownerOrganizations.some(
        (organization) => organization.organizationId === stored.organizationId,
      )
    ) {
      clearStoredAttempt();
      return;
    }
    attemptKey.current = stored.idempotencyKey;
    setFormOrganizationId(stored.organizationId);
    setDraft(stored.displayName);
    setCreation({
      kind:
        stored.reason === "reauthentication"
          ? "forbidden"
          : "service-unavailable",
      organizationId: stored.organizationId,
      message:
        "Retomamos el intento pendiente. Reintentá para confirmar el resultado sin duplicarlo.",
    });
  }, [ownerOrganizations]);

  useEffect(() => {
    if (formOrganizationId !== null) inputRef.current?.focus();
  }, [formOrganizationId]);

  const startCreate = useCallback((organizationId: string) => {
    clearStoredAttempt();
    attemptKey.current = null;
    setDraft("");
    setCreation({ kind: "idle" });
    setFormOrganizationId(organizationId);
  }, []);

  const cancelCreate = useCallback(() => {
    if (creation.kind === "submitting") return;
    clearStoredAttempt();
    attemptKey.current = null;
    setDraft("");
    setCreation({ kind: "idle" });
    setFormOrganizationId(null);
  }, [creation.kind]);

  const changeDraft = useCallback((value: string) => {
    clearStoredAttempt();
    attemptKey.current = null;
    setDraft(value);
    setCreation({ kind: "idle" });
  }, []);

  const submitCreate = useCallback(async () => {
    const organizationId = formOrganizationId;
    if (organizationId === null || creation.kind === "submitting") return;
    const displayName = normalizeFieldDisplayName(draft);
    if (displayName === null) {
      setCreation({
        kind: "validation",
        organizationId,
        message: "Ingresá entre 2 y 120 caracteres Unicode, sin controles.",
      });
      return;
    }
    const idempotencyKey = attemptKey.current ?? createFieldIdempotencyKey();
    attemptKey.current = idempotencyKey;
    storeAttempt({
      organizationId,
      displayName,
      idempotencyKey,
      reason: "pending",
    });
    setCreation({ kind: "submitting", organizationId });

    try {
      const created = await createField(
        organizationId,
        displayName,
        idempotencyKey,
      );
      clearStoredAttempt();
      attemptKey.current = null;
      setResources((current) => {
        const previous = current[organizationId];
        const items = previous?.kind === "ready" ? previous.items : [];
        const withoutCreated = items.filter(
          (field) => field.fieldId !== created.fieldId,
        );
        return {
          ...current,
          [organizationId]: {
            kind: "ready",
            items: [...withoutCreated, created],
          },
        };
      });
      setDetails((current) => ({
        ...current,
        [organizationId]: { kind: "ready", field: created },
      }));
      setDraft("");
      setFormOrganizationId(null);
      setCreation({
        kind: "success",
        organizationId,
        field: created,
        isReplay: created.isReplay,
      });
    } catch (error) {
      const failure = creationFailure(error, organizationId);
      if (failure.kind === "signed-out" || failure.kind === "forbidden") {
        storeAttempt({
          organizationId,
          displayName,
          idempotencyKey,
          reason: "reauthentication",
        });
      } else if (
        failure.kind === "reconciliation-required" ||
        failure.kind === "service-unavailable"
      ) {
        storeAttempt({
          organizationId,
          displayName,
          idempotencyKey,
          reason: "unavailable",
        });
      }
      if (
        failure.kind === "validation" ||
        failure.kind === "unavailable" ||
        failure.kind === "capacity-reached" ||
        failure.kind === "conflict"
      ) {
        clearStoredAttempt();
        attemptKey.current = null;
      }
      setCreation(failure);
    }
  }, [creation.kind, draft, formOrganizationId]);

  const openDetail = useCallback(
    async (organizationId: string, fieldId: string) => {
      setDetails((current) => ({
        ...current,
        [organizationId]: { kind: "loading", fieldId },
      }));
      try {
        const field = await getField(organizationId, fieldId);
        setDetails((current) => ({
          ...current,
          [organizationId]: { kind: "ready", field },
        }));
      } catch (error) {
        setDetails((current) => ({
          ...current,
          [organizationId]: {
            kind: "error",
            fieldId,
            message: detailFailure(error),
          },
        }));
      }
    },
    [],
  );

  const closeDetail = useCallback((organizationId: string) => {
    setDetails((current) => ({
      ...current,
      [organizationId]: { kind: "closed" },
    }));
  }, []);

  if (ownerOrganizations.length === 0) return null;

  const creationRetryLabel =
    creation.kind === "conflict" ? "Iniciar nuevo intento" : "Reintentar";

  return (
    <section
      aria-labelledby={titleId}
      className="field-management identity-card"
    >
      <div className="section-heading field-management__heading">
        <div>
          <p className="section-kicker">Unidades de manejo</p>
          <h2 id={titleId}>Tus campos</h2>
          <p>
            Creá la ficha privada ahora. El mapa, el área y la geometría se
            configuran en un paso posterior.
          </p>
        </div>
      </div>

      <div className="field-organizations">
        {ownerOrganizations.map((organization) => {
          const resource = resources[organization.organizationId] ?? {
            kind: "loading",
          };
          const detail = details[organization.organizationId] ?? {
            kind: "closed",
          };
          const formOpen = formOrganizationId === organization.organizationId;
          const creationForOrganization =
            "organizationId" in creation &&
            creation.organizationId === organization.organizationId;
          const submitting =
            creation.kind === "submitting" && creationForOrganization;

          return (
            <article
              className="field-organization"
              key={organization.organizationId}
            >
              <header className="field-organization__heading">
                <div>
                  <h3>{organization.organizationName}</h3>
                  <p>
                    Organización {formatShortId(organization.organizationId)}
                  </p>
                </div>
                {!formOpen ? (
                  <button
                    className="button button--secondary"
                    onClick={() => startCreate(organization.organizationId)}
                    type="button"
                  >
                    Crear campo
                  </button>
                ) : null}
              </header>

              {formOpen ? (
                <form
                  aria-busy={submitting}
                  className="field-form"
                  onSubmit={(event) => {
                    event.preventDefault();
                    void submitCreate();
                  }}
                >
                  <div className="form-field">
                    <label
                      htmlFor={`field-name-${organization.organizationId}`}
                    >
                      Nombre del campo
                    </label>
                    <input
                      aria-describedby={formHelpId}
                      aria-invalid={
                        creationForOrganization &&
                        creation.kind === "validation"
                      }
                      disabled={submitting}
                      id={`field-name-${organization.organizationId}`}
                      onChange={(event) => changeDraft(event.target.value)}
                      ref={inputRef}
                      value={draft}
                    />
                    <small id={formHelpId}>
                      Entre 2 y 120 caracteres Unicode, sin controles. Los
                      nombres pueden repetirse.
                    </small>
                  </div>

                  {creationForOrganization &&
                  creation.kind !== "submitting" &&
                  creation.kind !== "success" ? (
                    <div
                      className={`field-action-state field-action-state--${creation.kind}`}
                      role="alert"
                    >
                      <p>{creation.message}</p>
                    </div>
                  ) : null}

                  <div className="field-form__actions">
                    <button
                      className="button button--primary"
                      disabled={submitting}
                      type="submit"
                    >
                      {submitting ? <FieldSpinner /> : null}
                      {submitting
                        ? "Creando campo"
                        : creationForOrganization
                          ? creationRetryLabel
                          : "Crear campo"}
                    </button>
                    <button
                      className="button button--quiet"
                      disabled={submitting}
                      onClick={cancelCreate}
                      type="button"
                    >
                      Cancelar
                    </button>
                  </div>
                </form>
              ) : null}

              {creation.kind === "success" && creationForOrganization ? (
                <div
                  className="inline-notice inline-notice--success"
                  role="status"
                >
                  <p>
                    {creation.field.displayName} quedó como borrador sin
                    geometría
                    {creation.isReplay
                      ? "; confirmamos el intento anterior"
                      : ""}
                    .
                  </p>
                </div>
              ) : null}

              <div
                aria-busy={resource.kind === "loading"}
                aria-live="polite"
                className="field-resource"
              >
                {resource.kind === "loading" ? (
                  <div className="field-resource__state" role="status">
                    <FieldSpinner />
                    <span>Cargando campos</span>
                  </div>
                ) : null}
                {resource.kind !== "loading" && resource.kind !== "ready" ? (
                  <div className="field-resource__state" role="alert">
                    <span>{resource.message}</span>
                    <button
                      className="button button--quiet"
                      onClick={() =>
                        void refreshFields(organization.organizationId)
                      }
                      type="button"
                    >
                      Reintentar
                    </button>
                  </div>
                ) : null}
                {resource.kind === "ready" && resource.items.length === 0 ? (
                  <div className="field-empty" role="status">
                    <strong>Todavía no hay campos</strong>
                    <p>Creá una ficha para empezar, incluso sin geometría.</p>
                  </div>
                ) : null}
                {resource.kind === "ready" && resource.items.length > 0 ? (
                  <ul className="field-list">
                    {resource.items.map((field) => (
                      <li key={field.fieldId}>
                        <div className="field-list__copy">
                          <strong>{field.displayName}</strong>
                          <span>Campo {formatShortId(field.fieldId)}</span>
                          <div className="field-chips">
                            <span className="field-chip">Borrador</span>
                            <span className="field-chip field-chip--spatial">
                              Sin geometría
                            </span>
                          </div>
                        </div>
                        <button
                          aria-label={`Abrir ficha de ${field.displayName}, campo ${formatShortId(field.fieldId)}`}
                          className="button button--quiet"
                          onClick={() =>
                            void openDetail(
                              organization.organizationId,
                              field.fieldId,
                            )
                          }
                          type="button"
                        >
                          Abrir ficha
                        </button>
                      </li>
                    ))}
                  </ul>
                ) : null}
              </div>

              {detail.kind === "loading" ? (
                <div
                  className="field-detail field-detail--loading"
                  role="status"
                >
                  <FieldSpinner />
                  <span>Abriendo ficha</span>
                </div>
              ) : null}
              {detail.kind === "error" ? (
                <div className="field-detail field-detail--error" role="alert">
                  <p>{detail.message}</p>
                  <div className="field-detail__actions">
                    <button
                      className="button button--quiet"
                      onClick={() =>
                        void openDetail(
                          organization.organizationId,
                          detail.fieldId,
                        )
                      }
                      type="button"
                    >
                      Reintentar
                    </button>
                    <button
                      className="button button--quiet"
                      onClick={() => closeDetail(organization.organizationId)}
                      type="button"
                    >
                      Cerrar
                    </button>
                  </div>
                </div>
              ) : null}
              {detail.kind === "ready" ? (
                <section
                  aria-labelledby={`field-detail-${detail.field.fieldId}`}
                  className="field-detail"
                  onKeyDown={(event) => {
                    if (event.key === "Escape") {
                      event.preventDefault();
                      closeDetail(organization.organizationId);
                    }
                  }}
                  tabIndex={-1}
                >
                  <div>
                    <p className="section-kicker">Ficha del campo</p>
                    <h4 id={`field-detail-${detail.field.fieldId}`}>
                      {detail.field.displayName}
                    </h4>
                    <p>Campo {formatShortId(detail.field.fieldId)}</p>
                  </div>
                  <dl>
                    <div>
                      <dt>Estado</dt>
                      <dd>Borrador</dd>
                    </div>
                    <div>
                      <dt>Geometría</dt>
                      <dd>Sin geometría</dd>
                    </div>
                  </dl>
                  <button
                    className="button button--quiet"
                    onClick={() => closeDetail(organization.organizationId)}
                    type="button"
                  >
                    Cerrar ficha
                  </button>
                </section>
              ) : null}
            </article>
          );
        })}
      </div>
    </section>
  );
}
