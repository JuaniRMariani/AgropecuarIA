import {
  act,
  cleanup,
  fireEvent,
  render,
  screen,
  within,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
  getCatalogItem,
  listCatalogVersions,
  searchCatalog,
} from "../../features/catalog/catalog-api";
import type { CatalogSearch } from "../../features/catalog/catalog-types";
import {
  catalogItem,
  catalogResult,
  catalogVersions,
  currentVersion,
  historicalVersion,
} from "../fixtures/catalog";

const api = vi.hoisted(() => ({
  searchCatalog: vi.fn<typeof searchCatalog>(),
  getCatalogItem: vi.fn<typeof getCatalogItem>(),
  listCatalogVersions: vi.fn<typeof listCatalogVersions>(),
}));
vi.mock("../../features/catalog/catalog-api", async (original) => ({
  ...(await original<typeof import("../../features/catalog/catalog-api")>()),
  ...api,
}));
import { CatalogApiError } from "../../features/catalog/catalog-api";
import { CatalogReader } from "../../features/catalog/catalog-reader";

async function tick(milliseconds = 300) {
  await act(async () => {
    vi.advanceTimersByTime(milliseconds);
    await Promise.resolve();
  });
}
function expectNoFullUuid() {
  expect(document.body.outerHTML).not.toMatch(
    /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i,
  );
}
function deferred<T>() {
  let resolvePromise: (value: T) => void = () => undefined;
  const promise = new Promise<T>((resolve) => {
    resolvePromise = resolve;
  });
  return { promise, resolve: resolvePromise };
}
beforeEach(() => {
  vi.useFakeTimers();
  api.searchCatalog.mockReset().mockResolvedValue(catalogResult);
  api.getCatalogItem.mockReset().mockResolvedValue(catalogItem);
  api.listCatalogVersions.mockReset().mockResolvedValue(catalogVersions);
});
afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.clearAllMocks();
});

