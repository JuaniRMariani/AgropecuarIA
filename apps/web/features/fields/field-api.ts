import type { CreatedField, FieldSummary, RenamedField } from "./field-types";

export type FieldFailureKind =
  | "signed-out"
  | "reauthentication-required"
  | "unavailable"
  | "validation"
  | "in-progress"
  | "capacity-reached"
  | "conflict"
  | "stale"
  | "rate-limited"
  | "reconciliation-required"
  | "service-unavailable"
  | "error";

export class FieldApiError extends Error {
  readonly kind: FieldFailureKind;
  readonly status: number;
  readonly code: string;

  constructor(kind: FieldFailureKind, status: number, code = "") {
    super("Field request failed");
    this.name = "FieldApiError";
    this.kind = kind;
    this.status = status;
    this.code = code;
  }
}

type JsonRecord = Record<string, unknown>;

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const CONTROL_CHARACTER_PATTERN = /\p{General_Category=Control}/u;
const EDGE_WHITESPACE_PATTERN =
  /^[\p{White_Space}\uFEFF]+|[\p{White_Space}\uFEFF]+$/gu;

export function normalizeFieldDisplayName(value: string): string | null {
  const normalized = value
    .replace(EDGE_WHITESPACE_PATTERN, "")
    .normalize("NFC");
  let scalarCount = 0;
  for (const scalar of normalized) {
    const codePoint = scalar.codePointAt(0);
    if (
      codePoint === undefined ||
      (codePoint >= 0xd800 && codePoint <= 0xdfff) ||
      CONTROL_CHARACTER_PATTERN.test(scalar)
    ) {
      return null;
    }
    scalarCount += 1;
    if (scalarCount > 120) return null;
  }
  return scalarCount >= 2 ? normalized : null;
}

