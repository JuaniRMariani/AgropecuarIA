import { FileWorkflow } from "@/features/file-workflow/file-workflow";

const checkpoints = [
  ["01", "Validar", "Tipo, tamaño y nombre antes de transferir."],
  ["02", "Aislar", "Cuarentena obligatoria hasta un veredicto confiable."],
  [
    "03",
    "Habilitar",
    "Descarga breve y auditada solo para contenido aprobado.",
  ],
] as const;

export default function HomePage() {
  return (
    <main id="main-content">
      <header className="hero">
        <div className="brand-line">
          <span className="brand-mark" aria-hidden="true">
            A
          </span>
          <span>AgropecuarIA · Laboratorio R0</span>
        </div>
        <div className="hero-grid">
          <div>
            <p className="overline">Custodia digital / ensayo 005</p>
            <h1>
              Un archivo no está disponible hasta demostrar que es seguro.
            </h1>
          </div>
          <p className="hero-copy">
            Recorrido demostrable de carga privada, análisis antivirus y acceso
            temporal. Los fallos se muestran; nunca liberan contenido por
            omisión.
          </p>
        </div>
      </header>

      <section className="checkpoints" aria-label="Controles del recorrido">
        {checkpoints.map(([number, title, description]) => (
          <article key={number}>
            <span>{number}</span>
            <h2>{title}</h2>
            <p>{description}</p>
          </article>
        ))}
      </section>

      <div className="workspace">
        <FileWorkflow />

        <aside className="policy-panel" aria-labelledby="policy-title">
          <p className="overline">Gobierno del dato</p>
          <h2 id="policy-title">Política visible, no inventada</h2>
          <dl>
            <div>
              <dt>Retención</dt>
              <dd>
                <span className="pending-dot" /> Pendiente de política y
                revisión legal
              </dd>
            </div>
            <div>
              <dt>Legal hold</dt>
              <dd>
                <span className="pending-dot" /> Pendiente de política y
                revisión legal
              </dd>
            </div>
            <div>
              <dt>Residencia</dt>
              <dd>Región por definir; proveedor todavía no aprobado</dd>
            </div>
          </dl>
          <div className="rule-note">
            <strong>Regla innegociable</strong>
            <p>Si el antivirus no responde, el objeto permanece aislado.</p>
          </div>
        </aside>
      </div>

      <footer>
        <p>Prototipo descartable. No procesa ni persiste datos reales.</p>
        <p>AGRO-DIS-005 · evidencia reproducible</p>
      </footer>
    </main>
  );
}
