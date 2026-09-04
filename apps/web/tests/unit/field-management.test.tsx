import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import { useState } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import type {
  ArchivedField,
  CreatedField,
  FieldSummary,
  RenamedField,
} from "../../features/fields/field-types";

const apiMocks = vi.hoisted(() => ({
  archiveField:
    vi.fn<
      (
        request: import("../../features/fields/field-api").ArchiveFieldRequest,
      ) => Promise<ArchivedField>
    >(),
  createField:
    vi.fn<
      (
        organizationId: string,
        displayName: string,
        idempotencyKey: string,
      ) => Promise<CreatedField>
    >(),
  getField:
    vi.fn<
      (
        organizationId: string,
        fieldId: string,
        signal?: AbortSignal,
      ) => Promise<FieldSummary>
    >(),
  listFields:
    vi.fn<
      (
        organizationId: string,
        signal?: AbortSignal,
      ) => Promise<readonly FieldSummary[]>
    >(),
  renameField: vi.fn<
    (
      request: Readonly<{
        organizationId: string;
        fieldId: string;
        displayName: string;
        version: string;
        idempotencyKey: string;
        signal?: AbortSignal;
      }>,
    ) => Promise<RenamedField>
  >(),
}));

vi.mock("../../features/fields/field-api", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("../../features/fields/field-api")>();
  return { ...actual, ...apiMocks };
});

import { FieldApiError } from "../../features/fields/field-api";
import {
  FieldManagement,
  type FieldDeepLink,
  type FieldDeepLinkIntent,
} from "../../features/fields/field-management";

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
const fieldA = {
  fieldId: "f0185b77-3ee8-4e43-adf8-083975f8fa77",
  organizationId: organizationA.organizationId,
  displayName: "Campo Norte",
  type: "field",
  status: "draft",
  spatialStatus: "not_configured",
  createdAtUtc: "2026-08-18T18:00:00Z",
  version: "5a5ed442-141f-4fd4-a8e8-52598a5d7426",
} satisfies FieldSummary;
const fieldB = {
  ...fieldA,
  fieldId: "a1b2c3d4-5ee8-4e43-adf8-083975f8fa77",
  displayName: "Campo Sur",
  version: "6b6ed442-141f-4fd4-a8e8-52598a5d7426",
} satisfies FieldSummary;
const foreignField = {
  ...fieldA,
  fieldId: "b2c3d4e5-6ff9-4e43-adf8-083975f8fa77",
  organizationId: organizationB.organizationId,
  displayName: "Campo Ajeno",
} satisfies FieldSummary;
const renamedVersion = "bb8da0de-af15-42f1-87a6-40583dff5abe";
const renamedFieldA: RenamedField = {
  ...fieldA,
  displayName: "Campo Sur",
  version: renamedVersion,
  isReplay: false,
  revision: 2,
};

const FULL_UUID_PATTERN =
  /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;

function expectNoFullUuidInDom(): void {
  expect(document.body.outerHTML.match(FULL_UUID_PATTERN) ?? []).toEqual([]);
}

function ControlledFieldManagement({
  initialDeepLink,
  onIntent,
}: Readonly<{
  initialDeepLink: FieldDeepLink;
  onIntent?: (intent: FieldDeepLinkIntent) => void;
}>) {
  const [deepLink, setDeepLink] = useState(initialDeepLink);
  const handleIntent = (intent: FieldDeepLinkIntent): boolean => {
    onIntent?.(intent);
    switch (intent.kind) {
      case "select":
      case "restore":
        setDeepLink({ kind: "requested", prefix: intent.prefix });
        return true;
      case "clear":
      case "reject":
        setDeepLink({ kind: "none" });
        return true;
    }
  };
  return (
    <FieldManagement
      deepLink={deepLink}
      onDeepLinkIntent={handleIntent}
      organizations={[organizationA]}
    />
  );
}

beforeEach(() => {
  globalThis.sessionStorage.clear();
  apiMocks.createField.mockReset();
  apiMocks.getField.mockReset();
  apiMocks.listFields.mockReset();
  apiMocks.renameField.mockReset();
  apiMocks.archiveField.mockReset();
  apiMocks.listFields.mockResolvedValue([]);
});

afterEach(() => {
  cleanup();
  globalThis.sessionStorage.clear();
  vi.clearAllMocks();
});

