import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

import type { Page } from "@playwright/test";

type CreatedOrganizationFixture = Readonly<{
  organizationId: string;
  displayName: string;
}>;

type CreatedFieldFixture = Readonly<{
  fieldId: string;
  organizationId: string;
  displayName: string;
}>;

function shortId(value: string): string {
  return value.replaceAll("-", "").slice(0, 6).toUpperCase();
}

async function expectNoFullUuidInDom(page: Page): Promise<void> {
  const leaks = await page.locator("main").evaluate((root) => {
    const uuidPattern =
      /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i;
    const excludedSelector = "script, style, template";
    const findings: string[] = [];
    const elements = [root, ...root.querySelectorAll("*")];

    for (const element of elements) {
      if (element.matches(excludedSelector)) continue;
      for (const attribute of element.attributes) {
        if (uuidPattern.test(attribute.value)) {
          findings.push(
            `attribute:${element.tagName.toLowerCase()}:${attribute.name}`,
          );
        }
      }
    }

    const textNodes = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    let node = textNodes.nextNode();
    while (node !== null) {
      const parent = node.parentElement;
      if (
        parent !== null &&
        parent.closest(excludedSelector) === null &&
        uuidPattern.test(node.nodeValue ?? "")
      ) {
        findings.push(`text:${parent.tagName.toLowerCase()}`);
      }
      node = textNodes.nextNode();
    }

    return findings;
  });
  expect(leaks).toEqual([]);
}

async function antiforgeryToken(page: Page): Promise<string> {
  return page.evaluate(async () => {
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
      throw new Error("Antiforgery response is invalid.");
    }
    return payload.token;
  });
}

async function signIn(page: Page): Promise<void> {
  const token = await antiforgeryToken(page);
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

async function createOrganization(
  page: Page,
  displayName: string,
): Promise<CreatedOrganizationFixture> {
  const token = await antiforgeryToken(page);
  return page.evaluate(
    async ({ csrfToken, name }) => {
      const bytes = crypto.getRandomValues(new Uint8Array(16));
      const idempotencyKey = Array.from(bytes, (value) =>
        value.toString(16).padStart(2, "0"),
      ).join("");
      const response = await fetch("/api/identity/organizations", {
        method: "POST",
        cache: "no-store",
        credentials: "include",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
          "Idempotency-Key": idempotencyKey,
          "X-CSRF-TOKEN": csrfToken,
        },
        body: JSON.stringify({ displayName: name }),
      });
      const payload: unknown = await response.json();
      if (
        response.status !== 201 ||
        typeof payload !== "object" ||
        payload === null ||
        !("organization" in payload) ||
        typeof payload.organization !== "object" ||
        payload.organization === null ||
        !("organizationId" in payload.organization) ||
        typeof payload.organization.organizationId !== "string"
      ) {
        throw new Error(`Organization fixture failed with ${response.status}.`);
      }
      return {
        organizationId: payload.organization.organizationId,
        displayName: name,
      };
    },
    { csrfToken: token, name: displayName },
  );
}

async function createField(
  page: Page,
  organization: CreatedOrganizationFixture,
  displayName: string,
): Promise<CreatedFieldFixture> {
  const token = await antiforgeryToken(page);
  return page.evaluate(
    async ({ csrfToken, organizationId, name }) => {
      const bytes = crypto.getRandomValues(new Uint8Array(16));
      const idempotencyKey = Array.from(bytes, (value) =>
        value.toString(16).padStart(2, "0"),
      ).join("");
      const response = await fetch(
        `/api/organizations/${encodeURIComponent(organizationId)}/fields`,
        {
          method: "POST",
          cache: "no-store",
          credentials: "include",
          headers: {
            Accept: "application/json",
            "Content-Type": "application/json",
            "Idempotency-Key": idempotencyKey,
            "X-CSRF-TOKEN": csrfToken,
          },
          body: JSON.stringify({ displayName: name }),
        },
      );
      const payload: unknown = await response.json();
      if (
        response.status !== 201 ||
        typeof payload !== "object" ||
        payload === null ||
        !("fieldId" in payload) ||
        typeof payload.fieldId !== "string"
      ) {
        throw new Error(`Field fixture failed with ${response.status}.`);
      }
      return {
        fieldId: payload.fieldId,
        organizationId,
        displayName: name,
      };
    },
    {
      csrfToken: token,
      organizationId: organization.organizationId,
      name: displayName,
    },
  );
}

