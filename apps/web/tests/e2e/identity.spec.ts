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

async function loadOrganizationIds(page: Page): Promise<readonly string[]> {
  return page.evaluate(async () => {
    const response = await fetch("/api/identity/session", {
      cache: "no-store",
      credentials: "include",
    });
    const payload: unknown = await response.json();
    if (
      typeof payload !== "object" ||
      payload === null ||
      !("memberships" in payload) ||
      !Array.isArray(payload.memberships)
    ) {
      throw new Error("Session did not expose memberships.");
    }
    return payload.memberships.map((membership: unknown) => {
      if (
        typeof membership !== "object" ||
        membership === null ||
        !("organizationId" in membership) ||
        typeof membership.organizationId !== "string"
      ) {
        throw new Error("Session exposed an invalid membership.");
      }
      return membership.organizationId;
    });
  });
}

async function loadNewOrganizationId(
  page: Page,
  previousIds: readonly string[],
): Promise<string> {
  const currentIds = await loadOrganizationIds(page);
  const organizationId = currentIds.find(
    (candidate) => !previousIds.includes(candidate),
  );
  if (organizationId === undefined) {
    throw new Error("Session did not expose the newly created organization.");
  }
  return organizationId;
}

function shortId(value: string): string {
  return value.replaceAll("-", "").slice(0, 6).toUpperCase();
}

function identityProfiles(projectName: string): Readonly<{
  ownerFixture: string;
  inviteeFixture: string;
  inviteeDisplayName: string;
  attackerFixture: string;
  sessionFixture: string;
}> {
  return projectName === "mobile"
    ? {
        ownerFixture: "email-owner-4",
        inviteeFixture: "email-owner-1",
        inviteeDisplayName: "Productor demo 1",
        attackerFixture: "email-owner-2",
        sessionFixture: "email-owner-3",
      }
    : {
        ownerFixture: "email-owner-1",
        inviteeFixture: "email-owner-2",
        inviteeDisplayName: "Productor demo 2",
        attackerFixture: "email-owner-3",
        sessionFixture: "email-owner-4",
      };
}

async function loadCurrentSessionShortId(page: Page): Promise<string> {
  return page.evaluate(async () => {
    const response = await fetch("/api/identity/sessions?offset=0&limit=20", {
      cache: "no-store",
      credentials: "include",
    });
    const payload: unknown = await response.json();
    if (
      typeof payload !== "object" ||
      payload === null ||
      !("items" in payload) ||
      !Array.isArray(payload.items)
    ) {
      throw new Error("Own-session inventory returned an invalid page.");
    }
    const current = payload.items.find(
      (item: unknown) =>
        typeof item === "object" &&
        item !== null &&
        "isCurrent" in item &&
        item.isCurrent === true,
    );
    if (
      typeof current !== "object" ||
      current === null ||
      !("sessionId" in current) ||
      typeof current.sessionId !== "string"
    ) {
      throw new Error(
        "Own-session inventory did not mark the current session.",
      );
    }
    return current.sessionId.replaceAll("-", "").slice(0, 6).toUpperCase();
  });
}

async function waitForWorkspaceState(page: Page): Promise<void> {
  await expect(
    page
      .getByRole("button", { name: "Crear mi organización" })
      .or(page.getByRole("heading", { name: "Elegir organización" }))
      .or(page.getByRole("heading", { name: /^Espacio de / })),
  ).toBeVisible();
}

async function chooseFirstOrganizationWhenRequired(page: Page): Promise<void> {
  await waitForWorkspaceState(page);
  const selector = page.getByRole("heading", { name: "Elegir organización" });
  if (await selector.isVisible()) {
    await page
      .getByRole("listitem")
      .filter({ hasText: "Organización " })
      .first()
      .getByRole("button")
      .click();
  }
}

async function openOrganizationCreator(page: Page): Promise<void> {
  await waitForWorkspaceState(page);
  const firstOrganization = page.getByRole("button", {
    name: "Crear mi organización",
  });
  if (await firstOrganization.isVisible()) {
    await firstOrganization.click();
    return;
  }

  await chooseFirstOrganizationWhenRequired(page);
  await page.getByRole("button", { name: "Cuenta" }).click();
  await page.getByRole("button", { name: "Crear otra" }).click();
}

