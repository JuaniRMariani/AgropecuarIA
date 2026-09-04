"use client";

import { useEffect, useId, useRef, useState } from "react";
import { formatShortId } from "../../lib/format-id";
import {
  CatalogApiError,
  getCatalogItem,
  listCatalogVersions,
  searchCatalog,
} from "./catalog-api";
import type {
  CatalogFilters,
  CatalogItem,
  CatalogSearch,
  CatalogVersion,
  CatalogVersions,
} from "./catalog-types";

type Resource<T> =
  | Readonly<{ kind: "loading"; key: string }>
  | Readonly<{ kind: "ready"; key: string; value: T }>
  | Readonly<{ kind: "error"; key: string; message: string }>;
const DATE = new Intl.DateTimeFormat("es-AR", {
  dateStyle: "medium",
  timeZone: "UTC",
});
const CATEGORIES = [
  "AGRICULTURA",
  "GANADERIA",
  "FORRAJE",
  "HORTICULTURA",
  "FRUTICULTURA",
  "FORESTACION",
  "OTROS",
];
const SUPPORT_LABELS: Readonly<Record<string, string>> = {
  CATALOGADA: "Catalogada",
  FLUJO_GENERICO: "Flujo genérico",
  ESPECIALIZADA_VALIDADA: "Especialización declarada",
};
const CAPABILITY_LABELS: Readonly<Record<string, string>> = {
  specialized_rules: "Reglas técnicas especializadas",
  specialized_kpis: "Indicadores especializados",
  ai_recommendations: "Recomendaciones de IA",
};
function label(value: string): string {
  return Object.hasOwn(SUPPORT_LABELS, value)
    ? (SUPPORT_LABELS[value] ?? value)
    : value;
}
function printable(value: string): string {
  return value.replace(
    /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi,
    formatShortId,
  );
}
function failure(error: unknown): string {
  if (error instanceof CatalogApiError) {
    if (error.kind === "signed-out")
      return "Tu sesión venció. Volvé a ingresar para consultar el catálogo.";
    if (error.kind === "forbidden")
      return "Esta sesión no tiene acceso a la consulta del catálogo.";
    if (error.kind === "not-found")
      return "La entrada o versión solicitada no está disponible. Consultá la versión activa o elegí otra versión.";
    if (error.kind === "rate-limited")
      return "Hay demasiadas consultas. Esperá unos segundos y reintentá.";
    if (error.kind === "validation")
      return "Revisá el texto y los filtros de la consulta.";
    if (error.kind === "contract")
      return "El catálogo respondió con un formato inesperado. No mostramos información que no podamos verificar.";
  }
  return "No pudimos consultar el catálogo. Revisá tu conexión y reintentá.";
}