describe("CatalogReader", () => {
  it("keeps controls usable and only marks the stable results region busy", async () => {
    render(<CatalogReader />);
    const region = screen.getByRole("region", {
      name: "Resultados del catálogo",
    });
    expect(region).toHaveClass("catalog-table-viewport");
    expect(region).toHaveAttribute("aria-busy", "true");
    expect(screen.getByRole("searchbox")).toBeEnabled();
    expect(screen.getByRole("combobox", { name: "Categoría" })).toBeEnabled();
    await tick();
    expect(region).toHaveAttribute("aria-busy", "false");
    expect(screen.getByRole("table")).toBeVisible();
    expect(screen.getByText("Maíz de prueba")).toBeVisible();
    expectNoFullUuid();
  });
  it("debounces text and all filters without requiring an apply button", async () => {
    render(<CatalogReader />);
    await tick();
    const input = screen.getByRole("searchbox");
    input.focus();
    fireEvent.change(input, { target: { value: "m" } });
    await tick(100);
    fireEvent.change(input, { target: { value: "maiz" } });
    fireEvent.change(screen.getByRole("textbox", { name: "Jurisdicción" }), {
      target: { value: "AR" },
    });
    fireEvent.change(screen.getByRole("combobox", { name: "Categoría" }), {
      target: { value: "AGRICULTURA" },
    });
    fireEvent.change(
      screen.getByRole("combobox", { name: "Soporte declarado" }),
      { target: { value: "FLUJO_GENERICO" } },
    );
    await tick(299);
    expect(api.searchCatalog).toHaveBeenCalledTimes(1);
    await tick(1);
    expect(api.searchCatalog).toHaveBeenCalledTimes(2);
    expect(api.searchCatalog).toHaveBeenLastCalledWith(
      {
        query: "maiz",
        jurisdiction: "AR",
        category: "AGRICULTURA",
        supportLevel: "FLUJO_GENERICO",
        versionId: null,
      },
      expect.any(AbortSignal),
    );
    expect(input).toHaveFocus();
    expect(
      screen.queryByRole("button", { name: /aplicar|buscar/i }),
    ).not.toBeInTheDocument();
  });
  it("aborts old search requests and ignores their late results", async () => {
    const first = deferred<CatalogSearch>();
    api.searchCatalog
      .mockReturnValueOnce(first.promise)
      .mockResolvedValueOnce({ ...catalogResult, totalCount: 0, items: [] });
    const view = render(<CatalogReader />);
    await tick();
    const firstSignal = api.searchCatalog.mock.calls[0]?.[1];
    fireEvent.change(screen.getByRole("searchbox"), {
      target: { value: "no coincide" },
    });
    expect(firstSignal?.aborted).toBe(true);
    await tick();
    await act(async () => first.resolve(catalogResult));
    expect(screen.getByText("No encontramos coincidencias")).toBeVisible();
    expect(screen.queryByText("Maíz de prueba")).not.toBeInTheDocument();
    const lastSignal = api.searchCatalog.mock.calls[1]?.[1];
    view.unmount();
    expect(lastSignal?.aborted).toBe(true);
  });
  it("pins detail to its row version and exposes provenance, missing capabilities and safe focus", async () => {
    render(<CatalogReader />);
    await tick();
    const trigger = screen.getByRole("button", { name: /Ver detalle de Maíz/ });
    fireEvent.click(trigger);
    await tick(0);
    expect(api.getCatalogItem).toHaveBeenCalledWith(
      "MAIZ",
      currentVersion.id,
      expect.any(AbortSignal),
    );
    const detail = screen.getByRole("region", { name: "Detalle de actividad" });
    expect(detail).toHaveFocus();
    expect(
      within(detail).getByText("Fuente declarada: fuente-sintetica-local"),
    ).toBeVisible();
    expect(
      within(detail).getByText("Reglas técnicas especializadas"),
    ).toBeVisible();
    expect(
      within(detail).getByText("Indicadores especializados"),
    ).toBeVisible();
    expect(within(detail).getByText("Recomendaciones de IA")).toBeVisible();
    expect(
      within(detail).getByText("No hay capacidades activas informadas."),
    ).toBeVisible();
    expect(
      within(detail).getByText(/No certifica una fuente oficial/),
    ).toBeVisible();
    expectNoFullUuid();
    fireEvent.keyDown(detail, { key: "Escape" });
    expect(trigger).toHaveFocus();
    expect(
      screen.queryByRole("region", { name: "Detalle de actividad" }),
    ).not.toBeInTheDocument();
  });
  it("labels legacy provenance without treating a legacy support string as validation", async () => {
    api.getCatalogItem.mockResolvedValue({
      ...catalogItem,
      supportLevel: "ESPECIALIZADA_VALIDADA",
      provenanceStatus: "legacy_unavailable",
      sourceSnapshotId: null,
      sourceId: null,
      sourceHash: null,
      sourceIngestedAtUtc: null,
    });
    render(<CatalogReader />);
    await tick();
    fireEvent.click(
      screen.getByRole("button", { name: /Ver detalle de Maíz/ }),
    );
    await tick(0);
    const detail = screen.getByRole("region", { name: "Detalle de actividad" });
    expect(within(detail).getByText("Especialización declarada")).toBeVisible();
    expect(
      within(detail).getByText(/Procedencia histórica no disponible/),
    ).toBeVisible();
    expect(within(detail).getByText(/no habilita por sí solo/)).toBeVisible();
    expect(
      within(detail).queryByText(/Fuente declarada:/),
    ).not.toBeInTheDocument();
  });
  it("selects immutable history through opaque DOM options and can explicitly return to active", async () => {
    const historical = {
      ...catalogItem,
      versionId: historicalVersion.id,
      versionTag: historicalVersion.versionTag,
    };
    api.searchCatalog
      .mockResolvedValueOnce(catalogResult)
      .mockResolvedValueOnce({
        ...catalogResult,
        versionId: historicalVersion.id,
        versionTag: historicalVersion.versionTag,
        publishedAtUtc: historicalVersion.publishedAtUtc,
        isHistorical: true,
        items: [historical],
      })
      .mockResolvedValueOnce(catalogResult);
    api.getCatalogItem.mockResolvedValue(historical);
    render(<CatalogReader />);
    await tick();
    fireEvent.change(
      screen.getByRole("combobox", { name: "Versión consultada" }),
      { target: { value: "version-1" } },
    );
    await tick();
    expect(api.searchCatalog.mock.calls[1]?.[0].versionId).toBe(
      historicalVersion.id,
    );
    expect(
      screen.getByText(/Estás consultando una versión histórica/),
    ).toBeVisible();
    fireEvent.click(
      screen.getByRole("button", { name: /Ver detalle de Maíz/ }),
    );
    await tick(0);
    expect(api.getCatalogItem.mock.calls[0]?.[1]).toBe(historicalVersion.id);
    expectNoFullUuid();
    fireEvent.click(
      screen.getByRole("button", { name: "Consultar versión activa" }),
    );
    await tick();
    expect(api.searchCatalog.mock.calls[2]?.[0].versionId).toBeNull();
    expect(
      screen.queryByText(/Estás consultando una versión histórica/),
    ).not.toBeInTheDocument();
  });
  it("announces an observed active version change without claiming realtime freshness", async () => {
    api.searchCatalog
      .mockResolvedValueOnce(catalogResult)
      .mockResolvedValueOnce({
        ...catalogResult,
        activeVersionId: historicalVersion.id,
        isHistorical: true,
        items: [{ ...catalogItem, activeVersionId: historicalVersion.id }],
      });
    render(<CatalogReader />);
    await tick();
    fireEvent.change(screen.getByRole("searchbox"), {
      target: { value: "maiz" },
    });
    await tick();
    expect(
      screen.getByText(/La versión activa cambió desde la consulta anterior/),
    ).toBeVisible();
  });
  it("distinguishes no publication from zero matches", async () => {
    api.searchCatalog
      .mockResolvedValueOnce({
        versionId: null,
        versionTag: null,
        activeVersionId: null,
        publishedAtUtc: null,
        isHistorical: false,
        totalCount: 0,
        items: [],
      })
      .mockResolvedValueOnce({ ...catalogResult, items: [], totalCount: 0 });
    render(<CatalogReader />);
    await tick();
    expect(
      screen.getByText("Todavía no hay un catálogo publicado"),
    ).toBeVisible();
    fireEvent.change(screen.getByRole("searchbox"), {
      target: { value: "ninguno" },
    });
    await tick();
    expect(screen.getByText("No encontramos coincidencias")).toBeVisible();
  });
  it.each([
    "signed-out",
    "forbidden",
    "not-found",
    "rate-limited",
    "unavailable",
    "contract",
  ] as const)(
    "keeps the shell usable after %s and offers a scoped retry",
    async (kind) => {
      api.searchCatalog
        .mockRejectedValueOnce(new CatalogApiError(kind, 503))
        .mockResolvedValueOnce(catalogResult);
      render(<CatalogReader />);
      await tick();
      expect(screen.getByRole("alert")).toBeVisible();
      expect(
        screen.getByText("No pudimos determinar la versión de esta consulta."),
      ).toBeVisible();
      expect(
        screen.queryByText("Sin versión publicada para esta consulta."),
      ).not.toBeInTheDocument();
      expect(screen.getByRole("searchbox")).toBeEnabled();
      fireEvent.click(
        screen.getByRole("button", { name: "Reintentar consulta" }),
      );
      await tick();
      expect(screen.getByText("Maíz de prueba")).toBeVisible();
    },
  );
  it("retries history independently without replacing successful search results", async () => {
    api.listCatalogVersions
      .mockRejectedValueOnce(new CatalogApiError("unavailable", 503))
      .mockResolvedValueOnce(catalogVersions);
    render(<CatalogReader />);
    await tick();
    expect(screen.getByText("Maíz de prueba")).toBeVisible();
    fireEvent.click(
      screen.getByRole("button", { name: "Reintentar versiones" }),
    );
    await tick(0);
    expect(api.listCatalogVersions).toHaveBeenCalledTimes(2);
    expect(api.searchCatalog).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    expect(
      screen.getByRole("option", { name: /Fixture sintético v1/ }),
    ).toBeInTheDocument();
  });
  it("retries a failed detail with the same immutable version and leaves the filters unchanged", async () => {
    api.getCatalogItem
      .mockRejectedValueOnce(new CatalogApiError("unavailable", 503))
      .mockResolvedValueOnce(catalogItem);
    render(<CatalogReader />);
    await tick();
    fireEvent.click(
      screen.getByRole("button", { name: /Ver detalle de Maíz/ }),
    );
    await tick(0);
    expect(screen.getByRole("alert")).toBeVisible();
    fireEvent.click(screen.getByRole("button", { name: "Reintentar detalle" }));
    await tick(0);
    expect(api.getCatalogItem).toHaveBeenCalledTimes(2);
    expect(api.getCatalogItem).toHaveBeenLastCalledWith(
      "MAIZ",
      currentVersion.id,
      expect.any(AbortSignal),
    );
    expect(api.searchCatalog).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "Maíz de prueba" }),
    ).toBeVisible();
  });
  it("paginates versions without changing the consulted dataset", async () => {
    api.listCatalogVersions
      .mockResolvedValueOnce({
        ...catalogVersions,
        totalCount: 21,
        hasMore: true,
      })
      .mockResolvedValueOnce({
        ...catalogVersions,
        versions: [historicalVersion],
        totalCount: 21,
      });
    render(<CatalogReader />);
    await tick();
    fireEvent.click(
      screen.getByRole("button", { name: "Más versiones históricas" }),
    );
    await tick(0);
    expect(api.listCatalogVersions).toHaveBeenLastCalledWith(
      20,
      expect.any(AbortSignal),
    );
    expect(api.searchCatalog).toHaveBeenCalledTimes(1);
    expect(
      screen.getByRole("button", { name: "Más versiones históricas" }),
    ).toBeDisabled();
    expectNoFullUuid();
  });
  it("cancels and hides a pending detail when filters change", async () => {
    const pending = deferred<typeof catalogItem>();
    api.getCatalogItem.mockReturnValue(pending.promise);
    render(<CatalogReader />);
    await tick();
    fireEvent.click(
      screen.getByRole("button", { name: /Ver detalle de Maíz/ }),
    );
    const signal = api.getCatalogItem.mock.calls[0]?.[2];
    fireEvent.change(screen.getByRole("searchbox"), {
      target: { value: "otro" },
    });
    expect(signal?.aborted).toBe(true);
    await act(async () => pending.resolve(catalogItem));
    expect(
      screen.queryByRole("region", { name: "Detalle de actividad" }),
    ).not.toBeInTheDocument();
  });
});
