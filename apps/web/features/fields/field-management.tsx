"use client";

import {
  useCallback,
  useEffect,
  useId,
  useLayoutEffect,
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
  renameField,
} from "./field-api";

import type { FieldOrganization, FieldSummary } from "./field-types";

export type FieldContextGuard = "clear" | "dirty" | "pending";

export type FieldDeepLink =
  | Readonly<{ kind: "none" }>
  | Readonly<{ kind: "requested"; prefix: string }>
  | Readonly<{ kind: "invalid"; reason: "format" | "view" }>;

export type FieldDeepLinkIntent =
  | Readonly<{ kind: "select"; prefix: string }>
  | Readonly<{ kind: "clear" }>
  | Readonly<{
      kind: "reject";
      prefix: string;
      reason: "unknown" | "ambiguous" | "unavailable" | "signed-out";
    }>
  | Readonly<{ kind: "restore"; prefix: string }>;

export type FieldManagementProps = Readonly<{
  organizations: readonly FieldOrganization[];
  deepLink?: FieldDeepLink;
  onContextGuardChange?: (guard: FieldContextGuard) => void;
  onDeepLinkIntent?: (intent: FieldDeepLinkIntent) => boolean;
}>;

const NO_FIELD_DEEP_LINK: FieldDeepLink = { kind: "none" };
const acceptLocalFieldIntent = (): boolean => true;

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

type FieldDetailRequest = Readonly<{
  controller: AbortController;
  organizationId: string;
  fieldId: string;
  generation: number;
}>;

type FieldRenameAttempt = Readonly<{
  organizationId: string;
  fieldId: string;
  baseDisplayName: string;
  displayName: string;
  version: string;
  idempotencyKey: string;
}>;

type FieldRenameFailureKind =
  | "validation"
  | "signed-out"
  | "forbidden"
  | "unavailable"
  | "in-progress"
  | "capacity-reached"
  | "conflict"
  | "stale"
  | "rate-limited"
  | "reconciliation-required"
  | "service-unavailable"
  | "offline"
  | "error";

type FieldRenameState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{
      kind: "editing" | "submitting" | "reloading";
      attempt: FieldRenameAttempt;
    }>
  | Readonly<{
      kind: FieldRenameFailureKind;
      attempt: FieldRenameAttempt;
      message: string;
    }>
  | Readonly<{
      kind: "success";
      organizationId: string;
      field: FieldSummary;
      isReplay: boolean;
    }>;

type StoredFieldAttempt = Readonly<{
  organizationId: string;
  displayName: string;
  idempotencyKey: string;
  reason: "pending" | "reauthentication" | "unavailable";
}>;

type StoredFieldRenameAttempt = FieldRenameAttempt &
  Readonly<{
    reason: "pending" | "reauthentication" | "unavailable";
  }>;

const FIELD_ATTEMPT_STORAGE_KEY = "agropecuaria.fields.create-attempt.v1";
const FIELD_RENAME_ATTEMPT_STORAGE_KEY =
  "agropecuaria.fields.rename-attempt.v1";
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

function isStoredFieldRenameAttempt(
  value: unknown,
): value is StoredFieldRenameAttempt {
  return (
    typeof value === "object" &&
    value !== null &&
    "organizationId" in value &&
    typeof value.organizationId === "string" &&
    UUID_PATTERN.test(value.organizationId) &&
    "fieldId" in value &&
    typeof value.fieldId === "string" &&
    UUID_PATTERN.test(value.fieldId) &&
    "baseDisplayName" in value &&
    typeof value.baseDisplayName === "string" &&
    normalizeFieldDisplayName(value.baseDisplayName) ===
      value.baseDisplayName &&
    "displayName" in value &&
    typeof value.displayName === "string" &&
    normalizeFieldDisplayName(value.displayName) === value.displayName &&
    "version" in value &&
    typeof value.version === "string" &&
    UUID_PATTERN.test(value.version) &&
    "idempotencyKey" in value &&
    typeof value.idempotencyKey === "string" &&
    /^[A-Za-z0-9_-]{32,128}$/.test(value.idempotencyKey) &&
    "reason" in value &&
    (value.reason === "pending" ||
      value.reason === "reauthentication" ||
      value.reason === "unavailable")
  );
}