function CatalogDetail({ item }: Readonly<{ item: CatalogItem }>) {
  return (
    <>
      <div className="catalog-detail__heading">
        <div>
          <p className="section-kicker">
            Entrada publicada · {printable(item.code)}
          </p>
          <h3>{printable(item.displayName)}</h3>
        </div>
        <span className="field-chip">
          {printable(label(item.supportLevel))}
        </span>
      </div>
      <p className="catalog-meta">
        Versión {printable(item.versionTag)} · {formatShortId(item.versionId)} ·{" "}
        {item.versionId === item.activeVersionId
          ? "Activa al consultar"
          : "Histórica"}
      </p>
      <dl className="catalog-facts">
        <div>
          <dt>Jurisdicción</dt>
          <dd>{printable(item.jurisdiction)}</dd>
        </div>
        <div>
          <dt>Categoría</dt>
          <dd>{printable(item.category)}</dd>
        </div>
        <div>
          <dt>Sinónimos</dt>
          <dd>
            {item.synonyms.length === 0
              ? "Sin sinónimos publicados"
              : item.synonyms.map(printable).join(" · ")}
          </dd>
        </div>
        <div>
          <dt>Registro</dt>
          <dd>{formatShortId(item.id)}</dd>
        </div>
      </dl>
      <section
        aria-label="Procedencia de la entrada"
        className="catalog-provenance"
      >
        <h4>Procedencia</h4>
        {item.provenanceStatus === "verified_snapshot" ? (
          <>
            <p>
              Fuente declarada:{" "}
              {item.sourceId === null
                ? "No disponible"
                : printable(item.sourceId)}
            </p>
            <p>
              Snapshot{" "}
              {item.sourceSnapshotId === null
                ? "no disponible"
                : formatShortId(item.sourceSnapshotId)}{" "}
              · incorporación{" "}
              {item.sourceIngestedAtUtc === null
                ? "sin fecha"
                : DATE.format(new Date(item.sourceIngestedAtUtc))}
            </p>
            <details>
              <summary>Ver huella del contenido (SHA-256)</summary>
              <code className="catalog-hash">{item.sourceHash}</code>
            </details>
            <p>
              La referencia vincula la entrada con un snapshot. No certifica una
              fuente oficial ni una aprobación profesional.
            </p>
          </>
        ) : (
          <p>
            Procedencia histórica no disponible. Esta entrada conserva sus datos
            publicados, sin atribuirle una fuente verificada.
          </p>
        )}
      </section>
      <section aria-label="Alcance del soporte" className="catalog-support">
        <h4>Alcance del soporte</h4>
        <p>
          El nivel publicado no habilita por sí solo una especialización
          técnica. Este lector no activa capacidades ni permite registrar
          ciclos.
        </p>
        <p>
          {item.capabilities.length === 0
            ? "No hay capacidades activas informadas."
            : `Capacidades declaradas por el catálogo: ${item.capabilities.map(printable).join(" · ")}. Su uso no está habilitado en este lector.`}
        </p>
        <h4>Capacidades no disponibles</h4>
        {item.absentCapabilities.length === 0 ? (
          <p>
            El catálogo no informó una lista de capacidades ausentes. Esto no
            confirma disponibilidad.
          </p>
        ) : (
          <ul>
            {item.absentCapabilities.map((capability) => (
              <li key={capability}>
                {printable(CAPABILITY_LABELS[capability] ?? capability)}
              </li>
            ))}
          </ul>
        )}
      </section>
    </>
  );
}

