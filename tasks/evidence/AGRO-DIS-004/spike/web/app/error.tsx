"use client";

export default function GlobalError({
  reset,
}: Readonly<{ reset: () => void }>) {
  return (
    <main className="route-state" role="alert">
      <p className="eyebrow">La evidencia sigue siendo explícita</p>
      <h1>No pudimos abrir el laboratorio territorial.</h1>
      <p>
        El fallo no se interpreta como ausencia de riesgo ni como cobertura
        confirmada.
      </p>
      <button className="primary-button" type="button" onClick={reset}>
        Reintentar
      </button>
    </main>
  );
}
