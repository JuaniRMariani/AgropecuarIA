import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

test("switches scenarios and exposes every operational state", async ({
  page,
}) => {
  await page.goto("/");

  await expect(
    page.getByRole("heading", { name: "Capacidad sin humo." }),
  ).toBeVisible();
  await expect(page.getByText("INCOMPLETO / NO-GO")).toBeVisible();

  await page.getByLabel("Escenario sintético").selectOption("growth-10x");
  await expect(
    page.getByRole("heading", { name: "Crecimiento diez veces" }),
  ).toBeVisible();
  await expect(
    page.getByText("100.000", { exact: true }).first(),
  ).toBeVisible();

  const states = [
    ["loading", "Preparando estimación"],
    ["empty", "Todavía no hay mediciones reales"],
    ["error", "No se pudo calcular"],
    ["stale", "Estimación fuera de vigencia"],
    ["degraded", "Capacidad parcial"],
    ["conflict", "El escenario cambió"],
    ["provider-down", "Dependencia externa no disponible"],
  ];

  for (const [value, title] of states) {
    await page.getByLabel("Estado a inspeccionar").selectOption(value);
    await expect(page.getByTestId("lab-state")).toContainText(title);
  }
});

test("blocks real offline work and creates no client-side persistence", async ({
  context,
  page,
}) => {
  const serverMarkup = await (await page.request.get("/")).text();
  expect(serverMarkup).toContain("Comprobando conexión");
  expect(serverMarkup).toContain('disabled=""');

  await page.goto("/");
  await context.setOffline(true);

  await expect(page.getByTestId("connection-status")).toContainText(
    "Navegador sin conexión",
  );
  await expect(page.getByTestId("operation-button")).toBeDisabled();
  await expect(page.getByText("No guardamos trabajo offline.")).toBeVisible();
  await expect(page.getByTestId("operation-message")).toContainText(
    "No se creó una cola local",
  );

  const persisted = await page.evaluate(() => ({
    localStorage: window.localStorage.length,
    sessionStorage: window.sessionStorage.length,
    serviceWorker:
      "serviceWorker" in navigator &&
      navigator.serviceWorker.controller !== null,
  }));
  expect(persisted).toEqual({
    localStorage: 0,
    sessionStorage: 0,
    serviceWorker: false,
  });

  await context.setOffline(false);
});

test("retries with one key and deduplicates a repeated confirmation", async ({
  page,
}) => {
  await page.goto("/");

  await expect(page.locator(".key-policy")).toContainText(
    "clave de sesión protegida",
  );
  await page.getByTestId("operation-button").click();
  await expect(page.getByTestId("operation-message")).toContainText(
    "Fallo transitorio simulado",
  );
  await page.getByTestId("operation-button").click();
  await expect(page.getByTestId("operation-message")).toContainText(
    "Operación aceptada una vez",
  );
  await page.getByTestId("operation-button").click();
  await expect(page.getByTestId("operation-message")).toContainText(
    "operación aceptada sigue siendo una sola",
  );
  await expect(page.getByTestId("operation-button")).toBeDisabled();
});

test("is keyboard reachable, serious-error free and narrow-screen safe", async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");

  await page.keyboard.press("Tab");
  await expect(
    page.getByRole("link", { name: "Saltar al contenido" }),
  ).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.locator("#contenido")).toBeFocused();

  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > window.innerWidth,
  );
  expect(overflow).toBe(false);

  const results = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"])
    .analyze();
  const blocking = results.violations.filter(
    (violation) =>
      violation.impact === "critical" || violation.impact === "serious",
  );
  expect(blocking).toEqual([]);
});