export function CatalogReader() {
  const id = useId();
  const [query, setQuery] = useState("");
  const [jurisdiction, setJurisdiction] = useState("");
  const [category, setCategory] = useState("");
  const [supportLevel, setSupportLevel] = useState("");
  const [version, setVersion] = useState<CatalogVersion | null>(null);
  const [offset, setOffset] = useState(0);
  const [refresh, setRefresh] = useState(0);
  const [versionsRefresh, setVersionsRefresh] = useState(0);
  const [search, setSearch] = useState<Resource<CatalogSearch>>({
    kind: "loading",
    key: "",
  });
  const [versions, setVersions] = useState<Resource<CatalogVersions>>({
    kind: "loading",
    key: "",
  });
  const [selected, setSelected] = useState<Pick<
    CatalogItem,
    "code" | "versionId"
  > | null>(null);
  const [detail, setDetail] = useState<Resource<CatalogItem>>({
    kind: "loading",
    key: "",
  });
  const [detailRefresh, setDetailRefresh] = useState(0);
  const [activeChanged, setActiveChanged] = useState(false);
  const previousActive = useRef<string | null | undefined>(undefined);
  const searchGeneration = useRef(0);
  const detailGeneration = useRef(0);
  const versionsGeneration = useRef(0);
  const detailRef = useRef<HTMLElement>(null);
  const tableRef = useRef<HTMLDivElement>(null);
  const selectedTrigger = useRef<HTMLButtonElement | null>(null);
  const versionId = version?.id ?? null;
  const searchKey = JSON.stringify([
    query,
    jurisdiction,
    category,
    supportLevel,
    versionId,
    refresh,
  ]);
  const versionsKey = `${offset}:${versionsRefresh}`;
  const detailCode = selected?.code ?? null;
  const detailVersionId = selected?.versionId ?? null;
  const detailKey = JSON.stringify([
    detailCode,
    detailVersionId,
    detailRefresh,
  ]);

  useEffect(() => {
    const generation = ++searchGeneration.current;
    const controller = new AbortController();
    const timeout = window.setTimeout(() => {
      setSearch({ kind: "loading", key: searchKey });
      const filters: CatalogFilters = {
        query,
        jurisdiction,
        category,
        supportLevel,
        versionId,
      };
      void searchCatalog(filters, controller.signal)
        .then((value) => {
          if (
            controller.signal.aborted ||
            generation !== searchGeneration.current
          )
            return;
          if (
            previousActive.current !== undefined &&
            previousActive.current !== value.activeVersionId
          )
            setActiveChanged(true);
          previousActive.current = value.activeVersionId;
          setSearch({ kind: "ready", key: searchKey, value });
        })
        .catch((error: unknown) => {
          if (
            !controller.signal.aborted &&
            generation === searchGeneration.current
          )
            setSearch({
              kind: "error",
              key: searchKey,
              message: failure(error),
            });
        });
    }, 300);
    return () => {
      window.clearTimeout(timeout);
      controller.abort();
    };
  }, [query, jurisdiction, category, supportLevel, versionId, searchKey]);

  useEffect(() => {
    const generation = ++versionsGeneration.current;
    const controller = new AbortController();
    void listCatalogVersions(offset, controller.signal)
      .then((value) => {
        if (
          !controller.signal.aborted &&
          generation === versionsGeneration.current
        )
          setVersions({ kind: "ready", key: versionsKey, value });
      })
      .catch((error: unknown) => {
        if (
          !controller.signal.aborted &&
          generation === versionsGeneration.current
        )
          setVersions({
            kind: "error",
            key: versionsKey,
            message: failure(error),
          });
      });
    return () => controller.abort();
  }, [offset, versionsKey]);

  useEffect(() => {
    if (detailCode === null || detailVersionId === null) return;
    const generation = ++detailGeneration.current;
    const controller = new AbortController();
    void getCatalogItem(detailCode, detailVersionId, controller.signal)
      .then((value) => {
        if (
          !controller.signal.aborted &&
          generation === detailGeneration.current
        )
          setDetail({ kind: "ready", key: detailKey, value });
      })
      .catch((error: unknown) => {
        if (
          !controller.signal.aborted &&
          generation === detailGeneration.current
        )
          setDetail({ kind: "error", key: detailKey, message: failure(error) });
      });
    return () => controller.abort();
  }, [detailCode, detailVersionId, detailKey]);

  useEffect(() => {
    if (detailCode !== null) detailRef.current?.focus();
  }, [detailCode, detailVersionId]);

  const result =
    search.kind === "ready" && search.key === searchKey ? search.value : null;
  const searching = search.key !== searchKey || search.kind === "loading";
  const versionPage =
    versions.kind === "ready" && versions.key === versionsKey
      ? versions.value
      : null;
  const selectedIndex =
    versionPage?.versions.findIndex((item) => item.id === versionId) ?? -1;
  const selectedOption =
    version === null
      ? "active"
      : selectedIndex < 0
        ? "selected"
        : `version-${selectedIndex}`;
  const closeDetail = () => {
    setSelected(null);
    if (selectedTrigger.current?.isConnected) selectedTrigger.current.focus();
    else tableRef.current?.focus();
  };
  const consultActive = () => {
    setVersion(null);
    setSelected(null);
    setRefresh((value) => value + 1);
    setVersionsRefresh((value) => value + 1);
    setActiveChanged(false);
  };

  return (
    <section
      className="identity-card catalog-reader"
      aria-labelledby={`${id}-title`}
    >
      <header className="section-heading catalog-heading">
        <div>
          <p className="section-kicker">Referencia productiva</p>
          <h2 id={`${id}-title`}>Catálogo de actividades</h2>
          <p>
            Consulta de la referencia publicada. No modifica datos de tu
            organización ni acredita aptitud productiva o cumplimiento
            regulatorio.
          </p>
        </div>
        <span className="field-chip">Solo lectura</span>
      </header>
      <div className="catalog-version-panel">
        <div className="form-field">
          <label htmlFor={`${id}-version`}>Versión consultada</label>
          <select
            id={`${id}-version`}
            value={selectedOption}
            onChange={(event) => {
              const index = Number(event.target.value.replace("version-", ""));
              const next =
                event.target.value === "active"
                  ? null
                  : versionPage?.versions[index];
              if (next !== undefined) {
                setVersion(next);
                setSelected(null);
              }
            }}
          >
            <option value="active">Versión activa al consultar</option>
            {version !== null && selectedIndex < 0 ? (
              <option value="selected">
                {printable(version.versionTag)} · {formatShortId(version.id)}
              </option>
            ) : null}
            {versionPage?.versions.map((item, index) => (
              <option key={item.id} value={`version-${index}`}>
                {printable(item.versionTag)} · {formatShortId(item.id)}
                {item.isActive ? " · activa" : " · histórica"}
              </option>
            ))}
          </select>
        </div>
        <div className="catalog-version-controls">
          <button
            className="button button--quiet"
            type="button"
            disabled={offset === 0}
            onClick={() => setOffset(Math.max(0, offset - 20))}
          >
            Versiones anteriores de la lista
          </button>
          <button
            className="button button--quiet"
            type="button"
            disabled={
              versionPage === null || !versionPage.hasMore || offset >= 10000
            }
            onClick={() => setOffset(Math.min(10000, offset + 20))}
          >
            Más versiones históricas
          </button>
          <button
            className="button button--quiet"
            type="button"
            onClick={consultActive}
          >
            Consultar versión activa
          </button>
        </div>
        {versions.kind === "error" && versions.key === versionsKey ? (
          <div role="alert">
            <p>{versions.message}</p>
            <button
              type="button"
              className="button button--quiet"
              onClick={() => setVersionsRefresh((value) => value + 1)}
            >
              Reintentar versiones
            </button>
          </div>
        ) : versionPage === null ? (
          <p className="catalog-meta" role="status">
            Consultando versiones publicadas…
          </p>
        ) : (
          <p className="catalog-meta">
            {versionPage.totalCount} versiones publicadas.{" "}
            {versionPage.versions.length > 0
              ? `Mostrando ${offset + 1}–${offset + versionPage.versions.length}.`
              : ""}
          </p>
        )}
        <p className="catalog-meta" aria-live="polite">
          {result?.versionId
            ? `Versión ${printable(result.versionTag ?? "")} · ${formatShortId(result.versionId)} · publicación ${DATE.format(new Date(result.publishedAtUtc ?? ""))} · ${result.isHistorical ? "Histórica" : "Activa al consultar"}`
            : searching
              ? "Consultando la versión seleccionada…"
              : search.kind === "error"
                ? "No pudimos determinar la versión de esta consulta."
                : "Sin versión publicada para esta consulta."}
        </p>
        {activeChanged ? (
          <p className="inline-notice" role="status">
            La versión activa cambió desde la consulta anterior. Cada resultado
            conserva la versión indicada; podés consultar la activa cuando lo
            decidas.
          </p>
        ) : null}
        {result?.isHistorical ? (
          <p className="inline-notice">
            Estás consultando una versión histórica. Sus entradas no se
            reemplazan por la versión activa.
          </p>
        ) : null}
      </div>
      <div
        className="catalog-filters"
        role="search"
        aria-label="Filtros del catálogo"
      >
        <div className="form-field catalog-filter-query">
          <label htmlFor={`${id}-query`}>Nombre, código o sinónimo</label>
          <input
            id={`${id}-query`}
            type="search"
            maxLength={256}
            value={query}
            onChange={(event) => {
              setQuery(event.target.value);
              setSelected(null);
            }}
            aria-describedby={`${id}-filter-help`}
            placeholder="Por ejemplo, maíz o nombre científico"
          />
        </div>
        <div className="form-field">
          <label htmlFor={`${id}-jurisdiction`}>Jurisdicción</label>
          <input
            id={`${id}-jurisdiction`}
            maxLength={64}
            value={jurisdiction}
            onChange={(event) => {
              setJurisdiction(event.target.value);
              setSelected(null);
            }}
            placeholder="Todas"
          />
        </div>
        <div className="form-field">
          <label htmlFor={`${id}-category`}>Categoría</label>
          <select
            id={`${id}-category`}
            value={category}
            onChange={(event) => {
              setCategory(event.target.value);
              setSelected(null);
            }}
          >
            <option value="">Todas</option>
            {CATEGORIES.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </select>
        </div>
        <div className="form-field">
          <label htmlFor={`${id}-support`}>Soporte declarado</label>
          <select
            id={`${id}-support`}
            value={supportLevel}
            onChange={(event) => {
              setSupportLevel(event.target.value);
              setSelected(null);
            }}
          >
            <option value="">Todos los niveles</option>
            {Object.entries(SUPPORT_LABELS).map(([value, name]) => (
              <option key={value} value={value}>
                {name}
              </option>
            ))}
          </select>
        </div>
      </div>
      <p id={`${id}-filter-help`} className="catalog-meta">
        Los filtros se aplican automáticamente. La búsqueda tolera tildes y
        consulta los sinónimos publicados.
      </p>
      <p id={`${id}-scroll-help`} className="catalog-meta">
        La tabla tiene desplazamiento propio. Con foco en los resultados, usá
        las flechas para recorrer columnas y filas.
      </p>
      <div
        className="catalog-table-viewport"
        ref={tableRef}
        role="region"
        aria-label="Resultados del catálogo"
        aria-describedby={`${id}-scroll-help`}
        aria-busy={searching}
        tabIndex={0}
      >
        {searching ? (
          <div className="catalog-table-state" role="status">
            Consultando actividades…
          </div>
        ) : search.kind === "error" ? (
          <div className="catalog-table-state" role="alert">
            <p>{search.message}</p>
            <button
              className="button button--secondary"
              type="button"
              onClick={() => setRefresh((value) => value + 1)}
            >
              Reintentar consulta
            </button>
          </div>
        ) : result?.versionId === null ? (
          <div className="catalog-table-state" role="status">
            <h3>Todavía no hay un catálogo publicado</h3>
            <p>
              Una publicación editorial habilitará la consulta. No se muestran
              entradas de staging ni datos inventados.
            </p>
          </div>
        ) : result?.items.length === 0 ? (
          <div className="catalog-table-state" role="status">
            <h3>No encontramos coincidencias</h3>
            <p>Probá otro nombre, sinónimo o combinación de filtros.</p>
          </div>
        ) : result !== null ? (
          <table className="catalog-table">
            <caption>
              Actividades publicadas en {printable(result.versionTag ?? "")}
            </caption>
            <thead>
              <tr>
                <th scope="col">Código</th>
                <th scope="col">Actividad</th>
                <th scope="col">Categoría</th>
                <th scope="col">Jurisdicción</th>
                <th scope="col">Soporte declarado</th>
                <th scope="col">Consulta</th>
              </tr>
            </thead>
            <tbody>
              {result.items.map((item) => (
                <tr key={item.id}>
                  <td>{printable(item.code)}</td>
                  <th scope="row">{printable(item.displayName)}</th>
                  <td>{printable(item.category)}</td>
                  <td>{printable(item.jurisdiction)}</td>
                  <td>
                    <span className="field-chip">
                      {printable(label(item.supportLevel))}
                    </span>
                  </td>
                  <td>
                    <button
                      className="button button--quiet"
                      type="button"
                      aria-label={`Ver detalle de ${printable(item.displayName)}, código ${printable(item.code)}`}
                      onClick={(event) => {
                        selectedTrigger.current = event.currentTarget;
                        setSelected({
                          code: item.code,
                          versionId: item.versionId,
                        });
                      }}
                    >
                      Ver detalle
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
      </div>
      <p className="catalog-meta" role="status">
        {result === null
          ? ""
          : `${result.items.length} de ${result.totalCount} coincidencias.${result.totalCount > result.items.length ? " Se muestran hasta 50; afiná los filtros para encontrar una entrada." : ""}`}
      </p>
      {selected !== null ? (
        <section
          className="catalog-detail"
          aria-label="Detalle de actividad"
          ref={detailRef}
          tabIndex={-1}
          onKeyDown={(event) => {
            if (event.key === "Escape") {
              event.preventDefault();
              closeDetail();
            }
          }}
        >
          {detail.key !== detailKey || detail.kind === "loading" ? (
            <p role="status">Consultando detalle de la versión seleccionada…</p>
          ) : detail.kind === "error" ? (
            <div role="alert">
              <p>{detail.message}</p>
              <button
                type="button"
                className="button button--secondary"
                onClick={() => setDetailRefresh((value) => value + 1)}
              >
                Reintentar detalle
              </button>
            </div>
          ) : (
            <CatalogDetail item={detail.value} />
          )}
          <button
            type="button"
            className="button button--quiet"
            onClick={closeDetail}
          >
            Cerrar detalle
          </button>
        </section>
      ) : null}
    </section>
  );
}
