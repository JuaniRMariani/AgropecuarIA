import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import type {
  CreatedField,
  FieldSummary,
} from "../../features/fields/field-types";

const apiMocks = vi.hoisted(() => ({
  createField:
    vi.fn<
      (
        organizationId: string,
        displayName: string,
        idempotencyKey: string,
      ) => Promise<CreatedField>
    >(),
  getField:
    vi.fn<(organizationId: string, fieldId: string) => Promise<FieldSummary>>(),
  listFields:
    vi.fn<
      (
        organizationId: string,
        signal?: AbortSignal,
      ) => Promise<readonly FieldSummary[]>
    >(),
}));

vi.mock("../../features/fields/field-api", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("../../features/fields/field-api")>();
  return { ...actual, ...apiMocks };
});

import { FieldApiError } from "../../features/fields/field-api";
import { FieldManagement } from "../../features/fields/field-management";

const organizationA = {
  organizationId: "1266395e-ec88-481e-9a72-81fa2cc2904a",
  organizationName: "La Esperanza",
  role: "owner",
};
const organizationB = {
  organizationId: "64eb2297-6603-4fc9-b2b0-a582a7bf1992",
  organizationName: "Los Aromos",
  role: "owner",
};
const fieldA: FieldSummary = {
  fieldId: "f0185b77-3ee8-4e43-adf8-083975f8fa77",
  organizationId: organizationA.organizationId,
  displayName: "Campo Norte",
  type: "field",
  status: "draft",
  spatialStatus: "not_configured",
  createdAtUtc: "2026-08-18T18:00:00Z",
  version: "5a5ed442-141f-4fd4-a8e8-52598a5d7426",
};

beforeEach(() => {
  globalThis.sessionStorage.clear();
  apiMocks.createField.mockReset();
  apiMocks.getField.mockReset();
  apiMocks.listFields.mockReset();
  apiMocks.listFields.mockResolvedValue([]);
});

afterEach(() => {
  cleanup();
  globalThis.sessionStorage.clear();
  vi.clearAllMocks();
});

