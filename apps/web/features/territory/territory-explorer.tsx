"use client";

import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
  type FormEvent,
  type KeyboardEvent,
} from "react";

import {
  resolveTerritory,
  searchTerritories,
  TerritoryApiError,
} from "./territory-api";
import type {
  TerritoryLevel,
  TerritoryResolutionResult,
  TerritorySearchResult,
  TerritorySource,
  TerritoryUnit,
} from "./territory-types";

const LEVEL_LABELS = {
  province: "Provincia",
  department: "Departamento",
  municipality: "Gobierno local",
  locality: "Localidad",
} satisfies Record<TerritoryLevel, string>;

const SOURCE_DATE_FORMAT = new Intl.DateTimeFormat("es-AR", {
  dateStyle: "medium",
  timeZone: "UTC",
});

type SearchState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{ kind: "waiting" }>
  | Readonly<{ kind: "loading" }>
  | Readonly<{ kind: "ready"; result: TerritorySearchResult }>
  | Readonly<{ kind: "error"; message: string }>;

type ResolutionState =
  | Readonly<{ kind: "idle" }>
  | Readonly<{ kind: "loading" }>
  | Readonly<{ kind: "ready"; result: TerritoryResolutionResult }>
  | Readonly<{ kind: "error"; message: string }>;

function isAbort(error: unknown): boolean {
  return error instanceof Error && error.name === "AbortError";
}

function errorMessage(error: unknown, context: "search" | "resolve"): string {
  if (!(error instanceof TerritoryApiError)) {
    return "No pudimos consultar la referencia territorial. Revisá tu conexión e intentá de nuevo.";
  }
  if (error.kind === "signed-out") {
    return "Tu sesión venció. Volvé a ingresar para consultar la referencia territorial.";
  }
  if (error.kind === "rate-limited") {
    return "Hay demasiadas consultas en este momento. Esperá unos segundos antes de reintentar.";
  }
  if (error.kind === "validation") {
    return context === "resolve"
      ? "Revisá la latitud y la longitud antes de resolver la ubicación."
      : "Revisá el texto y los filtros de búsqueda.";
  }
  if (error.kind === "unavailable") {
    return "La referencia oficial no está disponible. Podés continuar con la búsqueda manual cuando vuelva el servicio.";
  }
  if (error.kind === "contract") {
    return "La referencia respondió con un formato inesperado. No mostraremos datos dudosos.";
  }
  return "No pudimos consultar la referencia territorial. Revisá tu conexión e intentá de nuevo.";
}

function SourceLine({ source }: Readonly<{ source: TerritorySource }>) {
  return (
    <p className="territory-source">
      Fuente Georef · versión {source.version} · captura{" "}
      {SOURCE_DATE_FORMAT.format(new Date(source.capturedAtUtc))}
    </p>
  );
}

function FreshnessNotice(
  props: Readonly<{
    status: "fresh" | "stale";
    source: TerritorySource;
  }>,
) {
  return (
    <div
      className={"territory-freshness territory-freshness--" + props.status}
      role={props.status === "stale" ? "status" : undefined}
    >
      <span aria-hidden="true">{props.status === "fresh" ? "●" : "◷"}</span>
      <div>
        <strong>
          {props.status === "fresh"
            ? "Referencia vigente"
            : "Última referencia válida"}
        </strong>
        {props.status === "stale" ? (
          <p>
            Mostramos la última referencia válida almacenada y señalamos su
            fecha para que puedas evaluar su vigencia.
          </p>
        ) : null}
        <SourceLine source={props.source} />
      </div>
    </div>
  );
}

function TerritoryUnitCard({
  unit,
  label = "Referencia seleccionada",
}: Readonly<{ unit: TerritoryUnit; label?: string }>) {
  return (
    <article className="territory-unit" aria-label={label}>
      <div className="territory-unit__code">
        <span>Código oficial</span>
        <strong>{unit.officialCode}</strong>
      </div>
      <div className="territory-unit__copy">
        <span>{LEVEL_LABELS[unit.level]}</span>
        <h4>{unit.name}</h4>
        <p>{unit.hierarchyLabel}</p>
      </div>
    </article>
  );
}

function parseCoordinate(
  value: string,
  minimum: number,
  maximum: number,
): number | null {
  const normalized = value.trim().replace(",", ".");
  if (normalized.length === 0) {
    return null;
  }
  const coordinate = Number(normalized);
  return Number.isFinite(coordinate) &&
    coordinate >= minimum &&
    coordinate <= maximum
    ? coordinate
    : null;
}

