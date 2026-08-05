import { describe, expect, it } from "vitest";

import { parseTerritoryFixture, territoryFixture } from "./territory";

describe("parseTerritoryFixture", () => {
  it("loads one unique, bounded point for every jurisdiction", () => {
    expect(territoryFixture.points).toHaveLength(24);
    expect(
      new Set(territoryFixture.points.map((point) => point.id)),
    ).toHaveLength(24);
    expect(
      territoryFixture.points.every((point) => /^\d{2}$/.test(point.id)),
    ).toBe(true);
    expect(
      territoryFixture.points.every((point) => Math.abs(point.latitude) <= 90),
    ).toBe(true);
    expect(
      territoryFixture.points.every(
        (point) => Math.abs(point.longitude) <= 180,
      ),
    ).toBe(true);
  });

  it("preserves UTF-8 jurisdiction names from the shared fixture", () => {
    expect(
      territoryFixture.points.find((point) => point.id === "14")?.name,
    ).toBe("Córdoba");
    expect(
      territoryFixture.points.find((point) => point.id === "90")?.name,
    ).toBe("Tucumán");
  });

  it("does not invent weather nature or freshness for territorial centroids", () => {
    expect(
      territoryFixture.points.every(
        (point) => point.evidence === "observed" && point.freshness === "fresh",
      ),
    ).toBe(true);
  });

  it("rejects an incomplete national fixture", () => {
    expect(() =>
      parseTerritoryFixture({
        fixtureVersion: "test",
        source: "fixture",
        purpose: "test",
        points: [],
      }),
    ).toThrow("Se esperaban 24 jurisdicciones");
  });

  it("rejects unbounded coordinates before they reach the map", () => {
    const points = Array.from({ length: 24 }, (_, index) => ({
      id: String(index).padStart(2, "0"),
      name: `Punto ${index}`,
      latitude: index === 3 ? 91 : -34,
      longitude: -64,
    }));

    expect(() =>
      parseTerritoryFixture({
        fixtureVersion: "test",
        source: "fixture",
        purpose: "test",
        points,
      }),
    ).toThrow("no cumple el contrato");
  });
});
