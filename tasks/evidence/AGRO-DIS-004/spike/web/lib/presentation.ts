import type { EvidenceNature, Freshness } from "./territory";

export type SimulationState =
  "ready" | "stale" | "unavailable" | "loading" | "empty" | "error";

export const SIMULATION_OPTIONS: readonly Readonly<{
  value: SimulationState;
  label: string;
  description: string;
}>[] = [
  {
    value: "ready",
    label: "Referencia",
    description: "Muestra el set territorial observado, sin clima inventado.",
  },
  {
    value: "stale",
    label: "Desactualizado",
    description: "Conserva datos con antigüedad visible.",
  },
  {
    value: "unavailable",
    label: "Proveedor caído",
    description: "El mapa falla; la tabla sigue disponible.",
  },
  {
    value: "loading",
    label: "Cargando",
    description: "Representa una espera con feedback explícito.",
  },
  {
    value: "empty",
    label: "Sin datos",
    description: "Representa una respuesta válida sin cobertura.",
  },
  {
    value: "error",
    label: "Error",
    description: "Representa un fallo recuperable.",
  },
];

export const EVIDENCE_LABELS: Readonly<Record<EvidenceNature, string>> = {
  observed: "Observado",
  estimated: "Estimado",
  forecast: "Pronosticado",
};

export const FRESHNESS_LABELS: Readonly<Record<Freshness, string>> = {
  fresh: "Vigente",
  stale: "Desactualizado",
  unavailable: "No disponible",
};

export function effectiveFreshness(
  freshness: Freshness,
  simulation: SimulationState,
): Freshness {
  if (simulation === "stale") return "stale";
  if (simulation === "unavailable") return "unavailable";
  return freshness;
}

export function formatCoordinate(value: number): string {
  return new Intl.NumberFormat("es-AR", {
    minimumFractionDigits: 3,
    maximumFractionDigits: 3,
  }).format(value);
}
