import capacityFixture from "../../../fixtures/capacity-scenarios.json";

export type ScenarioId = "pilot" | "growth-10x" | "burst-2x";

export type NetworkProfileId =
  "target" | "constrained" | "critical" | "offline";

export type LabStateId =
  | "healthy"
  | "loading"
  | "empty"
  | "error"
  | "stale"
  | "degraded"
  | "conflict"
  | "provider-down";

export type Scenario = Readonly<{
  id: ScenarioId;
  label: string;
  confidence: "low";
  volumes: Readonly<{
    tenants: number;
    registeredUsers: number;
    concurrentUsers: number;
    farms: number;
    fields: number;
    documentsPerMonth: number;
    averageDocumentMiB: number;
    importRowsPerDay: number;
    jobsPerHour: number;
  }>;
  demand: Readonly<{
    dailyReadRequests: number;
    dailyWriteRequests: number;
    peakFactor: number;
    retentionMonths: number;
    objectVersionFactor: number;
    workerRowsPerSecond: number;
  }>;
}>;

export type CapacityProjection = Readonly<{
  peakReadRequestsPerSecond: number;
  peakWriteRequestsPerSecond: number;
  retainedObjectStorageGiB: number;
  dailyImportDrainSeconds: number;
}>;

export type NetworkProfile = Readonly<{
  id: NetworkProfileId;
  label: string;
  latencyMs: number;
  downloadKbps: number;
  uploadKbps: number;
  packetLossPercent: number;
  online: boolean;
}>;

export type LabState = Readonly<{
  id: LabStateId;
  label: string;
  title: string;
  detail: string;
  tone: "healthy" | "neutral" | "warning" | "danger";
}>;

export const VALID_UNTIL = "30/09/2026";

function parseScenarioId(value: string): ScenarioId {
  switch (value) {
    case "pilot":
    case "growth-10x":
    case "burst-2x":
      return value;
    default:
      throw new Error(`Escenario sintético desconocido: ${value}`);
  }
}

function parseNetworkProfileId(value: string): NetworkProfileId {
  switch (value) {
    case "target":
    case "constrained":
    case "critical":
    case "offline":
      return value;
    default:
      throw new Error(`Perfil de red sintético desconocido: ${value}`);
  }
}

function requireFiniteNonNegative(values: readonly number[], label: string) {
  if (values.some((value) => !Number.isFinite(value) || value < 0)) {
    throw new Error(`${label} contiene una cantidad inválida`);
  }
}

function parseScenario(
  input: (typeof capacityFixture.scenarios)[number],
): Scenario {
  const id = parseScenarioId(input.id);
  if (
    input.confidence !== "low" ||
    input.owner !== "Delivery/SRE/Product" ||
    input.label.trim().length === 0
  ) {
    throw new Error(`${id} perdió procedencia, confianza u owner`);
  }

  requireFiniteNonNegative(Object.values(input.volumes), `${id}.volumes`);
  requireFiniteNonNegative(Object.values(input.demand), `${id}.demand`);
  if (
    Object.values(input.volumes).some((value) => value === 0) ||
    Object.values(input.demand).some((value) => value === 0) ||
    input.volumes.concurrentUsers > input.volumes.registeredUsers ||
    input.demand.peakFactor < 1 ||
    input.demand.objectVersionFactor < 1
  ) {
    throw new Error(`${id} contiene un escenario imposible`);
  }

  return {
    id,
    label: input.label,
    confidence: input.confidence,
    volumes: input.volumes,
    demand: input.demand,
  };
}

function parseNetworkProfile(
  input: (typeof capacityFixture.networkProfiles)[number],
): NetworkProfile {
  const id = parseNetworkProfileId(input.id);
  requireFiniteNonNegative(
    [
      input.latencyMs,
      input.downloadKbps,
      input.uploadKbps,
      input.packetLossPercent,
    ],
    `${id}.network`,
  );
  if (
    input.evidence !== "synthetic" ||
    input.label.trim().length === 0 ||
    input.packetLossPercent > 100 ||
    input.online !== (id !== "offline")
  ) {
    throw new Error(`${id} contiene un perfil de red inválido`);
  }

  return {
    id,
    label: input.label,
    latencyMs: input.latencyMs,
    downloadKbps: input.downloadKbps,
    uploadKbps: input.uploadKbps,
    packetLossPercent: input.packetLossPercent,
    online: input.online,
  };
}

const scenarios = capacityFixture.scenarios.map(parseScenario);
const networkProfiles =
  capacityFixture.networkProfiles.map(parseNetworkProfile);

function requireScenario(id: ScenarioId): Scenario {
  const matches = scenarios.filter((scenario) => scenario.id === id);
  if (matches.length !== 1) {
    throw new Error(`Se requiere exactamente un escenario ${id}`);
  }
  return matches[0];
}

