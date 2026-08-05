import { TerritoryExplorer } from "@/features/territory/territory-explorer";
import { territoryFixture } from "@/lib/territory";

export default function HomePage() {
  return (
    <main>
      <header className="masthead">
        <a className="skip-link" href="#cobertura">
          Saltar a la cobertura nacional
        </a>
        <div className="eyebrow-row">
          <p className="eyebrow">AgropecuarIA · Laboratorio territorial</p>
          <p className="release-mark">R0 / DIS-004</p>
        </div>
        <div className="hero-grid">
          <div>
            <p className="kicker">Argentina, medida antes de prometer</p>
            <h1>
              Cobertura territorial que se explica aun cuando el mapa calla.
            </h1>
          </div>
          <div className="hero-note">
            <span className="edition-number" aria-hidden="true">
              24
            </span>
            <p>
              jurisdicciones públicas para probar cobertura, evidencia y
              degradación sin exponer coordenadas productivas.
            </p>
          </div>
        </div>
      </header>

      <TerritoryExplorer fixture={territoryFixture} />

      <footer className="site-footer">
        <p>
          Prototipo R0 · no constituye una recomendación agronómica ni
          pronóstico operativo.
        </p>
        <p>
          Fixture {territoryFixture.fixtureVersion} · centroides administrativos
          públicos.
        </p>
      </footer>
    </main>
  );
}
