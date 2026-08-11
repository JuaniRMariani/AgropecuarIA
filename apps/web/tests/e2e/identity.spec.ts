import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

import type { Page } from "@playwright/test";

const browserErrors = new WeakMap<Page, string[]>();
const expectedSignedOutConsoleError =
  "Failed to load resource: the server responded with a status of 401 (Unauthorized)";

function trackBrowserErrors(page: Page): void {
  trackBrowserErrorsWithAllowList(page, [expectedSignedOutConsoleError]);
}

function trackBrowserErrorsWithAllowList(
  page: Page,
  allowedConsoleErrors: readonly string[],
): void {
  const errors: string[] = [];
  browserErrors.set(page, errors);
  page.on("pageerror", (error) => errors.push(`pageerror: ${error.message}`));
  page.on("console", (message) => {
    if (
      message.type() === "error" &&
      !allowedConsoleErrors.includes(message.text())
    ) {
      errors.push(`console: ${message.text()}`);
    }
  });
}

async function signInWithExplicitFixture(
  page: Page,
  fixture: string,
): Promise<void> {
  const antiforgeryToken = await page.evaluate(async () => {
    const response = await fetch("/api/identity/antiforgery", {
      cache: "no-store",
      credentials: "include",
    });
    const payload: unknown = await response.json();
    if (
      typeof payload !== "object" ||
      payload === null ||
      !("token" in payload) ||
      typeof payload.token !== "string"
    ) {
      throw new Error(
        "Synthetic sign-in did not receive an antiforgery token.",
      );
    }
    return payload.token;
  });
  const status = await page.evaluate(
    async ({ token, selectedFixture }) => {
      const response = await fetch("/api/development/identity/sign-in", {
        method: "POST",
        cache: "no-store",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
          "X-CSRF-TOKEN": token,
        },
        body: JSON.stringify({ fixture: selectedFixture }),
      });
      return response.status;
    },
    { token: antiforgeryToken, selectedFixture: fixture },
  );
  expect(status).toBe(204);
}

async function expectNoAccessibilityViolations(page: Page) {
  const result = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"])
    .analyze();
  expect(result.violations).toEqual([]);
}

test.describe("identity access", () => {
  test.beforeEach(async ({ page }) => {
    trackBrowserErrors(page);
  });

  test.afterEach(async ({ page }) => {
    expect(browserErrors.get(page) ?? []).toEqual([]);
  });

  test("signs in with the local fixture and links a second verified identity", async ({
    browser,
    page,
  }, testInfo) => {
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
    const startOrganization = page.getByRole("button", {
      name: /Crear (mi organización|otra)/,
    });
    await expect(startOrganization).toBeVisible();
    await startOrganization.click();
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

    const createOwnerInvitation = page
      .getByRole("button", { name: "Verificar y crear enlace" })
      .first();
    await createOwnerInvitation.focus();
    await expect(createOwnerInvitation).toBeFocused();
    await page.keyboard.press("Enter");
    await expect(
      page.getByRole("heading", { name: "Guardá el enlace ahora" }),
    ).toBeVisible();
    const invitationLink = await page
      .getByRole("textbox", { name: "Enlace de invitación" })
      .inputValue();
    const invitationUrl = new URL(invitationLink);
    expect(invitationUrl.search).toBe("");
    expect(invitationUrl.hash).toMatch(/^#owner-invitation=[A-Za-z0-9_-]{43}$/);

    const inviteeFixture =
      testInfo.project.name === "chromium" ? "email-owner-2" : "email-owner-4";
    const attackerFixture =
      testInfo.project.name === "chromium" ? "email-owner-3" : "email-owner-1";
    const inviteeContext = await browser.newContext();
    const inviteePage = await inviteeContext.newPage();
    trackBrowserErrors(inviteePage);
    await inviteePage.goto(invitationLink);
    await expect(
      inviteePage.getByRole("heading", { name: "Protegemos este acceso" }),
    ).toBeVisible();
    await signInWithExplicitFixture(inviteePage, inviteeFixture);
    await inviteePage.reload();
    await expect(
      inviteePage.getByText(
        "Ya sos co-owner de Establecimiento La Esperanza.",
        {
          exact: true,
        },
      ),
    ).toBeVisible();
    await expect(inviteePage).not.toHaveURL(/owner-invitation=/);
    await expect(
      inviteePage.getByText("owner", { exact: true }).first(),
    ).toBeVisible();
    expect(browserErrors.get(inviteePage) ?? []).toEqual([]);
    await inviteeContext.close();

    const attackerContext = await browser.newContext();
    const attackerPage = await attackerContext.newPage();
    trackBrowserErrorsWithAllowList(attackerPage, [
      expectedSignedOutConsoleError,
      "Failed to load resource: the server responded with a status of 404 (Not Found)",
    ]);
    await attackerPage.goto(invitationLink);
    await expect(
      attackerPage.getByRole("heading", { name: "Protegemos este acceso" }),
    ).toBeVisible();
    await signInWithExplicitFixture(attackerPage, attackerFixture);
    await attackerPage.reload();
    await expect(
      attackerPage.getByRole("heading", { name: "Enlace no disponible" }),
    ).toBeVisible();
    expect(browserErrors.get(attackerPage) ?? []).toEqual([]);
    await attackerContext.close();

    await page.reload();
    await expect(
      page.getByText("Aceptada", { exact: true }).first(),
    ).toBeVisible();

    await page
      .getByRole("button", { name: "Verificar y crear enlace" })
      .first()
      .click();
    await expect(
      page.getByRole("heading", { name: "Guardá el enlace ahora" }),
    ).toBeVisible();
    page.once("dialog", async (dialog) => {
      expect(dialog.message()).toContain("Revocar este enlace");
      await dialog.accept();
    });
    await page.getByRole("button", { name: "Revocar" }).click();
    await expect(
      page.getByText("Revocada", { exact: true }).first(),
    ).toBeVisible();
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
      name: /Crear (mi organización|otra)/,
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
