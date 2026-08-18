import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
  createField,
  FieldApiError,
  getField,
  invalidateFieldAntiforgeryToken,
  listFields,
  normalizeFieldDisplayName,
  parseCreatedField,
  parseFieldSummary,
} from "../../features/fields/field-api";

const organizationId = "1266395e-ec88-481e-9a72-81fa2cc2904a";
const fieldId = "f0185b77-3ee8-4e43-adf8-083975f8fa77";
const field = {
  fieldId,
  organizationId,
  displayName: "Campo Norte",
  type: "field",
  status: "draft",
  spatialStatus: "not_configured",
  createdAtUtc: "2026-08-18T18:00:00Z",
  version: "5a5ed442-141f-4fd4-a8e8-52598a5d7426",
} satisfies Record<string, unknown>;

const fetchMock = vi.fn<typeof fetch>();

function jsonResponse(value: unknown, status = 200): Response {
  return new Response(JSON.stringify(value), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

beforeEach(() => {
  vi.stubGlobal("fetch", fetchMock);
  invalidateFieldAntiforgeryToken();
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.clearAllMocks();
  invalidateFieldAntiforgeryToken();
});

describe("field API boundary", () => {
  it("normalizes NFC after trimming and rejects Unicode controls", () => {
    expect(normalizeFieldDisplayName("  e\u0301🌱  ")).toBe("é🌱");
    expect(normalizeFieldDisplayName("\u0085Campo Norte\uFEFF")).toBe(
      "Campo Norte",
    );
    expect(normalizeFieldDisplayName("Campo\nNorte")).toBeNull();
    expect(normalizeFieldDisplayName("Campo\u0085Norte")).toBeNull();
    expect(
      normalizeFieldDisplayName(`A${String.fromCharCode(0xd800)}B`),
    ).toBeNull();
  });

  it("rejects response names that are not already in canonical form", () => {
    expect(() =>
      parseFieldSummary({ ...field, displayName: "\uFEFFCampo Norte" }),
    ).toThrow(FieldApiError);
    expect(() =>
      parseFieldSummary({ ...field, displayName: "\u0085Campo Norte" }),
    ).toThrow(FieldApiError);
    expect(() =>
      parseFieldSummary({ ...field, displayName: "e\u0301 Campo" }),
    ).toThrow(FieldApiError);
  });

  it("accepts exactly 120 astral Unicode scalars", () => {
    const displayName = "🌱".repeat(120);

    expect(normalizeFieldDisplayName(displayName)).toBe(displayName);
    expect(Array.from(displayName)).toHaveLength(120);
  });

  it("rejects 121 astral Unicode scalars", () => {
    expect(normalizeFieldDisplayName("🌱".repeat(121))).toBeNull();
  });

  it("accepts only the frozen draft and non-spatial literals", () => {
    expect(parseFieldSummary(field)).toEqual(field);
    expect(parseCreatedField({ ...field, isReplay: false })).toEqual({
      ...field,
      isReplay: false,
    });

    expect(() =>
      parseFieldSummary({ ...field, spatialStatus: "configured" }),
    ).toThrow(FieldApiError);
    expect(() => parseCreatedField(field)).toThrow(FieldApiError);
  });

  it("rejects a list item attributed to a different organization", async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse([
        {
          ...field,
          organizationId: "64eb2297-6603-4fc9-b2b0-a582a7bf1992",
        },
      ]),
    );

    await expect(listFields(organizationId)).rejects.toMatchObject({
      kind: "error",
      status: 502,
    });
  });

  it("sends the exact create body with CSRF and the caller's idempotency key", async () => {
    const idempotencyKey = "8c63a2efc3346cc563db904db0f88d73";
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ token: "csrf-token" }))
      .mockResolvedValueOnce(jsonResponse({ ...field, isReplay: false }, 201));

    await expect(
      createField(organizationId, "Campo Norte", idempotencyKey),
    ).resolves.toEqual({ ...field, isReplay: false });

    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      "/api/identity/antiforgery",
      expect.objectContaining({ credentials: "include" }),
    );
    const createCall = fetchMock.mock.calls[1];
    expect(createCall?.[0]).toBe(`/api/organizations/${organizationId}/fields`);
    const request = createCall?.[1];
    expect(request?.method).toBe("POST");
    expect(new Headers(request?.headers).get("X-CSRF-TOKEN")).toBe(
      "csrf-token",
    );
    expect(new Headers(request?.headers).get("Idempotency-Key")).toBe(
      idempotencyKey,
    );
    const body: unknown = JSON.parse(String(request?.body));
    expect(body).toEqual({ displayName: "Campo Norte" });
  });

  it("retries an invalid antiforgery token without changing the idempotency key", async () => {
    const idempotencyKey = "a3481243a3186ea420caa585355a4308";
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ token: "stale" }))
      .mockResolvedValueOnce(
        jsonResponse({ code: "request.invalid_antiforgery" }, 400),
      )
      .mockResolvedValueOnce(jsonResponse({ token: "fresh" }))
      .mockResolvedValueOnce(jsonResponse({ ...field, isReplay: false }, 201));

    await createField(organizationId, "Campo Norte", idempotencyKey);

    const firstPost = fetchMock.mock.calls[1]?.[1];
    const retriedPost = fetchMock.mock.calls[3]?.[1];
    expect(new Headers(firstPost?.headers).get("Idempotency-Key")).toBe(
      idempotencyKey,
    );
    expect(new Headers(retriedPost?.headers).get("Idempotency-Key")).toBe(
      idempotencyKey,
    );
    expect(new Headers(retriedPost?.headers).get("X-CSRF-TOKEN")).toBe("fresh");
  });

  it.each([
    [401, "session.revoked", "signed-out"],
    [403, "authorization.denied", "reauthentication-required"],
    [404, "resource.not_available", "unavailable"],
    [409, "idempotency.in_progress", "in-progress"],
    [
      409,
      "productive_core.management_unit_capacity_reached",
      "capacity-reached",
    ],
    [409, "idempotency.key_reused", "conflict"],
    [429, "request.rate_limited", "rate-limited"],
    [503, "idempotency.reconciliation_required", "reconciliation-required"],
    [503, "productive_core.unavailable", "service-unavailable"],
  ])("maps HTTP %i with %s to %s", async (status, code, kind) => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ code }, status));

    await expect(listFields(organizationId)).rejects.toMatchObject({
      kind,
      status,
      code,
    });
  });

  it("loads detail only when both route identifiers match the response", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse(field));
    await expect(getField(organizationId, fieldId)).resolves.toEqual(field);

    expect(fetchMock).toHaveBeenCalledWith(
      `/api/organizations/${organizationId}/fields/${fieldId}`,
      expect.objectContaining({ credentials: "include" }),
    );
  });
});
