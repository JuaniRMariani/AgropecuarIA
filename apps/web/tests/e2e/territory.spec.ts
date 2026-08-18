import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

async function signIn(page: import("@playwright/test").Page): Promise<void> {
  const token = await page.evaluate(async () => {
    const response = await fetch("/api/identity/antiforgery", {
      cache: "no-store",
      credentials: "include",
    });
    const payload = (await response.json()) as { token?: unknown };
    if (typeof payload.token !== "string") {
      throw new Error("Antiforgery token is missing.");
    }
    return payload.token;
  });

  const status = await page.evaluate(async (csrfToken) => {
    const response = await fetch("/api/development/identity/sign-in", {
      method: "POST",
      cache: "no-store",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        "X-CSRF-TOKEN": csrfToken,
      },
      body: JSON.stringify({ fixture: "email-owner" }),
    });
    return response.status;
  }, token);

  expect(status).toBe(204);
}

async function openTerritoryWorkspace(
  page: import("@playwright/test").Page,
  projectName: string,
): Promise<void> {
  const createFirstOrganization = page.getByRole("button", {
    name: "Crear mi organización",
  });
  const selector = page.getByRole("heading", { name: "Elegir organización" });
  const activeWorkspace = page.getByRole("heading", { name: /^Espacio de / });
  await expect(
    createFirstOrganization.or(selector).or(activeWorkspace),
  ).toBeVisible();

  if (await createFirstOrganization.isVisible()) {
    await createFirstOrganization.click();
    await page
      .getByRole("textbox", { name: "Nombre de la organización" })
      .fill(`Territorio E2E ${projectName}`);
    await page.getByRole("button", { name: "Crear organización" }).click();
    await expect(
      page.getByRole("button", { name: "Territorio" }),
    ).toBeVisible();
  } else if (await selector.isVisible()) {
    await page
      .getByRole("listitem")
      .filter({ hasText: "Organización " })
      .first()
      .getByRole("button")
      .click();
  }

  await page.getByRole("button", { name: "Territorio" }).click();
  await expect(
    page.getByRole("button", { name: "Territorio" }),
  ).toHaveAttribute("aria-current", "page");
}

test("searches the official territory snapshot reactively and by keyboard", async ({
  page,
}, testInfo) => {
  if (testInfo.project.name === "mobile") {
    await page.setViewportSize({ width: 390, height: 844 });
  }
  await page.goto("/");
  await signIn(page);
  await page.reload();
  await openTerritoryWorkspace(page, testInfo.project.name);
  await page.route("**/api/territory/search?*", async (route) => {
    await new Promise((resolve) => setTimeout(resolve, 300));
    await route.continue();
  });

  const search = page.getByRole("combobox", {
    name: "Provincia, departamento, gobierno local o localidad",
  });
  await expect(search).toBeVisible();
  await search.fill("cordoba");

  const results = page.getByRole("region", {
    name: "Resultados territoriales",
  });
  await expect(results).toHaveAttribute("aria-busy", "true");
  await expect(page.getByRole("option", { name: /Córdoba/i })).toBeVisible();
  await search.press("ArrowDown");
  await search.press("Enter");
  await expect(
    page.getByRole("article", { name: "Referencia seleccionada" }),
  ).toContainText("Córdoba");
  await expect(results).toContainText(
    /Referencia vigente|Última referencia válida/,
  );

  await page.getByRole("textbox", { name: "Latitud" }).fill("-34.6037");
  await page.getByRole("textbox", { name: "Longitud" }).fill("-58.3816");
  await page.getByRole("button", { name: "Resolver referencia" }).click();
  await expect(
    page.getByText("Sin resolución automática", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Ir a búsqueda manual" }),
  ).toBeVisible();

  const accessibility = await new AxeBuilder({ page })
    .include(".territory-explorer")
    .withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"])
    .analyze();
  expect(accessibility.violations).toEqual([]);
});