function requireNetworkProfile(id: NetworkProfileId): NetworkProfile {
  const matches = networkProfiles.filter((profile) => profile.id === id);
  if (matches.length !== 1) {
    throw new Error(`Se requiere exactamente un perfil de red ${id}`);
  }
  return matches[0];
}

export const SCENARIOS: Readonly<Record<ScenarioId, Scenario>> = {
  pilot: requireScenario("pilot"),
  "growth-10x": requireScenario("growth-10x"),
  "burst-2x": requireScenario("burst-2x"),
};

export const NETWORK_PROFILES: Readonly<
  Record<NetworkProfileId, NetworkProfile>
> = {
  target: requireNetworkProfile("target"),
  constrained: requireNetworkProfile("constrained"),
  critical: requireNetworkProfile("critical"),
  offline: requireNetworkProfile("offline"),
};

export const LAB_STATES: Readonly<Record<LabStateId, LabState>> = {
  healthy: {
    id: "healthy",
    label: "Disponible",
    title: "Escenario calculado",
    detail:
      "La estimación sintética está disponible. No representa una medición productiva.",
    tone: "healthy",
  },
  loading: {
    id: "loading",
    label: "Cargando",
    title: "Preparando estimación",
    detail: "La interfaz conserva contexto mientras espera el cálculo.",
    tone: "neutral",
  },
  empty: {
    id: "empty",
    label: "Vacío",
    title: "Todavía no hay mediciones reales",
    detail:
      "Se requieren muestras del piloto antes de convertir estimaciones en compromisos.",
    tone: "neutral",
  },
  error: {
    id: "error",
    label: "Error",
    title: "No se pudo calcular",
    detail:
      "El error queda visible; no se reemplaza con un valor por defecto engañoso.",
    tone: "danger",
  },
  stale: {
    id: "stale",
    label: "Vencido",
    title: "Estimación fuera de vigencia",
    detail: "Revalidar volúmenes y supuestos antes de usar este escenario.",
    tone: "warning",
  },
  degraded: {
    id: "degraded",
    label: "Degradado",
    title: "Capacidad parcial",
    detail:
      "La operación principal sigue disponible con menor evidencia y reintentos controlados.",
    tone: "warning",
  },
  conflict: {
    id: "conflict",
    label: "Conflicto",
    title: "El escenario cambió",
    detail:
      "La confirmación se detiene para evitar sobrescribir una revisión más nueva.",
    tone: "danger",
  },
  "provider-down": {
    id: "provider-down",
    label: "Proveedor caído",
    title: "Dependencia externa no disponible",
    detail:
      "El núcleo se mantiene separado del SLO externo y muestra el estado degradado.",
    tone: "warning",
  },
};

export const MISSING_COST_DRIVERS = [
  "Proveedor y región",
  "Cómputo por hora",
  "Almacenamiento GiB-mes",
  "Egreso por GiB",
  "Procesamiento por millón de filas",
  "Soporte mensual e impuestos",
] as const;

export const SLO_TARGETS = [
  {
    label: "Núcleo mensual",
    value: "99,9 %",
    detail: "43 min 12 s de presupuesto de error / 30 días",
  },
  {
    label: "Lectura API p95",
    value: "≤ 400 ms",
    detail: "Objetivo futuro; no medido por este prototipo",
  },
  {
    label: "Escritura API p95",
    value: "≤ 800 ms",
    detail: "Objetivo futuro; no medido por este prototipo",
  },
  {
    label: "Recuperación",
    value: "RPO 15 m · RTO 2 h",
    detail: "Hipótesis hasta aprobación operativa",
  },
] as const;

export function projectCapacity(scenario: Scenario): CapacityProjection {
  const secondsPerDay = 86_400;
  const retainedObjectStorageGiB =
    (scenario.volumes.documentsPerMonth *
      scenario.volumes.averageDocumentMiB *
      scenario.demand.retentionMonths *
      scenario.demand.objectVersionFactor) /
    1_024;

  return {
    peakReadRequestsPerSecond:
      (scenario.demand.dailyReadRequests / secondsPerDay) *
      scenario.demand.peakFactor,
    peakWriteRequestsPerSecond:
      (scenario.demand.dailyWriteRequests / secondsPerDay) *
      scenario.demand.peakFactor,
    retainedObjectStorageGiB,
    dailyImportDrainSeconds:
      scenario.volumes.importRowsPerDay / scenario.demand.workerRowsPerSecond,
  };
}

export function formatNumber(value: number, maximumFractionDigits = 0): string {
  return new Intl.NumberFormat("es-AR", { maximumFractionDigits }).format(
    value,
  );
}
