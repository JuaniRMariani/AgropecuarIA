"use client";

import dynamic from "next/dynamic";
import { useCallback, useState } from "react";

import {
  EVIDENCE_LABELS,
  FRESHNESS_LABELS,
  SIMULATION_OPTIONS,
  effectiveFreshness,
  formatCoordinate,
  type SimulationState,
} from "@/lib/presentation";
import type { TerritoryFixture } from "@/lib/territory";

const TerritoryMap = dynamic(
  () => import("./territory-map").then((module) => module.TerritoryMap),
  {
    ssr: false,
    loading: () => (
      <div className="map-placeholder" role="status">
        Preparando el visor accesible…
      </div>
    ),
  },
);

function StateNotice({
  state,
}: Readonly<{ state: Exclude<SimulationState, "ready"> }>) {
  const notices = {
    stale: {
      eyebrow: "Evidencia con antigüedad",
      title: "Los datos se conservan, claramente rotulados.",
      body: "No se presentan como actuales y ninguna recomendación debería ocultar su fecha.",
    },
    unavailable: {
      eyebrow: "Proveedor no disponible",
      title: "Argenmap no responde. La cobertura sigue en la tabla.",
      body: "No intentamos completar el mapa ni inferir cobertura; el flujo principal puede continuar en modo degradado.",
    },
    loading: {
      eyebrow: "Carga en curso",
      title: "Consultando la referencia territorial…",
      body: "La espera es explícita y no bloquea el acceso a la explicación del fixture.",
    },
    empty: {
      eyebrow: "Respuesta válida, sin cobertura",
      title: "No hay jurisdicciones para mostrar.",
      body: "Un conjunto vacío no se confunde con un error ni con cobertura nacional aprobada.",
    },
    error: {
      eyebrow: "Fallo recuperable",
      title: "No pudimos preparar esta vista territorial.",
      body: "El incidente queda visible; la interfaz no inventa datos para llenar el espacio.",
    },
  } as const;
  const notice = notices[state];

  return (
    <div
      className={`state-notice state-notice--${state}`}
      role={state === "error" ? "alert" : "status"}
    >
      <span className="state-symbol" aria-hidden="true">
        {state === "loading"
          ? "···"
          : state === "stale"
            ? "◷"
            : state === "empty"
              ? "○"
              : "!"}
      </span>
      <div>
        <p className="eyebrow">{notice.eyebrow}</p>
        <h2>{notice.title}</h2>
        <p>{notice.body}</p>
      </div>
    </div>
  );
}

