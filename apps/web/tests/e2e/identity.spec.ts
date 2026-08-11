import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

import type { Page } from "@playwright/test";

const browserErrors = new WeakMap<Page, string[]>();
const expectedSignedOutConsoleError =
  "Failed to load resource: the server responded with a status of 401 (Unauthorized)";

async function expectNoAccessibilityViolations(page: Page) {
  const result = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"])
    .analyze();
  expect(result.violations).toEqual([]);
}

test.describe("identity access", () => {
  test.beforeEach(async ({ page }) => {
    const errors: string[] = [];
    browserErrors.set(page, errors);
    page.on("pageerror", (error) => errors.push(`pageerror: ${error.message}`));
    page.on("console", (message) => {
      if (
        message.type() === "error" &&
        message.text() !== expectedSignedOutConsoleError
      ) {
        errors.push(`console: ${message.text()}`);
      }
    });
  });

  test.afterEach(async ({ page }) => {
    expect(browserErrors.get(page) ?? []).toEqual([]);
  });

  test("signs in with the local fixture and links a second verified identity", async ({
    page,
  }) => {
    await page.goto("/");
    await expectNoAccessibilityViolations(page);

    const fixtureSignIn = page.getByRole("button", {
      name: "Entrar con fixture seguro",
    });
    await expect(fixtureSignIn).toBeVisible();
    await fixtureSignIn.focus();
    await expect(fixtureSignIn).toBeFocused();
    await page.keyboard.press("Enter");

    await expect(
      page.getByRole("heading", { name: "Tu acceso", exact: true }),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Crear mi organización" }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Crear mi organización" }).click();
    const organizationName = page.getByRole("textbox", {
      name: "Nombre de la organización",
    });
    await organizationName.fill("Establecimiento La Esperanza");
    await page.getByRole("button", { name: "Crear organización" }).click();
    await expect(
      page.getByText("Establecimiento La Esperanza ya está lista y sos owner."),
    ).toBeVisible();
    await expect(
      page.getByRole("heading", { name: "Tus organizaciones" }),
    ).toBeVisible();
    await expect(page.getByText("owner", { exact: true })).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Crear otra" }),
    ).toBeVisible();

    const linkGoogle = page.getByRole("button", {
      name: "Vincular Google",
      exact: true,
    });
    if (await linkGoogle.isVisible()) {
      await linkGoogle.focus();
      await expect(linkGoogle).toBeFocused();
      await page.keyboard.press("Enter");
      await expect(
        page.getByText("La identidad quedó vinculada y verificada."),
      ).toBeVisible();
    }

    await expect(
      page.getByText("Correo verificado", { exact: true }),
    ).toBeVisible();
    await expect(
      page.getByText("Google", { exact: true }).first(),
    ).toBeVisible();
    await expect(page.getByLabel("2 identidades")).toBeVisible();

    const verifyMfa = page.getByRole("button", {
      name: "Verificar con MFA",
      exact: true,
    });
    await expect(verifyMfa).toBeVisible();
    await verifyMfa.focus();
    await expect(verifyMfa).toBeFocused();
    await page.keyboard.press("Enter");
    await expect(page.getByText("MFA vigente", { exact: true })).toBeVisible();
    await expect(
      page.getByText(/verificación reforzada confirmada/i),
    ).toBeVisible();
    await expect(page.getByText(/custodiados por el proveedor/i)).toBeVisible();
    await expectNoAccessibilityViolations(page);
  });

  test("keeps the critical flow keyboard reachable in a narrow viewport", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto("/");

    const signIn = page.getByRole("button", {
      name: "Entrar con fixture seguro",
    });
    const sessionHeading = page.getByRole("heading", {
      name: "Tu acceso",
      exact: true,
    });
    await expect(signIn).toBeVisible();
    for (let tab = 0; tab < 4; tab += 1) {
      await page.keyboard.press("Tab");
    }
    await expect(signIn).toBeFocused();
    await page.keyboard.press("Enter");
    await expect(sessionHeading).toBeVisible();

    const createOrganization = page.getByRole("button", {
      name: "Crear mi organización",
    });
    await createOrganization.focus();
    await expect(createOrganization).toBeFocused();
    await page.keyboard.press("Enter");
    const organizationName = page.getByRole("textbox", {
      name: "Nombre de la organización",
    });
    await organizationName.fill("Campo móvil");
    await organizationName.press("Enter");
    await expect(
      page.getByText("Campo móvil ya está lista y sos owner."),
    ).toBeVisible();
    await expectNoAccessibilityViolations(page);

    const revoke = page.getByRole("button", { name: "Cerrar sesión" });
    await revoke.focus();
    await expect(revoke).toBeFocused();
    page.once("dialog", async (dialog) => {
      expect(dialog.message()).toContain("revocar esta sesión");
      await dialog.dismiss();
    });
    await page.keyboard.press("Enter");
    await expect(sessionHeading).toBeVisible();

    const hasPageOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > window.innerWidth,
    );
    expect(hasPageOverflow).toBe(false);

    const box = await revoke.boundingBox();
    expect(box).not.toBeNull();
    expect(box?.x ?? -1).toBeGreaterThanOrEqual(0);
    expect((box?.x ?? 0) + (box?.width ?? 0)).toBeLessThanOrEqual(390);
  });
});