describe("FieldManagement", () => {
  it("creates a duplicate-friendly draft, shows only short IDs and opens its non-spatial ficha", async () => {
    apiMocks.createField.mockResolvedValue({ ...fieldA, isReplay: false });
    render(<FieldManagement organizations={[organizationA]} />);

    expect(await screen.findByText("Todavía no hay campos")).toBeVisible();
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));
    const input = screen.getByRole("textbox", { name: "Nombre del campo" });
    expect(input).toHaveFocus();
    fireEvent.change(input, { target: { value: "Campo Norte" } });
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));

    await waitFor(() => expect(apiMocks.createField).toHaveBeenCalledOnce());
    expect(apiMocks.createField).toHaveBeenCalledWith(
      organizationA.organizationId,
      "Campo Norte",
      expect.stringMatching(/^[0-9a-f]{32}$/),
    );
    expect(
      screen.getByText(/quedó como borrador sin geometría/i),
    ).toBeVisible();
    expect(screen.getAllByText("Campo F0185B")).toHaveLength(2);
    expect(screen.queryByText(fieldA.fieldId)).not.toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "Campo Norte", level: 4 }),
    ).toBeVisible();
    expect(screen.getByText("Sin geometría", { selector: "dd" })).toBeVisible();
  });

  it("submits 120 astral scalars and rejects the 121st without an HTML UTF-16 limit", async () => {
    const validName = "🌱".repeat(120);
    apiMocks.createField.mockResolvedValue({
      ...fieldA,
      displayName: validName,
      isReplay: false,
    });
    render(<FieldManagement organizations={[organizationA]} />);
    await screen.findByText("Todavía no hay campos");
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));
    const input = screen.getByRole("textbox", { name: "Nombre del campo" });
    expect(input).not.toHaveAttribute("maxlength");
    fireEvent.change(input, { target: { value: validName } });
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));

    await waitFor(() => expect(apiMocks.createField).toHaveBeenCalledOnce());
    expect(apiMocks.createField.mock.calls[0]?.[1]).toBe(validName);

    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));
    fireEvent.change(
      screen.getByRole("textbox", { name: "Nombre del campo" }),
      { target: { value: "🌱".repeat(121) } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));

    expect(
      await screen.findByText(
        "Ingresá entre 2 y 120 caracteres Unicode, sin controles.",
      ),
    ).toBeVisible();
    expect(apiMocks.createField).toHaveBeenCalledTimes(1);
  });

  it("lists the same display name independently for two organizations", async () => {
    apiMocks.listFields.mockImplementation(async (organizationId) => [
      {
        ...fieldA,
        organizationId,
        fieldId:
          organizationId === organizationA.organizationId
            ? fieldA.fieldId
            : "ceb92343-63e6-41da-b338-c70f7d87a41d",
      },
    ]);

    render(<FieldManagement organizations={[organizationA, organizationB]} />);

    expect(await screen.findAllByText("Campo Norte")).toHaveLength(2);
    expect(apiMocks.listFields).toHaveBeenCalledWith(
      organizationA.organizationId,
      expect.any(AbortSignal),
    );
    expect(apiMocks.listFields).toHaveBeenCalledWith(
      organizationB.organizationId,
      expect.any(AbortSignal),
    );
  });

  it("preserves the exact key in sessionStorage and on retry after a 503", async () => {
    apiMocks.createField
      .mockRejectedValueOnce(
        new FieldApiError(
          "service-unavailable",
          503,
          "productive_core.unavailable",
        ),
      )
      .mockResolvedValueOnce({ ...fieldA, isReplay: true });
    render(<FieldManagement organizations={[organizationA]} />);
    await screen.findByText("Todavía no hay campos");
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));
    fireEvent.change(
      screen.getByRole("textbox", { name: "Nombre del campo" }),
      { target: { value: "Campo Norte" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));

    expect(await screen.findByText(/intento quedó guardado/i)).toBeVisible();
    const stored = globalThis.sessionStorage.getItem(
      "agropecuaria.fields.create-attempt.v1",
    );
    expect(stored).not.toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));

    await waitFor(() => expect(apiMocks.createField).toHaveBeenCalledTimes(2));
    const firstKey = apiMocks.createField.mock.calls[0]?.[2];
    const secondKey = apiMocks.createField.mock.calls[1]?.[2];
    expect(firstKey).toBeDefined();
    expect(secondKey).toBe(firstKey);
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.fields.create-attempt.v1",
      ),
    ).toBeNull();
    expect(screen.getByText(/confirmamos el intento anterior/i)).toBeVisible();
  });

  it("shows capacity reached distinctly and clears the terminal attempt", async () => {
    apiMocks.createField.mockRejectedValue(
      new FieldApiError(
        "capacity-reached",
        409,
        "productive_core.management_unit_capacity_reached",
      ),
    );
    render(<FieldManagement organizations={[organizationA]} />);
    await screen.findByText(/no hay campos/i);
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));
    fireEvent.change(
      screen.getByRole("textbox", { name: "Nombre del campo" }),
      { target: { value: "Campo Norte" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));

    expect(
      await screen.findByText(
        "La organización alcanzó el máximo local de 100 campos para este incremento.",
      ),
    ).toBeVisible();
    expect(screen.queryByText(/clave ya corresponde/i)).not.toBeInTheDocument();
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.fields.create-attempt.v1",
      ),
    ).toBeNull();
  });

  it.each([
    ["offline", new TypeError("Failed to fetch")],
    ["generic response loss", new Error("Response lost after send")],
    [
      "rate limited",
      new FieldApiError("rate-limited", 429, "request.rate_limited"),
    ],
    [
      "idempotency in progress",
      new FieldApiError("in-progress", 409, "idempotency.in_progress"),
    ],
    [
      "reconciliation required",
      new FieldApiError(
        "reconciliation-required",
        503,
        "idempotency.reconciliation_required",
      ),
    ],
    [
      "service unavailable",
      new FieldApiError(
        "service-unavailable",
        503,
        "productive_core.unavailable",
      ),
    ],
    ["signed out", new FieldApiError("signed-out", 401, "session.revoked")],
    [
      "reauthentication required",
      new FieldApiError(
        "reauthentication-required",
        403,
        "authorization.step_up_required",
      ),
    ],
  ])("retains the persisted attempt after %s", async (_scenario, failure) => {
    apiMocks.createField.mockRejectedValue(failure);
    render(<FieldManagement organizations={[organizationA]} />);
    await screen.findByText(/no hay campos/i);
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));
    fireEvent.change(
      screen.getByRole("textbox", { name: "Nombre del campo" }),
      { target: { value: "Campo Norte" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));

    await waitFor(() => expect(apiMocks.createField).toHaveBeenCalledOnce());
    const key = apiMocks.createField.mock.calls[0]?.[2];
    const serialized = globalThis.sessionStorage.getItem(
      "agropecuaria.fields.create-attempt.v1",
    );
    expect(key).toBeDefined();
    expect(serialized).not.toBeNull();
    expect(JSON.parse(serialized ?? "null")).toMatchObject({
      organizationId: organizationA.organizationId,
      displayName: "Campo Norte",
      idempotencyKey: key,
    });
  });

  it("resumes the persisted attempt with the same key after reload", async () => {
    apiMocks.createField
      .mockRejectedValueOnce(new Error("Response lost after send"))
      .mockResolvedValueOnce({ ...fieldA, isReplay: true });
    render(<FieldManagement organizations={[organizationA]} />);
    await screen.findByText(/no hay campos/i);
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));
    fireEvent.change(
      screen.getByRole("textbox", { name: "Nombre del campo" }),
      { target: { value: "Campo Norte" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));

    await waitFor(() => expect(apiMocks.createField).toHaveBeenCalledOnce());
    const originalKey = apiMocks.createField.mock.calls[0]?.[2];
    expect(originalKey).toBeDefined();

    cleanup();
    render(<FieldManagement organizations={[organizationA]} />);
    expect(
      await screen.findByText(/retomamos el intento pendiente/i),
    ).toBeVisible();
    expect(
      screen.getByRole("textbox", { name: "Nombre del campo" }),
    ).toHaveValue("Campo Norte");
    fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));

    await waitFor(() => expect(apiMocks.createField).toHaveBeenCalledTimes(2));
    expect(apiMocks.createField.mock.calls[1]?.[2]).toBe(originalKey);
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.fields.create-attempt.v1",
      ),
    ).toBeNull();
  });

  it("persists the attempt before the create request settles", async () => {
    let resolveCreate: ((field: CreatedField) => void) | undefined;
    apiMocks.createField.mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveCreate = resolve;
        }),
    );
    render(<FieldManagement organizations={[organizationA]} />);
    await screen.findByText(/no hay campos/i);
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));
    fireEvent.change(
      screen.getByRole("textbox", { name: "Nombre del campo" }),
      { target: { value: "Campo Norte" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));

    expect(apiMocks.createField).toHaveBeenCalledOnce();
    const key = apiMocks.createField.mock.calls[0]?.[2];
    expect(
      JSON.parse(
        globalThis.sessionStorage.getItem(
          "agropecuaria.fields.create-attempt.v1",
        ) ?? "null",
      ),
    ).toMatchObject({
      organizationId: organizationA.organizationId,
      displayName: "Campo Norte",
      idempotencyKey: key,
      reason: "pending",
    });

    resolveCreate?.({ ...fieldA, isReplay: false });
    await waitFor(() =>
      expect(
        globalThis.sessionStorage.getItem(
          "agropecuaria.fields.create-attempt.v1",
        ),
      ).toBeNull(),
    );
  });

  it("loads detail from the dedicated endpoint and closes it with Escape", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField.mockResolvedValue(fieldA);
    render(<FieldManagement organizations={[organizationA]} />);
    const organization = await screen.findByRole("article");
    fireEvent.click(
      within(organization).getByRole("button", { name: /abrir ficha/i }),
    );

    expect(
      await screen.findByRole("heading", { name: "Campo Norte", level: 4 }),
    ).toBeVisible();
    expect(apiMocks.getField).toHaveBeenCalledWith(
      organizationA.organizationId,
      fieldA.fieldId,
    );
    const detail = screen.getByText("Ficha del campo").closest("section");
    if (detail === null) {
      throw new Error("Expected the field detail section.");
    }
    fireEvent.keyDown(detail, { key: "Escape" });
    expect(
      screen.queryByRole("heading", { name: "Campo Norte", level: 4 }),
    ).not.toBeInTheDocument();
  });

  it("renders a neutral unavailable state without leaking fields", async () => {
    apiMocks.listFields.mockRejectedValue(
      new FieldApiError("unavailable", 404, "resource.not_available"),
    );
    render(<FieldManagement organizations={[organizationA]} />);

    expect(
      await screen.findByText(
        /organización o sus campos no están disponibles/i,
      ),
    ).toBeVisible();
    expect(screen.queryByText("Campo Norte")).not.toBeInTheDocument();
  });
});