async function selectOrganization(
  page: Page,
  organizationId: string,
): Promise<void> {
  const prefix = shortId(organizationId);
  if (new URL(page.url()).searchParams.get("org") === prefix) return;

  const changeOrganization = page.getByRole("button", {
    name: "Cambiar organización",
  });
  if (await changeOrganization.isVisible()) {
    await changeOrganization.click();
  }
  await expect(
    page.getByRole("heading", { name: "Elegir organización" }),
  ).toBeVisible();
  await page
    .getByRole("listitem")
    .filter({ hasText: `Organización ${prefix}` })
    .getByRole("button")
    .click();
  await expect(page).toHaveURL(new RegExp(`\\?org=${prefix}&view=fields$`));
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
    testInfo.setTimeout(90_000);
    const profiles = identityProfiles(testInfo.project.name);
    await page.goto("/");
    await expectNoAccessibilityViolations(page);
    await signInWithExplicitFixture(page, profiles.ownerFixture);
    await page.reload();

    await expect(
      page.getByRole("heading", { name: "Tu acceso", exact: true }),
    ).toBeVisible();
    const previousOrganizationIds = await loadOrganizationIds(page);
    await openOrganizationCreator(page);
    const ownerOrganizationName = `Establecimiento La Esperanza ${testInfo.project.name}`;
    const organizationName = page.getByRole("textbox", {
      name: "Nombre de la organización",
    });
    await organizationName.fill(ownerOrganizationName);
    await page.getByRole("button", { name: "Crear organización" }).click();
    await expect(
      page.getByText(`${ownerOrganizationName} ya está lista y sos owner.`),
    ).toBeVisible();
    const ownerOrganizationId = await loadNewOrganizationId(
      page,
      previousOrganizationIds,
    );
    await selectOrganization(page, ownerOrganizationId);
    await page.getByRole("button", { name: "Cuenta" }).click();
    await expect(
      page.getByRole("heading", { name: "Tus organizaciones" }),
    ).toBeVisible();
    await expect(
      page.getByText("owner", { exact: true }).first(),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Crear otra" }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Campos" }).click();

    await page.getByRole("button", { name: "Crear campo" }).click();
    const fieldName = page.getByRole("textbox", {
      name: "Nombre del campo",
    });
    await fieldName.fill("Campo Norte");
    await fieldName.press("Enter");
    await expect(
      page.getByText(/Campo Norte quedó como borrador sin geometría/i),
    ).toBeVisible();
    await expectNoAccessibilityViolations(page);

    const ownerField = await page.evaluate(async (selectedOrganizationName) => {
      const sessionResponse = await fetch("/api/identity/session", {
        cache: "no-store",
        credentials: "include",
      });
      const sessionPayload: unknown = await sessionResponse.json();
      if (
        typeof sessionPayload !== "object" ||
        sessionPayload === null ||
        !("memberships" in sessionPayload) ||
        !Array.isArray(sessionPayload.memberships)
      ) {
        throw new Error("Owner session did not expose memberships.");
      }
      const membership = sessionPayload.memberships.find(
        (item: unknown) =>
          typeof item === "object" &&
          item !== null &&
          "organizationName" in item &&
          item.organizationName === selectedOrganizationName,
      );
      if (
        typeof membership !== "object" ||
        membership === null ||
        !("organizationId" in membership) ||
        typeof membership.organizationId !== "string"
      ) {
        throw new Error("Owner session did not expose the created org.");
      }
      const fieldsResponse = await fetch(
        `/api/organizations/${encodeURIComponent(membership.organizationId)}/fields`,
        { cache: "no-store", credentials: "include" },
      );
      const fieldsPayload: unknown = await fieldsResponse.json();
      if (
        !Array.isArray(fieldsPayload) ||
        fieldsPayload.length !== 1 ||
        typeof fieldsPayload[0] !== "object" ||
        fieldsPayload[0] === null ||
        !("fieldId" in fieldsPayload[0]) ||
        typeof fieldsPayload[0].fieldId !== "string"
      ) {
        throw new Error("Owner field list did not expose exactly one field.");
      }
      return {
        organizationId: membership.organizationId,
        fieldId: fieldsPayload[0].fieldId,
      };
    }, ownerOrganizationName);

    await page.reload();
    await expect(
      page.getByRole("heading", { name: "Tus campos" }),
    ).toBeVisible();
    const fieldDetailButton = page.getByRole("button", {
      name: /Abrir ficha de Campo Norte/i,
    });
    await expect(fieldDetailButton).toBeVisible();
    await fieldDetailButton.focus();
    await expect(fieldDetailButton).toBeFocused();
    await page.keyboard.press("Enter");
    await expect(
      page.getByRole("heading", { name: "Campo Norte", level: 4 }),
    ).toBeVisible();
    await expect(
      page.getByText("Sin geometría", { exact: true }).last(),
    ).toBeVisible();

    const editFieldName = page.getByRole("button", { name: "Editar nombre" });
    await editFieldName.focus();
    await expect(editFieldName).toBeFocused();
    await page.keyboard.press("Enter");
    const renamedFieldName = page.getByRole("textbox", {
      name: "Nuevo nombre del campo",
    });
    await expect(renamedFieldName).toBeFocused();
    await expect(renamedFieldName).toHaveValue("Campo Norte");
    await renamedFieldName.fill("Campo Sur");
    await page.keyboard.press("Escape");
    await expect(editFieldName).toBeFocused();
    await page.keyboard.press("Enter");
    await renamedFieldName.fill("Campo Sur");
    await page.getByRole("button", { name: "Guardar nombre" }).click();
    await expect(
      page.getByRole("heading", { name: "Campo Sur", level: 4 }),
    ).toBeVisible();
    await expect(page.getByText(/ahora se llama Campo Sur/i)).toBeVisible();
    await expectNoAccessibilityViolations(page);

    await page.reload();
    const renamedFieldDetailButton = page.getByRole("button", {
      name: /Abrir ficha de Campo Sur/i,
    });
    await expect(renamedFieldDetailButton).toBeVisible();
    await renamedFieldDetailButton.click();
    await expect(
      page.getByRole("heading", { name: "Campo Sur", level: 4 }),
    ).toBeVisible();

    await page.getByRole("button", { name: "Cuenta" }).click();

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

    await page.getByRole("button", { name: "Equipo" }).click();

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

    const inviteeContext = await browser.newContext();
    const inviteePage = await inviteeContext.newPage();
    trackBrowserErrorsWithAllowList(inviteePage, [
      expectedSignedOutConsoleError,
      "Failed to load resource: the server responded with a status of 404 (Not Found)",
    ]);
    await inviteePage.goto(invitationLink);
    await expect(
      inviteePage.getByRole("heading", { name: "Protegemos este acceso" }),
    ).toBeVisible();
    await signInWithExplicitFixture(inviteePage, profiles.inviteeFixture);
    await inviteePage.reload();
    await expect(
      inviteePage.getByText(`Ya sos co-owner de ${ownerOrganizationName}.`, {
        exact: true,
      }),
    ).toBeVisible();
    await expect(inviteePage).not.toHaveURL(/owner-invitation=/);
    const organizationId = await inviteePage.evaluate(
      async (selectedOrganizationName) => {
        const response = await fetch("/api/identity/session", {
          cache: "no-store",
          credentials: "include",
        });
        const payload: unknown = await response.json();
        if (
          typeof payload !== "object" ||
          payload === null ||
          !("memberships" in payload) ||
          !Array.isArray(payload.memberships)
        ) {
          throw new Error("Invitee session did not expose memberships.");
        }
        const membership = payload.memberships.find(
          (item: unknown) =>
            typeof item === "object" &&
            item !== null &&
            "organizationName" in item &&
            item.organizationName === selectedOrganizationName,
        );
        if (
          typeof membership !== "object" ||
          membership === null ||
          !("organizationId" in membership) ||
          typeof membership.organizationId !== "string"
        ) {
          throw new Error("Invitee session did not expose the accepted org.");
        }
        return membership.organizationId;
      },
      ownerOrganizationName,
    );
    await selectOrganization(inviteePage, organizationId);
    await inviteePage.getByRole("button", { name: "Equipo" }).click();
    await expect(
      inviteePage
        .getByRole("region", { name: "Co-owners" })
        .getByText("Owner", { exact: true })
        .first(),
    ).toBeVisible();
    expect(browserErrors.get(inviteePage) ?? []).toEqual([]);

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
    await signInWithExplicitFixture(attackerPage, profiles.attackerFixture);
    await attackerPage.reload();
    await expect(
      attackerPage.getByRole("heading", { name: "Enlace no disponible" }),
    ).toBeVisible();
    const attackerOrganizationIds = await loadOrganizationIds(attackerPage);
    await openOrganizationCreator(attackerPage);
    const attackerOrganizationName = `Organización B ${testInfo.project.name}`;
    await attackerPage
      .getByRole("textbox", { name: "Nombre de la organización" })
      .fill(attackerOrganizationName);
    await attackerPage
      .getByRole("button", { name: "Crear organización" })
      .click();
    await expect(
      attackerPage.getByText(
        `${attackerOrganizationName} ya está lista y sos owner.`,
      ),
    ).toBeVisible();
    const tenantBOrganizationId = await loadNewOrganizationId(
      attackerPage,
      attackerOrganizationIds,
    );
    await selectOrganization(attackerPage, tenantBOrganizationId);
    const tenantBCard = attackerPage.getByRole("article").filter({
      hasText: `Organización ${shortId(tenantBOrganizationId)}`,
    });
    await tenantBCard.getByRole("button", { name: "Crear campo" }).click();
    await tenantBCard
      .getByRole("textbox", { name: "Nombre del campo" })
      .fill("Campo Norte");
    await tenantBCard.getByRole("button", { name: "Crear campo" }).click();
    await expect(
      attackerPage.getByText(/Campo Norte quedó como borrador sin geometría/i),
    ).toBeVisible();

    const tenantBResult = await attackerPage.evaluate(
      async ({ tenantAOrganizationId, ownOrganizationId }) => {
        const crossTenantResponse = await fetch(
          `/api/organizations/${encodeURIComponent(tenantAOrganizationId)}/fields`,
          { cache: "no-store", credentials: "include" },
        );
        const ownFieldsResponse = await fetch(
          `/api/organizations/${encodeURIComponent(ownOrganizationId)}/fields`,
          { cache: "no-store", credentials: "include" },
        );
        const ownFields: unknown = await ownFieldsResponse.json();
        if (
          !Array.isArray(ownFields) ||
          ownFields.length !== 1 ||
          typeof ownFields[0] !== "object" ||
          ownFields[0] === null ||
          !("displayName" in ownFields[0]) ||
          !("fieldId" in ownFields[0]) ||
          typeof ownFields[0].fieldId !== "string"
        ) {
          throw new Error("Tenant B did not expose exactly its own field.");
        }
        return {
          crossTenantStatus: crossTenantResponse.status,
          displayName: ownFields[0].displayName,
          fieldId: ownFields[0].fieldId,
        };
      },
      {
        tenantAOrganizationId: ownerField.organizationId,
        ownOrganizationId: tenantBOrganizationId,
      },
    );
    expect(tenantBResult.crossTenantStatus).toBe(404);
    expect(tenantBResult.displayName).toBe("Campo Norte");
    expect(tenantBResult.fieldId).not.toBe(ownerField.fieldId);
    expect(browserErrors.get(attackerPage) ?? []).toEqual([]);
    await attackerContext.close();

    await page.reload();
    await expect(
      page.getByText("Aceptada", { exact: true }).first(),
    ).toBeVisible();

    const inviteeDisplayName = profiles.inviteeDisplayName;
    if (testInfo.project.name === "mobile") {
      await page.setViewportSize({ width: 390, height: 844 });
    }
    const removeCoOwner = page.getByRole("button", {
      name: `Quitar acceso a ${inviteeDisplayName}`,
    });
    await expect(removeCoOwner).toBeVisible();
    await removeCoOwner.focus();
    await expect(removeCoOwner).toBeFocused();
    await page.keyboard.press("Enter");
    let confirmation = page.getByRole("alertdialog", {
      name: `¿Quitar acceso a ${inviteeDisplayName}?`,
    });
    await expect(confirmation).toBeVisible();
    await expect(
      confirmation.getByRole("button", { name: "Cancelar" }),
    ).toBeFocused();
    await expectNoAccessibilityViolations(page);
    await page.keyboard.press("Escape");
    await expect(confirmation).toBeHidden();
    await expect(removeCoOwner).toBeFocused();

    await page.keyboard.press("Enter");
    confirmation = page.getByRole("alertdialog", {
      name: `¿Quitar acceso a ${inviteeDisplayName}?`,
    });
    await confirmation
      .getByRole("button", { name: `Quitar acceso a ${inviteeDisplayName}` })
      .click();
    await expect(
      page.getByText(
        `${inviteeDisplayName} ya no tiene acceso a la organización.`,
      ),
    ).toBeVisible();
    await expect(removeCoOwner).toBeHidden();
    const hasRemovalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > window.innerWidth,
    );
    expect(hasRemovalOverflow).toBe(false);
    await expectNoAccessibilityViolations(page);

    await inviteePage.reload();
    await waitForWorkspaceState(inviteePage);
    await expect(
      inviteePage.getByText(ownerOrganizationName, { exact: true }),
    ).toBeHidden();
    const removedInviteeDirectoryStatus = await inviteePage.evaluate(
      async (acceptedOrganizationId) => {
        const response = await fetch(
          `/api/identity/organizations/${encodeURIComponent(acceptedOrganizationId)}/owner-memberships`,
          { cache: "no-store", credentials: "include" },
        );
        return response.status;
      },
      organizationId,
    );
    expect(removedInviteeDirectoryStatus).toBe(404);
    const removedInviteeFieldStatuses = await inviteePage.evaluate(
      async ({ acceptedOrganizationId, acceptedFieldId }) => {
        const listResponse = await fetch(
          `/api/organizations/${encodeURIComponent(acceptedOrganizationId)}/fields`,
          { cache: "no-store", credentials: "include" },
        );
        const detailResponse = await fetch(
          `/api/organizations/${encodeURIComponent(acceptedOrganizationId)}/fields/${encodeURIComponent(acceptedFieldId)}`,
          { cache: "no-store", credentials: "include" },
        );
        return [listResponse.status, detailResponse.status];
      },
      {
        acceptedOrganizationId: ownerField.organizationId,
        acceptedFieldId: ownerField.fieldId,
      },
    );
    expect(removedInviteeFieldStatuses).toEqual([404, 404]);
    expect(browserErrors.get(inviteePage) ?? []).toEqual([]);
    await inviteeContext.close();

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
  }, testInfo) => {
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

    const previousOrganizationIds = await loadOrganizationIds(page);
    await openOrganizationCreator(page);
    const createOrganization = page.getByRole("button", {
      name: "Crear organización",
    });
    const mobileOrganizationName = `Campo móvil ${testInfo.project.name}`;
    const organizationName = page.getByRole("textbox", {
      name: "Nombre de la organización",
    });
    await expect(organizationName).toBeFocused();
    await organizationName.fill(mobileOrganizationName);
    await organizationName.press("Enter");
    await expect(
      page.getByText(`${mobileOrganizationName} ya está lista y sos owner.`),
    ).toBeVisible();
    await expect(createOrganization).toBeHidden();
    const mobileOrganizationId = await loadNewOrganizationId(
      page,
      previousOrganizationIds,
    );
    await selectOrganization(page, mobileOrganizationId);
    const mobileOrganizationCard = page.getByRole("article").filter({
      hasText: `Organización ${shortId(mobileOrganizationId)}`,
    });
    const createField = mobileOrganizationCard.getByRole("button", {
      name: "Crear campo",
    });
    await createField.focus();
    await expect(createField).toBeFocused();
    await page.keyboard.press("Enter");
    const mobileFieldName = mobileOrganizationCard.getByRole("textbox", {
      name: "Nombre del campo",
    });
    await mobileFieldName.fill("Lote móvil");
    await mobileFieldName.press("Enter");
    await expect(
      page.getByText(/Lote móvil quedó como borrador sin geometría/i),
    ).toBeVisible();
    const mobileEditFieldName = mobileOrganizationCard.getByRole("button", {
      name: "Editar nombre",
    });
    await mobileEditFieldName.focus();
    await expect(mobileEditFieldName).toBeFocused();
    await page.keyboard.press("Enter");
    const mobileRenameInput = mobileOrganizationCard.getByRole("textbox", {
      name: "Nuevo nombre del campo",
    });
    await expect(mobileRenameInput).toBeFocused();
    await mobileRenameInput.fill("Lote móvil sur");
    await mobileRenameInput.press("Enter");
    await expect(
      mobileOrganizationCard.getByRole("heading", {
        name: "Lote móvil sur",
        level: 4,
      }),
    ).toBeVisible();
    await expectNoAccessibilityViolations(page);

    await page.getByRole("button", { name: "Cuenta" }).click();

    const revoke = page.getByRole("button", {
      name: "Cerrar sesión",
      exact: true,
    });
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

  test("lists and revokes another own session without exposing private metadata", async ({
    browser,
    page,
  }, testInfo) => {
    testInfo.setTimeout(90_000);
    const profiles = identityProfiles(testInfo.project.name);
    const otherContext = await browser.newContext();
    const otherPage = await otherContext.newPage();
    await otherPage.goto("/");
    await signInWithExplicitFixture(otherPage, profiles.sessionFixture);
    await otherPage.reload();
    await expect(
      otherPage.getByRole("heading", { name: "Tu acceso", exact: true }),
    ).toBeVisible();
    const otherSessionShortId = await loadCurrentSessionShortId(otherPage);

    await page.goto("/");
    await signInWithExplicitFixture(page, profiles.sessionFixture);
    await page.reload();
    await waitForWorkspaceState(page);
    const createFirstOrganization = page.getByRole("button", {
      name: "Crear mi organización",
    });
    if (await createFirstOrganization.isVisible()) {
      await createFirstOrganization.click();
      await page
        .getByRole("textbox", { name: "Nombre de la organización" })
        .fill(`Sesiones QA ${testInfo.project.name}`);
      await page.getByRole("button", { name: "Crear organización" }).click();
      await expect(page.getByText(/ya está lista y sos owner/i)).toBeVisible();
    } else {
      await chooseFirstOrganizationWhenRequired(page);
    }
    await page.getByRole("button", { name: "Cuenta" }).click();

    const sessions = page.getByRole("heading", { name: "Sesiones abiertas" });
    await expect(sessions).toBeVisible();
    await expect(page.getByText("Actual", { exact: true })).toBeVisible();
    const otherSessionButton = page.getByRole("button", {
      name: `Cerrar sesión ${otherSessionShortId}`,
    });
    await expect(otherSessionButton).toBeVisible();

    const accountText = await page.locator("main").innerText();
    expect(accountText).not.toMatch(
      /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i,
    );
    expect(accountText).not.toMatch(/user[- ]?agent|ip address|token hash/i);

    await otherSessionButton.focus();
    await page.keyboard.press("Enter");
    const confirmation = page.getByRole("alertdialog", {
      name: `¿Cerrar la sesión ${otherSessionShortId}?`,
    });
    await expect(confirmation).toBeVisible();
    await expect(
      confirmation.getByRole("button", { name: "Cancelar" }),
    ).toBeFocused();
    await page.keyboard.press("Escape");
    await expect(otherSessionButton).toBeFocused();

    await page.keyboard.press("Enter");
    await confirmation
      .getByRole("button", {
        name: `Confirmar cierre de sesión ${otherSessionShortId}`,
      })
      .click();
    await expect(page.getByText("La otra sesión fue cerrada.")).toBeVisible();

    const revokedStatus = await otherPage.evaluate(async () => {
      const response = await fetch("/api/identity/session", {
        cache: "no-store",
        credentials: "include",
      });
      return response.status;
    });
    expect(revokedStatus).toBe(401);
    const currentStatus = await page.evaluate(async () => {
      const response = await fetch("/api/identity/session", {
        cache: "no-store",
        credentials: "include",
      });
      return response.status;
    });
    expect(currentStatus).toBe(200);

    await page.setViewportSize({ width: 390, height: 844 });
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth > window.innerWidth,
      ),
    ).toBe(false);
    await expectNoAccessibilityViolations(page);
    await otherContext.close();
  });
});