async function expectNoSeriousAccessibilityViolations(page: Page) {
  const result = await new AxeBuilder({ page })
    .include(".owner-workspace")
    .withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"])
    .analyze();
  expect(
    result.violations.filter(
      ({ impact }) => impact === "critical" || impact === "serious",
    ),
  ).toEqual([]);
}

test.describe("owner workspace", () => {
  test("cancels field archival without sending a write and restores keyboard focus", async ({
    page,
  }) => {
    await page.goto("/");
    await signIn(page);
    const organization = await createOrganization(
      page,
      `Cancelación de archivado ${Date.now()}`,
    );
    const field = await createField(page, organization, "Campo conservado");
    const fieldPath = `/api/organizations/${organization.organizationId}/fields/${field.fieldId}`;
    const archiveRequests: string[] = [];
    page.on("request", (request) => {
      if (
        new URL(request.url()).pathname === `${fieldPath}/archive` &&
        request.method() === "POST"
      )
        archiveRequests.push(request.url());
    });

    await page.goto(
      `/?org=${shortId(organization.organizationId)}&view=fields`,
    );
    await page
      .getByRole("button", {
        name: `Abrir ficha de ${field.displayName}, campo ${shortId(field.fieldId)}`,
      })
      .click();
    const archive = page.getByRole("button", {
      name: "Archivar campo",
      exact: true,
    });
    await archive.click();
    const confirmation = page.getByRole("group", {
      name: `Archivar campo ${shortId(field.fieldId)}`,
    });
    const confirm = confirmation.getByRole("button", {
      name: "Confirmar archivado",
    });
    await expect(confirm).toBeFocused();
    await expect(confirmation).toContainText("No se borrará su historial");
    await expectNoFullUuidInDom(page);
    expect(archiveRequests).toEqual([]);

    await confirm.press("Escape");
    await expect(confirmation).toBeHidden();
    await expect(archive).toBeFocused();
    await archive.press("Enter");
    await confirmation
      .getByRole("button", { name: "Cancelar archivado" })
      .click();
    await expect(confirmation).toBeHidden();
    await expect(archive).toBeFocused();
    expect(archiveRequests).toEqual([]);

    const response = await page.request.get(fieldPath);
    expect(response.status()).toBe(200);
    const payload: unknown = await response.json();
    expect(payload).toMatchObject({ fieldId: field.fieldId, status: "draft" });
    await page.getByRole("button", { name: "Cerrar ficha" }).click();
    await expect(
      page.getByRole("button", {
        name: `Abrir ficha de ${field.displayName}, campo ${shortId(field.fieldId)}`,
      }),
    ).toBeVisible();
    expect(archiveRequests).toEqual([]);
  });

  test("archives a real field, refreshes the active list and preserves a read-only summary", async ({
    page,
  }, testInfo) => {
    testInfo.setTimeout(60_000);
    await page.goto("/");
    await signIn(page);
    const organization = await createOrganization(
      page,
      `Archivado confirmado ${Date.now()}`,
    );
    const field = await createField(page, organization, "Campo para archivar");
    const survivor = await createField(page, organization, "Campo activo");
    const listPath = `/api/organizations/${organization.organizationId}/fields`;
    const fieldPath = `${listPath}/${field.fieldId}`;
    await page.goto(
      `/?org=${shortId(organization.organizationId)}&view=fields`,
    );
    const openField = page.getByRole("button", {
      name: `Abrir ficha de ${field.displayName}, campo ${shortId(field.fieldId)}`,
    });
    await openField.click();
    await page
      .getByRole("button", { name: "Archivar campo", exact: true })
      .click();
    const confirmation = page.getByRole("group", {
      name: `Archivar campo ${shortId(field.fieldId)}`,
    });
    const confirm = confirmation.getByRole("button", {
      name: "Confirmar archivado",
    });
    await expect(confirm).toBeFocused();
    await expect(
      page.getByRole("button", { name: "Cerrar ficha" }),
    ).toBeDisabled();
    await expectNoSeriousAccessibilityViolations(page);
    await expectNoFullUuidInDom(page);

    // Observe the real browser requests; this flow does not intercept or stub HTTP.
    const archivedResponse = page.waitForResponse(
      (response) =>
        new URL(response.url()).pathname === `${fieldPath}/archive` &&
        response.request().method() === "POST",
    );
    const refreshedListResponse = page.waitForResponse(
      (response) =>
        new URL(response.url()).pathname === listPath &&
        response.request().method() === "GET",
    );
    await confirm.click();
    const response = await archivedResponse;
    expect(response.status()).toBe(200);
    const payload: unknown = await response.json();
    expect(payload).toMatchObject({
      fieldId: field.fieldId,
      organizationId: organization.organizationId,
      status: "archived",
      isReplay: false,
      revision: 2,
    });
    const headers = await response.request().allHeaders();
    expect(headers["x-csrf-token"]).toBeTruthy();
    expect(headers["idempotency-key"]).toMatch(/^[A-Za-z0-9_-]{32,128}$/);
    expect(headers["if-match"]).toMatch(/^"[0-9a-f-]{36}"$/i);
    expect(response.request().postData()).toBeNull();

    const refreshedList = await refreshedListResponse;
    expect(refreshedList.status()).toBe(200);
    const activeFields: unknown = await refreshedList.json();
    expect(activeFields).toEqual([
      expect.objectContaining({ fieldId: survivor.fieldId, status: "draft" }),
    ]);
    await expect(
      page.getByText("Campo archivado. Su historial se conserva.", {
        exact: true,
      }),
    ).toBeVisible();
    await expect(openField).toBeHidden();
    await expect(
      page.getByRole("button", {
        name: `Abrir ficha de ${survivor.displayName}, campo ${shortId(survivor.fieldId)}`,
      }),
    ).toBeVisible();
    const detail = page.locator(".field-detail").filter({
      has: page.getByRole("heading", { name: field.displayName, level: 4 }),
    });
    await expect(detail.getByText("Archivado", { exact: true })).toBeVisible();
    await expect(
      detail.getByRole("button", { name: "Editar nombre" }),
    ).toBeHidden();
    await expect(
      detail.getByRole("button", { name: "Archivar campo", exact: true }),
    ).toBeHidden();
    await expect(page).toHaveURL(
      new RegExp(`&field=${shortId(field.fieldId)}$`),
    );
    await expectNoFullUuidInDom(page);
    await expectNoSeriousAccessibilityViolations(page);

    await detail.getByRole("button", { name: "Cerrar ficha" }).click();
    await expect(
      page.getByRole("heading", { name: "Tus campos" }),
    ).toBeFocused();
    await page.reload();
    await expect(
      page.getByRole("button", {
        name: `Abrir ficha de ${survivor.displayName}, campo ${shortId(survivor.fieldId)}`,
      }),
    ).toBeVisible();
    await expect(openField).toBeHidden();
    const persistedResponse = await page.request.get(fieldPath);
    expect(persistedResponse.status()).toBe(200);
    const persisted: unknown = await persistedResponse.json();
    expect(persisted).toMatchObject({
      fieldId: field.fieldId,
      displayName: field.displayName,
      status: "archived",
    });
  });

  test("isolates duplicate-labelled organizations across selector, history and mobile layout", async ({
    page,
  }, testInfo) => {
    testInfo.setTimeout(90_000);
    if (testInfo.project.name === "mobile") {
      await page.setViewportSize({ width: 390, height: 844 });
    }

    await page.goto("/");
    await signIn(page);
    const sharedOrganizationName = `Establecimiento gemelo ${Date.now()}`;
    const organizationA = await createOrganization(
      page,
      sharedOrganizationName,
    );
    const organizationB = await createOrganization(
      page,
      sharedOrganizationName,
    );
    const fieldA = await createField(page, organizationA, "Lote compartido");
    const fieldB = await createField(page, organizationB, "Lote compartido");

    const tenantRequests: string[] = [];
    page.on("request", (request) => {
      const url = request.url();
      if (
        url.includes("/fields") ||
        url.includes("/owner-memberships") ||
        url.includes("/owner-invitations")
      ) {
        tenantRequests.push(url);
      }
    });
    await page.goto("/");

    await expect(
      page.getByRole("heading", { name: "Elegir organización" }),
    ).toBeVisible();
    await expect(page.getByText("Lote compartido")).toBeHidden();
    expect(tenantRequests).toEqual([]);

    const organizationAChoice = page.getByRole("listitem").filter({
      hasText: `Organización ${shortId(organizationA.organizationId)}`,
    });
    await organizationAChoice
      .getByRole("button", {
        name: `Abrir ${sharedOrganizationName}, organización ${shortId(organizationA.organizationId)}`,
      })
      .click();

    const workspaceHeading = page.getByRole("heading", {
      name: `Espacio de ${sharedOrganizationName}`,
    });
    await expect(workspaceHeading).toBeFocused();
    await expect(page).toHaveURL(
      new RegExp(
        `\\?org=${shortId(organizationA.organizationId)}&view=fields$`,
      ),
    );
    await expect(
      page.getByText(`Campo ${shortId(fieldA.fieldId)}`),
    ).toBeVisible();
    await expect(
      page.getByText(`Campo ${shortId(fieldB.fieldId)}`),
    ).toBeHidden();
    expect(
      tenantRequests.some((url) =>
        url.includes(`/organizations/${organizationA.organizationId}/fields`),
      ),
    ).toBe(true);
    expect(
      tenantRequests.some((url) =>
        url.includes(`/organizations/${organizationB.organizationId}/fields`),
      ),
    ).toBe(false);

    const openFieldA = page.getByRole("button", {
      name: `Abrir ficha de ${fieldA.displayName}, campo ${shortId(fieldA.fieldId)}`,
    });
    await openFieldA.click();
    await expect(page).toHaveURL(
      new RegExp(
        `\\?org=${shortId(organizationA.organizationId)}&view=fields&field=${shortId(fieldA.fieldId)}$`,
      ),
    );
    const fieldADetail = page.locator(".field-detail").filter({
      has: page.getByRole("heading", { name: fieldA.displayName, level: 4 }),
    });
    await expect(fieldADetail).toBeFocused();
    await expectNoFullUuidInDom(page);

    await page.reload();
    await expect(
      page.getByRole("heading", { name: fieldA.displayName, level: 4 }),
    ).toBeVisible();
    await expect(fieldADetail).toBeFocused();
    await fieldADetail.press("Escape");
    await expect(page).toHaveURL(
      new RegExp(
        `\\?org=${shortId(organizationA.organizationId)}&view=fields$`,
      ),
    );
    await expect(openFieldA).toBeFocused();

    await page.goBack();
    await expect(
      page.getByRole("heading", { name: fieldA.displayName, level: 4 }),
    ).toBeVisible();
    await page.goForward();
    await expect(openFieldA).toBeFocused();

    const foreignDetailPath = `/organizations/${organizationA.organizationId}/fields/${fieldB.fieldId}`;
    await page.goto(
      `/?org=${shortId(organizationA.organizationId)}&view=fields&field=${shortId(fieldB.fieldId)}`,
    );
    await expect(page).toHaveURL(
      new RegExp(
        `\\?org=${shortId(organizationA.organizationId)}&view=fields$`,
      ),
    );
    await expect(
      page
        .locator(".inline-notice")
        .getByText(/no est. disponible en esta organizaci.n/i),
    ).toBeVisible();
    expect(tenantRequests.some((url) => url.includes(foreignDetailPath))).toBe(
      false,
    );

    await page.goto(
      `/?org=${shortId(organizationA.organizationId)}&view=fields&field=${fieldA.fieldId}`,
    );
    await expect(page).toHaveURL(
      new RegExp(
        `\\?org=${shortId(organizationA.organizationId)}&view=fields$`,
      ),
    );
    await expect(
      page
        .locator(".inline-notice")
        .getByText(/c.digo corto del campo no es v.lido/i),
    ).toBeVisible();
    await expectNoFullUuidInDom(page);

    await page.getByRole("button", { name: "Cambiar organización" }).click();
    const organizationBChoice = page.getByRole("listitem").filter({
      hasText: `Organización ${shortId(organizationB.organizationId)}`,
    });
    await organizationBChoice
      .getByRole("button", {
        name: `Abrir ${sharedOrganizationName}, organización ${shortId(organizationB.organizationId)}`,
      })
      .click();
    await expect(workspaceHeading).toBeFocused();
    await expect(
      page.getByText(`Campo ${shortId(fieldB.fieldId)}`),
    ).toBeVisible();
    await expect(
      page.getByText(`Campo ${shortId(fieldA.fieldId)}`),
    ).toBeHidden();
    await expect(page.locator("body")).not.toContainText(
      organizationA.organizationId,
    );
    await expect(page.locator("body")).not.toContainText(
      organizationB.organizationId,
    );
    await expectNoFullUuidInDom(page);

    await page.getByRole("button", { name: "Equipo" }).click();
    await expect(page.getByRole("button", { name: "Equipo" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    await expect(page).toHaveURL(
      new RegExp(`\\?org=${shortId(organizationB.organizationId)}&view=team$`),
    );
    expect(
      tenantRequests.some((url) =>
        url.includes(
          `/identity/organizations/${organizationB.organizationId}/owner-memberships`,
        ),
      ),
    ).toBe(true);

    await page.goBack();
    await expect(page.getByRole("button", { name: "Campos" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    await page.goForward();
    await expect(page.getByRole("button", { name: "Equipo" })).toHaveAttribute(
      "aria-current",
      "page",
    );

    const hasHorizontalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > window.innerWidth,
    );
    expect(hasHorizontalOverflow).toBe(false);
    await expectNoSeriousAccessibilityViolations(page);
  });

  test("fails closed for a colliding field prefix without requesting detail", async ({
    page,
  }, testInfo) => {
    testInfo.setTimeout(90_000);
    if (testInfo.project.name === "mobile") {
      await page.setViewportSize({ width: 390, height: 844 });
    }

    await page.goto("/");
    await signIn(page);
    const organization = await createOrganization(
      page,
      `Establecimiento colision ${Date.now()}`,
    );
    const collidingFields = [
      {
        fieldId: "abcdef11-1111-4111-8111-111111111111",
        organizationId: organization.organizationId,
        displayName: "Campo Alfa",
        type: "field",
        status: "draft",
        spatialStatus: "not_configured",
        createdAtUtc: "2026-08-18T18:00:00Z",
        version: "11111111-2222-4333-8444-555555555555",
      },
      {
        fieldId: "abcdef22-2222-4222-8222-222222222222",
        organizationId: organization.organizationId,
        displayName: "Campo Beta",
        type: "field",
        status: "draft",
        spatialStatus: "not_configured",
        createdAtUtc: "2026-08-18T18:01:00Z",
        version: "66666666-7777-4888-8999-aaaaaaaaaaaa",
      },
    ] as const;
    const detailRequests: string[] = [];
    page.on("request", (request) => {
      if (/\/fields\/[0-9a-f-]{36}$/i.test(request.url())) {
        detailRequests.push(request.url());
      }
    });
    await page.route(
      `**/api/organizations/${organization.organizationId}/fields`,
      async (route) => {
        if (route.request().method() !== "GET") {
          await route.continue();
          return;
        }
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify(collidingFields),
        });
      },
    );

    await page.goto(
      `/?org=${shortId(organization.organizationId)}&view=fields&field=abcdef`,
    );

    await expect(page).toHaveURL(
      new RegExp(`\\?org=${shortId(organization.organizationId)}&view=fields$`),
    );
    await expect(
      page.locator(".inline-notice").getByText(/coincide con m.s de un campo/i),
    ).toBeVisible();
    expect(detailRequests).toEqual([]);
    await expect(page.locator("body")).not.toContainText(
      collidingFields[0].fieldId,
    );
    await expect(page.locator("body")).not.toContainText(
      collidingFields[1].fieldId,
    );
    await expectNoFullUuidInDom(page);
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth > window.innerWidth,
      ),
    ).toBe(false);
    await expectNoSeriousAccessibilityViolations(page);
  });
});
