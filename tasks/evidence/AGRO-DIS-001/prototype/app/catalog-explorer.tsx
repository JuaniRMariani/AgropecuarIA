"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import type { CatalogEntry, CatalogMetadata, SupportLevel } from "../lib/catalog-types";
import {
  searchCatalog,
  type DomainFilter,
  type SupportFilter,
} from "../lib/search";
import {
  resolveCatalogViewState,
  type PrototypeScenario,
} from "../lib/view-state";

const RESULT_LIMIT = 48;

const SUPPORT_DETAILS: ReadonlyArray<
  Readonly<{ level: SupportLevel; label: string; detail: string }>
> = [
  {
    level: "CATALOGADA",
    label: "Catalogada",
    detail: "Existe en el denominador nacional. Sin lógica técnica específica.",
  },
  {
    level: "FLUJO_GENERICO",
    label: "Flujo genérico",
    detail: "Puede usar registros comunes cuando una tarea posterior lo habilite.",
  },
  {
    level: "ESPECIALIZADA_VALIDADA",
    label: "Especializada validada",
    detail: "Requiere perfil y revisión profesional explícitos.",
  },
];

const SCENARIOS: ReadonlyArray<
  Readonly<{ value: PrototypeScenario; label: string }>
> = [
  { value: "READY", label: "Normal" },
  { value: "LOADING", label: "Loading" },
  { value: "ERROR", label: "Error" },
  { value: "STALE", label: "Fuente stale" },
];

function readDomainFilter(value: string): DomainFilter {
  if (value === "VEGETAL" || value === "ANIMAL") {
    return value;
  }

  return "ALL";
}

function readSupportFilter(value: string): SupportFilter {
  if (
    value === "CATALOGADA" ||
    value === "FLUJO_GENERICO" ||
    value === "ESPECIALIZADA_VALIDADA"
  ) {
    return value;
  }

  return "ALL";
}

function getEvidenceSummary(entry: CatalogEntry): Readonly<{
  confidence: string;
  missing: string;
}> {
  if (entry.reviewStatus === "REVIEW_REQUIRED" || entry.requiresValidatedProfile) {
    return {
      confidence: "Baja · revisión pendiente",
      missing: "Revisión profesional y perfil jurisdiccional validados.",
    };
  }

  return {
    confidence: "Alta · presencia en baseline",
    missing: "Especialización técnica no evaluada.",
  };
}

