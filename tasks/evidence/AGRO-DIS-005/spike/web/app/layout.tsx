import type { Metadata, Viewport } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Custodia de archivos | AgropecuarIA",
  description:
    "Prototipo R0 de carga, cuarentena y recuperación segura de archivos.",
};

export const viewport: Viewport = { themeColor: "#123e2b" };

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="es">
      <body>
        <a className="skip-link" href="#main-content">
          Saltar al contenido principal
        </a>
        {children}
      </body>
    </html>
  );
}