function readStoredRenameAttempt(): StoredFieldRenameAttempt | null {
  try {
    const serialized = globalThis.sessionStorage.getItem(
      FIELD_RENAME_ATTEMPT_STORAGE_KEY,
    );
    if (serialized === null) return null;
    const value: unknown = JSON.parse(serialized);
    if (isStoredFieldRenameAttempt(value)) return value;
    globalThis.sessionStorage.removeItem(FIELD_RENAME_ATTEMPT_STORAGE_KEY);
  } catch {
    // Browser storage is optional; malformed state never crosses the boundary.
  }
  return null;
}

function storeRenameAttempt(attempt: StoredFieldRenameAttempt): void {
  try {
    globalThis.sessionStorage.setItem(
      FIELD_RENAME_ATTEMPT_STORAGE_KEY,
      JSON.stringify(attempt),
    );
  } catch {
    // The in-memory state still preserves the attempt for this page lifetime.
  }
}

function clearStoredRenameAttempt(): void {
  try {
    globalThis.sessionStorage.removeItem(FIELD_RENAME_ATTEMPT_STORAGE_KEY);
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
      case "stale":
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
      case "stale":
        return {
          kind: "conflict",
          organizationId,
          message: "El estado cambió. Iniciá un intento nuevo.",
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

function renameFailure(
  error: unknown,
  attempt: FieldRenameAttempt,
): Exclude<
  FieldRenameState,
  { kind: "idle" | "editing" | "submitting" | "reloading" | "success" }
> {
  if (error instanceof FieldApiError) {
    switch (error.kind) {
      case "validation":
        return {
          kind: "validation",
          attempt,
          message: "Ingresá un nombre válido y distinto del actual.",
        };
      case "signed-out":
        return {
          kind: "signed-out",
          attempt,
          message:
            "Tu sesión venció. Conservamos el nombre para retomarlo al ingresar.",
        };
      case "reauthentication-required":
        return {
          kind: "forbidden",
          attempt,
          message:
            "Verificá nuevamente tu acceso. Conservamos el nombre y la clave del intento.",
        };
      case "unavailable":
        return {
          kind: "unavailable",
          attempt,
          message: "El campo no está disponible o ya no tenés acceso.",
        };
      case "in-progress":
        return {
          kind: "in-progress",
          attempt,
          message:
            "El cambio sigue en curso. Reintentá con el mismo nombre y clave.",
        };
      case "capacity-reached":
        return {
          kind: "capacity-reached",
          attempt,
          message: "No pudimos modificar este campo.",
        };
      case "conflict":
        return {
          kind: "conflict",
          attempt,
          message:
            "La clave ya corresponde a otro cambio. Revisá el nombre e intentá nuevamente.",
        };
      case "stale":
        return {
          kind: "stale",
          attempt,
          message:
            "El campo cambió en otra sesión. Recargá y revisá antes de guardar.",
        };
      case "rate-limited":
        return {
          kind: "rate-limited",
          attempt,
          message:
            "Hay demasiadas solicitudes. Esperá y reintentá el mismo cambio.",
        };
      case "reconciliation-required":
        return {
          kind: "reconciliation-required",
          attempt,
          message:
            "Todavía no podemos confirmar el cambio. Conservamos el intento para reconciliarlo.",
        };
      case "service-unavailable":
        return {
          kind: "service-unavailable",
          attempt,
          message:
            "Campos no está disponible. Conservamos el intento para reintentar sin duplicarlo.",
        };
      case "error":
        return {
          kind: "error",
          attempt,
          message:
            "No pudimos confirmar el cambio. Reintentá para comprobar el resultado.",
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
      attempt,
      message:
        "La red no respondió. Conservamos el nombre para reintentar el mismo cambio.",
    };
  }
  return {
    kind: "error",
    attempt,
    message:
      "No pudimos confirmar el cambio. Reintentá para comprobar el resultado.",
  };
}

function isTerminalRenameFailure(kind: FieldRenameFailureKind): boolean {
  return (
    kind === "validation" ||
    kind === "unavailable" ||
    kind === "capacity-reached" ||
    kind === "conflict" ||
    kind === "stale"
  );
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

function deepLinkKey(deepLink: FieldDeepLink): string {
  switch (deepLink.kind) {
    case "none":
      return "none";
    case "requested":
      return `requested:${deepLink.prefix}`;
    case "invalid":
      return `invalid:${deepLink.reason}`;
  }
}

function invalidDeepLinkMessage(deepLink: FieldDeepLink): string | null {
  if (deepLink.kind !== "invalid") return null;
  return deepLink.reason === "view"
    ? "Abrí el enlace del campo desde la vista Campos. Conservamos la organización activa."
    : "El código corto del campo no es válido. Mostramos la lista sin consultar una ficha.";
}

function FieldSpinner() {
  return <span aria-hidden="true" className="spinner" />;
}

export function FieldManagement(props: FieldManagementProps) {
  const {
    organizations,
    deepLink = NO_FIELD_DEEP_LINK,
    onContextGuardChange,
  } = props;
  const deepLinkControlled = props.onDeepLinkIntent !== undefined;
  const onDeepLinkIntent = props.onDeepLinkIntent ?? acceptLocalFieldIntent;
  const titleId = useId();
  const formHelpId = useId();
  const domId = useId();
  const ownerOrganizations = useMemo(
    () =>
      organizations.filter(
        (organization) => organization.role.toLowerCase() === "owner",
      ),
    [organizations],
  );
  const ownerOrganizationIdsKey = ownerOrganizations
    .map((organization) => organization.organizationId)
    .join(",");
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
  const [rename, setRename] = useState<FieldRenameState>({ kind: "idle" });
  const attemptKey = useRef<string | null>(null);
  const restoredAttempt = useRef(false);
  const restoredRenameAttempt = useRef(false);
  const restoreRenameFocusToFieldId = useRef<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const renameInputRef = useRef<HTMLInputElement>(null);
  const detailRef = useRef<HTMLElement>(null);
  const detailRequestRef = useRef<FieldDetailRequest | null>(null);
  const detailRequestGenerationRef = useRef(0);
  const detailOrganizationKeyRef = useRef(ownerOrganizationIdsKey);
  const focusedDetailIdRef = useRef<string | null>(null);
  const rejectedDeepLinkRef = useRef<string | null>(null);
  const previousDeepLinkKeyRef = useRef(deepLinkKey(deepLink));
  const restoringDeepLinkPrefixRef = useRef<string | null>(null);
  const closedDeepLinkPrefixRef = useRef<string | null>(null);
  const restoreDetailFocusToFieldIdRef = useRef<string | null>(null);
  const restoreDetailFocusElementRef = useRef<HTMLButtonElement | null>(null);
  const detailTriggerRefs = useRef(new Map<string, HTMLButtonElement>());
  const renameTriggerRefs = useRef(new Map<string, HTMLButtonElement>());
  const [deepLinkNotice, setDeepLinkNotice] = useState<string | null>(null);

  const contextGuard: FieldContextGuard = useMemo(() => {
    const creationPending =
      creation.kind === "submitting" ||
      creation.kind === "in-progress" ||
      creation.kind === "reconciliation-required" ||
      creation.kind === "service-unavailable";
    const renamePending =
      rename.kind === "submitting" ||
      rename.kind === "reloading" ||
      rename.kind === "in-progress" ||
      rename.kind === "reconciliation-required" ||
      rename.kind === "service-unavailable";
    if (creationPending || renamePending) return "pending";

    const createDraftOpen =
      formOrganizationId !== null && draft.trim().length > 0;
    const renameDraftChanged =
      "attempt" in rename &&
      rename.attempt.displayName !== rename.attempt.baseDisplayName;
    return createDraftOpen || renameDraftChanged ? "dirty" : "clear";
  }, [creation.kind, draft, formOrganizationId, rename]);

  useEffect(() => {
    onContextGuardChange?.(contextGuard);
    return () => onContextGuardChange?.("clear");
  }, [contextGuard, onContextGuardChange]);

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

  const applyField = useCallback((field: FieldSummary) => {
    setResources((current) => {
      const previous = current[field.organizationId];
      if (previous?.kind !== "ready") return current;
      return {
        ...current,
        [field.organizationId]: {
          kind: "ready",
          items: previous.items.map((item) =>
            item.fieldId === field.fieldId ? field : item,
          ),
        },
      };
    });
    setDetails((current) => ({
      ...current,
      [field.organizationId]: { kind: "ready", field },
    }));
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    for (const organizationId of ownerOrganizationIdsKey.split(",")) {
      if (organizationId.length > 0) {
        void refreshFields(organizationId, controller.signal);
      }
    }
    return () => controller.abort();
  }, [ownerOrganizationIdsKey, refreshFields]);

  useEffect(() => {
    if (restoredAttempt.current || ownerOrganizations.length === 0) return;
    restoredAttempt.current = true;
    const stored = readStoredAttempt();
    if (stored === null) {
      clearStoredAttempt();
      return;
    }
    if (
      !ownerOrganizations.some(
        (organization) => organization.organizationId === stored.organizationId,
      )
    ) {
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

  useLayoutEffect(() => {
    if (detailOrganizationKeyRef.current === ownerOrganizationIdsKey) return;
    detailOrganizationKeyRef.current = ownerOrganizationIdsKey;
    detailRequestGenerationRef.current += 1;
    detailRequestRef.current?.controller.abort();
    detailRequestRef.current = null;
    focusedDetailIdRef.current = null;
  }, [ownerOrganizationIdsKey]);

  const openDetail = useCallback(
    async (organizationId: string, fieldId: string) => {
      detailRequestRef.current?.controller.abort();
      const controller = new AbortController();
      const request: FieldDetailRequest = {
        controller,
        organizationId,
        fieldId,
        generation: detailRequestGenerationRef.current + 1,
      };
      detailRequestGenerationRef.current = request.generation;
      detailRequestRef.current = request;
      const isCurrentRequest = (): boolean => {
        const current = detailRequestRef.current;
        return (
          current !== null &&
          current.generation === request.generation &&
          current.organizationId === request.organizationId &&
          current.fieldId === request.fieldId
        );
      };
      setDetails((current) => ({
        ...current,
        [organizationId]: { kind: "loading", fieldId },
      }));
      try {
        const field = await getField(
          organizationId,
          fieldId,
          controller.signal,
        );
        if (!isCurrentRequest()) return;
        setDetails((current) => ({
          ...current,
          [organizationId]: { kind: "ready", field },
        }));
      } catch (error) {
        if (isAbortError(error) || !isCurrentRequest()) {
          return;
        }
        setDetails((current) => ({
          ...current,
          [organizationId]: {
            kind: "error",
            fieldId,
            message: detailFailure(error),
          },
        }));
        if (
          deepLink.kind === "requested" &&
          error instanceof FieldApiError &&
          (error.kind === "unavailable" || error.kind === "signed-out")
        ) {
          setDeepLinkNotice(detailFailure(error));
          onDeepLinkIntent({
            kind: "reject",
            prefix: deepLink.prefix,
            reason: error.kind === "signed-out" ? "signed-out" : "unavailable",
          });
        }
      } finally {
        if (isCurrentRequest()) {
          detailRequestRef.current = null;
        }
      }
    },
    [deepLink, onDeepLinkIntent],
  );

  useEffect(
    () => () => {
      detailRequestRef.current?.controller.abort();
      detailRequestGenerationRef.current += 1;
      detailRequestRef.current = null;
    },
    [],
  );

  useEffect(() => {
    if (restoredRenameAttempt.current || ownerOrganizations.length === 0) {
      return;
    }
    restoredRenameAttempt.current = true;
    const stored = readStoredRenameAttempt();
    if (stored === null) {
      clearStoredRenameAttempt();
      return;
    }
    if (
      !ownerOrganizations.some(
        (organization) => organization.organizationId === stored.organizationId,
      )
    ) {
      return;
    }

    const attempt: FieldRenameAttempt = {
      organizationId: stored.organizationId,
      fieldId: stored.fieldId,
      baseDisplayName: stored.baseDisplayName,
      displayName: stored.displayName,
      version: stored.version,
      idempotencyKey: stored.idempotencyKey,
    };
    setRename({
      kind:
        stored.reason === "reauthentication"
          ? "forbidden"
          : "service-unavailable",
      attempt,
      message:
        "Retomamos el cambio pendiente. Reintentá para confirmar el resultado sin repetirlo.",
    });
    const restoredPrefix = formatShortId(stored.fieldId);
    restoringDeepLinkPrefixRef.current = restoredPrefix;
    onDeepLinkIntent({ kind: "restore", prefix: restoredPrefix });
    void openDetail(stored.organizationId, stored.fieldId);
  }, [onDeepLinkIntent, openDetail, ownerOrganizations]);

  useEffect(() => {
    if ("attempt" in rename) {
      if (renameInputRef.current !== document.activeElement) {
        renameInputRef.current?.focus();
      }
      return;
    }
    if (
      rename.kind === "idle" &&
      restoreRenameFocusToFieldId.current !== null
    ) {
      const fieldId = restoreRenameFocusToFieldId.current;
      restoreRenameFocusToFieldId.current = null;
      renameTriggerRefs.current.get(fieldId)?.focus();
    }
  }, [details, rename]);

  const startRename = useCallback((field: FieldSummary) => {
    clearStoredRenameAttempt();
    setRename({
      kind: "editing",
      attempt: {
        organizationId: field.organizationId,
        fieldId: field.fieldId,
        baseDisplayName: field.displayName,
        displayName: field.displayName,
        version: field.version,
        idempotencyKey: createFieldIdempotencyKey(),
      },
    });
  }, []);

  const cancelRename = useCallback(() => {
    if (rename.kind === "submitting" || rename.kind === "reloading") return;
    clearStoredRenameAttempt();
    if ("attempt" in rename) {
      restoreRenameFocusToFieldId.current = rename.attempt.fieldId;
    }
    setRename({ kind: "idle" });
  }, [rename]);

  const changeRenameDraft = useCallback((value: string) => {
    setRename((current) => {
      if (!("attempt" in current)) return current;
      const newIntent = current.kind !== "editing";
      if (newIntent) clearStoredRenameAttempt();
      return {
        kind: "editing",
        attempt: {
          ...current.attempt,
          displayName: value,
          idempotencyKey: newIntent
            ? createFieldIdempotencyKey()
            : current.attempt.idempotencyKey,
        },
      };
    });
  }, []);

  const submitRename = useCallback(async () => {
    if (
      !("attempt" in rename) ||
      rename.kind === "submitting" ||
      rename.kind === "reloading" ||
      rename.kind === "stale"
    ) {
      return;
    }
    const displayName = normalizeFieldDisplayName(rename.attempt.displayName);
    if (
      displayName === null ||
      displayName === rename.attempt.baseDisplayName
    ) {
      clearStoredRenameAttempt();
      setRename({
        kind: "validation",
        attempt: {
          ...rename.attempt,
          displayName: displayName ?? rename.attempt.displayName,
          idempotencyKey: createFieldIdempotencyKey(),
        },
        message: "Ingresá un nombre válido y distinto del actual.",
      });
      return;
    }

    const attempt: FieldRenameAttempt = {
      ...rename.attempt,
      displayName,
    };
    storeRenameAttempt({ ...attempt, reason: "pending" });
    setRename({ kind: "submitting", attempt });

    try {
      const renamed = await renameField({
        organizationId: attempt.organizationId,
        fieldId: attempt.fieldId,
        displayName: attempt.displayName,
        version: attempt.version,
        idempotencyKey: attempt.idempotencyKey,
      });
      clearStoredRenameAttempt();
      applyField(renamed);
      setRename({
        kind: "success",
        organizationId: attempt.organizationId,
        field: renamed,
        isReplay: renamed.isReplay,
      });
    } catch (error) {
      const failure = renameFailure(error, attempt);
      if (failure.kind === "signed-out" || failure.kind === "forbidden") {
        storeRenameAttempt({ ...attempt, reason: "reauthentication" });
      } else if (
        failure.kind === "reconciliation-required" ||
        failure.kind === "service-unavailable"
      ) {
        storeRenameAttempt({ ...attempt, reason: "unavailable" });
      }

      if (isTerminalRenameFailure(failure.kind)) {
        clearStoredRenameAttempt();
        setRename({
          ...failure,
          attempt: {
            ...attempt,
            idempotencyKey: createFieldIdempotencyKey(),
          },
        });
      } else {
        setRename(failure);
      }
    }
  }, [applyField, rename]);

  const reloadRename = useCallback(async () => {
    if (!("attempt" in rename) || rename.kind !== "stale") return;
    const staleAttempt = rename.attempt;
    setRename({ kind: "reloading", attempt: staleAttempt });
    try {
      const field = await getField(
        staleAttempt.organizationId,
        staleAttempt.fieldId,
      );
      applyField(field);
      clearStoredRenameAttempt();
      setRename({
        kind: "editing",
        attempt: {
          ...staleAttempt,
          baseDisplayName: field.displayName,
          version: field.version,
          idempotencyKey: createFieldIdempotencyKey(),
        },
      });
    } catch {
      setRename({
        kind: "stale",
        attempt: staleAttempt,
        message:
          "No pudimos recargar el campo. Reintentá para revisar la versión actual.",
      });
    }
  }, [applyField, rename]);

  const closeDetail = useCallback(
    (organizationId: string) => {
      const detail = details[organizationId];
      const closingFieldId =
        detail?.kind === "ready"
          ? detail.field.fieldId
          : detail?.kind === "loading" || detail?.kind === "error"
            ? detail.fieldId
            : null;
      const closingTrigger =
        closingFieldId === null
          ? null
          : (detailTriggerRefs.current.get(closingFieldId) ?? null);
      const renameForOrganization =
        ("attempt" in rename &&
          rename.attempt.organizationId === organizationId) ||
        (rename.kind === "success" && rename.organizationId === organizationId);
      if (
        renameForOrganization &&
        (rename.kind === "submitting" || rename.kind === "reloading")
      ) {
        return;
      }
      if (!onDeepLinkIntent({ kind: "clear" })) return;
      detailRequestRef.current?.controller.abort();
      detailRequestGenerationRef.current += 1;
      detailRequestRef.current = null;
      restoreDetailFocusToFieldIdRef.current = closingFieldId;
      restoreDetailFocusElementRef.current = closingTrigger;
      closedDeepLinkPrefixRef.current =
        deepLink.kind === "requested" ? deepLink.prefix : null;
      setDeepLinkNotice(null);
      if (renameForOrganization) {
        clearStoredRenameAttempt();
        setRename({ kind: "idle" });
      }
      setDetails((current) => ({
        ...current,
        [organizationId]: { kind: "closed" },
      }));
    },
    [deepLink, details, onDeepLinkIntent, rename],
  );

  const selectField = useCallback(
    (field: FieldSummary) => {
      setDeepLinkNotice(null);
      closedDeepLinkPrefixRef.current = null;
      if (!deepLinkControlled) {
        void openDetail(field.organizationId, field.fieldId);
        return;
      }
      onDeepLinkIntent({
        kind: "select",
        prefix: formatShortId(field.fieldId),
      });
    },
    [deepLinkControlled, onDeepLinkIntent, openDetail],
  );

  useEffect(() => {
    const nextKey = deepLinkKey(deepLink);
    const previousKey = previousDeepLinkKeyRef.current;
    if (nextKey === previousKey) return;
    previousDeepLinkKeyRef.current = nextKey;
    rejectedDeepLinkRef.current = null;
    closedDeepLinkPrefixRef.current = null;
    detailRequestRef.current?.controller.abort();
    detailRequestGenerationRef.current += 1;
    detailRequestRef.current = null;

    const restoringPrefix = restoringDeepLinkPrefixRef.current;
    const isRestoring =
      deepLink.kind === "requested" && deepLink.prefix === restoringPrefix;
    if (isRestoring) {
      restoringDeepLinkPrefixRef.current = null;
      return;
    }

    clearStoredAttempt();
    clearStoredRenameAttempt();
    attemptKey.current = null;
    setDraft("");
    setFormOrganizationId(null);
    setCreation({ kind: "idle" });
    setRename({ kind: "idle" });
    if (deepLink.kind !== "requested") {
      if (deepLink.kind === "none" && previousKey.startsWith("requested:")) {
        const organization =
          ownerOrganizations.length === 1 ? ownerOrganizations[0] : undefined;
        const detail =
          organization === undefined
            ? undefined
            : details[organization.organizationId];
        const fieldId =
          detail?.kind === "ready"
            ? detail.field.fieldId
            : detail?.kind === "loading" || detail?.kind === "error"
              ? detail.fieldId
              : null;
        if (fieldId !== null) {
          restoreDetailFocusToFieldIdRef.current = fieldId;
          restoreDetailFocusElementRef.current =
            detailTriggerRefs.current.get(fieldId) ?? null;
        }
      }
      focusedDetailIdRef.current = null;
      setDetails((current) => {
        const next = { ...current };
        for (const organization of ownerOrganizations) {
          next[organization.organizationId] = { kind: "closed" };
        }
        return next;
      });
    }
  }, [deepLink, details, ownerOrganizations]);

  useEffect(() => {
    if (deepLink.kind !== "requested" || contextGuard === "pending") return;
    if (closedDeepLinkPrefixRef.current === deepLink.prefix) return;
    const organization =
      ownerOrganizations.length === 1 ? ownerOrganizations[0] : undefined;
    if (organization === undefined) return;
    const resource = resources[organization.organizationId];
    if (resource?.kind === "signed-out" || resource?.kind === "unavailable") {
      const terminalListReason = resource.kind;
      const rejectionKey = `${organization.organizationId}:${deepLink.prefix}:list:${terminalListReason}`;
      if (rejectedDeepLinkRef.current === rejectionKey) return;
      rejectedDeepLinkRef.current = rejectionKey;
      setDeepLinkNotice(resource.message);
      onDeepLinkIntent({
        kind: "reject",
        prefix: deepLink.prefix,
        reason: terminalListReason,
      });
      return;
    }
    if (resource?.kind !== "ready") return;

    const matches = resource.items.filter(
      (field) => formatShortId(field.fieldId) === deepLink.prefix,
    );
    if (matches.length !== 1) {
      const reason = matches.length === 0 ? "unknown" : "ambiguous";
      const rejectionKey = `${organization.organizationId}:${deepLink.prefix}:${reason}`;
      if (rejectedDeepLinkRef.current === rejectionKey) return;
      rejectedDeepLinkRef.current = rejectionKey;
      setDeepLinkNotice(
        reason === "ambiguous"
          ? "Ese código corto coincide con más de un campo. Elegí la ficha desde la lista."
          : "Ese campo ya no está disponible en esta organización. Mostramos la lista vigente.",
      );
      onDeepLinkIntent({
        kind: "reject",
        prefix: deepLink.prefix,
        reason,
      });
      return;
    }

    rejectedDeepLinkRef.current = null;
    setDeepLinkNotice(null);
    const field = matches[0];
    if (field === undefined) return;
    const detail = details[organization.organizationId];
    const sameField =
      (detail?.kind === "loading" && detail.fieldId === field.fieldId) ||
      (detail?.kind === "ready" && detail.field.fieldId === field.fieldId) ||
      (detail?.kind === "error" && detail.fieldId === field.fieldId);
    if (!sameField) {
      void openDetail(organization.organizationId, field.fieldId);
    }
  }, [
    contextGuard,
    deepLink,
    details,
    onDeepLinkIntent,
    openDetail,
    ownerOrganizations,
    resources,
  ]);

  useEffect(() => {
    const organization =
      ownerOrganizations.length === 1 ? ownerOrganizations[0] : undefined;
    if (organization === undefined) return;
    const detail = details[organization.organizationId];
    if (detail?.kind !== "ready") return;
    if (focusedDetailIdRef.current === detail.field.fieldId) return;
    focusedDetailIdRef.current = detail.field.fieldId;
    detailRef.current?.focus();
  }, [details, ownerOrganizations]);

  useEffect(() => {
    const fieldId = restoreDetailFocusToFieldIdRef.current;
    if (fieldId === null) return;
    const detailStillOpen = Object.values(details).some(
      (detail) =>
        (detail.kind === "ready" && detail.field.fieldId === fieldId) ||
        ((detail.kind === "loading" || detail.kind === "error") &&
          detail.fieldId === fieldId),
    );
    if (detailStillOpen) return;
    const fieldListReady = Object.values(resources).some(
      (resource) =>
        resource.kind === "ready" &&
        resource.items.some((field) => field.fieldId === fieldId),
    );
    if (!fieldListReady) return;
    const rememberedTrigger = restoreDetailFocusElementRef.current;
    const trigger =
      detailTriggerRefs.current.get(fieldId) ??
      (rememberedTrigger?.isConnected === true ? rememberedTrigger : undefined);
    if (trigger === undefined) return;
    restoreDetailFocusToFieldIdRef.current = null;
    restoreDetailFocusElementRef.current = null;
    trigger.focus();
  }, [details, resources]);

  if (ownerOrganizations.length === 0) return null;

  const creationRetryLabel =
    creation.kind === "conflict" ? "Iniciar nuevo intento" : "Reintentar";
  const visibleDeepLinkNotice =
    invalidDeepLinkMessage(deepLink) ?? deepLinkNotice;

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

      {visibleDeepLinkNotice !== null ? (
        <div className="inline-notice" role="status">
          <p>{visibleDeepLinkNotice}</p>
        </div>
      ) : null}

      <div className="field-organizations">
        {ownerOrganizations.map((organization, organizationIndex) => {
          const organizationDomId = `${domId}-organization-${organizationIndex}`;
          const createNameId = `${organizationDomId}-field-name`;
          const detailTitleId = `${organizationDomId}-detail-title`;
          const renameNameId = `${organizationDomId}-rename-name`;
          const renameHelpId = `${organizationDomId}-rename-help`;
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
          const renameAttemptState =
            "attempt" in rename &&
            rename.attempt.organizationId === organization.organizationId
              ? rename
              : null;
          const renameSuccessForOrganization =
            rename.kind === "success" &&
            rename.organizationId === organization.organizationId;
          const renameBusy =
            renameAttemptState?.kind === "submitting" ||
            renameAttemptState?.kind === "reloading";

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
                    <label htmlFor={createNameId}>Nombre del campo</label>
                    <input
                      aria-describedby={formHelpId}
                      aria-invalid={
                        creationForOrganization &&
                        creation.kind === "validation"
                      }
                      disabled={submitting}
                      id={createNameId}
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
                          onClick={() => selectField(field)}
                          ref={(element) => {
                            if (element === null) {
                              detailTriggerRefs.current.delete(field.fieldId);
                            } else {
                              detailTriggerRefs.current.set(
                                field.fieldId,
                                element,
                              );
                            }
                          }}
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
                  aria-labelledby={detailTitleId}
                  className="field-detail"
                  onKeyDown={(event) => {
                    if (
                      event.key === "Escape" &&
                      !(
                        renameAttemptState !== null &&
                        renameAttemptState.attempt.fieldId ===
                          detail.field.fieldId
                      )
                    ) {
                      event.preventDefault();
                      closeDetail(organization.organizationId);
                    }
                  }}
                  ref={detailRef}
                  tabIndex={-1}
                >
                  <div className="field-detail__heading">
                    <div>
                      <p className="section-kicker">Ficha del campo</p>
                      <h4 id={detailTitleId}>{detail.field.displayName}</h4>
                      <p>Campo {formatShortId(detail.field.fieldId)}</p>
                    </div>
                    {renameAttemptState === null ||
                    renameAttemptState.attempt.fieldId !==
                      detail.field.fieldId ? (
                      <button
                        className="button button--quiet"
                        disabled={
                          rename.kind === "submitting" ||
                          rename.kind === "reloading"
                        }
                        onClick={() => startRename(detail.field)}
                        ref={(element) => {
                          if (element === null) {
                            renameTriggerRefs.current.delete(
                              detail.field.fieldId,
                            );
                          } else {
                            renameTriggerRefs.current.set(
                              detail.field.fieldId,
                              element,
                            );
                          }
                        }}
                        type="button"
                      >
                        Editar nombre
                      </button>
                    ) : null}
                  </div>

                  {renameAttemptState !== null &&
                  renameAttemptState.attempt.fieldId ===
                    detail.field.fieldId ? (
                    <form
                      aria-busy={renameBusy}
                      className="field-rename-form"
                      onKeyDown={(event) => {
                        if (event.key === "Escape" && !renameBusy) {
                          event.preventDefault();
                          event.stopPropagation();
                          cancelRename();
                        }
                      }}
                      onSubmit={(event) => {
                        event.preventDefault();
                        void submitRename();
                      }}
                    >
                      <div className="form-field">
                        <label htmlFor={renameNameId}>
                          Nuevo nombre del campo
                        </label>
                        <input
                          aria-describedby={renameHelpId}
                          aria-invalid={
                            renameAttemptState.kind === "validation"
                          }
                          disabled={
                            renameBusy || renameAttemptState.kind === "stale"
                          }
                          id={renameNameId}
                          onChange={(event) =>
                            changeRenameDraft(event.target.value)
                          }
                          ref={renameInputRef}
                          value={renameAttemptState.attempt.displayName}
                        />
                        <small id={renameHelpId}>
                          Entre 2 y 120 caracteres Unicode, sin controles.
                        </small>
                        {renameAttemptState.attempt.displayName !==
                        renameAttemptState.attempt.baseDisplayName ? (
                          <small className="field-rename-current">
                            Nombre actual:{" "}
                            {renameAttemptState.attempt.baseDisplayName}
                          </small>
                        ) : null}
                      </div>

                      {"message" in renameAttemptState ? (
                        <div
                          className={`field-action-state field-action-state--${renameAttemptState.kind}`}
                          role="alert"
                        >
                          <p>{renameAttemptState.message}</p>
                        </div>
                      ) : null}

                      <div className="field-rename-form__actions">
                        {renameAttemptState.kind === "stale" ? (
                          <button
                            className="button button--secondary"
                            onClick={() => void reloadRename()}
                            type="button"
                          >
                            Recargar y revisar
                          </button>
                        ) : (
                          <button
                            className="button button--primary"
                            disabled={renameBusy}
                            type="submit"
                          >
                            {renameAttemptState.kind === "submitting" ? (
                              <>
                                <FieldSpinner /> Guardando nombre
                              </>
                            ) : (
                              "Guardar nombre"
                            )}
                          </button>
                        )}
                        <button
                          className="button button--quiet"
                          disabled={renameBusy}
                          onClick={cancelRename}
                          type="button"
                        >
                          Cancelar
                        </button>
                      </div>
                    </form>
                  ) : null}

                  {renameSuccessForOrganization &&
                  rename.field.fieldId === detail.field.fieldId ? (
                    <div
                      className="field-action-state field-action-state--success"
                      role="status"
                    >
                      <p>
                        {rename.isReplay
                          ? `Confirmamos el cambio anterior: ahora se llama ${rename.field.displayName}.`
                          : `El campo ahora se llama ${rename.field.displayName}.`}
                      </p>
                    </div>
                  ) : null}
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
                    disabled={renameBusy}
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
