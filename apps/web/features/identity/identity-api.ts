import type {
  AuthenticationAssurance,
  AvailableConnection,
  CreatedOwnerInvitation,
  CreatedOrganization,
  IdentityCapabilities,
  IdentityConnection,
  IdentitySession,
  LinkStart,
  LinkedIdentity,
  MembershipSummary,
  OwnerInvitationStatus,
  OwnerInvitationSummary,
  StepUpAttempt,
  StepUpPurpose,
} from "./identity-types";

export type IdentityFailureKind =
  | "signed-out"
  | "revoked"
  | "provider-down"
  | "unavailable"
  | "validation"
  | "reauthentication-required"
  | "in-progress"
  | "reconciliation-required"
  | "rate-limited"
  | "conflict"
  | "replay"
  | "error";

export class IdentityApiError extends Error {
  readonly kind: IdentityFailureKind;
  readonly status: number;
  readonly code: string;

  constructor(kind: IdentityFailureKind, status: number, code = "") {
    super("Identity request failed");
    this.name = "IdentityApiError";
    this.kind = kind;
    this.status = status;
    this.code = code;
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
  if (
    value === "manage_authentication_methods" ||
    value === "manage_organization_owners"
  ) {
    return value;
  }
  throw new IdentityApiError("error", 502);
}

function nullableString(record: JsonRecord, key: string): string | null {
  const value = record[key];
  if (value === null) {
    return null;
  }
  return requiredString(record, key);
}

function requiredBoolean(record: JsonRecord, key: string): boolean {
  const value = record[key];
  if (typeof value !== "boolean") {
    throw new IdentityApiError("error", 502);
  }
  return value;
}

function requiredPositiveInteger(record: JsonRecord, key: string): number {
  const value = record[key];
  if (typeof value !== "number" || !Number.isInteger(value) || value < 1) {
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
    ownerInvitationsAvailable: requiredBoolean(
      value,
      "ownerInvitationsAvailable",
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

export function parseCreatedOrganization(value: unknown): CreatedOrganization {
  if (
    !isRecord(value) ||
    !isRecord(value.organization) ||
    !isRecord(value.membership)
  ) {
    throw new IdentityApiError("error", 502);
  }

  const organizationStatus = requiredString(value.organization, "status");
  const membershipRole = requiredString(value.membership, "role");
  const membershipStatus = requiredString(value.membership, "status");
  if (
    organizationStatus !== "active" ||
    membershipRole !== "owner" ||
    membershipStatus !== "active"
  ) {
    throw new IdentityApiError("error", 502);
  }

  return {
    organization: {
      organizationId: requiredUuid(value.organization, "organizationId"),
      displayName: requiredString(value.organization, "displayName"),
      status: organizationStatus,
    },
    membership: {
      membershipId: requiredUuid(value.membership, "membershipId"),
      role: membershipRole,
      status: membershipStatus,
      authorizationVersion: requiredPositiveInteger(
        value.membership,
        "authorizationVersion",
      ),
    },
  };
}

function parseOwnerInvitationStatus(value: unknown): OwnerInvitationStatus {
  if (
    value === "pending" ||
    value === "accepted" ||
    value === "revoked" ||
    value === "expired"
  ) {
    return value;
  }
  throw new IdentityApiError("error", 502);
}

export function parseOwnerInvitationSummary(
  value: unknown,
): OwnerInvitationSummary {
  if (!isRecord(value)) {
    throw new IdentityApiError("error", 502);
  }
  const status = parseOwnerInvitationStatus(value.status);
  const acceptedAtUtc = nullableDateTime(value, "acceptedAtUtc");
  const revokedAtUtc = nullableDateTime(value, "revokedAtUtc");
  if (
    (status === "accepted" && acceptedAtUtc === null) ||
    (status !== "accepted" && acceptedAtUtc !== null) ||
    (status === "revoked" && revokedAtUtc === null) ||
    (status !== "revoked" && revokedAtUtc !== null)
  ) {
    throw new IdentityApiError("error", 502);
  }
  return {
    invitationId: requiredUuid(value, "invitationId"),
    organizationId: requiredUuid(value, "organizationId"),
    status,
    createdAtUtc: requiredDateTime(value, "createdAtUtc"),
    expiresAtUtc: requiredDateTime(value, "expiresAtUtc"),
    acceptedAtUtc,
    revokedAtUtc,
    version: requiredUuid(value, "version"),
  };
}

export function parseOwnerInvitationList(
  value: unknown,
): readonly OwnerInvitationSummary[] {
  if (!isRecord(value)) {
    throw new IdentityApiError("error", 502);
  }
  return requiredArray(value, "items").map(parseOwnerInvitationSummary);
}

export function parseCreatedOwnerInvitation(
  value: unknown,
): CreatedOwnerInvitation {
  if (!isRecord(value)) {
    throw new IdentityApiError("error", 502);
  }
  const invitation = parseOwnerInvitationSummary(value.invitation);
  if (invitation.status !== "pending") {
    throw new IdentityApiError("error", 502);
  }
  const token = nullableString(value, "token");
  const isReplay = requiredBoolean(value, "isReplay");
  if (
    (!isReplay && (token === null || !/^[A-Za-z0-9_-]{43}$/.test(token))) ||
    (isReplay && token !== null)
  ) {
    throw new IdentityApiError("error", 502);
  }
  return { invitation, token, isReplay };
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
  if (status === 404) {
    return "unavailable";
  }
  if (status === 503) {
    return code.includes("reconciliation")
      ? "reconciliation-required"
      : "provider-down";
  }
  if (status === 400) {
    return "validation";
  }
  if (status === 403) {
    return "reauthentication-required";
  }
  if (status === 429) {
    return "rate-limited";
  }
  if (status === 409) {
    if (code.includes("in_progress")) {
      return "in-progress";
    }
    if (code.includes("key_reused") || code.includes("fingerprint_mismatch")) {
      return "conflict";
    }
    return code.includes("replay") ||
      code.includes("used") ||
      code.includes("expired")
      ? "replay"
      : "conflict";
  }
  if (status === 412) {
    return "conflict";
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
  idempotencyKey?: string,
  ifMatch?: string,
): Promise<Response> {
  const token = await getAntiforgeryToken(signal);
  const headers: Record<string, string> = {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-CSRF-TOKEN": token,
  };
  if (idempotencyKey !== undefined) {
    headers["Idempotency-Key"] = idempotencyKey;
  }
  if (ifMatch !== undefined) {
    if (!UUID_PATTERN.test(ifMatch)) {
      throw new IdentityApiError("validation", 400);
    }
    headers["If-Match"] = `"${ifMatch}"`;
  }
  const response = await fetch(path, {
    method,
    cache: "no-store",
    credentials: "include",
    headers,
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
    return mutate(path, method, body, signal, false, idempotencyKey, ifMatch);
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

export async function createOrganization(
  displayName: string,
  idempotencyKey: string,
  signal?: AbortSignal,
): Promise<CreatedOrganization> {
  const response = await mutate(
    "/api/identity/organizations",
    "POST",
    { displayName },
    signal,
    true,
    idempotencyKey,
  );
  return parseCreatedOrganization(await readJson(response));
}

export async function listOwnerInvitations(
  organizationId: string,
  signal?: AbortSignal,
): Promise<readonly OwnerInvitationSummary[]> {
  return parseOwnerInvitationList(
    await getJson(
      `/api/identity/organizations/${encodeURIComponent(organizationId)}/owner-invitations`,
      signal,
    ),
  );
}

export async function createOwnerInvitation(
  organizationId: string,
  idempotencyKey: string,
  signal?: AbortSignal,
): Promise<CreatedOwnerInvitation> {
  const response = await mutate(
    `/api/identity/organizations/${encodeURIComponent(organizationId)}/owner-invitations`,
    "POST",
    {},
    signal,
    true,
    idempotencyKey,
  );
  return parseCreatedOwnerInvitation(await readJson(response));
}

export async function revokeOwnerInvitation(
  organizationId: string,
  invitationId: string,
  version: string,
  signal?: AbortSignal,
): Promise<OwnerInvitationSummary> {
  const response = await mutate(
    `/api/identity/organizations/${encodeURIComponent(organizationId)}/owner-invitations/${encodeURIComponent(invitationId)}/revoke`,
    "POST",
    undefined,
    signal,
    true,
    undefined,
    version,
  );
  return parseOwnerInvitationSummary(await readJson(response));
}

export async function acceptOwnerInvitation(
  token: string,
  signal?: AbortSignal,
): Promise<CreatedOrganization> {
  const response = await mutate(
    "/api/identity/owner-invitations/accept",
    "POST",
    { token },
    signal,
  );
  return parseCreatedOrganization(await readJson(response));
}