function isRecord(value: unknown): value is JsonRecord {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function requiredString(record: JsonRecord, key: string): string {
  const value = record[key];
  if (typeof value !== "string" || value.length === 0) {
    throw new FieldApiError("error", 502);
  }
  return value;
}

function requiredUuid(record: JsonRecord, key: string): string {
  const value = requiredString(record, key);
  if (!UUID_PATTERN.test(value)) {
    throw new FieldApiError("error", 502);
  }
  return value;
}

function requiredDateTime(record: JsonRecord, key: string): string {
  const value = requiredString(record, key);
  if (!value.includes("T") || Number.isNaN(Date.parse(value))) {
    throw new FieldApiError("error", 502);
  }
  return value;
}

function requiredBoolean(record: JsonRecord, key: string): boolean {
  const value = record[key];
  if (typeof value !== "boolean") {
    throw new FieldApiError("error", 502);
  }
  return value;
}

function requiredInteger(
  record: JsonRecord,
  key: string,
  minimum: number,
): number {
  const value = record[key];
  if (
    typeof value !== "number" ||
    !Number.isSafeInteger(value) ||
    value < minimum
  ) {
    throw new FieldApiError("error", 502);
  }
  return value;
}

function validateIdentifier(value: string): void {
  if (!UUID_PATTERN.test(value)) {
    throw new FieldApiError("validation", 400);
  }
}

export function parseFieldSummary(value: unknown): FieldSummary {
  if (
    !isRecord(value) ||
    value.type !== "field" ||
    value.status !== "draft" ||
    value.spatialStatus !== "not_configured"
  ) {
    throw new FieldApiError("error", 502);
  }

  const displayName = requiredString(value, "displayName");
  if (normalizeFieldDisplayName(displayName) !== displayName) {
    throw new FieldApiError("error", 502);
  }

  return {
    fieldId: requiredUuid(value, "fieldId"),
    organizationId: requiredUuid(value, "organizationId"),
    displayName,
    type: "field",
    status: "draft",
    spatialStatus: "not_configured",
    createdAtUtc: requiredDateTime(value, "createdAtUtc"),
    version: requiredUuid(value, "version"),
  };
}

export function parseFieldList(value: unknown): readonly FieldSummary[] {
  if (!Array.isArray(value) || value.length > 100) {
    throw new FieldApiError("error", 502);
  }
  return value.map(parseFieldSummary);
}

export function parseCreatedField(value: unknown): CreatedField {
  if (!isRecord(value)) {
    throw new FieldApiError("error", 502);
  }
  return {
    ...parseFieldSummary(value),
    isReplay: requiredBoolean(value, "isReplay"),
  };
}

export function parseRenamedField(value: unknown): RenamedField {
  const expectedKeys = new Set([
    "fieldId",
    "organizationId",
    "displayName",
    "type",
    "status",
    "spatialStatus",
    "createdAtUtc",
    "version",
    "isReplay",
    "revision",
  ]);
  if (
    !isRecord(value) ||
    Object.keys(value).length !== expectedKeys.size ||
    Object.keys(value).some((key) => !expectedKeys.has(key))
  ) {
    throw new FieldApiError("error", 502);
  }
  return {
    ...parseFieldSummary(value),
    isReplay: requiredBoolean(value, "isReplay"),
    revision: requiredInteger(value, "revision", 2),
  };
}

async function readJson(response: Response): Promise<unknown> {
  const text = await response.text();
  if (text.length === 0) {
    return null;
  }
  try {
    const value: unknown = JSON.parse(text);
    return value;
  } catch {
    throw new FieldApiError("error", response.status || 502);
  }
}

function problemCode(body: unknown): string {
  return isRecord(body) && typeof body.code === "string"
    ? body.code.toLowerCase()
    : "";
}

function classifyFailure(status: number, body: unknown): FieldFailureKind {
  const code = problemCode(body);
  if (status === 400) return "validation";
  if (status === 401) return "signed-out";
  if (status === 403) return "reauthentication-required";
  if (status === 404) return "unavailable";
  if (status === 409) {
    if (code === "productive_core.management_unit_capacity_reached") {
      return "capacity-reached";
    }
    return code.includes("in_progress") ? "in-progress" : "conflict";
  }
  if (status === 412) return "stale";
  if (status === 429) return "rate-limited";
  if (status === 503) {
    return code.includes("reconciliation")
      ? "reconciliation-required"
      : "service-unavailable";
  }
  return "error";
}

async function ensureSuccessful(response: Response): Promise<void> {
  if (response.ok) return;
  const body = await readJson(response);
  throw new FieldApiError(
    classifyFailure(response.status, body),
    response.status,
    problemCode(body),
  );
}

async function getJson(path: string, signal?: AbortSignal): Promise<unknown> {
  const response = await fetch(path, {
    cache: "no-store",
    credentials: "include",
    headers: { Accept: "application/json" },
    signal,
  });
  await ensureSuccessful(response);
  return readJson(response);
}

let antiforgeryToken: string | null = null;

export function invalidateFieldAntiforgeryToken(): void {
  antiforgeryToken = null;
}

async function getAntiforgeryToken(signal?: AbortSignal): Promise<string> {
  if (antiforgeryToken !== null) return antiforgeryToken;
  const value = await getJson("/api/identity/antiforgery", signal);
  if (!isRecord(value)) {
    throw new FieldApiError("error", 502);
  }
  antiforgeryToken = requiredString(value, "token");
  return antiforgeryToken;
}

type FieldMutationRequest = Readonly<{
  path: string;
  method: "POST" | "PATCH";
  displayName: string;
  idempotencyKey: string;
  version?: string;
  signal?: AbortSignal;
  retryAntiforgery?: boolean;
}>;

async function mutateField(request: FieldMutationRequest): Promise<Response> {
  const token = await getAntiforgeryToken(request.signal);
  const headers: Record<string, string> = {
    Accept: "application/json",
    "Content-Type": "application/json",
    "Idempotency-Key": request.idempotencyKey,
    "X-CSRF-TOKEN": token,
  };
  if (request.version !== undefined) {
    validateIdentifier(request.version);
    headers["If-Match"] = `"${request.version}"`;
  }
  const response = await fetch(request.path, {
    method: request.method,
    cache: "no-store",
    credentials: "include",
    headers,
    body: JSON.stringify({ displayName: request.displayName }),
    signal: request.signal,
  });

  if (!response.ok && request.retryAntiforgery !== false) {
    const body = await readJson(response.clone());
    if (problemCode(body) === "request.invalid_antiforgery") {
      invalidateFieldAntiforgeryToken();
      return mutateField({ ...request, retryAntiforgery: false });
    }
  }
  await ensureSuccessful(response);
  return response;
}

export function createFieldIdempotencyKey(): string {
  const bytes = new Uint8Array(16);
  globalThis.crypto.getRandomValues(bytes);
  return Array.from(bytes, (value) => value.toString(16).padStart(2, "0")).join(
    "",
  );
}

function validateStrongFieldEtag(response: Response, version: string): void {
  const etag = response.headers.get("ETag");
  const match = etag?.match(
    /^"([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})"$/i,
  );
  if (match?.[1]?.toLowerCase() !== version.toLowerCase()) {
    throw new FieldApiError("error", 502);
  }
}

export async function listFields(
  organizationId: string,
  signal?: AbortSignal,
): Promise<readonly FieldSummary[]> {
  validateIdentifier(organizationId);
  const fields = parseFieldList(
    await getJson(
      `/api/organizations/${encodeURIComponent(organizationId)}/fields`,
      signal,
    ),
  );
  if (fields.some((field) => field.organizationId !== organizationId)) {
    throw new FieldApiError("error", 502);
  }
  return fields;
}

export async function getField(
  organizationId: string,
  fieldId: string,
  signal?: AbortSignal,
): Promise<FieldSummary> {
  validateIdentifier(organizationId);
  validateIdentifier(fieldId);
  const field = parseFieldSummary(
    await getJson(
      `/api/organizations/${encodeURIComponent(organizationId)}/fields/${encodeURIComponent(fieldId)}`,
      signal,
    ),
  );
  if (field.organizationId !== organizationId || field.fieldId !== fieldId) {
    throw new FieldApiError("error", 502);
  }
  return field;
}

export async function createField(
  organizationId: string,
  displayName: string,
  idempotencyKey: string,
  signal?: AbortSignal,
): Promise<CreatedField> {
  validateIdentifier(organizationId);
  if (!/^[A-Za-z0-9_-]{32,128}$/.test(idempotencyKey)) {
    throw new FieldApiError("validation", 400);
  }
  const response = await mutateField({
    path: `/api/organizations/${encodeURIComponent(organizationId)}/fields`,
    method: "POST",
    displayName,
    idempotencyKey,
    signal,
  });
  const field = parseCreatedField(await readJson(response));
  if (field.organizationId !== organizationId) {
    throw new FieldApiError("error", 502);
  }
  return field;
}

export async function renameField({
  organizationId,
  fieldId,
  displayName,
  version,
  idempotencyKey,
  signal,
}: Readonly<{
  organizationId: string;
  fieldId: string;
  displayName: string;
  version: string;
  idempotencyKey: string;
  signal?: AbortSignal;
}>): Promise<RenamedField> {
  validateIdentifier(organizationId);
  validateIdentifier(fieldId);
  validateIdentifier(version);
  if (!/^[A-Za-z0-9_-]{32,128}$/.test(idempotencyKey)) {
    throw new FieldApiError("validation", 400);
  }
  const response = await mutateField({
    path: `/api/organizations/${encodeURIComponent(organizationId)}/fields/${encodeURIComponent(fieldId)}`,
    method: "PATCH",
    displayName,
    idempotencyKey,
    version,
    signal,
  });
  const field = parseRenamedField(await readJson(response));
  if (field.organizationId !== organizationId || field.fieldId !== fieldId) {
    throw new FieldApiError("error", 502);
  }
  validateStrongFieldEtag(response, field.version);
  return field;
}
