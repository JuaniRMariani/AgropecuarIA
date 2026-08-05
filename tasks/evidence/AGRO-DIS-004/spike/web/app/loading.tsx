export default function Loading() {
  return (
    <main className="route-state" aria-busy="true" aria-live="polite">
      <p className="eyebrow">Preparando evidencia territorial</p>
      <h1>Cargando cobertura nacional…</h1>
      <div className="loading-line" aria-hidden="true" />
    </main>
  );
}
