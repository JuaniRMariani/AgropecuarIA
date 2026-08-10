import type {
  AuthenticationAssurance,
  AvailableConnection,
  IdentityCapabilities,
  IdentityConnection,
  IdentitySession,
  LinkStart,
  LinkedIdentity,
  MembershipSummary,
  StepUpAttempt,
  StepUpPurpose,
} from "./identity-types";

export type IdentityFailureKind =
  "signed-out" | "revoked" | "provider-down" | "conflict" | "replay" | "error";

export class IdentityApiError extends Error {
  readonly kind: IdentityFailureKind;
  readonly status: number;

  constructor(kind: IdentityFailureKind, status: number) {
    super("Identity request failed");
    this.name = "IdentityApiError";
    this.kind = kind;
    this.status = status;
  }
}

type JsonRecord = Record<string, unknown>;
const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function isRecord(value: unknown): value is JsonRecord {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function requiredString(record: JsonRecord, key: string): string {
  const value = record[key];
  if (typeof value !== "string" || value.length === 0) {
    throw new IdentityApiError("error", 502);
  }
  return value;
}

function requiredUuid(record: JsonRecord, key: string): string {
  const value = requiredString(record, key);
  if (!UUID_PATTERN.test(value)) {
    throw new IdentityApiError("error", 502);
  }
  return value;
}

function requiredDateTime(record: JsonRecord, key: string): string {
  const value = requiredString(record, key);
  if (!value.includes("T") || Number.isNaN(Date.parse(value))) {
    throw new IdentityApiError("error", 502);
  }
  return value;
}

function nullableDateTime(record: JsonRecord, key: string): string | null {
  const value = record[key];
  if (value === null) {
    return null;
  }
  return requiredDateTime(record, key);
}

function parseStepUpPurpose(value: unknown): StepUpPurpose {
  if (value === "manage_authentication_methods") {
    return value;
  }
  throw new IdentityApiError("error", 502);
}

function requiredBoolean(record: JsonRecord, key: string): boolean {
  const value = record[key];
  if (typeof value !== "boolean") {
    throw new IdentityApiError("error", 502);
  }
  return value;
}

function requiredArray(record: JsonRecord, key: string): readonly unknown[] {
  const value = record[key];
  if (!Array.isArray(value)) {
    throw new IdentityApiError("error", 502);
  }
  return value;
}

function parseConnection(value: unknown): IdentityConnection {
  if (value === "email" || value === "google") {
    return value;
  }
  throw new IdentityApiError("error", 502);
}

function parseAvailableConnection(value: unknown): AvailableConnection {
  if (!isRecord(value)) {
    throw new IdentityApiError("error", 502);
  }
  return {
    id: parseConnection(value.id),
    label: requiredString(value, "label"),
    available: requiredBoolean(value, "available"),
  };
}

function parseLinkedIdentity(value: unknown): LinkedIdentity {
  if (!isRecord(value)) {
    throw new IdentityApiError("error", 502);
  }
  return {
    identityId: requiredUuid(value, "identityId"),
    connection: parseConnection(value.connection),
    label: requiredString(value, "label"),
    verifiedAtUtc: requiredDateTime(value, "verifiedAtUtc"),
  };
}

function parseMembership(value: unknown): MembershipSummary {
  if (!isRecord(value)) {
    throw new IdentityApiError("error", 502);
  }
  return {
    organizationId: requiredUuid(value, "organizationId"),
    organizationName: requiredString(value, "organizationName"),
    role: requiredString(value, "role"),
  };
}

export function parseIdentityCapabilities(
  value: unknown,
): IdentityCapabilities {
  if (!isRecord(value)) {
    throw new IdentityApiError("error", 502);
  }
  return {
    environment: requiredString(value, "environment"),
    oidcConfigured: requiredBoolean(value, "oidcConfigured"),
    developmentProviderEnabled: requiredBoolean(
      value,
      "developmentProviderEnabled",
    ),
    strongAuthenticationAvailable: requiredBoolean(
      value,
      "strongAuthenticationAvailable",
    ),
    connections: requiredArray(value, "connections").map(
      parseAvailableConnection,
    ),
  };
}

export function parseIdentitySession(value: unknown): IdentitySession {
  if (!isRecord(value)) {
    throw new IdentityApiError("error", 502);
  }
  const identities = requiredArray(value, "identities").map(
    parseLinkedIdentity,
  );
  if (identities.length === 0) {
    throw new IdentityApiError("error", 502);
  }
  return {
    userId: requiredUuid(value, "userId"),
    displayName: requiredString(value, "displayName"),
    authentication: parseAuthenticationAssurance(value.authentication),
    identities,
    memberships: requiredArray(value, "memberships").map(parseMembership),
  };
}

function parseAuthenticationAssurance(value: unknown): AuthenticationAssurance {
  if (!isRecord(value)) {
    throw new IdentityApiError("error", 502);
  }
  const level = requiredString(value, "level");
  if (level !== "primary" && level !== "strong") {
    throw new IdentityApiError("error", 502);
  }
  const purposeValue = value.purpose;
  const purpose =
    purposeValue === null ? null : parseStepUpPurpose(purposeValue);
  const strongAuthenticatedAtUtc = nullableDateTime(
    value,
    "strongAuthenticatedAtUtc",
  );
  const expiresAtUtc = nullableDateTime(value, "expiresAtUtc");
  const hasStrongContext =
    purpose !== null &&
    strongAuthenticatedAtUtc !== null &&
    expiresAtUtc !== null;
  const hasPrimaryContext =
    purpose === null &&
    strongAuthenticatedAtUtc === null &&
    expiresAtUtc === null;
  if (
    (level === "strong" && !hasStrongContext) ||
    (level === "primary" && !hasPrimaryContext)
  ) {
    throw new IdentityApiError("error", 502);
  }
  return {
    level,
    authenticatedAtUtc: requiredDateTime(value, "authenticatedAtUtc"),
    purpose,
    strongAuthenticatedAtUtc,
    expiresAtUtc,
  };
}

export function parseLinkStart(value: unknown): LinkStart {
  if (!isRecord(value)) {
    throw new IdentityApiError("error", 502);
  }
  return {
    attemptId: requiredUuid(value, "attemptId"),
    connection: parseConnection(value.connection),
    expiresAtUtc: requiredDateTime(value, "expiresAtUtc"),
    authorizationUrl: requiredString(value, "authorizationUrl"),
  };
}

export function parseStepUpAttempt(value: unknown): StepUpAttempt {
  if (!isRecord(value)) {
    throw new IdentityApiError("error", 502);
  }
  return {
    attemptId: requiredUuid(value, "attemptId"),
    purpose: parseStepUpPurpose(value.purpose),
    expiresAtUtc: requiredDateTime(value, "expiresAtUtc"),
    authorizationUrl: requiredString(value, "authorizationUrl"),
  };
}

async function readJson(response: Response): Promise<unknown> {
  const text = await response.text();
  if (text.length === 0) {
    return null;
  }
  try {
    return JSON.parse(text) as unknown;
  } catch {
    throw new IdentityApiError("error", response.status || 502);
  }
}

function problemCode(body: unknown): string {
  return isRecord(body) && typeof body.code === "string"
    ? body.code.toLowerCase()
    : "";
}

function classifyFailure(status: number, body: unknown): IdentityFailureKind {
  const code = problemCode(body);
  if (status === 401) {
    return code.includes("revok") ? "revoked" : "signed-out";
  }
  if (status === 503) {
    return "provider-down";
  }
  if (status === 409) {
    return code.includes("replay") ||
      code.includes("used") ||
      code.includes("expired")
      ? "replay"
      : "conflict";
  }
  return "error";
}

async function ensureSuccessful(response: Response): Promise<void> {
  if (response.ok) {
    return;
  }
  const body = await readJson(response);
  throw new IdentityApiError(
    classifyFailure(response.status, body),
    response.status,
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
let antiforgeryRequest: Promise<string> | null = null;

export function invalidateAntiforgeryToken(): void {
  antiforgeryToken = null;
}

async function getAntiforgeryToken(signal?: AbortSignal): Promise<string> {
  if (antiforgeryToken !== null) {
    return antiforgeryToken;
  }
  if (antiforgeryRequest === null) {
    antiforgeryRequest = getJson("/api/identity/antiforgery", signal)
      .then((value) => {
        if (!isRecord(value)) {
          throw new IdentityApiError("error", 502);
        }
        return requiredString(value, "token");
      })
      .then((token) => {
        antiforgeryToken = token;
        return token;
      })
      .finally(() => {
        antiforgeryRequest = null;
      });
  }
  return antiforgeryRequest;
}

async function mutate(
  path: string,
  method: "POST" | "DELETE",
  body?: Readonly<Record<string, string>>,
  signal?: AbortSignal,
  retryAntiforgery = true,
): Promise<Response> {
  const token = await getAntiforgeryToken(signal);
  const response = await fetch(path, {
    method,
    cache: "no-store",
    credentials: "include",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": token,
    },
    body: body === undefined ? undefined : JSON.stringify(body),
    signal,
  });
  if (!response.ok && retryAntiforgery) {
    const failedResponse = response.clone();
    const failedBody = await readJson(failedResponse);
    if (problemCode(failedBody) !== "request.invalid_antiforgery") {
      await ensureSuccessful(response);
      return response;
    }
    invalidateAntiforgeryToken();
    return mutate(path, method, body, signal, false);
  }
  await ensureSuccessful(response);
  return response;
}

export function identityLoginUrl(connection: IdentityConnection): string {
  return `/api/identity/login/${connection}`;
}

export function resolveAuthorizationUrl(value: string): string | null {
  try {
    const base = window.location.origin;
    const url = new URL(value, base);
    return url.origin === base ? url.toString() : null;
  } catch {
    return null;
  }
}

export async function loadCapabilities(
  signal?: AbortSignal,
): Promise<IdentityCapabilities> {
  return parseIdentityCapabilities(
    await getJson("/api/identity/capabilities", signal),
  );
}

export async function loadSession(
  signal?: AbortSignal,
): Promise<IdentitySession> {
  return parseIdentitySession(await getJson("/api/identity/session", signal));
}

export async function startLink(
  connection: IdentityConnection,
  signal?: AbortSignal,
): Promise<LinkStart> {
  const response = await mutate(
    "/api/identity/link-attempts",
    "POST",
    { connection },
    signal,
  );
  return parseLinkStart(await readJson(response));
}

export async function startStepUp(
  purpose: StepUpPurpose,
  signal?: AbortSignal,
): Promise<StepUpAttempt> {
  const response = await mutate(
    "/api/identity/step-up-attempts",
    "POST",
    { purpose },
    signal,
  );
  return parseStepUpAttempt(await readJson(response));
}

export async function completeDevelopmentStepUp(
  attempt: StepUpAttempt,
  signal?: AbortSignal,
): Promise<IdentitySession> {
  const response = await mutate(
    `/api/development/identity/step-up-attempts/${encodeURIComponent(attempt.attemptId)}/complete`,
    "POST",
    undefined,
    signal,
  );
  invalidateAntiforgeryToken();
  return parseIdentitySession(await readJson(response));
}

export async function completeDevelopmentLink(
  attempt: LinkStart,
  signal?: AbortSignal,
): Promise<IdentitySession> {
  const fixture =
    attempt.connection === "email" ? "email-owner" : "google-owner";
  await mutate(
    `/api/development/identity/link-attempts/${encodeURIComponent(attempt.attemptId)}/verify`,
    "POST",
    { fixture },
    signal,
  );
  const response = await mutate(
    `/api/identity/link-attempts/${encodeURIComponent(attempt.attemptId)}/complete`,
    "POST",
    undefined,
    signal,
  );
  return parseIdentitySession(await readJson(response));
}

export async function unlinkIdentity(
  identityId: string,
  signal?: AbortSignal,
): Promise<void> {
  await mutate(
    `/api/identity/identities/${encodeURIComponent(identityId)}`,
    "DELETE",
    undefined,
    signal,
  );
}

export async function revokeSession(signal?: AbortSignal): Promise<void> {
  await mutate("/api/identity/session/revoke", "POST", undefined, signal);
  invalidateAntiforgeryToken();
}

export async function developmentSignIn(
  signal?: AbortSignal,
): Promise<IdentitySession> {
  await mutate(
    "/api/development/identity/sign-in",
    "POST",
    { fixture: "email-owner" },
    signal,
  );
  // ASP.NET Core binds antiforgery tokens to the current principal.
  invalidateAntiforgeryToken();
  return loadSession(signal);
}
