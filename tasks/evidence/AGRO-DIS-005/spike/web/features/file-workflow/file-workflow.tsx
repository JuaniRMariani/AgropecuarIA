"use client";

import { useReducer, useRef, useState } from "react";
import {
  formatBytes,
  isScenario,
  shortId,
  workflowReducer,
  type Scenario,
  type WorkflowState,
} from "./workflow-model";

const scenarioLabels = {
  clean: "Análisis aprobado",
  quarantine: "Amenaza detectada",
  scan_unavailable: "Antivirus no disponible",
  provider_unavailable: "Storage no disponible",
  conflict: "Conflicto de versión",
  upload_error: "Carga interrumpida",
  expired: "Enlace de descarga vencido",
} satisfies Record<Scenario, string>;

const statusCopy = {
  idle: "Esperando un archivo",
  validation_error: "Validación rechazada",
  ready: "Listo para cargar",
  uploading: "Carga en curso",
  scanning: "En cuarentena preventiva",
  available: "Disponible",
  quarantined: "Cuarentena confirmada",
  scan_unavailable: "Análisis demorado",
  provider_unavailable: "Proveedor no disponible",
  conflict: "Conflicto de versión",
  upload_error: "Carga interrumpida",
} satisfies Record<WorkflowState["kind"], string>;

function FileSummary({ state }: Readonly<{ state: WorkflowState }>) {
  if (state.kind === "idle" || state.kind === "validation_error") return null;

  return (
    <dl className="file-summary" aria-label="Archivo seleccionado">
      <div>
        <dt>Archivo</dt>
        <dd>{state.file.name}</dd>
      </div>
      <div>
        <dt>Tamaño</dt>
        <dd>{formatBytes(state.file.sizeBytes)}</dd>
      </div>
      <div>
        <dt>Tipo declarado</dt>
        <dd>{state.file.mediaType}</dd>
      </div>
      {"fileId" in state ? (
        <div>
          <dt>ID</dt>
          <dd className="short-id" translate="no">
            {shortId(state.fileId)}
          </dd>
        </div>
      ) : null}
    </dl>
  );
}

function StateMessage({ state }: Readonly<{ state: WorkflowState }>) {
  switch (state.kind) {
    case "idle":
      return <p>Elegí un comprobante o una imagen para comenzar.</p>;
    case "validation_error":
      return <p className="message danger">{state.message}</p>;
    case "ready":
      return (
        <p>La validación inicial pasó. Todavía no se envió ningún contenido.</p>
      );
    case "uploading":
      return (
        <div className="progress-stack">
          <div className="progress-label">
            <span>Transferencia cifrada simulada</span>
            <strong>{state.progress}%</strong>
          </div>
          <progress value={state.progress} max={100}>
            {state.progress}%
          </progress>
          <p>
            El archivo permanece fuera de disponibilidad hasta finalizar el
            análisis.
          </p>
        </div>
      );
    case "scanning":
      return (
        <p className="message warning">
          El archivo está aislado. Nadie puede descargarlo mientras el análisis
          está pendiente.
        </p>
      );
    case "available":
      return state.grant === "active" ? (
        <p className="message success">
          {state.downloaded
            ? "Descarga sintética autorizada y auditada. No se transfirió contenido real."
            : "Análisis aprobado. La descarga temporal está habilitada."}
        </p>
      ) : (
        <p className="message warning">
          El enlace temporal venció. Renovalo para volver a descargar sin
          exponer el objeto.
        </p>
      );
    case "quarantined":
      return (
        <p className="message danger">
          Amenaza sintética detectada. El objeto sigue aislado y no puede
          descargarse.
        </p>
      );
    case "scan_unavailable":
      return (
        <p className="message warning">
          El antivirus no respondió. La política fail-closed conserva el archivo
          en cuarentena.
        </p>
      );
    case "provider_unavailable":
      return (
        <p className="message danger">
          El proveedor de almacenamiento no respondió. No se confirmó la carga.
        </p>
      );
    case "conflict":
      return (
        <p className="message warning">
          Existe una versión más reciente (v{state.currentVersion}). Revisá
          antes de reintentar.
        </p>
      );
    case "upload_error":
      return (
        <p className="message danger">
          La transferencia se interrumpió. No se creó una versión disponible.
        </p>
      );
    default: {
      const exhaustive: never = state;
      return exhaustive;
    }
  }
}

