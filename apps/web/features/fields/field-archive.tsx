"use client";

import {
  useCallback,
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  useState,
} from "react";
import type { RefObject } from "react";

import { formatShortId } from "../../lib/format-id";
import {
  archiveField,
  createFieldIdempotencyKey,
  FieldApiError,
  getField,
} from "./field-api";
import type { ArchiveFieldRequest, FieldFailureKind } from "./field-api";
import type { FieldSummary } from "./field-types";

type ArchiveAttempt = Omit<ArchiveFieldRequest, "signal">;
type ArchiveState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{
      kind: "confirming" | "submitting" | "reloading";
      attempt: ArchiveAttempt;
    }>
  | Readonly<{
      kind: "error";
      attempt: ArchiveAttempt;
      failure: FieldFailureKind;
      message: string;
    }>
  | Readonly<{ kind: "complete"; field: FieldSummary; message: string }>;

const STORAGE_KEY = "agropecuaria.fields.archive-attempt.v1";
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function readAttempt(): ArchiveAttempt | null {
  try {
    const serialized = globalThis.sessionStorage.getItem(STORAGE_KEY);
    if (serialized === null) return null;
    const value: unknown = JSON.parse(serialized);
    if (
      typeof value === "object" &&
      value !== null &&
      "organizationId" in value &&
      typeof value.organizationId === "string" &&
      UUID.test(value.organizationId) &&
      "fieldId" in value &&
      typeof value.fieldId === "string" &&
      UUID.test(value.fieldId) &&
      "version" in value &&
      typeof value.version === "string" &&
      UUID.test(value.version) &&
      "idempotencyKey" in value &&
      typeof value.idempotencyKey === "string" &&
      /^[A-Za-z0-9_-]{32,128}$/.test(value.idempotencyKey)
    ) {
      return {
        organizationId: value.organizationId,
        fieldId: value.fieldId,
        version: value.version,
        idempotencyKey: value.idempotencyKey,
      };
    }
  } catch {
    // Storage is optional; no malformed operation is ever replayed.
  }
  return null;
}

function persistAttempt(attempt: ArchiveAttempt | null): void {
  try {
    if (attempt === null) globalThis.sessionStorage.removeItem(STORAGE_KEY);
    else
      globalThis.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(attempt));
  } catch {
    // Keep the immutable in-memory attempt when storage is blocked.
  }
}

function terminal(failure: FieldFailureKind): boolean {
  return (
    failure === "stale" ||
    failure === "conflict" ||
    failure === "validation" ||
    failure === "unavailable"
  );
}

function failureMessage(failure: FieldFailureKind): string {
  if (failure === "stale")
    return "El campo cambió. Recargá su estado y revisá antes de confirmar otro archivado.";
  if (failure === "conflict")
    return "Este intento no coincide con el registrado. Recargá el campo antes de iniciar otro.";
  if (failure === "unavailable")
    return "El campo ya no está disponible para esta cuenta.";
  if (failure === "validation")
    return "El servidor rechazó el archivado. Revisá el campo antes de volver a intentarlo.";
  if (failure === "signed-out" || failure === "reauthentication-required")
    return "Ingresá nuevamente para consultar el resultado o reintentar el mismo archivado.";
  return "No confirmamos el resultado del archivado. Conservamos el mismo intento; consultá su estado o reintentá sin cambiarlo.";
}

