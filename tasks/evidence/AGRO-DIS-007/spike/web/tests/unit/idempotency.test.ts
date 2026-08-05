import { describe, expect, it } from "vitest";

import {
  acceptSubmission,
  createSubmission,
  failSubmission,
  submissionMessage,
} from "../../lib/idempotency";

describe("idempotent retry model", () => {
  it("keeps the same key through failure and retry", () => {
    const initial = createSubmission("capacity-session-key");
    const failed = failSubmission(initial);
    const accepted = acceptSubmission(failed);

    expect(failed.key).toBe(initial.key);
    expect(accepted.key).toBe(initial.key);
    expect(accepted).toMatchObject({
      kind: "accepted",
      attempts: 2,
      acceptedCount: 1,
    });
  });

  it("deduplicates a repeated accepted operation", () => {
    const accepted = acceptSubmission(createSubmission("same-key"));
    const duplicate = acceptSubmission(accepted);

    expect(duplicate).toMatchObject({
      kind: "duplicate",
      acceptedCount: 1,
      key: "same-key",
    });
    expect(submissionMessage(duplicate)).toContain("sigue siendo una sola");
  });
});
