import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

test.describe("laboratorio territorial", () => {
  test("expone las 24 jurisdicciones y una alternativa equivalente al mapa", async ({
    page,
  }) => {
    const browserErrors: string[] = [];
    page.on("console", (message) => {
      if (message.type() === "error") browserErrors.push(message.text());
    });

    await page.goto("/");

    await expect(page.getByRole("heading", { level: 1 })).toContainText(
      "Cobertura territorial",
    );
    await expect(page.getByRole("table")).toBeVisible();
    await expect(page.getByRole("row")).toHaveCount(25);
    await expect(page.getByText("24 de 24")).toBeVisible();
    await expect(
      page.getByText("Observado", { exact: true }).first(),
    ).toBeVisible();
    await expect(
      page.getByText("Estimado", { exact: true }).first(),
    ).toBeVisible();
    await expect(
      page.getByText("Pronosticado", { exact: true }).first(),
    ).toBeVisible();
    await expect(
      page.getByText(/no representan clima ni mediciones/i),
    ).toBeVisible();
    const table = page.getByRole("table");
    await expect(table.getByText("Observado", { exact: true })).toHaveCount(24);
    await expect(table.getByText("Vigente", { exact: true })).toHaveCount(24);
    await expect(table.getByText("Estimado", { exact: true })).toHaveCount(0);
    await expect(table.getByText("Pronosticado", { exact: true })).toHaveCount(
      0,
    );
    await expect(
      page.getByText("Vigente", { exact: true }).first(),
    ).toBeVisible();

    const accessibility = await new AxeBuilder({ page }).analyze();
    expect(accessibility.violations).toEqual([]);
    expect(browserErrors).toEqual([]);
  });

  test("mantiene la tabla cuando el proveedor cartográfico está caído", async ({
    page,
  }) => {
    await page.goto("/");
    await page.getByLabel("Proveedor caído").check();

    await expect(page.getByRole("status")).toContainText(
      "Argenmap no responde",
    );
    await expect(page.getByRole("table")).toBeVisible();
    await expect(page.getByRole("row")).toHaveCount(25);
    await expect(
      page.getByText("No disponible", { exact: true }).first(),
    ).toBeVisible();
    await expect(
      page.getByRole("region", { name: /Mapa de referencia/ }),
    ).toHaveCount(0);
  });

  test("permite recorrer controles con teclado y recuperarse del error", async ({
    page,
  }) => {
    await page.goto("/");
    await page.locator(".map-loading").waitFor({ state: "hidden" });
    await page
      .getByRole("link", { name: "Saltar a la cobertura nacional" })
      .focus();
    await page.keyboard.press("Enter");
    await expect(page.locator("#cobertura")).toBeInViewport();

    await page.getByRole("radio", { name: /^Error/ }).focus();
    await page.keyboard.press("Space");
    await expect(page.locator(".state-notice--error")).toContainText(
      "No pudimos preparar",
    );
    await page
      .getByRole("button", { name: "Reintentar con el fixture" })
      .click();
    await expect(page.getByRole("table")).toBeVisible();
  });

  test("conserva lectura usable en 390 píxeles", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto("/");

    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    await expect(page.getByRole("table")).toBeVisible();
    await expect(page.locator("body")).toHaveCSS("overflow-x", "visible");
    const bodyWidth = await page
      .locator("body")
      .evaluate((element) => element.scrollWidth);
    expect(bodyWidth).toBeLessThanOrEqual(390);
  });
});