export function FileWorkflow() {
  const [state, dispatch] = useReducer(workflowReducer, { kind: "idle" });
  const [scenario, setScenario] = useState<Scenario>("clean");
  const fileInput = useRef<HTMLInputElement>(null);
  const statusHeading = useRef<HTMLHeadingElement>(null);

  function focusStatus() {
    globalThis.setTimeout(() => statusHeading.current?.focus(), 0);
  }

  function selectFile(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.currentTarget.files?.item(0);
    if (!file) return;
    dispatch({
      type: "select",
      file: { name: file.name, mediaType: file.type, sizeBytes: file.size },
    });
    focusStatus();
  }

  function reset() {
    dispatch({ type: "reset" });
    if (fileInput.current) fileInput.current.value = "";
    fileInput.current?.focus();
  }

  function renderAction() {
    switch (state.kind) {
      case "idle":
      case "validation_error":
        return null;
      case "ready":
        return (
          <button
            className="button primary"
            onClick={() => dispatch({ type: "start_upload" })}
          >
            Cargar en cuarentena
          </button>
        );
      case "uploading":
        return (
          <button
            className="button primary"
            onClick={() => dispatch({ type: "advance_upload" })}
          >
            Avanzar transferencia
          </button>
        );
      case "scanning":
        return (
          <button
            className="button primary"
            onClick={() => {
              dispatch({ type: "finish_scan", scenario });
              focusStatus();
            }}
          >
            Completar análisis sintético
          </button>
        );
      case "available":
        return state.grant === "active" ? (
          <div className="button-row">
            <button
              className="button primary"
              type="button"
              onClick={() => dispatch({ type: "request_download" })}
            >
              Descargar con enlace temporal
            </button>
            <button
              className="button ghost"
              onClick={() => dispatch({ type: "expire_grant" })}
            >
              Vencer enlace de prueba
            </button>
          </div>
        ) : (
          <button
            className="button primary"
            onClick={() => dispatch({ type: "renew_grant" })}
          >
            Renovar enlace temporal
          </button>
        );
      case "scan_unavailable":
      case "provider_unavailable":
      case "conflict":
      case "upload_error":
        return (
          <button
            className="button primary"
            onClick={() => dispatch({ type: "retry" })}
          >
            Reintentar de forma segura
          </button>
        );
      case "quarantined":
        return (
          <button className="button ghost" onClick={reset}>
            Elegir otro archivo
          </button>
        );
      default: {
        const exhaustive: never = state;
        return exhaustive;
      }
    }
  }

  return (
    <section className="workflow-panel" aria-labelledby="workflow-title">
      <div className="panel-kicker">Ensayo controlado · sin red</div>
      <h2 id="workflow-title">Recorrido seguro del archivo</h2>
      <p className="panel-intro">
        Este prototipo no sube información. Hace visibles las decisiones y
        fallos antes de conectar un proveedor real.
      </p>

      <div className="field-grid">
        <div className="field">
          <label htmlFor="evidence-file">Archivo de prueba</label>
          <input
            id="evidence-file"
            name="evidence-file"
            ref={fileInput}
            type="file"
            autoComplete="off"
            accept="application/pdf,image/jpeg,image/png"
            onChange={selectFile}
          />
          <span className="field-help">PDF, JPG o PNG · hasta 10 MB</span>
        </div>

        <div className="field">
          <label htmlFor="scenario">Resultado a simular</label>
          <select
            id="scenario"
            name="scenario"
            autoComplete="off"
            value={scenario}
            onChange={(event) => {
              const selected = event.currentTarget.value;
              if (isScenario(selected)) setScenario(selected);
            }}
          >
            {Object.entries(scenarioLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
          <span className="field-help">
            Fixture local, sin llamadas externas
          </span>
        </div>
      </div>

      <div className={`state-card state-${state.kind}`} data-state={state.kind}>
        <div className="state-header">
          <span className="signal" aria-hidden="true" />
          <div>
            <span className="eyebrow">Estado actual</span>
            <h3 ref={statusHeading} tabIndex={-1}>
              {statusCopy[state.kind]}
            </h3>
          </div>
        </div>

        <div
          className="state-live"
          role="status"
          aria-live="polite"
          aria-atomic="true"
        >
          <StateMessage state={state} />
        </div>
        <FileSummary state={state} />
        <div className="state-actions">
          {renderAction()}
          {state.kind !== "idle" && state.kind !== "quarantined" ? (
            <button className="button text" onClick={reset}>
              Reiniciar ensayo
            </button>
          ) : null}
        </div>
      </div>
    </section>
  );
}