function SupportLegend() {
  return (
    <section className="support-ledger" aria-labelledby="support-title">
      <div className="section-heading">
        <p className="eyebrow">Cómo leer el soporte</p>
        <h2 id="support-title">Tres niveles, ninguna promesa implícita</h2>
      </div>
      <div className="support-ledger__grid">
        {SUPPORT_DETAILS.map((item, index) => (
          <article className="support-level" data-support={item.level} key={item.level}>
            <span aria-hidden="true">0{index + 1}</span>
            <div>
              <h3>{item.label}</h3>
              <code>{item.level}</code>
              <p>{item.detail}</p>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}

function CatalogCard({ entry, cutoffDate }: Readonly<{ entry: CatalogEntry; cutoffDate: string }>) {
  const evidence = getEvidenceSummary(entry);
  const domain = entry.domain;

  return (
    <article className="catalog-card" data-domain={domain}>
      <header className="catalog-card__header">
        <div>
          <p className="catalog-card__family">{entry.familyCode.replaceAll("-", " ")}</p>
          <h3>{entry.canonicalName}</h3>
          {entry.scientificName ? <p className="scientific-name">{entry.scientificName}</p> : null}
        </div>
        <span className="domain-stamp">{domain === "VEGETAL" ? "Vegetal" : "Animal"}</span>
      </header>

      <p className="entry-code">{entry.code}</p>
      {entry.aliases.length > 0 ? (
        <p className="aliases">
          <strong>También:</strong> {entry.aliases.join(", ")}
        </p>
      ) : null}

      <dl className="evidence-grid">
        <div>
          <dt>Soporte</dt>
          <dd>
            <span className="support-chip">Catalogada</span>
          </dd>
        </div>
        <div>
          <dt>Confianza</dt>
          <dd>{evidence.confidence}</dd>
        </div>
        <div>
          <dt>Fuente</dt>
          <dd>{entry.sourceIds.join(" · ")}</dd>
        </div>
        <div>
          <dt>Fecha de corte</dt>
          <dd>
            <time dateTime={cutoffDate}>{cutoffDate}</time>
          </dd>
        </div>
        <div className="evidence-grid__wide">
          <dt>Faltantes</dt>
          <dd>{evidence.missing}</dd>
        </div>
      </dl>
    </article>
  );
}

function LoadingResults() {
  return (
    <div className="catalog-state catalog-state--loading" role="status" aria-live="polite">
      <span className="sr-only">Cargando resultados del catálogo.</span>
      <div className="loading-mark" aria-hidden="true">
        <span />
        <span />
        <span />
      </div>
      <h3>Revisando el cuaderno…</h3>
      <p>La consulta conserva sus filtros mientras se prepara la fuente.</p>
    </div>
  );
}

export function CatalogExplorer({
  initialEntries,
  metadata,
}: Readonly<{
  initialEntries: readonly CatalogEntry[];
  metadata: CatalogMetadata;
}>) {
  const [query, setQuery] = useState("");
  const [domain, setDomain] = useState<DomainFilter>("ALL");
  const [support, setSupport] = useState<SupportFilter>("ALL");
  const [scenario, setScenario] = useState<PrototypeScenario>("READY");
  const searchRef = useRef<HTMLInputElement>(null);
  const errorRef = useRef<HTMLDivElement>(null);

  const results = useMemo(
    () => searchCatalog(initialEntries, { query, domain, support }),
    [domain, initialEntries, query, support],
  );
  const viewState = resolveCatalogViewState({ scenario, resultCount: results.length });
  const visibleResults = results.slice(0, RESULT_LIMIT);

  useEffect(() => {
    if (viewState.kind === "error") {
      errorRef.current?.focus();
    }
  }, [viewState.kind]);

  function resetSearch() {
    setQuery("");
    setDomain("ALL");
    setSupport("ALL");
    setScenario("READY");
    requestAnimationFrame(() => searchRef.current?.focus());
  }

  return (
    <main>
      <header className="masthead">
        <a className="skip-link" href="#catalog-results">
          Saltar a resultados
        </a>
        <div className="masthead__topline">
          <p className="brand">Agropecuar<span>IA</span></p>
          <p className="edition">Edición R0 · {metadata.catalogVersion}</p>
        </div>
        <div className="masthead__content">
          <div>
            <p className="eyebrow">Catálogo Nacional v1</p>
            <h1>
              El mapa productivo,
              <br /> leído con evidencia.
            </h1>
          </div>
          <div className="masthead__brief">
            <p>
              Explorá especies y grupos productivos de la línea base candidata. Encontrar una
              entrada no implica recomendación, habilitación ni especialización técnica.
            </p>
            <dl>
              <div>
                <dt>Entradas</dt>
                <dd>{initialEntries.length}</dd>
              </div>
              <div>
                <dt>Corte</dt>
                <dd>{metadata.cutoffDate}</dd>
              </div>
              <div>
                <dt>Estado</dt>
                <dd>Candidata</dd>
              </div>
            </dl>
          </div>
        </div>
      </header>

      <SupportLegend />

      <section className="catalog-workbench" aria-labelledby="search-title">
        <div className="section-heading section-heading--search">
          <div>
            <p className="eyebrow">Mesa de consulta</p>
            <h2 id="search-title">Buscar una producción</h2>
          </div>
          <p>Por código, nombre o alias · sin distinguir tildes ni mayúsculas</p>
        </div>

        <form className="search-panel" role="search" onSubmit={(event) => event.preventDefault()}>
          <div className="search-field">
            <label htmlFor="catalog-query">¿Qué necesitás encontrar?</label>
            <div className="search-field__input">
              <span aria-hidden="true">⌕</span>
              <input
                ref={searchRef}
                id="catalog-query"
                type="search"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Ej.: maíz, ponedoras o AR-ANI-BOVINOS"
                autoComplete="off"
              />
            </div>
          </div>

          <div className="filter-row">
            <label>
              Dominio
              <select
                value={domain}
                onChange={(event) => setDomain(readDomainFilter(event.target.value))}
              >
                <option value="ALL">Todos</option>
                <option value="VEGETAL">Vegetal</option>
                <option value="ANIMAL">Animal</option>
              </select>
            </label>
            <label>
              Nivel de soporte
              <select
                value={support}
                onChange={(event) => setSupport(readSupportFilter(event.target.value))}
              >
                <option value="ALL">Todos</option>
                <option value="CATALOGADA">Catalogada</option>
                <option value="FLUJO_GENERICO">Flujo genérico</option>
                <option value="ESPECIALIZADA_VALIDADA">Especializada validada</option>
              </select>
            </label>
            <button className="text-button" type="button" onClick={resetSearch}>
              Limpiar consulta
            </button>
          </div>
        </form>

        <fieldset className="scenario-switcher">
          <legend>Escenario de fuente</legend>
          <div>
            {SCENARIOS.map((item) => (
              <label key={item.value}>
                <input
                  type="radio"
                  name="source-scenario"
                  value={item.value}
                  checked={scenario === item.value}
                  onChange={() => setScenario(item.value)}
                />
                <span>{item.label}</span>
              </label>
            ))}
          </div>
          <p>Controles de demostración: no alteran ni persisten el catálogo.</p>
        </fieldset>

        {(viewState.kind === "ready" || viewState.kind === "empty") && viewState.stale ? (
          <aside className="stale-notice" role="status">
            <div aria-hidden="true">!</div>
            <p>
              <strong>Fuente desactualizada.</strong> Corte {metadata.cutoffDate}. Revalidar antes de
              publicar o elevar soporte; los resultados se muestran solo como referencia.
            </p>
          </aside>
        ) : null}

        <section id="catalog-results" className="results" aria-labelledby="results-title">
          <div className="results__heading">
            <h2 id="results-title">Resultados</h2>
            <p aria-live="polite" aria-atomic="true">
              {results.length} {results.length === 1 ? "coincidencia" : "coincidencias"}
            </p>
          </div>

          {viewState.kind === "loading" ? <LoadingResults /> : null}

          {viewState.kind === "error" ? (
            <div
              ref={errorRef}
              className="catalog-state catalog-state--error"
              role="alert"
              tabIndex={-1}
            >
              <p className="state-mark" aria-hidden="true">×</p>
              <h3>No pudimos abrir la fuente</h3>
              <p>{viewState.message}</p>
              <button className="primary-button" type="button" onClick={() => setScenario("READY")}>
                Reintentar
              </button>
            </div>
          ) : null}

          {viewState.kind === "empty" ? (
            <div className="catalog-state catalog-state--empty">
              <p className="state-mark" aria-hidden="true">∅</p>
              <h3>No hay coincidencias en este recorte</h3>
              <p>Probá otro nombre, un alias o quitá algún filtro. El catálogo no inventa entradas.</p>
              <button className="primary-button" type="button" onClick={resetSearch}>
                Ver todo el catálogo
              </button>
            </div>
          ) : null}

          {viewState.kind === "ready" ? (
            <>
              <div className="catalog-grid">
                {visibleResults.map((entry) => (
                  <CatalogCard entry={entry} cutoffDate={metadata.cutoffDate} key={entry.code} />
                ))}
              </div>
              {results.length > RESULT_LIMIT ? (
                <p className="result-limit" role="status">
                  Se muestran {RESULT_LIMIT} de {results.length} coincidencias. Refiná la búsqueda
                  para revisar el resto.
                </p>
              ) : null}
            </>
          ) : null}
        </section>
      </section>

      <aside className="assumption-sheet" aria-labelledby="assumptions-title">
        <div>
          <p className="eyebrow">Transparencia</p>
          <h2 id="assumptions-title">Supuestos y límites del corte</h2>
        </div>
        <ul>
          {metadata.assumptions.map((assumption) => (
            <li key={assumption}>{assumption}</li>
          ))}
          <li>
            La confianza mostrada refiere a trazabilidad dentro del baseline, no a aptitud
            agronómica, sanitaria o regulatoria.
          </li>
        </ul>
      </aside>

      <footer>
        <p>Prototipo descartable de validación · sin API, autenticación ni persistencia</p>
        <p>AgropecuarIA · Catálogo Nacional v1</p>
      </footer>
    </main>
  );
}