export function TerritoryExplorer() {
  const listboxId = useId();
  const searchInput = useRef<HTMLInputElement>(null);
  const searchGeneration = useRef(0);
  const resolutionGeneration = useRef(0);
  const resolutionController = useRef<AbortController | null>(null);
  const [query, setQuery] = useState("");
  const [level, setLevel] = useState<TerritoryLevel | "">("");
  const [parentCode, setParentCode] = useState("");
  const [searchState, setSearchState] = useState<SearchState>({ kind: "idle" });
  const [selectedUnit, setSelectedUnit] = useState<TerritoryUnit | null>(null);
  const [listOpen, setListOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);
  const [latitude, setLatitude] = useState("");
  const [longitude, setLongitude] = useState("");
  const [resolutionState, setResolutionState] = useState<ResolutionState>({
    kind: "idle",
  });

  useEffect(() => {
    const generation = ++searchGeneration.current;
    const normalizedQuery = query.trim();
    if (normalizedQuery.length < 2) {
      return;
    }

    const controller = new AbortController();
    const timeout = window.setTimeout(() => {
      setSearchState({ kind: "loading" });
      void searchTerritories(
        {
          query: normalizedQuery,
          level: level === "" ? undefined : level,
          parentCode: parentCode.trim() || undefined,
          limit: 10,
        },
        controller.signal,
      )
        .then((result) => {
          if (generation !== searchGeneration.current) {
            return;
          }
          setSearchState({ kind: "ready", result });
          setActiveIndex(result.items.length === 0 ? -1 : 0);
          setListOpen(
            result.items.length > 0 &&
              searchInput.current === globalThis.document.activeElement,
          );
        })
        .catch((error: unknown) => {
          if (generation !== searchGeneration.current || isAbort(error)) {
            return;
          }
          setSearchState({
            kind: "error",
            message: errorMessage(error, "search"),
          });
          setListOpen(false);
          setActiveIndex(-1);
        });
    }, 250);

    return () => {
      window.clearTimeout(timeout);
      controller.abort();
    };
  }, [level, parentCode, query]);

  useEffect(
    () => () => {
      resolutionController.current?.abort();
    },
    [],
  );

  const chooseUnit = useCallback((unit: TerritoryUnit) => {
    setSelectedUnit(unit);
    setListOpen(false);
  }, []);

  const handleSearchKeyDown = useCallback(
    (event: KeyboardEvent<HTMLInputElement>) => {
      if (
        searchState.kind !== "ready" ||
        searchState.result.items.length === 0
      ) {
        return;
      }
      const lastIndex = searchState.result.items.length - 1;
      if (event.key === "ArrowDown") {
        event.preventDefault();
        setListOpen(true);
        setActiveIndex((current) => (current >= lastIndex ? 0 : current + 1));
        return;
      }
      if (event.key === "ArrowUp") {
        event.preventDefault();
        setListOpen(true);
        setActiveIndex((current) => (current <= 0 ? lastIndex : current - 1));
        return;
      }
      if (event.key === "Home") {
        event.preventDefault();
        setListOpen(true);
        setActiveIndex(0);
        return;
      }
      if (event.key === "End") {
        event.preventDefault();
        setListOpen(true);
        setActiveIndex(lastIndex);
        return;
      }
      if (event.key === "Enter" && listOpen && activeIndex >= 0) {
        const unit = searchState.result.items[activeIndex];
        if (unit !== undefined) {
          event.preventDefault();
          chooseUnit(unit);
        }
        return;
      }
      if (event.key === "Escape") {
        event.preventDefault();
        setListOpen(false);
      }
    },
    [activeIndex, chooseUnit, listOpen, searchState],
  );

  const handleResolve = useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      resolutionController.current?.abort();
      const generation = ++resolutionGeneration.current;
      const parsedLatitude = parseCoordinate(latitude, -55.2, -21.7);
      const parsedLongitude = parseCoordinate(longitude, -73.7, -53.5);
      if (parsedLatitude === null || parsedLongitude === null) {
        setResolutionState({
          kind: "error",
          message:
            "Ingresá coordenadas dentro de la Argentina continental: latitud -55,2 a -21,7 y longitud -73,7 a -53,5.",
        });
        return;
      }

      const controller = new AbortController();
      resolutionController.current = controller;
      setResolutionState({ kind: "loading" });
      void resolveTerritory(
        { latitude: parsedLatitude, longitude: parsedLongitude },
        controller.signal,
      )
        .then((result) => {
          if (generation === resolutionGeneration.current) {
            setResolutionState({ kind: "ready", result });
          }
        })
        .catch((error: unknown) => {
          if (generation !== resolutionGeneration.current || isAbort(error)) {
            return;
          }
          setResolutionState({
            kind: "error",
            message: errorMessage(error, "resolve"),
          });
        });
    },
    [latitude, longitude],
  );

  const activeOptionId =
    listOpen && activeIndex >= 0
      ? listboxId + "-option-" + activeIndex
      : undefined;
  const searchBusy = searchState.kind === "loading";
  const resolutionBusy = resolutionState.kind === "loading";

  return (
    <section className="territory-explorer" aria-labelledby="territory-title">
      <header className="territory-heading">
        <div>
          <p className="territory-kicker">Cobertura argentina</p>
          <h2 id="territory-title">Referencia territorial</h2>
          <p>
            Buscá códigos oficiales de Georef sin confundir una división
            administrativa con un campo o una parcela catastral.
          </p>
        </div>
        <span className="territory-heading__mark" aria-hidden="true">
          AR
        </span>
      </header>

      <div className="territory-grid">
        <div className="territory-search-panel">
          <div className="territory-search-fields">
            <div className="territory-field territory-field--wide">
              <label htmlFor={listboxId + "-search"}>
                Provincia, departamento, gobierno local o localidad
              </label>
              <div className="territory-combobox">
                <input
                  aria-activedescendant={activeOptionId}
                  aria-autocomplete="list"
                  aria-controls={listboxId}
                  aria-expanded={listOpen}
                  autoComplete="off"
                  id={listboxId + "-search"}
                  maxLength={80}
                  onBlur={() => setListOpen(false)}
                  onChange={(event) => {
                    const nextQuery = event.currentTarget.value;
                    setQuery(nextQuery);
                    setSelectedUnit(null);
                    setListOpen(false);
                    setActiveIndex(-1);
                    setSearchState(
                      nextQuery.trim().length < 2
                        ? { kind: "idle" }
                        : { kind: "waiting" },
                    );
                  }}
                  onFocus={() => {
                    if (
                      searchState.kind === "ready" &&
                      searchState.result.items.length > 0
                    ) {
                      setListOpen(true);
                    }
                  }}
                  onKeyDown={handleSearchKeyDown}
                  placeholder="Ej. Córdoba o General Roca"
                  ref={searchInput}
                  role="combobox"
                  spellCheck={false}
                  type="search"
                  value={query}
                />
                {listOpen && searchState.kind === "ready" ? (
                  <ul
                    className="territory-options"
                    id={listboxId}
                    role="listbox"
                  >
                    {searchState.result.items.map((unit, index) => (
                      <li
                        aria-selected={index === activeIndex}
                        className="territory-option"
                        id={listboxId + "-option-" + index}
                        key={unit.level + ":" + unit.officialCode}
                        onMouseDown={(event) => {
                          event.preventDefault();
                          chooseUnit(unit);
                        }}
                        role="option"
                      >
                        <span className="territory-option__code">
                          {unit.officialCode}
                        </span>
                        <span>
                          <strong>{unit.name}</strong>
                          <small>{unit.hierarchyLabel}</small>
                        </span>
                      </li>
                    ))}
                  </ul>
                ) : null}
              </div>
              <small>
                La búsqueda comienza automáticamente con 2 caracteres.
              </small>
            </div>

            <div className="territory-field">
              <label htmlFor={listboxId + "-level"}>Nivel (opcional)</label>
              <select
                id={listboxId + "-level"}
                onChange={(event) => {
                  const value = event.currentTarget.value;
                  if (
                    value === "" ||
                    value === "province" ||
                    value === "department" ||
                    value === "municipality" ||
                    value === "locality"
                  ) {
                    setLevel(value);
                    setSelectedUnit(null);
                    if (query.trim().length >= 2) {
                      setSearchState({ kind: "waiting" });
                      setListOpen(false);
                    }
                  }
                }}
                value={level}
              >
                <option value="">Todos</option>
                <option value="province">Provincia</option>
                <option value="department">Departamento</option>
                <option value="municipality">Gobierno local</option>
                <option value="locality">Localidad</option>
              </select>
            </div>

            <div className="territory-field">
              <label htmlFor={listboxId + "-parent"}>
                Código padre (opcional)
              </label>
              <input
                id={listboxId + "-parent"}
                maxLength={16}
                onChange={(event) => {
                  setParentCode(event.currentTarget.value);
                  setSelectedUnit(null);
                  if (query.trim().length >= 2) {
                    setSearchState({ kind: "waiting" });
                    setListOpen(false);
                  }
                }}
                placeholder="Ej. 06"
                spellCheck={false}
                type="text"
                value={parentCode}
              />
            </div>
          </div>

          <div
            aria-busy={searchBusy}
            aria-live="polite"
            aria-label="Resultados territoriales"
            className="territory-results"
            role="region"
          >
            {searchState.kind === "idle" ? (
              <p>Escribí al menos 2 caracteres para explorar la referencia.</p>
            ) : null}
            {searchState.kind === "waiting" ? (
              <p>Preparando la búsqueda…</p>
            ) : null}
            {searchState.kind === "loading" ? (
              <div className="territory-loading" role="status">
                <span aria-hidden="true" className="territory-spinner" />
                <span>Buscando en la referencia oficial…</span>
              </div>
            ) : null}
            {searchState.kind === "error" ? (
              <div
                className="territory-state territory-state--error"
                role="alert"
              >
                <strong>No pudimos completar la búsqueda</strong>
                <p>{searchState.message}</p>
              </div>
            ) : null}
            {searchState.kind === "ready" ? (
              <>
                <FreshnessNotice
                  source={searchState.result.source}
                  status={searchState.result.status}
                />
                {searchState.result.items.length === 0 ? (
                  <div className="territory-state">
                    <strong>Sin coincidencias</strong>
                    <p>
                      Probá otro nombre o quitá los filtros. No vamos a inventar
                      un código oficial.
                    </p>
                  </div>
                ) : (
                  <p className="territory-result-count">
                    {searchState.result.items.length} referencias encontradas
                  </p>
                )}
              </>
            ) : null}
          </div>

          {selectedUnit !== null ? (
            <TerritoryUnitCard unit={selectedUnit} />
          ) : null}
        </div>

        <form className="territory-resolve" onSubmit={handleResolve}>
          <div className="territory-resolve__heading">
            <span aria-hidden="true">⌖</span>
            <div>
              <h3>Resolver una coordenada</h3>
              <p>
                Ingresala de forma explícita. El resultado ubica una referencia
                administrativa: no demuestra parcela, dominio ni precisión
                agronómica.
              </p>
            </div>
          </div>

          <div className="territory-coordinate-fields">
            <div className="territory-field">
              <label htmlFor={listboxId + "-latitude"}>Latitud</label>
              <input
                id={listboxId + "-latitude"}
                inputMode="decimal"
                onChange={(event) => setLatitude(event.currentTarget.value)}
                placeholder="-34.6037"
                type="text"
                value={latitude}
              />
            </div>
            <div className="territory-field">
              <label htmlFor={listboxId + "-longitude"}>Longitud</label>
              <input
                id={listboxId + "-longitude"}
                inputMode="decimal"
                onChange={(event) => setLongitude(event.currentTarget.value)}
                placeholder="-58.3816"
                type="text"
                value={longitude}
              />
            </div>
          </div>

          <button
            className="button button--secondary territory-resolve__action"
            disabled={resolutionBusy}
            type="submit"
          >
            {resolutionBusy ? (
              <span aria-hidden="true" className="territory-spinner" />
            ) : null}
            {resolutionBusy ? "Resolviendo…" : "Resolver referencia"}
          </button>

          <div
            aria-busy={resolutionBusy}
            aria-live="polite"
            aria-label="Resultado de coordenada"
            className="territory-resolution-result"
            role="region"
          >
            {resolutionState.kind === "idle" ? (
              <p>
                No se persisten en este navegador. Se envían al servidor solo
                para resolver la referencia.
              </p>
            ) : null}
            {resolutionState.kind === "loading" ? (
              <p role="status">Consultando Georef sin bloquear la búsqueda…</p>
            ) : null}
            {resolutionState.kind === "error" ? (
              <div
                className="territory-state territory-state--error"
                role="alert"
              >
                <strong>No pudimos resolver la coordenada</strong>
                <p>{resolutionState.message}</p>
              </div>
            ) : null}
            {resolutionState.kind === "ready" ? (
              <ResolutionResult
                onManualSearch={() => {
                  searchInput.current?.focus();
                  searchInput.current?.scrollIntoView({
                    behavior: "smooth",
                    block: "center",
                  });
                }}
                result={resolutionState.result}
              />
            ) : null}
          </div>
        </form>
      </div>
    </section>
  );
}

function ResolutionResult({
  result,
  onManualSearch,
}: Readonly<{
  result: TerritoryResolutionResult;
  onManualSearch: () => void;
}>) {
  const unavailable = result.status === "unavailable" || result.unit === null;
  if (unavailable) {
    return (
      <div className="territory-state territory-state--warning" role="status">
        <strong>Sin resolución automática</strong>
        <p>
          No adivinamos una jurisdicción desde un centroide. Usá la búsqueda por
          nombre para seleccionar una referencia oficial.
        </p>
        {result.fallback.searchAvailable ? (
          <button
            className="button button--quiet territory-manual-action"
            onClick={onManualSearch}
            type="button"
          >
            Ir a búsqueda manual
          </button>
        ) : null}
      </div>
    );
  }

  return (
    <div className="territory-resolution-success">
      {result.source !== null ? (
        <FreshnessNotice source={result.source} status={result.status} />
      ) : null}
      <TerritoryUnitCard label="Referencia resuelta" unit={result.unit} />
    </div>
  );
}
