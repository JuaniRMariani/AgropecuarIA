import type { Metadata } from "next";
import "maplibre-gl/dist/maplibre-gl.css";

import "./globals.css";

export const metadata: Metadata = {
  title: "Cobertura territorial | AgropecuarIA",
  description:
    "Prototipo verificable de cobertura GIS nacional y degradación de proveedores.",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="es-AR">
      <body>{children}</body>
    </html>
  );
}
