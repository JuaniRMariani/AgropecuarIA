export type SubmissionState =
  | Readonly<{ kind: "idle"; key: string; attempts: 0 }>
  | Readonly<{ kind: "failed"; key: string; attempts: number }>
  | Readonly<{
      kind: "accepted";
      key: string;
      attempts: number;
      acceptedCount: 1;
    }>
  | Readonly<{
      kind: "duplicate";
      key: string;
      attempts: number;
      acceptedCount: 1;
    }>;

export function createSubmission(key: string): SubmissionState {
  return { kind: "idle", key, attempts: 0 };
}

export function failSubmission(state: SubmissionState): SubmissionState {
  return { kind: "failed", key: state.key, attempts: state.attempts + 1 };
}

export function acceptSubmission(state: SubmissionState): SubmissionState {
  if (state.kind === "accepted" || state.kind === "duplicate") {
    return {
      kind: "duplicate",
      key: state.key,
      attempts: state.attempts + 1,
      acceptedCount: 1,
    };
  }

  return {
    kind: "accepted",
    key: state.key,
    attempts: state.attempts + 1,
    acceptedCount: 1,
  };
}

export function submissionMessage(state: SubmissionState): string {
  switch (state.kind) {
    case "idle":
      return "La primera confirmación simulará un fallo transitorio para comprobar el reintento.";
    case "failed":
      return "Fallo transitorio simulado. Reintentá: se conservará exactamente la misma clave.";
    case "accepted":
      return "Operación aceptada una vez. Podés simular un reenvío para verificar la deduplicación.";
    case "duplicate":
      return "Reenvío duplicado detectado: la operación aceptada sigue siendo una sola.";
    default: {
      const exhaustive: never = state;
      return exhaustive;
    }
  }
}
