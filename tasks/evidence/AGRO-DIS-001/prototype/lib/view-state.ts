export type PrototypeScenario = "READY" | "LOADING" | "ERROR" | "STALE";
export type SourceFreshness = "FRESH" | "STALE";

export type CatalogViewState =
  | Readonly<{ kind: "loading" }>
  | Readonly<{ kind: "error"; message: string }>
  | Readonly<{ kind: "empty"; stale: false }>
  | Readonly<{ kind: "empty"; stale: true }>
  | Readonly<{ kind: "ready"; stale: false }>
  | Readonly<{ kind: "ready"; stale: true }>;

export function resolveCatalogViewState({
  scenario,
  resultCount,
}: Readonly<{
  scenario: PrototypeScenario;
  resultCount: number;
}>): CatalogViewState {
  switch (scenario) {
    case "LOADING":
      return { kind: "loading" };
    case "ERROR":
      return {
        kind: "error",
        message: "No pudimos leer la fuente candidata. Conservá el contexto y reintentá.",
      };
    case "READY":
      return resultCount === 0
        ? { kind: "empty", stale: false }
        : { kind: "ready", stale: false };
    case "STALE":
      return resultCount === 0
        ? { kind: "empty", stale: true }
        : { kind: "ready", stale: true };
    default: {
      const exhaustive: never = scenario;
      return exhaustive;
    }
  }
}

export function classifySourceFreshness({
  capturedAt,
  evaluatedAt,
  staleAfterDays,
}: Readonly<{
  capturedAt: string;
  evaluatedAt: string;
  staleAfterDays: number;
}>): SourceFreshness {
  const capturedTime = Date.parse(capturedAt);
  const evaluatedTime = Date.parse(evaluatedAt);
  const millisecondsPerDay = 86_400_000;

  if (
    !Number.isFinite(capturedTime) ||
    !Number.isFinite(evaluatedTime) ||
    !Number.isFinite(staleAfterDays) ||
    staleAfterDays < 0 ||
    evaluatedTime < capturedTime
  ) {
    return "STALE";
  }

  return evaluatedTime - capturedTime <= staleAfterDays * millisecondsPerDay
    ? "FRESH"
    : "STALE";
}

export function canPublishFromSource(freshness: SourceFreshness): boolean {
  return freshness === "FRESH";
}
