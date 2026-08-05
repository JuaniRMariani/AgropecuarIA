export default function Loading() {
  return (
    <main className="route-state" aria-busy="true">
      <p className="eyebrow">Catálogo Nacional v1</p>
      <h1>Preparando el cuaderno de campo…</h1>
      <div className="route-state__line" aria-hidden="true" />
      <span className="sr-only" role="status">
        Cargando catálogo.
      </span>
    </main>
  );
}
