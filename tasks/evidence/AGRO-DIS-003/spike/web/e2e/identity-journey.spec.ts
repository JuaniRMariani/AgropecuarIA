import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

const contextRailName = "Contexto de acceso actual";

async function expectState(page: Page, state: string, heading: string): Promise<void> {
  await expect(page.getByRole("heading", { level: 1, name: heading })).toBeFocused();
  await expect(
    page.getByRole("complementary", { name: contextRailName }).getByText(state, { exact: true }),
  ).toBeVisible();

  const accessibility = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"])
    .analyze();
  expect(accessibility.violations).toEqual([]);
}

async function startLogin(page: Page): Promise<void> {
  await page.getByRole("button", { name: "Continuar con identidad digital" }).click();
  await expectState(page, "loading", "Estamos consultando tu identidad.");
}

test("recorre todos los estados críticos sin errores de accesibilidad ni consola", async ({ page }, testInfo) => {
  const consoleProblems: string[] = [];
  page.on("console", (message) => {
    if (message.type() === "error" || message.type() === "warning") {
      consoleProblems.push(`${message.type()}: ${message.text()}`);
    }
  });
  page.on("pageerror", (error) => consoleProblems.push(`pageerror: ${error.message}`));

  await page.goto("/");
  await expectState(page, "signed-out", "Volvé al campo con tu contexto a la vista.");
  await page.keyboard.press("Tab");
  await expect(page.getByRole("button", { name: "Continuar con identidad digital" })).toBeFocused();

  await page.reload();
  await page.getByRole("button", { name: "No puedo acceder a mi cuenta" }).click();
  await expectState(page, "recovery-accepted", "Revisá tus medios de contacto.");

  await page.reload();
  await page.getByRole("button", { name: "Ver límite de intentos" }).click();
  await expectState(page, "rate-limited", "Pausamos los intentos por unos minutos.");

  await page.reload();
  await startLogin(page);
  await page.getByRole("button", { name: "Sin organizaciones" }).click();
  await expectState(page, "zero-organizations", "Todavía no tenés una organización habilitada.");

  await page.reload();
  await startLogin(page);
  await page.getByRole("button", { name: "Una organización" }).click();
  await expectState(page, "one-organization", "Encontramos una organización.");
  await expect(page.getByText("Organización A10B2C", { exact: true })).toBeVisible();

  await page.reload();
  await startLogin(page);
  await page.getByRole("button", { name: "Proveedor no disponible" }).click();
  await expectState(page, "provider-down", "No pudimos verificar tu identidad.");

  await page.reload();
  await startLogin(page);
  await page.getByRole("button", { name: "Varias organizaciones" }).click();
  await expectState(page, "many-organizations", "¿En qué contexto vas a trabajar?");
  await page.getByRole("button", { name: "Elegir La Rinconada" }).click();
  await expectState(page, "active", "Estás trabajando en La Rinconada.");
  await page.getByRole("button", { name: "Vincular otro acceso" }).click();
  await expectState(page, "linking", "Confirmá ambos accesos.");
  await expect(page.getByRole("button", { name: "Confirmar vinculación" })).toBeDisabled();
  await page.getByRole("button", { name: "Simular conflicto" }).click();
  await expectState(page, "linking-conflict", "Ese acceso ya pertenece a otra identidad.");

  await page.reload();
  await startLogin(page);
  await page.getByRole("button", { name: "Una organización" }).click();
  await page.getByRole("button", { name: "Entrar a esta organización" }).click();
  await page.setViewportSize({ width: 390, height: 844 });
  await expectState(page, "active", "Estás trabajando en La Rinconada.");
  await expect.poll(async () => page.evaluate(() => document.documentElement.scrollWidth)).toBe(390);
  await page.screenshot({ path: testInfo.outputPath("active-mobile.png"), fullPage: true });
  await page.getByRole("button", { name: "Simular acceso revocado" }).click();
  await expectState(page, "revoked", "Tu permiso cambió y cerramos la sesión.");

  expect(consoleProblems).toEqual([]);
});
