import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

import {
  canPublishFromSource,
  classifySourceFreshness,
  resolveCatalogViewState,
} from "../lib/view-state";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

test("modela loading y error como estados excluyentes", () => {
  assert.deepEqual(resolveCatalogViewState({ scenario: "LOADING", resultCount: 8 }), {
    kind: "loading",
  });
  assert.equal(resolveCatalogViewState({ scenario: "ERROR", resultCount: 8 }).kind, "error");
});

test("modela vacío sólo cuando la fuente está disponible", () => {
  assert.deepEqual(resolveCatalogViewState({ scenario: "READY", resultCount: 0 }), {
    kind: "empty",
    stale: false,
  });
  assert.deepEqual(resolveCatalogViewState({ scenario: "STALE", resultCount: 0 }), {
    kind: "empty",
    stale: true,
  });
});

test("conserva stale sin resultados para impedir una lectura falsa de frescura", () => {
  const state = resolveCatalogViewState({ scenario: "STALE", resultCount: 0 });

  assert.deepEqual(state, { kind: "empty", stale: true });
  if (state.kind !== "empty") {
    assert.fail("El escenario sin resultados debe conservar el estado empty.");
  }
  assert.equal(state.stale, true);
  assert.equal(canPublishFromSource("STALE"), false);
});

test("conserva la señal stale cuando existen resultados", () => {
  assert.deepEqual(resolveCatalogViewState({ scenario: "STALE", resultCount: 2 }), {
    kind: "ready",
    stale: true,
  });
  assert.deepEqual(resolveCatalogViewState({ scenario: "READY", resultCount: 2 }), {
    kind: "ready",
    stale: false,
  });
});

test("clasifica el fixture stale y bloquea publicación en modo fail-closed", async () => {
  const fixtureUrl = new URL("./fixtures/source-stale.json", import.meta.url);
  const parsed: unknown = JSON.parse(await readFile(fixtureUrl, "utf8"));
  if (!isRecord(parsed)) {
    assert.fail("El fixture stale debe ser un objeto.");
  }

  assert.equal(typeof parsed.capturedAt, "string");
  assert.equal(typeof parsed.evaluatedAt, "string");
  assert.equal(typeof parsed.staleAfterDays, "number");
  assert.equal(parsed.expectedFreshness, "STALE");
  assert.equal(parsed.canPublish, false);

  if (
    typeof parsed.capturedAt !== "string" ||
    typeof parsed.evaluatedAt !== "string" ||
    typeof parsed.staleAfterDays !== "number"
  ) {
    assert.fail("El fixture stale contiene tipos inválidos.");
  }

  const freshness = classifySourceFreshness({
    capturedAt: parsed.capturedAt,
    evaluatedAt: parsed.evaluatedAt,
    staleAfterDays: parsed.staleAfterDays,
  });

  assert.equal(freshness, "STALE");
  assert.equal(canPublishFromSource(freshness), false);
});
