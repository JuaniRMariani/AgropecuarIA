import { describe, expect, it } from "vitest";

import {
  LAB_STATES,
  NETWORK_PROFILES,
  SCENARIOS,
  projectCapacity,
  type LabStateId,
  type NetworkProfileId,
  type ScenarioId,
} from "../../lib/capacity-model";

describe("capacity model", () => {
  it("loads every typed scenario and network profile from the shared fixture", () => {
    const scenarioIds: readonly ScenarioId[] = [
      "pilot",
      "growth-10x",
      "burst-2x",
    ];
    const networkIds: readonly NetworkProfileId[] = [
      "target",
      "constrained",
      "critical",
      "offline",
    ];
    const stateIds: readonly LabStateId[] = [
      "healthy",
      "loading",
      "empty",
      "error",
      "stale",
      "degraded",
      "conflict",
      "provider-down",
    ];

    expect(Object.keys(SCENARIOS)).toEqual(scenarioIds);
    expect(Object.keys(NETWORK_PROFILES)).toEqual(networkIds);
    expect(Object.keys(LAB_STATES)).toEqual(stateIds);
    expect(SCENARIOS.pilot.volumes.documentsPerMonth).toBe(10_000);
    expect(SCENARIOS["growth-10x"].demand.dailyReadRequests).toBe(1_000_000);
    expect(SCENARIOS["burst-2x"].demand.peakFactor).toBe(10);
    expect(NETWORK_PROFILES.critical).toMatchObject({
      latencyMs: 600,
      downloadKbps: 400,
      uploadKbps: 100,
      packetLossPercent: 2,
    });
  });

  it("projects the pilot scenario with transparent deterministic formulas", () => {
    const projection = projectCapacity(SCENARIOS.pilot);

    expect(projection.peakReadRequestsPerSecond).toBeCloseTo(9.259, 3);
    expect(projection.peakWriteRequestsPerSecond).toBeCloseTo(1.852, 3);
    expect(projection.retainedObjectStorageGiB).toBeCloseTo(366.211, 3);
    expect(projection.dailyImportDrainSeconds).toBe(40);
  });

  it("keeps growth and burst projections monotonic", () => {
    const pilot = projectCapacity(SCENARIOS.pilot);
    const growth = projectCapacity(SCENARIOS["growth-10x"]);
    const burst = projectCapacity(SCENARIOS["burst-2x"]);

    expect(growth.peakReadRequestsPerSecond).toBeGreaterThan(
      pilot.peakReadRequestsPerSecond,
    );
    expect(burst.peakReadRequestsPerSecond).toBeGreaterThan(
      growth.peakReadRequestsPerSecond,
    );
    expect(growth.retainedObjectStorageGiB).toBeGreaterThan(
      pilot.retainedObjectStorageGiB,
    );
    expect(burst.retainedObjectStorageGiB).toBeGreaterThan(
      growth.retainedObjectStorageGiB,
    );
  });
});
