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
});
