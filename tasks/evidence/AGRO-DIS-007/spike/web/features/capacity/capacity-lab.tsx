"use client";

import { useEffect, useId, useState } from "react";

import {
  LAB_STATES,
  MISSING_COST_DRIVERS,
  NETWORK_PROFILES,
  SCENARIOS,
  SLO_TARGETS,
  VALID_UNTIL,
  formatNumber,
  projectCapacity,
  type LabStateId,
  type NetworkProfileId,
  type ScenarioId,
} from "@/lib/capacity-model";
import {
  acceptSubmission,
  createSubmission,
  failSubmission,
  submissionMessage,
  type SubmissionState,
} from "@/lib/idempotency";

const scenarioIds: readonly ScenarioId[] = ["pilot", "growth-10x", "burst-2x"];
const networkProfileIds: readonly NetworkProfileId[] = [
  "target",
  "constrained",
  "critical",
  "offline",
];
const labStateIds: readonly LabStateId[] = [
  "healthy",
  "loading",
  "empty",
  "error",
  "stale",
  "degraded",
  "conflict",
  "provider-down",
];

type BrowserConnection = "checking" | "online" | "offline";

function isScenarioId(value: string): value is ScenarioId {
  return value === "pilot" || value === "growth-10x" || value === "burst-2x";
}

function isNetworkProfileId(value: string): value is NetworkProfileId {
  return (
    value === "target" ||
    value === "constrained" ||
    value === "critical" ||
    value === "offline"
  );
}

function isLabStateId(value: string): value is LabStateId {
  return labStateIds.some((stateId) => stateId === value);
}

function operationButtonLabel(state: SubmissionState): string {
  switch (state.kind) {
    case "idle":
      return "Confirmar operación sintética";
    case "failed":
      return "Reintentar con la misma clave";
    case "accepted":
      return "Simular reenvío duplicado";
    case "duplicate":
      return "Duplicado bloqueado";
    default: {
      const exhaustive: never = state;
      return exhaustive;
    }
  }
}

