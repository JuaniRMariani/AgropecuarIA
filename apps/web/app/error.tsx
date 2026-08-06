"use client";

export default function ErrorPage({ reset }: Readonly<{ reset: () => void }>) {
  return (
    <main className="fatal-state">
      <div className="fatal-state__card" role="alert">
        <p className="eyebrow">Acceso seguro</p>
        <h1>No pudimos abrir esta pantalla</h1>
        <p>Tu información no se modificó. Podés intentar nuevamente.</p>
        <button
          className="button button--primary"
          onClick={reset}
          type="button"
        >
          Reintentar
        </button>
      </div>
    </main>
  );
}
