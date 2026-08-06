import type { NextConfig } from "next";

const apiOrigin =
  process.env.AGRO_API_ORIGIN ??
  (process.env.NODE_ENV === "development" ? "http://127.0.0.1:5080" : null);

const contentSecurityPolicy = [
  "default-src 'self'",
  "base-uri 'self'",
  "frame-ancestors 'none'",
  "form-action 'self'",
  "object-src 'none'",
  "img-src 'self' data:",
  "style-src 'self' 'unsafe-inline'",
  `script-src 'self' 'unsafe-inline'${process.env.NODE_ENV === "development" ? " 'unsafe-eval'" : ""}`,
  `connect-src 'self'${process.env.NODE_ENV === "development" ? " ws: wss:" : ""}`,
].join("; ");

const nextConfig: NextConfig = {
  async rewrites() {
    return apiOrigin === null
      ? []
      : [{ source: "/api/:path*", destination: `${apiOrigin}/api/:path*` }];
  },
  async headers() {
    return [
      {
        source: "/:path*",
        headers: [
          { key: "Content-Security-Policy", value: contentSecurityPolicy },
          { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "X-Frame-Options", value: "DENY" },
          {
            key: "Permissions-Policy",
            value: "camera=(), geolocation=(), microphone=()",
          },
        ],
      },
    ];
  },
  poweredByHeader: false,
};

export default nextConfig;
