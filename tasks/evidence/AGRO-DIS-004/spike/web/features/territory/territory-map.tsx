"use client";

import { useEffect, useRef, useState } from "react";
import {
  LngLatBounds,
  Map,
  Marker,
  NavigationControl,
  type LngLatBoundsLike,
  type StyleSpecification,
} from "maplibre-gl";

import type { TerritoryPoint } from "@/lib/territory";

const ARGENTINA_BOUNDS: LngLatBoundsLike = [
  [-74.5, -85],
  [-52, -20],
];

const ARGENMAP_STYLE: StyleSpecification = {
  version: 8,
  sources: {
    argenmap: {
      type: "raster",
      tiles: [
        "https://wms.ign.gob.ar/geoserver/gwc/service/tms/1.0.0/capabaseargenmap@EPSG%3A3857@png/{z}/{x}/{y}.png",
      ],
      tileSize: 256,
      scheme: "tms",
      minzoom: 2,
      maxzoom: 18,
      attribution:
        "Instituto Geográfico Nacional y colaboradores de OpenStreetMap",
    },
  },
  layers: [{ id: "argenmap", type: "raster", source: "argenmap" }],
};

export function TerritoryMap({
  points,
  onProviderError,
}: Readonly<{
  points: readonly TerritoryPoint[];
  onProviderError: () => void;
}>) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    if (!containerRef.current) return;

    const map = new Map({
      container: containerRef.current,
      style: ARGENMAP_STYLE,
      center: [-64, -45],
      zoom: 2.4,
      minZoom: 2,
      maxZoom: 18,
      maxBounds: ARGENTINA_BOUNDS,
      attributionControl: false,
      cooperativeGestures: true,
    });

    map.addControl(new NavigationControl({ showCompass: false }), "top-right");

    const bounds = new LngLatBounds();
    for (const point of points) {
      const marker = document.createElement("span");
      marker.className = `territory-marker territory-marker--${point.evidence}`;
      marker.setAttribute("aria-hidden", "true");
      marker.title = `${point.id} · ${point.name}`;

      new Marker({ element: marker })
        .setLngLat([point.longitude, point.latitude])
        .addTo(map);
      marker.tabIndex = -1;
      bounds.extend([point.longitude, point.latitude]);
    }

    const handleLoad = () => {
      map.fitBounds(bounds, { padding: 46, duration: 0, maxZoom: 4 });
      setIsReady(true);
    };
    const handleError = () => onProviderError();

    map.once("load", handleLoad);
    map.once("error", handleError);

    return () => {
      map.remove();
    };
  }, [onProviderError, points]);

  return (
    <div className="map-frame">
      <div
        ref={containerRef}
        className="map-canvas"
        role="region"
        aria-label="Mapa de referencia con centroides de 23 provincias y la Ciudad Autónoma de Buenos Aires. La tabla siguiente ofrece la misma información."
      />
      {isReady ? null : (
        <div className="map-loading" role="status">
          <span className="pulse-dot" aria-hidden="true" />
          Conectando con Argenmap…
        </div>
      )}
      <p className="map-attribution">
        Mapa base:{" "}
        <a
          href="https://www.ign.gob.ar/AreaServicios/Argenmap/Introduccion"
          target="_blank"
          rel="noreferrer"
        >
          Instituto Geográfico Nacional · Argenmap
        </a>{" "}
        +{" "}
        <a
          href="https://www.openstreetmap.org/copyright"
          target="_blank"
          rel="noreferrer"
        >
          colaboradores de OpenStreetMap
        </a>
      </p>
    </div>
  );
}