describe("FieldManagement", () => {
  it("opens a concurrently archived detail read-only and refreshes the stale draft list", async () => {
    apiMocks.listFields.mockResolvedValueOnce([fieldA]).mockResolvedValue([]);
    apiMocks.getField.mockResolvedValue({ ...fieldA, status: "archived" });
    render(<ControlledFieldManagement initialDeepLink={{ kind: "none" }} />);
    fireEvent.click(
      await screen.findByRole("button", { name: /Abrir ficha de Campo Norte/ }),
    );
    await screen.findByText("Archivado");
    await waitFor(() => expect(apiMocks.listFields).toHaveBeenCalledTimes(2));
    expect(
      screen.queryByRole("button", { name: "Editar nombre" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Archivar campo" }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "Campo Norte", level: 4 }),
    ).toBeVisible();
    expect(apiMocks.archiveField).not.toHaveBeenCalled();
  });

  it.each(["malformed", "foreign"])(
    "discards a %s stored archive without fetching or writing its target",
    async (scenario) => {
      globalThis.sessionStorage.setItem(
        "agropecuaria.fields.archive-attempt.v1",
        scenario === "malformed"
          ? "{"
          : JSON.stringify({
              organizationId: organizationB.organizationId,
              fieldId: foreignField.fieldId,
              version: foreignField.version,
              idempotencyKey: "a".repeat(32),
            }),
      );
      render(<FieldManagement organizations={[organizationA]} />);
      await screen.findByText("Todavía no hay campos");
      expect(apiMocks.getField).not.toHaveBeenCalled();
      expect(apiMocks.archiveField).not.toHaveBeenCalled();
      expect(
        globalThis.sessionStorage.getItem(
          "agropecuaria.fields.archive-attempt.v1",
        ),
      ).toBeNull();
    },
  );

  it("preserves the immutable archive operation across unmount and explicit retry", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField.mockResolvedValue(fieldA);
    apiMocks.archiveField
      .mockRejectedValueOnce(new FieldApiError("service-unavailable", 503))
      .mockResolvedValueOnce({
        ...fieldA,
        status: "archived",
        version: renamedVersion,
        revision: 2,
        isReplay: true,
      });
    const first = render(<FieldManagement organizations={[organizationA]} />);
    fireEvent.click(await openArchive());
    await screen.findByRole("button", { name: "Reintentar mismo archivado" });
    const original = apiMocks.archiveField.mock.calls[0]?.[0];
    first.unmount();
    render(<FieldManagement organizations={[organizationA]} />);
    const retry = await screen.findByRole("button", {
      name: "Reintentar mismo archivado",
    });
    expect(apiMocks.archiveField).toHaveBeenCalledTimes(1);
    fireEvent.click(retry);
    await screen.findByText("Confirmamos el archivado anterior.");
    expect(apiMocks.archiveField.mock.calls[1]?.[0]).toEqual(original);
  });

  it("can cancel a conclusively rejected archive without dispatching another write", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField.mockResolvedValue(fieldA);
    apiMocks.archiveField.mockRejectedValue(new FieldApiError("stale", 412));
    const guard = vi.fn();
    render(
      <FieldManagement
        organizations={[organizationA]}
        onContextGuardChange={guard}
      />,
    );
    fireEvent.click(await openArchive());
    fireEvent.click(
      await screen.findByRole("button", { name: "Cancelar archivado" }),
    );
    expect(guard).toHaveBeenLastCalledWith("clear");
    expect(
      screen.getByRole("button", { name: "Archivar campo" }),
    ).toHaveFocus();
    expect(apiMocks.archiveField).toHaveBeenCalledTimes(1);
  });

  const archivedField: ArchivedField = {
    ...fieldA,
    status: "archived",
    version: renamedVersion,
    revision: 2,
    isReplay: false,
  };
  const archiveStorageKey = "agropecuaria.fields.archive-attempt.v1";
  async function openArchive() {
    fireEvent.click(
      await screen.findByRole("button", { name: /Abrir ficha de Campo Norte/ }),
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Archivar campo" }),
    );
    return screen.getByRole("button", { name: "Confirmar archivado" });
  }

  it("confirms archive, refreshes the active list and keeps a read-only archived detail", async () => {
    apiMocks.listFields.mockResolvedValueOnce([fieldA]).mockResolvedValue([]);
    apiMocks.getField.mockResolvedValue(fieldA);
    apiMocks.archiveField.mockResolvedValue(archivedField);
    const onIntent = vi.fn();
    render(
      <ControlledFieldManagement
        initialDeepLink={{ kind: "none" }}
        onIntent={onIntent}
      />,
    );
    const confirm = await openArchive();
    expect(apiMocks.archiveField).not.toHaveBeenCalled();
    expect(confirm).toHaveFocus();
    expect(
      screen.getByRole("heading", { name: "Archivar campo F0185B" }),
    ).toBeVisible();
    expectNoFullUuidInDom();
    fireEvent.click(confirm);
    await screen.findByText("Campo archivado. Su historial se conserva.");
    await screen.findByText("Archivado");
    expect(apiMocks.listFields).toHaveBeenCalledTimes(2);
    expect(
      screen.queryByRole("button", { name: /Abrir ficha de Campo Norte/ }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Editar nombre" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Archivar campo" }),
    ).not.toBeInTheDocument();
    expect(onIntent).not.toHaveBeenCalledWith(
      expect.objectContaining({ kind: "reject" }),
    );
    expect(globalThis.sessionStorage.getItem(archiveStorageKey)).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Cerrar ficha" }));
    await waitFor(() =>
      expect(screen.getByRole("heading", { name: "Tus campos" })).toHaveFocus(),
    );
  });

  it("cancels archive confirmation with Escape, restores focus and clears the context guard", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField.mockResolvedValue(fieldA);
    const guard = vi.fn();
    render(
      <FieldManagement
        organizations={[organizationA]}
        onContextGuardChange={guard}
      />,
    );
    const confirm = await openArchive();
    expect(guard).toHaveBeenLastCalledWith("dirty");
    fireEvent.keyDown(confirm, { key: "Escape" });
    expect(
      screen.getByRole("button", { name: "Archivar campo" }),
    ).toHaveFocus();
    expect(guard).toHaveBeenLastCalledWith("clear");
    expect(apiMocks.archiveField).not.toHaveBeenCalled();
    expect(globalThis.sessionStorage.getItem(archiveStorageKey)).toBeNull();
  });

  it("blocks duplicate and conflicting actions while an archive request is pending", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA, fieldB]);
    apiMocks.getField.mockResolvedValue(fieldA);
    apiMocks.archiveField.mockReturnValue(new Promise(() => {}));
    const guard = vi.fn();
    render(
      <FieldManagement
        organizations={[organizationA]}
        onContextGuardChange={guard}
      />,
    );
    const confirm = await openArchive();
    fireEvent.click(confirm);
    fireEvent.click(confirm);
    expect(apiMocks.archiveField).toHaveBeenCalledTimes(1);
    expect(guard).toHaveBeenLastCalledWith("pending");
    for (const name of [
      "Crear campo",
      "Editar nombre",
      "Cerrar ficha",
      "Archivar campo",
    ])
      expect(screen.getByRole("button", { name })).toBeDisabled();
    expect(
      screen.getByRole("button", { name: /Abrir ficha de Campo Sur/ }),
    ).toBeDisabled();
    expect(
      screen.queryByRole("button", { name: "Cancelar archivado" }),
    ).not.toBeInTheDocument();
  });

  it("retries uncertain archive with exactly the original version and key after a newer draft read", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField
      .mockResolvedValueOnce(fieldA)
      .mockResolvedValue({ ...fieldA, version: renamedVersion });
    apiMocks.archiveField
      .mockRejectedValueOnce(new FieldApiError("service-unavailable", 503))
      .mockResolvedValueOnce(archivedField);
    render(<FieldManagement organizations={[organizationA]} />);
    fireEvent.click(await openArchive());
    fireEvent.click(
      await screen.findByRole("button", {
        name: "Consultar estado del archivado",
      }),
    );
    await screen.findByText(/todavía figura como borrador/);
    expect(
      screen.queryByRole("button", { name: "Cancelar archivado" }),
    ).not.toBeInTheDocument();
    fireEvent.click(
      screen.getByRole("button", { name: "Reintentar mismo archivado" }),
    );
    await screen.findByText("Campo archivado. Su historial se conserva.");
    expect(apiMocks.archiveField.mock.calls[1]?.[0]).toEqual(
      apiMocks.archiveField.mock.calls[0]?.[0],
    );
    expect(apiMocks.archiveField.mock.calls[1]?.[0].version).toBe(
      fieldA.version,
    );
  });

  it.each(["stale", "conflict"] as const)(
    "requires reload and a new confirmation after %s archive rejection",
    async (failure) => {
      apiMocks.listFields.mockResolvedValue([fieldA]);
      apiMocks.getField
        .mockResolvedValueOnce(fieldA)
        .mockResolvedValueOnce({ ...fieldA, version: renamedVersion });
      apiMocks.archiveField
        .mockRejectedValueOnce(
          new FieldApiError(failure, failure === "stale" ? 412 : 409),
        )
        .mockResolvedValueOnce(archivedField);
      render(<FieldManagement organizations={[organizationA]} />);
      fireEvent.click(await openArchive());
      fireEvent.click(
        await screen.findByRole("button", {
          name: "Recargar y revisar archivado",
        }),
      );
      const confirm = await screen.findByRole("button", {
        name: "Confirmar archivado",
      });
      expect(apiMocks.archiveField).toHaveBeenCalledTimes(1);
      fireEvent.click(confirm);
      await screen.findByText("Campo archivado. Su historial se conserva.");
      const first = apiMocks.archiveField.mock.calls[0]?.[0];
      const next = apiMocks.archiveField.mock.calls[1]?.[0];
      expect(next?.version).toBe(renamedVersion);
      expect(next?.idempotencyKey).not.toBe(first?.idempotencyKey);
    },
  );

  it("restores a submitted archive without automatically writing and reconciles an archived field absent from the list", async () => {
    const attempt = {
      organizationId: fieldA.organizationId,
      fieldId: fieldA.fieldId,
      version: fieldA.version,
      idempotencyKey: "a".repeat(32),
    };
    globalThis.sessionStorage.setItem(
      archiveStorageKey,
      JSON.stringify(attempt),
    );
    apiMocks.listFields.mockResolvedValue([]);
    apiMocks.getField.mockResolvedValue(archivedField);
    render(<FieldManagement organizations={[organizationA]} />);
    await screen.findByText("Archivado");
    expect(apiMocks.archiveField).not.toHaveBeenCalled();
    expect(apiMocks.getField).toHaveBeenCalledWith(
      fieldA.organizationId,
      fieldA.fieldId,
      expect.any(AbortSignal),
    );
    fireEvent.click(
      screen.getByRole("button", { name: "Consultar estado del archivado" }),
    );
    await screen.findByText(
      "El campo está archivado. Su historial se conserva.",
    );
    expect(globalThis.sessionStorage.getItem(archiveStorageKey)).toBeNull();
    expectNoFullUuidInDom();
  });

  it("ignores late archive results after the authorized organization context changes", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField.mockResolvedValue(fieldA);
    let resolveArchive: ((field: ArchivedField) => void) | undefined;
    apiMocks.archiveField.mockReturnValue(
      new Promise((resolve) => {
        resolveArchive = resolve;
      }),
    );
    const view = render(<FieldManagement organizations={[organizationA]} />);
    fireEvent.click(await openArchive());
    apiMocks.listFields.mockResolvedValue([]);
    view.rerender(<FieldManagement organizations={[organizationB]} />);
    resolveArchive?.(archivedField);
    await waitFor(() =>
      expect(screen.queryByText("Archivado")).not.toBeInTheDocument(),
    );
    expect(
      screen.queryByText("Campo archivado. Su historial se conserva."),
    ).not.toBeInTheDocument();
    expect(globalThis.sessionStorage.getItem(archiveStorageKey)).toBeNull();
  });

  it("shows registered geometry in a mixed list and detail without claiming validation", async () => {
    const configured = {
      ...fieldB,
      spatialStatus: "configured",
    } satisfies FieldSummary;
    apiMocks.listFields.mockResolvedValue([fieldA, configured]);
    apiMocks.getField.mockResolvedValue(configured);
    render(<FieldManagement organizations={[organizationA]} />);

    expect(await screen.findByText("Geometría registrada")).toBeVisible();
    expect(screen.getByText("Sin geometría")).toBeVisible();
    fireEvent.click(
      screen.getByRole("button", { name: /Abrir ficha de Campo Sur/ }),
    );
    await screen.findByRole("heading", { name: "Campo Sur", level: 4 });
    expect(screen.getAllByText("Geometría registrada")).toHaveLength(2);
    expect(screen.getAllByText("Sin geometría")).toHaveLength(1);
    expect(screen.queryByText(/geometría validada/i)).not.toBeInTheDocument();
    expectNoFullUuidInDom();
  });

  it("keeps registered geometry visible after renaming a configured field", async () => {
    const configured = {
      ...fieldA,
      spatialStatus: "configured",
    } satisfies FieldSummary;
    apiMocks.listFields.mockResolvedValue([configured]);
    apiMocks.getField.mockResolvedValue(configured);
    apiMocks.renameField.mockResolvedValue({
      ...renamedFieldA,
      spatialStatus: "configured",
    });
    render(<FieldManagement organizations={[organizationA]} />);

    fireEvent.click(
      await screen.findByRole("button", { name: /abrir ficha/i }),
    );
    await screen.findByRole("heading", { name: "Campo Norte", level: 4 });
    fireEvent.click(screen.getByRole("button", { name: "Editar nombre" }));
    fireEvent.change(
      screen.getByRole("textbox", { name: "Nuevo nombre del campo" }),
      { target: { value: "Campo Sur" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Guardar nombre" }));

    await screen.findByRole("heading", { name: "Campo Sur", level: 4 });
    expect(screen.getAllByText("Geometría registrada")).toHaveLength(2);
    expect(screen.queryByText("Sin geometría")).not.toBeInTheDocument();
    expectNoFullUuidInDom();
  });

  it("reports dirty, pending and clear guards for an organization-scoped create attempt", async () => {
    let resolveCreate: ((field: CreatedField) => void) | undefined;
    apiMocks.createField.mockImplementation(
      () =>
        new Promise<CreatedField>((resolve) => {
          resolveCreate = resolve;
        }),
    );
    const onContextGuardChange = vi.fn();
    render(
      <FieldManagement
        onContextGuardChange={onContextGuardChange}
        organizations={[organizationA]}
      />,
    );
    await screen.findByText("Todavía no hay campos");
    expect(onContextGuardChange).toHaveBeenLastCalledWith("clear");

    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));
    fireEvent.change(
      screen.getByRole("textbox", { name: "Nombre del campo" }),
      { target: { value: "Campo Norte" } },
    );
    expect(onContextGuardChange).toHaveBeenLastCalledWith("dirty");

    fireEvent.click(screen.getByRole("button", { name: "Crear campo" }));
    await waitFor(() =>
      expect(onContextGuardChange).toHaveBeenLastCalledWith("pending"),
    );
    if (resolveCreate === undefined) {
      throw new Error("Create request did not start.");
    }
    resolveCreate({ ...fieldA, isReplay: false });
    await waitFor(() =>
      expect(onContextGuardChange).toHaveBeenLastCalledWith("clear"),
    );
  });

  it("keeps create success and its local detail visible without mutating the field locator", async () => {
    apiMocks.createField.mockResolvedValue({ ...fieldA, isReplay: false });
    const onDeepLinkIntent = vi.fn(() => true);
    render(
      <FieldManagement
        deepLink={{ kind: "none" }}
        onDeepLinkIntent={onDeepLinkIntent}
        organizations={[organizationA]}
      />,
    );

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
    expect(
      screen.getByRole("heading", { name: "Campo Norte", level: 4 }),
    ).toBeVisible();
    expect(screen.getByRole("button", { name: "Editar nombre" })).toBeVisible();
    expect(apiMocks.getField).not.toHaveBeenCalled();
    expect(onDeepLinkIntent).not.toHaveBeenCalled();
    expectNoFullUuidInDom();
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
      expect.any(AbortSignal),
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

  it("opens a canonical deep link, focuses its ficha and closes through the controlled intent", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField.mockResolvedValue(fieldA);
    const onDeepLinkIntent = vi.fn<(intent: FieldDeepLinkIntent) => void>();
    render(
      <ControlledFieldManagement
        initialDeepLink={{ kind: "requested", prefix: "F0185B" }}
        onIntent={onDeepLinkIntent}
      />,
    );

    const heading = await screen.findByRole("heading", {
      name: "Campo Norte",
      level: 4,
    });
    const detail = heading.closest("section");
    if (detail === null)
      throw new Error("Expected the deep-linked field detail.");
    expect(detail).toHaveFocus();
    expectNoFullUuidInDom();
    expect(apiMocks.getField).toHaveBeenCalledWith(
      organizationA.organizationId,
      fieldA.fieldId,
      expect.any(AbortSignal),
    );

    fireEvent.keyDown(detail, { key: "Escape" });

    expect(onDeepLinkIntent).toHaveBeenCalledWith({ kind: "clear" });
    expect(
      screen.queryByRole("heading", { name: "Campo Norte", level: 4 }),
    ).not.toBeInTheDocument();
    expect(
      await screen.findByRole("button", {
        name: "Abrir ficha de Campo Norte, campo F0185B",
      }),
    ).toHaveFocus();
  });

  it("emits a short select intent before issuing any controlled detail request", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    const onDeepLinkIntent = vi.fn(() => true);
    render(
      <FieldManagement
        deepLink={{ kind: "none" }}
        onDeepLinkIntent={onDeepLinkIntent}
        organizations={[organizationA]}
      />,
    );

    fireEvent.click(
      await screen.findByRole("button", {
        name: "Abrir ficha de Campo Norte, campo F0185B",
      }),
    );

    expect(onDeepLinkIntent).toHaveBeenCalledWith({
      kind: "select",
      prefix: "F0185B",
    });
    expect(apiMocks.getField).not.toHaveBeenCalled();
  });

  it("restores focus when browser history removes the active field locator", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField.mockResolvedValue(fieldA);
    const onDeepLinkIntent = vi.fn(() => true);
    const { rerender } = render(
      <FieldManagement
        deepLink={{ kind: "requested", prefix: "F0185B" }}
        onDeepLinkIntent={onDeepLinkIntent}
        organizations={[organizationA]}
      />,
    );
    const heading = await screen.findByRole("heading", {
      name: "Campo Norte",
      level: 4,
    });
    expect(heading.closest("section")).toHaveFocus();

    rerender(
      <FieldManagement
        deepLink={{ kind: "none" }}
        onDeepLinkIntent={onDeepLinkIntent}
        organizations={[organizationA]}
      />,
    );

    expect(
      await screen.findByRole("button", {
        name: "Abrir ficha de Campo Norte, campo F0185B",
      }),
    ).toHaveFocus();
    expect(
      screen.queryByRole("heading", { name: "Campo Norte", level: 4 }),
    ).not.toBeInTheDocument();
  });

  it.each([
    ["unknown", [fieldA], "FFFFFF", "unknown"],
    [
      "collision",
      [
        fieldA,
        {
          ...fieldA,
          fieldId: "f0185baa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
          displayName: "Campo colisionado",
        },
      ],
      "F0185B",
      "ambiguous",
    ],
    ["cross-tenant", [fieldA], "B2C3D4", "unknown"],
  ] as const)(
    "rejects a %s locator against the authorized list without requesting detail",
    async (_, items, prefix, reason) => {
      apiMocks.listFields.mockResolvedValue(items);
      const onDeepLinkIntent = vi.fn(() => true);
      render(
        <FieldManagement
          deepLink={{ kind: "requested", prefix }}
          onDeepLinkIntent={onDeepLinkIntent}
          organizations={[organizationA]}
        />,
      );

      await waitFor(() =>
        expect(onDeepLinkIntent).toHaveBeenCalledWith({
          kind: "reject",
          prefix,
          reason,
        }),
      );
      expect(apiMocks.getField).not.toHaveBeenCalled();
      expect(document.body).not.toHaveTextContent(foreignField.fieldId);
    },
  );

  it("renders an invalid field locator without consulting detail", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    const onDeepLinkIntent = vi.fn(() => true);
    render(
      <FieldManagement
        deepLink={{ kind: "invalid", reason: "format" }}
        onDeepLinkIntent={onDeepLinkIntent}
        organizations={[organizationA]}
      />,
    );

    expect(
      await screen.findByText(/c.digo corto del campo no es v.lido/i),
    ).toBeVisible();
    expect(apiMocks.getField).not.toHaveBeenCalled();
    expect(onDeepLinkIntent).not.toHaveBeenCalled();
  });

  it.each([
    ["offline", new TypeError("Failed to fetch")],
    ["rate-limited", new FieldApiError("rate-limited", 429)],
    ["service-unavailable", new FieldApiError("service-unavailable", 503)],
  ])(
    "preserves a requested locator when the list is %s",
    async (_, failure) => {
      apiMocks.listFields.mockRejectedValue(failure);
      const onDeepLinkIntent = vi.fn(() => true);
      render(
        <FieldManagement
          deepLink={{ kind: "requested", prefix: "F0185B" }}
          onDeepLinkIntent={onDeepLinkIntent}
          organizations={[organizationA]}
        />,
      );

      expect(await screen.findByRole("alert")).toBeVisible();
      expect(apiMocks.getField).not.toHaveBeenCalled();
      expect(onDeepLinkIntent).not.toHaveBeenCalled();
    },
  );

  it.each([
    ["signed-out", new FieldApiError("signed-out", 401), "signed-out"],
    ["unavailable", new FieldApiError("unavailable", 404), "unavailable"],
  ] as const)(
    "clears a requested locator when the authorized list becomes %s",
    async (_, failure, reason) => {
      apiMocks.listFields.mockRejectedValue(failure);
      const onDeepLinkIntent = vi.fn(() => true);
      render(
        <FieldManagement
          deepLink={{ kind: "requested", prefix: "F0185B" }}
          onDeepLinkIntent={onDeepLinkIntent}
          organizations={[organizationA]}
        />,
      );

      await waitFor(() =>
        expect(onDeepLinkIntent).toHaveBeenCalledWith({
          kind: "reject",
          prefix: "F0185B",
          reason,
        }),
      );
      expect(apiMocks.getField).not.toHaveBeenCalled();
    },
  );

  it.each([
    ["signed-out", new FieldApiError("signed-out", 401), "signed-out"],
    ["unavailable", new FieldApiError("unavailable", 404), "unavailable"],
  ] as const)(
    "clears a requested locator when detail becomes %s",
    async (_, failure, reason) => {
      apiMocks.listFields.mockResolvedValue([fieldA]);
      apiMocks.getField.mockRejectedValue(failure);
      const onDeepLinkIntent = vi.fn(() => true);
      render(
        <FieldManagement
          deepLink={{ kind: "requested", prefix: "F0185B" }}
          onDeepLinkIntent={onDeepLinkIntent}
          organizations={[organizationA]}
        />,
      );

      await waitFor(() =>
        expect(onDeepLinkIntent).toHaveBeenCalledWith({
          kind: "reject",
          prefix: "F0185B",
          reason,
        }),
      );
      expect(apiMocks.getField).toHaveBeenCalledOnce();
    },
  );

  it("ignores a late field-one detail after field two becomes current", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA, fieldB]);
    let resolveFieldA: ((field: FieldSummary) => void) | undefined;
    let resolveFieldB: ((field: FieldSummary) => void) | undefined;
    const detailSignals: AbortSignal[] = [];
    apiMocks.getField.mockImplementation((_organizationId, fieldId, signal) => {
      if (signal !== undefined) detailSignals.push(signal);
      return new Promise<FieldSummary>((resolve) => {
        if (fieldId === fieldA.fieldId) resolveFieldA = resolve;
        else resolveFieldB = resolve;
      });
    });
    const onDeepLinkIntent = vi.fn(() => true);
    const { rerender } = render(
      <FieldManagement
        deepLink={{ kind: "requested", prefix: "F0185B" }}
        onDeepLinkIntent={onDeepLinkIntent}
        organizations={[organizationA]}
      />,
    );
    await waitFor(() => expect(apiMocks.getField).toHaveBeenCalledOnce());

    rerender(
      <FieldManagement
        deepLink={{ kind: "requested", prefix: "A1B2C3" }}
        onDeepLinkIntent={onDeepLinkIntent}
        organizations={[organizationA]}
      />,
    );
    await waitFor(() => expect(apiMocks.getField).toHaveBeenCalledTimes(2));
    expect(detailSignals[0]?.aborted).toBe(true);

    resolveFieldB?.(fieldB);
    expect(
      await screen.findByRole("heading", { name: "Campo Sur", level: 4 }),
    ).toBeVisible();
    resolveFieldA?.(fieldA);
    await waitFor(() =>
      expect(
        screen.queryByRole("heading", { name: "Campo Norte", level: 4 }),
      ).not.toBeInTheDocument(),
    );
    expect(
      screen.getByRole("heading", { name: "Campo Sur", level: 4 }),
    ).toBeVisible();
  });

  it("aborts organization A and never renders it after switching to B", async () => {
    let resolveOrganizationA:
      ((fields: readonly FieldSummary[]) => void) | undefined;
    let resolveOrganizationB:
      ((fields: readonly FieldSummary[]) => void) | undefined;
    const listSignals = new Map<string, AbortSignal>();
    apiMocks.listFields.mockImplementation((organizationId, signal) => {
      if (signal !== undefined) listSignals.set(organizationId, signal);
      return new Promise<readonly FieldSummary[]>((resolve) => {
        if (organizationId === organizationA.organizationId) {
          resolveOrganizationA = resolve;
        } else {
          resolveOrganizationB = resolve;
        }
      });
    });
    apiMocks.getField.mockResolvedValue(foreignField);
    const onDeepLinkIntent = vi.fn(() => true);
    const { rerender } = render(
      <FieldManagement
        deepLink={{ kind: "requested", prefix: "F0185B" }}
        onDeepLinkIntent={onDeepLinkIntent}
        organizations={[organizationA]}
      />,
    );
    await waitFor(() => expect(apiMocks.listFields).toHaveBeenCalledOnce());

    rerender(
      <FieldManagement
        deepLink={{ kind: "requested", prefix: "B2C3D4" }}
        onDeepLinkIntent={onDeepLinkIntent}
        organizations={[organizationB]}
      />,
    );
    await waitFor(() => expect(apiMocks.listFields).toHaveBeenCalledTimes(2));
    expect(listSignals.get(organizationA.organizationId)?.aborted).toBe(true);

    resolveOrganizationB?.([foreignField]);
    expect(
      await screen.findByRole("heading", { name: "Campo Ajeno", level: 4 }),
    ).toBeVisible();
    resolveOrganizationA?.([fieldA]);
    await waitFor(() =>
      expect(screen.queryByText("Campo Norte")).not.toBeInTheDocument(),
    );
    expect(apiMocks.getField).toHaveBeenLastCalledWith(
      organizationB.organizationId,
      foreignField.fieldId,
      expect.any(AbortSignal),
    );
  });

  it("ignores a terminal detail from organization A after organization B takes the same prefix", async () => {
    const fieldWithSamePrefixInB: FieldSummary = {
      ...fieldA,
      fieldId: "f0185baa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
      organizationId: organizationB.organizationId,
      displayName: "Campo vigente en B",
      version: "7c7ed442-141f-4fd4-a8e8-52598a5d7426",
    };
    let rejectOrganizationA: ((reason: FieldApiError) => void) | undefined;
    let resolveOrganizationBList:
      ((fields: readonly FieldSummary[]) => void) | undefined;
    apiMocks.listFields.mockImplementation((organizationId) => {
      if (organizationId === organizationA.organizationId) {
        return Promise.resolve([fieldA]);
      }
      return new Promise<readonly FieldSummary[]>((resolve) => {
        resolveOrganizationBList = resolve;
      });
    });
    apiMocks.getField.mockImplementation((organizationId) => {
      if (organizationId === organizationA.organizationId) {
        return new Promise<FieldSummary>((_resolve, reject) => {
          rejectOrganizationA = reject;
        });
      }
      return Promise.resolve(fieldWithSamePrefixInB);
    });
    const onDeepLinkIntent = vi.fn(() => true);
    const { rerender } = render(
      <FieldManagement
        deepLink={{ kind: "requested", prefix: "F0185B" }}
        onDeepLinkIntent={onDeepLinkIntent}
        organizations={[organizationA]}
      />,
    );
    await waitFor(() => expect(apiMocks.getField).toHaveBeenCalledOnce());

    rerender(
      <FieldManagement
        deepLink={{ kind: "requested", prefix: "F0185B" }}
        onDeepLinkIntent={onDeepLinkIntent}
        organizations={[organizationB]}
      />,
    );
    await waitFor(() =>
      expect(apiMocks.listFields).toHaveBeenCalledWith(
        organizationB.organizationId,
        expect.any(AbortSignal),
      ),
    );
    rejectOrganizationA?.(
      new FieldApiError("unavailable", 404, "productive.field_not_found"),
    );
    await waitFor(() => expect(onDeepLinkIntent).not.toHaveBeenCalled());

    resolveOrganizationBList?.([fieldWithSamePrefixInB]);
    expect(
      await screen.findByRole("heading", {
        name: "Campo vigente en B",
        level: 4,
      }),
    ).toBeVisible();
    expect(apiMocks.getField).toHaveBeenLastCalledWith(
      organizationB.organizationId,
      fieldWithSamePrefixInB.fieldId,
      expect.any(AbortSignal),
    );
    expect(onDeepLinkIntent).not.toHaveBeenCalled();
    expect(screen.queryByText("Campo Norte")).not.toBeInTheDocument();
    expectNoFullUuidInDom();
  });

  it("renames only after confirmation and keeps the short field ID", async () => {
    let resolveRename: ((field: RenamedField) => void) | undefined;
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField.mockResolvedValue(fieldA);
    apiMocks.renameField.mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveRename = resolve;
        }),
    );
    render(<FieldManagement organizations={[organizationA]} />);
    fireEvent.click(
      await screen.findByRole("button", { name: /abrir ficha/i }),
    );
    await screen.findByRole("heading", { name: "Campo Norte", level: 4 });
    fireEvent.click(screen.getByRole("button", { name: "Editar nombre" }));
    const input = screen.getByRole("textbox", {
      name: "Nuevo nombre del campo",
    });
    expect(input).toHaveFocus();
    expect(input).toHaveValue("Campo Norte");
    expect(input).not.toHaveAttribute("maxlength");
    fireEvent.change(input, { target: { value: "  Campo Sur  " } });
    fireEvent.click(screen.getByRole("button", { name: "Guardar nombre" }));

    await waitFor(() => expect(apiMocks.renameField).toHaveBeenCalledOnce());
    const request = apiMocks.renameField.mock.calls[0]?.[0];
    expect(request).toEqual({
      organizationId: organizationA.organizationId,
      fieldId: fieldA.fieldId,
      displayName: "Campo Sur",
      version: fieldA.version,
      idempotencyKey: expect.stringMatching(/^[0-9a-f]{32}$/),
    });
    expect(
      screen.getByRole("heading", { name: "Campo Norte", level: 4 }),
    ).toBeVisible();
    expect(
      screen.getByRole("button", { name: "Guardando nombre" }),
    ).toBeDisabled();

    resolveRename?.(renamedFieldA);
    expect(
      await screen.findByRole("heading", { name: "Campo Sur", level: 4 }),
    ).toBeVisible();
    expect(screen.getAllByText("Campo F0185B")).toHaveLength(2);
    expect(screen.queryByText(fieldA.fieldId)).not.toBeInTheDocument();
    expect(screen.getByText(/ahora se llama Campo Sur/i)).toBeVisible();
  });

  it("cancels rename with Escape and restores focus to its trigger", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField.mockResolvedValue(fieldA);
    render(<FieldManagement organizations={[organizationA]} />);
    fireEvent.click(
      await screen.findByRole("button", { name: /abrir ficha/i }),
    );
    const trigger = await screen.findByRole("button", {
      name: "Editar nombre",
    });
    fireEvent.click(trigger);
    const input = screen.getByRole("textbox", {
      name: "Nuevo nombre del campo",
    });
    fireEvent.change(input, { target: { value: "Campo Sur" } });
    fireEvent.keyDown(input, { key: "Escape" });

    expect(
      screen.queryByRole("textbox", { name: "Nuevo nombre del campo" }),
    ).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Editar nombre" })).toHaveFocus();
    expect(apiMocks.renameField).not.toHaveBeenCalled();
  });

  it("rejects an unchanged canonical name without sending PATCH", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField.mockResolvedValue(fieldA);
    render(<FieldManagement organizations={[organizationA]} />);
    fireEvent.click(
      await screen.findByRole("button", { name: /abrir ficha/i }),
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Editar nombre" }),
    );
    fireEvent.click(screen.getByRole("button", { name: "Guardar nombre" }));

    expect(
      await screen.findByText(/nombre válido y distinto del actual/i),
    ).toBeVisible();
    expect(apiMocks.renameField).not.toHaveBeenCalled();
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.fields.rename-attempt.v1",
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
  ])("retains the rename attempt after %s", async (_scenario, failure) => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField.mockResolvedValue(fieldA);
    apiMocks.renameField.mockRejectedValue(failure);
    render(<FieldManagement organizations={[organizationA]} />);
    fireEvent.click(
      await screen.findByRole("button", { name: /abrir ficha/i }),
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Editar nombre" }),
    );
    fireEvent.change(
      screen.getByRole("textbox", { name: "Nuevo nombre del campo" }),
      { target: { value: "Campo Sur" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Guardar nombre" }));

    await waitFor(() => expect(apiMocks.renameField).toHaveBeenCalledOnce());
    const key = apiMocks.renameField.mock.calls[0]?.[0].idempotencyKey;
    const serialized = globalThis.sessionStorage.getItem(
      "agropecuaria.fields.rename-attempt.v1",
    );
    expect(serialized).not.toBeNull();
    expect(JSON.parse(serialized ?? "null")).toMatchObject({
      organizationId: organizationA.organizationId,
      fieldId: fieldA.fieldId,
      baseDisplayName: "Campo Norte",
      displayName: "Campo Sur",
      version: fieldA.version,
      idempotencyKey: key,
    });
  });

  it.each([
    [
      "neutral 404",
      new FieldApiError("unavailable", 404, "resource.not_available"),
      /campo no está disponible/i,
    ],
    [
      "idempotency mismatch",
      new FieldApiError("conflict", 409, "idempotency.key_reused"),
      /clave ya corresponde a otro cambio/i,
    ],
  ])(
    "clears the terminal rename attempt after %s",
    async (_scenario, failure, message) => {
      apiMocks.listFields.mockResolvedValue([fieldA]);
      apiMocks.getField.mockResolvedValue(fieldA);
      apiMocks.renameField.mockRejectedValue(failure);
      render(<FieldManagement organizations={[organizationA]} />);
      fireEvent.click(
        await screen.findByRole("button", { name: /abrir ficha/i }),
      );
      fireEvent.click(
        await screen.findByRole("button", { name: "Editar nombre" }),
      );
      fireEvent.change(
        screen.getByRole("textbox", { name: "Nuevo nombre del campo" }),
        { target: { value: "Campo Sur" } },
      );
      fireEvent.click(screen.getByRole("button", { name: "Guardar nombre" }));

      expect(await screen.findByText(message)).toBeVisible();
      expect(
        globalThis.sessionStorage.getItem(
          "agropecuaria.fields.rename-attempt.v1",
        ),
      ).toBeNull();
    },
  );

  it("resumes an ambiguous rename after reload with the same key", async () => {
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField.mockResolvedValue(fieldA);
    apiMocks.renameField
      .mockRejectedValueOnce(new Error("Response lost after send"))
      .mockResolvedValueOnce({ ...renamedFieldA, isReplay: true });
    render(<FieldManagement organizations={[organizationA]} />);
    fireEvent.click(
      await screen.findByRole("button", { name: /abrir ficha/i }),
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Editar nombre" }),
    );
    fireEvent.change(
      screen.getByRole("textbox", { name: "Nuevo nombre del campo" }),
      { target: { value: "Campo Sur" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Guardar nombre" }));
    await waitFor(() => expect(apiMocks.renameField).toHaveBeenCalledOnce());
    const originalKey = apiMocks.renameField.mock.calls[0]?.[0].idempotencyKey;

    cleanup();
    const onContextGuardChange = vi.fn();
    const onDeepLinkIntent = vi.fn(() => true);
    render(
      <FieldManagement
        onContextGuardChange={onContextGuardChange}
        onDeepLinkIntent={onDeepLinkIntent}
        organizations={[organizationA]}
      />,
    );
    expect(
      await screen.findByText(/retomamos el cambio pendiente/i),
    ).toBeVisible();
    expect(onDeepLinkIntent).toHaveBeenCalledWith({
      kind: "restore",
      prefix: "F0185B",
    });
    expect(onContextGuardChange).toHaveBeenLastCalledWith("pending");
    expect(
      screen.getByRole("textbox", { name: "Nuevo nombre del campo" }),
    ).toHaveValue("Campo Sur");
    fireEvent.click(screen.getByRole("button", { name: "Guardar nombre" }));

    await waitFor(() => expect(apiMocks.renameField).toHaveBeenCalledTimes(2));
    expect(apiMocks.renameField.mock.calls[1]?.[0].idempotencyKey).toBe(
      originalKey,
    );
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.fields.rename-attempt.v1",
      ),
    ).toBeNull();
    expect(screen.getByText(/confirmamos el cambio anterior/i)).toBeVisible();
  });

  it("reloads a stale field, preserves the draft and retries on the new version", async () => {
    const concurrentField = {
      ...fieldA,
      displayName: "Campo Este",
      version: "c081ae09-ea8a-4be6-8bb2-844dd75d9872",
    };
    apiMocks.listFields.mockResolvedValue([fieldA]);
    apiMocks.getField
      .mockResolvedValueOnce(fieldA)
      .mockResolvedValueOnce(concurrentField);
    apiMocks.renameField
      .mockRejectedValueOnce(
        new FieldApiError(
          "stale",
          412,
          "productive_core.management_unit_version_stale",
        ),
      )
      .mockResolvedValueOnce(renamedFieldA);
    render(<FieldManagement organizations={[organizationA]} />);
    fireEvent.click(
      await screen.findByRole("button", { name: /abrir ficha/i }),
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Editar nombre" }),
    );
    fireEvent.change(
      screen.getByRole("textbox", { name: "Nuevo nombre del campo" }),
      { target: { value: "Campo Sur" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Guardar nombre" }));

    expect(
      await screen.findByRole("button", { name: "Recargar y revisar" }),
    ).toBeVisible();
    const firstKey = apiMocks.renameField.mock.calls[0]?.[0].idempotencyKey;
    expect(
      globalThis.sessionStorage.getItem(
        "agropecuaria.fields.rename-attempt.v1",
      ),
    ).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Recargar y revisar" }));

    expect(
      await screen.findByRole("heading", { name: "Campo Este", level: 4 }),
    ).toBeVisible();
    expect(
      screen.getByRole("textbox", { name: "Nuevo nombre del campo" }),
    ).toHaveValue("Campo Sur");
    fireEvent.click(screen.getByRole("button", { name: "Guardar nombre" }));

    await waitFor(() => expect(apiMocks.renameField).toHaveBeenCalledTimes(2));
    expect(apiMocks.renameField.mock.calls[1]?.[0]).toMatchObject({
      displayName: "Campo Sur",
      version: concurrentField.version,
    });
    expect(apiMocks.renameField.mock.calls[1]?.[0].idempotencyKey).not.toBe(
      firstKey,
    );
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
