"use client";

import { useEffect } from "react";

export default function ErrorBoundary({
  error,
  retry,
}: Readonly<{
  error: Error & { digest?: string };
  retry: () => void;
}>) {
  useEffect(() => {
    document.querySelector<HTMLElement>("[data-error-heading]")?.focus();
  }, []);

  return (
    <main className="route-state route-state--error">
      <p className="eyebrow">Fuente no disponible</p>
      <h1 data-error-heading tabIndex={-1}>
        El catálogo no pudo abrirse.
      </h1>
      <p>
        El prototipo falló de forma segura. No se muestran datos parciales ni se eleva el nivel de
        soporte.
      </p>
      <button className="primary-button" type="button" onClick={retry}>
        Reintentar lectura
      </button>
      {error.digest ? <p className="technical-note">Referencia: {error.digest}</p> : null}
    </main>
  );
}
