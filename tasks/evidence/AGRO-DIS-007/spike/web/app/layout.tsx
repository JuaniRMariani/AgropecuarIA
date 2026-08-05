import type { Metadata } from "next";
import type { ReactNode } from "react";

import "./globals.css";

export const metadata: Metadata = {
  title: "Laboratorio de capacidad | AgropecuarIA",
  description:
    "Prototipo R0 con escenarios sintéticos para revisar capacidad, SLO, costos y conectividad.",
};

type RootLayoutProps = Readonly<{
  children: ReactNode;
}>;

export default function RootLayout({ children }: RootLayoutProps) {
  return (
    <html lang="es-AR">
      <body>{children}</body>
    </html>
  );
}