export function useFieldArchive(organizationIdsKey: string) {
  const [state, setState] = useState<ArchiveState>({ kind: "idle" });
  const [observed, setObserved] = useState<FieldSummary | null>(null);
  const generation = useRef(0);
  const inFlight = useRef(false);
  const previousOrganizations = useRef(organizationIdsKey);
  const restored = useRef(false);

  useEffect(() => {
    if (previousOrganizations.current !== organizationIdsKey) {
      previousOrganizations.current = organizationIdsKey;
      generation.current += 1;
      inFlight.current = false;
      setState({ kind: "idle" });
      setObserved(null);
      if (restored.current) persistAttempt(null);
    }
    if (restored.current || organizationIdsKey.length === 0) return;
    restored.current = true;
    const attempt = readAttempt();
    if (
      attempt !== null &&
      organizationIdsKey.split(",").includes(attempt.organizationId)
    ) {
      // Hydrate browser-only storage after mount so SSR and the first client render agree.
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setState({
        kind: "error",
        attempt,
        failure: "service-unavailable",
        message: failureMessage("service-unavailable"),
      });
    } else persistAttempt(null);
  }, [organizationIdsKey]);

  useEffect(
    () => () => {
      generation.current += 1;
    },
    [],
  );

  const start = useCallback(
    (field: FieldSummary) => {
      if (
        field.status !== "draft" ||
        (state.kind !== "idle" && state.kind !== "complete")
      )
        return;
      setObserved(null);
      setState({
        kind: "confirming",
        attempt: {
          organizationId: field.organizationId,
          fieldId: field.fieldId,
          version: field.version,
          idempotencyKey: createFieldIdempotencyKey(),
        },
      });
    },
    [state.kind],
  );

  const submit = useCallback(async () => {
    if (
      inFlight.current ||
      (state.kind !== "confirming" &&
        !(state.kind === "error" && !terminal(state.failure)))
    )
      return;
    const attempt = state.attempt;
    const requestGeneration = generation.current;
    inFlight.current = true;
    persistAttempt(attempt);
    setState({ kind: "submitting", attempt });
    try {
      const field = await archiveField(attempt);
      if (requestGeneration !== generation.current) return;
      persistAttempt(null);
      setObserved(field);
      setState({
        kind: "complete",
        field,
        message: field.isReplay
          ? "Confirmamos el archivado anterior."
          : "Campo archivado. Su historial se conserva.",
      });
    } catch (error) {
      if (requestGeneration !== generation.current) return;
      const failure = error instanceof FieldApiError ? error.kind : "error";
      if (terminal(failure)) persistAttempt(null);
      setState({
        kind: "error",
        attempt,
        failure,
        message: failureMessage(failure),
      });
    } finally {
      if (requestGeneration === generation.current) inFlight.current = false;
    }
  }, [state]);

  const reload = useCallback(async () => {
    if (state.kind !== "error" || inFlight.current) return;
    const previous = state;
    const { attempt } = previous;
    const requestGeneration = generation.current;
    inFlight.current = true;
    setState({ kind: "reloading", attempt });
    try {
      const field = await getField(attempt.organizationId, attempt.fieldId);
      if (requestGeneration !== generation.current) return;
      setObserved(field);
      if (field.status === "archived") {
        persistAttempt(null);
        setState({
          kind: "complete",
          field,
          message: "El campo está archivado. Su historial se conserva.",
        });
      } else if (terminal(previous.failure)) {
        setState({
          kind: "confirming",
          attempt: {
            ...attempt,
            version: field.version,
            idempotencyKey: createFieldIdempotencyKey(),
          },
        });
      } else {
        // A read of a draft does not prove that an in-flight write will not commit.
        setState({
          ...previous,
          message:
            "El campo todavía figura como borrador. Conservamos el mismo intento hasta confirmar su resultado.",
        });
      }
    } catch {
      if (requestGeneration === generation.current)
        setState({
          ...previous,
          message:
            "No pudimos consultar el estado. Conservamos el intento para volver a consultar.",
        });
    } finally {
      if (requestGeneration === generation.current) inFlight.current = false;
    }
  }, [state]);

  const canCancel =
    state.kind === "confirming" ||
    (state.kind === "error" && terminal(state.failure));
  const cancel = useCallback(() => {
    if (!canCancel || inFlight.current) return;
    persistAttempt(null);
    setState({ kind: "idle" });
  }, [canCancel]);
  const guard: "clear" | "dirty" | "pending" =
    state.kind === "idle" || state.kind === "complete"
      ? "clear"
      : canCancel
        ? "dirty"
        : "pending";
  return { state, observed, guard, canCancel, start, submit, reload, cancel };
}

export function FieldArchivePanel({
  controller,
  returnFocusRef,
}: Readonly<{
  controller: ReturnType<typeof useFieldArchive>;
  returnFocusRef: RefObject<HTMLButtonElement | null>;
}>) {
  const { state, canCancel } = controller;
  const titleId = useId();
  const panelRef = useRef<HTMLDivElement>(null);
  const confirmingRef = useRef<HTMLButtonElement>(null);
  const previousKind = useRef(state.kind);
  useLayoutEffect(() => {
    if (state.kind === "confirming") confirmingRef.current?.focus();
    if (state.kind === "complete" || state.kind === "error")
      panelRef.current?.focus();
    if (state.kind === "idle" && previousKind.current !== "idle")
      returnFocusRef.current?.focus();
    previousKind.current = state.kind;
  }, [state.kind, returnFocusRef]);
  if (state.kind === "idle") return null;
  if (state.kind === "complete")
    return (
      <div
        className="field-action-state field-action-state--success"
        role="status"
        ref={panelRef}
        tabIndex={-1}
      >
        <p>{state.message}</p>
      </div>
    );
  const busy = state.kind === "submitting" || state.kind === "reloading";
  const canRetry =
    state.kind === "confirming" ||
    (state.kind === "error" && !terminal(state.failure));
  return (
    <div
      aria-labelledby={titleId}
      aria-busy={busy}
      className="field-rename-form"
      role="group"
      ref={panelRef}
      tabIndex={-1}
      onKeyDown={(event) => {
        if (event.key === "Escape") {
          event.preventDefault();
          event.stopPropagation();
          controller.cancel();
        }
      }}
    >
      <h4 id={titleId}>
        Archivar campo {formatShortId(state.attempt.fieldId)}
      </h4>
      <p>
        El campo dejará de aparecer en la lista activa. No se borrará su
        historial. Esta pantalla no permite restaurarlo.
      </p>
      {state.kind === "error" ? <p role="alert">{state.message}</p> : null}
      <div className="field-rename-form__actions">
        {canRetry || state.kind === "submitting" ? (
          <button
            className="button button--primary"
            disabled={busy}
            onClick={() => void controller.submit()}
            ref={confirmingRef}
            type="button"
          >
            {state.kind === "submitting"
              ? "Archivando campo"
              : state.kind === "confirming"
                ? "Confirmar archivado"
                : "Reintentar mismo archivado"}
          </button>
        ) : null}
        {state.kind === "error" || state.kind === "reloading" ? (
          <button
            className="button button--secondary"
            disabled={busy}
            onClick={() => void controller.reload()}
            type="button"
          >
            {state.kind === "reloading"
              ? "Consultando estado"
              : state.kind === "error" && terminal(state.failure)
                ? "Recargar y revisar archivado"
                : "Consultar estado del archivado"}
          </button>
        ) : null}
        {canCancel ? (
          <button
            className="button button--quiet"
            onClick={controller.cancel}
            type="button"
          >
            Cancelar archivado
          </button>
        ) : null}
      </div>
    </div>
  );
}
