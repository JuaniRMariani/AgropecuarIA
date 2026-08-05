import type { Metadata } from "next";
import type { ReactNode } from "react";

import "./globals.css";

export const metadata: Metadata = {
  title: "Catálogo Nacional v1 · AgropecuarIA",
  description: "Prototipo R0 de búsqueda y lectura de soporte del Catálogo Nacional v1.",
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="es-AR">
      <body>{children}</body>
    </html>
  );
}