export function CapacityLab() {
  const stableId = useId().replaceAll(":", "");
  const [scenarioId, setScenarioId] = useState<ScenarioId>("pilot");
  const [networkProfileId, setNetworkProfileId] =
    useState<NetworkProfileId>("target");
  const [labStateId, setLabStateId] = useState<LabStateId>("healthy");
  const [browserConnection, setBrowserConnection] =
    useState<BrowserConnection>("checking");
  const [submission, setSubmission] = useState<SubmissionState>(() =>
    createSubmission(`capacity-lab-${stableId}`),
  );

  useEffect(() => {
    const updateConnection = () =>
      setBrowserConnection(navigator.onLine ? "online" : "offline");
    updateConnection();
    window.addEventListener("online", updateConnection);
    window.addEventListener("offline", updateConnection);
    return () => {
      window.removeEventListener("online", updateConnection);
      window.removeEventListener("offline", updateConnection);
    };
  }, []);

  const scenario = SCENARIOS[scenarioId];
  const projection = projectCapacity(scenario);
  const networkProfile = NETWORK_PROFILES[networkProfileId];
  const labState = LAB_STATES[labStateId];
  const canSubmit =
    browserConnection === "online" &&
    networkProfile.online &&
    submission.kind !== "duplicate";

  const handleOperation = () => {
    if (!canSubmit) {
      return;
    }

    setSubmission((current) =>
      current.kind === "idle"
        ? failSubmission(current)
        : acceptSubmission(current),
    );
  };

  return (
    <>
      <a
        className="skip-link"
        href="#contenido"
        onClick={(event) => {
          event.preventDefault();
          document.getElementById("contenido")?.focus();
        }}
      >
        Saltar al contenido
      </a>
      <main className="shell" id="contenido" tabIndex={-1}>
        <header className="masthead">
          <div>
            <p className="eyebrow">
              AgropecuarIA · Evidencia R0 / AGRO-DIS-007
            </p>
            <h1>Capacidad sin humo.</h1>
            <p className="lede">
              Un laboratorio reproducible para discutir volúmenes, SLO,
              conectividad y costo sin convertir estimaciones sintéticas en
              promesas de producción.
            </p>
          </div>
          <div
            className="proof-stamp"
            aria-label="Clasificación: estimado, confianza baja"
          >
            Estimado
            <span>Confianza baja</span>
          </div>
        </header>

        <div className="notice" role="note">
          <strong>NO ES UN BENCHMARK</strong>
          <p>
            Válido hasta {VALID_UNTIL}. Requiere medición del piloto y
            aprobación del sponsor.
          </p>
        </div>

        <section
          className="control-strip"
          aria-label="Controles del laboratorio"
        >
          <label className="control">
            <span className="label">Escenario sintético</span>
            <select
              aria-label="Escenario sintético"
              value={scenarioId}
              onChange={(event) => {
                if (isScenarioId(event.target.value)) {
                  setScenarioId(event.target.value);
                }
              }}
            >
              {scenarioIds.map((id) => (
                <option key={id} value={id}>
                  {SCENARIOS[id].label}
                </option>
              ))}
            </select>
          </label>

          <label className="control">
            <span className="label">Perfil de conectividad</span>
            <select
              aria-label="Perfil de conectividad"
              value={networkProfileId}
              onChange={(event) => {
                if (isNetworkProfileId(event.target.value)) {
                  setNetworkProfileId(event.target.value);
                }
              }}
            >
              {networkProfileIds.map((id) => (
                <option key={id} value={id}>
                  {NETWORK_PROFILES[id].label}
                </option>
              ))}
            </select>
          </label>

          <label className="control">
            <span className="label">Estado a inspeccionar</span>
            <select
              aria-label="Estado a inspeccionar"
              value={labStateId}
              onChange={(event) => {
                if (isLabStateId(event.target.value)) {
                  setLabStateId(event.target.value);
                }
              }}
            >
              {labStateIds.map((id) => (
                <option key={id} value={id}>
                  {LAB_STATES[id].label}
                </option>
              ))}
            </select>
          </label>
        </section>

        <div className="dashboard">
          <div className="stack">
            <section className="panel" aria-labelledby="volumenes-title">
              <div className="panel-header">
                <div>
                  <p className="kicker">Escenario / {scenario.id}</p>
                  <h2 id="volumenes-title">{scenario.label}</h2>
                  <p>Rangos de discusión; no son demanda observada.</p>
                </div>
                <span className="confidence">Confianza baja</span>
              </div>

              <div className="metrics" aria-label="Volúmenes del escenario">
                <Metric
                  label="Empresas"
                  value={formatNumber(scenario.volumes.tenants)}
                />
                <Metric
                  label="Usuarios concurrentes"
                  value={formatNumber(scenario.volumes.concurrentUsers)}
                />
                <Metric
                  label="Campos"
                  value={formatNumber(scenario.volumes.farms)}
                />
                <Metric
                  label="Lotes"
                  value={formatNumber(scenario.volumes.fields)}
                />
                <Metric
                  label="Documentos / mes"
                  value={formatNumber(scenario.volumes.documentsPerMonth)}
                />
                <Metric
                  label="Filas importadas / día"
                  value={formatNumber(scenario.volumes.importRowsPerDay)}
                />
                <Metric
                  label="Jobs / hora"
                  value={formatNumber(scenario.volumes.jobsPerHour)}
                />
                <Metric
                  label="Retención supuesta"
                  value={`${scenario.demand.retentionMonths} meses`}
                />
              </div>
            </section>

            <section
              className="panel panel--dark"
              aria-labelledby="projection-title"
            >
              <div className="panel-header">
                <div>
                  <p className="kicker">Modelo determinístico</p>
                  <h2 id="projection-title">Proyección calculada</h2>
                  <p>
                    Derivada de los volúmenes seleccionados, sin tráfico ni
                    infraestructura real.
                  </p>
                </div>
              </div>
              <div className="projection-grid">
                <Projection
                  label="Lecturas pico"
                  value={`${formatNumber(projection.peakReadRequestsPerSecond, 1)} req/s`}
                  detail={`Promedio diario × ${scenario.demand.peakFactor}`}
                />
                <Projection
                  label="Escrituras pico"
                  value={`${formatNumber(projection.peakWriteRequestsPerSecond, 1)} req/s`}
                  detail={`Promedio diario × ${scenario.demand.peakFactor}`}
                />
                <Projection
                  label="Objetos retenidos"
                  value={`${formatNumber(projection.retainedObjectStorageGiB, 1)} GiB`}
                  detail="Incluye retención y factor de versiones"
                />
                <Projection
                  label="Drenaje diario de importación"
                  value={`${formatNumber(projection.dailyImportDrainSeconds, 1)} s`}
                  detail="A 250 filas/s hipotéticas"
                />
              </div>
            </section>

            <section className="panel" aria-labelledby="slo-title">
              <div className="panel-header">
                <div>
                  <p className="kicker">Objetivos sujetos a aprobación</p>
                  <h2 id="slo-title">SLI / SLO propuestos</h2>
                  <p>Núcleo y proveedores externos se evalúan por separado.</p>
                </div>
              </div>
              <div className="slo-list">
                {SLO_TARGETS.map((target) => (
                  <article className="slo-item" key={target.label}>
                    <span className="label">{target.label}</span>
                    <strong className="slo-value">{target.value}</strong>
                    <p>{target.detail}</p>
                  </article>
                ))}
              </div>
            </section>
          </div>

          <aside className="stack" aria-label="Gates y simulaciones">
            <section className="panel cost-gate" aria-labelledby="cost-title">
              <p className="kicker">Gate FinOps</p>
              <h2 id="cost-title">Costo</h2>
              <p className="no-go">INCOMPLETO / NO-GO</p>
              <p>No se imputan precios faltantes como cero. Faltan:</p>
              <ul className="driver-list">
                {MISSING_COST_DRIVERS.map((driver) => (
                  <li key={driver}>{driver}</li>
                ))}
              </ul>
            </section>

            <section className="panel" aria-labelledby="states-title">
              <div className="panel-header">
                <div>
                  <p className="kicker">Máquina de estados</p>
                  <h2 id="states-title">Respuesta visible</h2>
                </div>
              </div>
              <div
                aria-live="polite"
                className="state-card"
                data-testid="lab-state"
                data-tone={labState.tone}
              >
                <p className="status-label">{labState.label}</p>
                <h3>{labState.title}</h3>
                <p>{labState.detail}</p>
              </div>
            </section>

            <section className="panel" aria-labelledby="network-title">
              <div className="panel-header">
                <div>
                  <p className="kicker">Prueba de conectividad</p>
                  <h2 id="network-title">Operación online</h2>
                </div>
              </div>

              <div
                className="connection"
                aria-live="polite"
                data-testid="connection-status"
              >
                <span
                  className="connection-dot"
                  data-online={browserConnection === "online"}
                  aria-hidden="true"
                />
                <div>
                  <p>
                    <strong>
                      {browserConnection === "checking"
                        ? "Comprobando conexión"
                        : browserConnection === "online"
                          ? "Navegador con conexión"
                          : "Navegador sin conexión"}
                    </strong>
                  </p>
                  <p>
                    Perfil: {networkProfile.label} ·{" "}
                    {formatNumber(networkProfile.latencyMs)} ms · pérdida{" "}
                    {networkProfile.packetLossPercent} %
                  </p>
                </div>
              </div>

              <p className="offline-policy">No guardamos trabajo offline.</p>

              <div className="operation">
                <p className="key-policy">
                  El reintento conserva una clave de sesión protegida; nunca se
                  muestra ni se agrega a telemetría.
                </p>
                <button
                  type="button"
                  disabled={!canSubmit}
                  onClick={handleOperation}
                  data-testid="operation-button"
                >
                  {operationButtonLabel(submission)}
                </button>
                <p
                  aria-live="polite"
                  className="operation-message"
                  data-kind={submission.kind}
                  data-testid="operation-message"
                >
                  {browserConnection === "checking"
                    ? "Confirmación bloqueada mientras comprobamos la conexión."
                    : browserConnection === "offline" || !networkProfile.online
                      ? "Confirmación bloqueada: recuperá la conexión para continuar. No se creó una cola local."
                      : submissionMessage(submission)}
                </p>
              </div>
            </section>
          </aside>
        </div>

        <footer className="fine-print">
          Evidencia sintética y descartable · Sin API, base de datos, proveedor
          cloud, precios reales, almacenamiento offline ni compromiso de fecha.
          Las mediciones del piloto reemplazarán estas hipótesis.
        </footer>
      </main>
    </>
  );
}

type MetricProps = Readonly<{ label: string; value: string }>;

function Metric({ label, value }: MetricProps) {
  return (
    <div className="metric">
      <span className="metric-label">{label}</span>
      <strong className="metric-value">{value}</strong>
    </div>
  );
}

type ProjectionProps = Readonly<{
  label: string;
  value: string;
  detail: string;
}>;

function Projection({ label, value, detail }: ProjectionProps) {
  return (
    <article className="projection-card">
      <span className="label">{label}</span>
      <strong className="projection-value">{value}</strong>
      <p>{detail}</p>
    </article>
  );
}
