import { describe, expect, it } from "vitest";

import { formatShortId } from "../../lib/format-id";

describe("formatShortId", () => {
  it("shows only the first six uppercase UUID characters without dashes", () => {
    expect(formatShortId("a1b2c3d4-e5f6-47a8-b9c0-112233445566")).toBe(
      "A1B2C3",
    );
  });
});
