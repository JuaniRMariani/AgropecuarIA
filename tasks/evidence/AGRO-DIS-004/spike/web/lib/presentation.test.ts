import { describe, expect, it } from "vitest";

import { effectiveFreshness, formatCoordinate } from "./presentation";

describe("presentation rules", () => {
  it("does not hide stale or unavailable simulations behind fixture state", () => {
    expect(effectiveFreshness("fresh", "stale")).toBe("stale");
    expect(effectiveFreshness("fresh", "unavailable")).toBe("unavailable");
    expect(effectiveFreshness("unavailable", "ready")).toBe("unavailable");
  });

  it("formats coordinates with stable Argentine decimal notation", () => {
    expect(formatCoordinate(-34.614442)).toBe("-34,614");
  });
});