export function TerritoryExplorer({
  fixture,
}: Readonly<{ fixture: TerritoryFixture }>) {
  const [simulation, setSimulation] = useState<SimulationState>("ready");
  const handleProviderError = useCallback(
    () => setSimulation("unavailable"),
    [],
  );
  const hasData =
    simulation !== "empty" &&
    simulation !== "error" &&
    simulation !== "loading";
  const canRenderMap = hasData && simulation !== "unavailable";

  return (
    <section
      id="cobertura"
      className="explorer"
      aria-labelledby="coverage-title"
    >
      <div className="explorer-heading">
        <div>
          <p className="section-number">01 / Evidencia nacional</p>
          <h2 id="coverage-title">
            Un contrato visible, no una falsa precisión.
          </h2>
        </div>
        <p className="fixture-copy">{fixture.purpose}</p>
      </div>

      <fieldset className="simulator">
        <legend>Simular estado de la experiencia</legend>
        <div className="simulator-options">
          {SIMULATION_OPTIONS.map((option) => (
            <label key={option.value} className="simulation-option">
              <input
                type="radio"
                name="simulation"
                value={option.value}
                checked={simulation === option.value}
                onChange={() => setSimulation(option.value)}
              />
              <span>{option.label}</span>
              <small>{option.description}</small>
            </label>
          ))}
        </div>
      </fieldset>

      {simulation === "ready" ? null : <StateNotice state={simulation} />}

      {simulation === "error" ? (
        <button
          className="primary-button"
          type="button"
          onClick={() => setSimulation("ready")}
        >
          Reintentar con el fixture
        </button>
      ) : null}

      {hasData ? (
        <div className="contract-demo" aria-labelledby="weather-contract-title">
          <div>
            <p className="eyebrow">Demostración sintética</p>
            <h3 id="weather-contract-title">Naturaleza del dato climático</h3>
            <p>
              Estos rótulos prueban el contrato visual; no representan clima ni
              mediciones de las jurisdicciones mostradas.
            </p>
          </div>
          <div
            className="legend"
            aria-label="Ejemplos sintéticos de naturaleza climática"
          >
            {(
              Object.keys(EVIDENCE_LABELS) as Array<
                keyof typeof EVIDENCE_LABELS
              >
            ).map((nature) => (
              <span
                key={nature}
                className={`evidence-chip evidence-chip--${nature}`}
              >
                <i aria-hidden="true" />
                {EVIDENCE_LABELS[nature]}
              </span>
            ))}
          </div>
        </div>
      ) : null}

      {canRenderMap ? (
        <div className="map-section" aria-labelledby="map-title">
          <div className="panel-title">
            <div>
              <p className="eyebrow">Mejora progresiva</p>
              <h3 id="map-title">Visor Argenmap</h3>
            </div>
            <p>
              La base cartográfica puede fallar sin quitar acceso a los 24
              registros.
            </p>
          </div>
          <TerritoryMap
            points={fixture.points}
            onProviderError={handleProviderError}
          />
        </div>
      ) : null}

      {hasData ? (
        <div className="table-section" aria-labelledby="table-title">
          <div className="panel-title">
            <div>
              <p className="eyebrow">Alternativa equivalente</p>
              <h3 id="table-title">Matriz de jurisdicciones</h3>
            </div>
            <p>
              <strong>{fixture.points.length} de 24</strong> referencias
              cargadas · coordenadas públicas, no lotes.
            </p>
          </div>

          <div
            className="legend"
            aria-label="Procedencia de las referencias territoriales"
          >
            <span>
              Los 24 centroides son referencias observadas de la fuente oficial
              Georef; no son observaciones meteorológicas.
            </span>
          </div>

          <div className="table-shell">
            <table>
              <caption className="sr-only">
                Centroides administrativos de las 24 jurisdicciones argentinas
                con naturaleza y frescura de evidencia.
              </caption>
              <thead>
                <tr>
                  <th scope="col">ID</th>
                  <th scope="col">Jurisdicción</th>
                  <th scope="col">Latitud</th>
                  <th scope="col">Longitud</th>
                  <th scope="col">Naturaleza</th>
                  <th scope="col">Frescura</th>
                </tr>
              </thead>
              <tbody>
                {fixture.points.map((point) => {
                  const freshness = effectiveFreshness(
                    point.freshness,
                    simulation,
                  );
                  return (
                    <tr key={point.id}>
                      <td data-label="ID">
                        <span className="short-id">{point.id}</span>
                      </td>
                      <th data-label="Jurisdicción" scope="row">
                        {point.name}
                      </th>
                      <td data-label="Latitud" className="numeric">
                        {formatCoordinate(point.latitude)}
                      </td>
                      <td data-label="Longitud" className="numeric">
                        {formatCoordinate(point.longitude)}
                      </td>
                      <td data-label="Naturaleza">
                        <span
                          className={`evidence-chip evidence-chip--${point.evidence}`}
                        >
                          <i aria-hidden="true" />
                          {EVIDENCE_LABELS[point.evidence]}
                        </span>
                      </td>
                      <td data-label="Frescura">
                        <span
                          className={`freshness-chip freshness-chip--${freshness}`}
                        >
                          <i aria-hidden="true" />
                          {FRESHNESS_LABELS[freshness]}
                        </span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}
    </section>
  );
}
