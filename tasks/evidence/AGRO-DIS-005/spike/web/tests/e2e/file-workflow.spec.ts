import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";
import type { Page } from "@playwright/test";

const pdfFile = {
  name: "informe-lote.pdf",
  mimeType: "application/pdf",
  buffer: Buffer.from("%PDF-1.7 synthetic fixture"),
};

async function reachScanning(page: Page) {
  await page.getByLabel("Archivo de prueba").setInputFiles(pdfFile);
  await page.getByRole("button", { name: "Cargar en cuarentena" }).click();
  await page.getByRole("button", { name: "Avanzar transferencia" }).click();
  await expect(page.getByRole("progressbar")).toHaveAttribute("value", "50");
  await page.getByRole("button", { name: "Avanzar transferencia" }).click();
  await expect(page.locator("[data-state='scanning']")).toBeVisible();
}

test.beforeEach(async ({ page }) => {
  await page.goto("/");
});

test("recorre carga, análisis, descarga y vencimiento sin exponer el UUID completo", async ({
  page,
}) => {
  await reachScanning(page);
  await page
    .getByRole("button", { name: "Completar análisis sintético" })
    .click();
  await expect(page.locator("[data-state='available']")).toBeVisible();
  await expect(page.getByText("19F64E", { exact: true })).toBeVisible();
  await expect(
    page.getByText("19f64ec7-21ca-4762-bac8-02d233cf57e2"),
  ).toHaveCount(0);

  await page
    .getByRole("button", { name: "Descargar con enlace temporal" })
    .click();
  await expect(
    page.getByText(/Descarga sintética autorizada y auditada/i),
  ).toBeVisible();

  await page.getByRole("button", { name: "Vencer enlace de prueba" }).click();
  await expect(page.getByText(/enlace temporal venció/i)).toBeVisible();
  await page.getByRole("button", { name: "Renovar enlace temporal" }).click();
  await expect(
    page.getByText(/descarga temporal está habilitada/i),
  ).toBeVisible();
});

test("aplica fail-closed cuando el antivirus no está disponible", async ({
  page,
}) => {
  await page.getByLabel("Resultado a simular").selectOption("scan_unavailable");
  await reachScanning(page);
  await page
    .getByRole("button", { name: "Completar análisis sintético" })
    .click();
  await expect(page.locator("[data-state='scan_unavailable']")).toContainText(
    "archivo en cuarentena",
  );
  await expect(page.getByRole("button", { name: /descargar/i })).toHaveCount(0);
  await page
    .getByRole("button", { name: "Reintentar de forma segura" })
    .click();
  await expect(page.locator("[data-state='scanning']")).toBeVisible();
});

test("bloquea una amenaza sintética y conserva visibles las decisiones legales pendientes", async ({
  page,
}) => {
  await page.getByLabel("Resultado a simular").selectOption("quarantine");
  await reachScanning(page);
  await page
    .getByRole("button", { name: "Completar análisis sintético" })
    .click();
  await expect(page.locator("[data-state='quarantined']")).toContainText(
    "Amenaza sintética detectada",
  );
  await expect(
    page.getByText("Pendiente de política y revisión legal"),
  ).toHaveCount(2);
});

test("representa validación, proveedor caído, conflicto y carga interrumpida", async ({
  page,
}) => {
  await page.getByLabel("Archivo de prueba").setInputFiles({
    name: "declaracion.html",
    mimeType: "text/html",
    buffer: Buffer.from("<p>fixture</p>"),
  });
  await expect(page.locator("[data-state='validation_error']")).toContainText(
    "tipo declarado no está permitido",
  );

  for (const scenario of [
    "provider_unavailable",
    "conflict",
    "upload_error",
  ] as const) {
    await page.getByRole("button", { name: "Reiniciar ensayo" }).click();
    await page.getByLabel("Resultado a simular").selectOption(scenario);
    await reachScanning(page);
    await page
      .getByRole("button", { name: "Completar análisis sintético" })
      .click();
    await expect(page.locator(`[data-state='${scenario}']`)).toBeVisible();
    await page
      .getByRole("button", { name: "Reintentar de forma segura" })
      .click();
    await expect(page.locator("[data-state='ready']")).toBeVisible();
  }
});

test("no presenta violaciones axe serias y cabe en una pantalla rural angosta", async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  const accessibility = await new AxeBuilder({ page }).analyze();
  const serious = accessibility.violations.filter(
    (violation) =>
      violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
  expect(
    await page.evaluate(() => document.documentElement.scrollWidth),
  ).toBeLessThanOrEqual(390);
});
