"use client";

/**
 * MapPicker — interactive Leaflet map for geo-tagging farms.
 *
 * Features:
 *   - Click-to-place marker (draggable)
 *   - "Use my location" button using the Geolocation API
 *   - Reverse-geocode lookup via Nominatim (OpenStreetMap) to prefill the
 *     address field — the parent form can opt in via `onAddressResolved`
 *   - Controlled lat/lng values
 *
 * No API key required (uses OpenStreetMap tiles + Nominatim). For production
 * volume, swap in Mapbox by changing the tile layer URL and using a token.
 */

import { useEffect, useRef, useState } from "react";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { LocateFixed, MapPin, Search } from "lucide-react";

// Fix default marker icon paths under bundlers
const DefaultIcon = L.icon({
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  iconRetinaUrl:
    "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41],
});
L.Marker.prototype.options.icon = DefaultIcon;

export interface MapPickerProps {
  latitude?: number | null;
  longitude?: number | null;
  onChange: (lat: number, lng: number) => void;
  onAddressResolved?: (address: string) => void;
  height?: number;
  initialZoom?: number;
}

const DEFAULT_CENTER: [number, number] = [22.5937, 78.9629]; // India centroid
const DEFAULT_ZOOM = 5;

export function MapPicker({
  latitude,
  longitude,
  onChange,
  onAddressResolved,
  height = 320,
  initialZoom,
}: MapPickerProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<L.Map | null>(null);
  const markerRef = useRef<L.Marker | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [searching, setSearching] = useState(false);
  const [locating, setLocating] = useState(false);

  // Initialize the map once
  useEffect(() => {
    if (!containerRef.current || mapRef.current) return;

    const hasInitial =
      typeof latitude === "number" && typeof longitude === "number";
    const center: [number, number] = hasInitial
      ? [latitude!, longitude!]
      : DEFAULT_CENTER;
    const zoom = hasInitial ? 13 : initialZoom ?? DEFAULT_ZOOM;

    const map = L.map(containerRef.current, {
      center,
      zoom,
      scrollWheelZoom: true,
      attributionControl: true,
    });
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution:
        '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      maxZoom: 19,
    }).addTo(map);

    if (hasInitial) {
      markerRef.current = L.marker(center, { draggable: true }).addTo(map);
      markerRef.current.on("dragend", () => {
        const ll = markerRef.current!.getLatLng();
        onChange(ll.lat, ll.lng);
      });
    }

    // Click to (re)place marker
    map.on("click", (e: L.LeafletMouseEvent) => {
      const { lat, lng } = e.latlng;
      if (markerRef.current) {
        markerRef.current.setLatLng([lat, lng]);
      } else {
        markerRef.current = L.marker([lat, lng], { draggable: true }).addTo(map);
        markerRef.current.on("dragend", () => {
          const ll = markerRef.current!.getLatLng();
          onChange(ll.lat, ll.lng);
        });
      }
      onChange(lat, lng);
    });

    mapRef.current = map;

    // Force a re-render once the container has been laid out
    setTimeout(() => map.invalidateSize(), 50);

    return () => {
      map.remove();
      mapRef.current = null;
      markerRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Sync external lat/lng changes (e.g. from search/geolocation) to marker
  useEffect(() => {
    if (!mapRef.current) return;
    if (typeof latitude !== "number" || typeof longitude !== "number") return;
    const latlng: [number, number] = [latitude, longitude];
    if (markerRef.current) {
      markerRef.current.setLatLng(latlng);
    } else {
      markerRef.current = L.marker(latlng, { draggable: true }).addTo(
        mapRef.current
      );
      markerRef.current.on("dragend", () => {
        const ll = markerRef.current!.getLatLng();
        onChange(ll.lat, ll.lng);
      });
    }
    mapRef.current.setView(latlng, Math.max(mapRef.current.getZoom(), 13), {
      animate: true,
    });
  }, [latitude, longitude, onChange]);

  // Reverse geocode on marker change (debounced via effect dependency)
  useEffect(() => {
    if (typeof latitude !== "number" || typeof longitude !== "number") return;
    if (!onAddressResolved) return;
    let cancelled = false;
    const t = setTimeout(async () => {
      try {
        const res = await fetch(
          `https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${latitude}&lon=${longitude}&zoom=14`,
          { headers: { "Accept-Language": "en" } }
        );
        if (!res.ok) return;
        const data = await res.json();
        if (cancelled) return;
        if (data && data.display_name) {
          onAddressResolved(data.display_name as string);
        }
      } catch {
        /* ignore */
      }
    }, 800);
    return () => {
      cancelled = true;
      clearTimeout(t);
    };
  }, [latitude, longitude, onAddressResolved]);

  const handleSearch = async () => {
    if (!searchQuery.trim()) return;
    setSearching(true);
    try {
      const res = await fetch(
        `https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&q=${encodeURIComponent(
          searchQuery
        )}`,
        { headers: { "Accept-Language": "en" } }
      );
      const data = (await res.json()) as Array<{
        lat: string;
        lon: string;
        display_name: string;
      }>;
      if (data && data[0]) {
        const lat = parseFloat(data[0].lat);
        const lng = parseFloat(data[0].lon);
        onChange(lat, lng);
        if (onAddressResolved) onAddressResolved(data[0].display_name);
      }
    } catch {
      /* ignore */
    } finally {
      setSearching(false);
    }
  };

  const handleLocate = () => {
    if (!navigator.geolocation) return;
    setLocating(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        onChange(pos.coords.latitude, pos.coords.longitude);
        setLocating(false);
      },
      () => setLocating(false),
      { enableHighAccuracy: true, timeout: 8000 }
    );
  };

  return (
    <div className="space-y-2">
      <div className="flex gap-2">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
          <Input
            placeholder="Search a place, e.g. Vijayawada, Andhra Pradesh"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                handleSearch();
              }
            }}
            className="pl-9 h-9"
          />
        </div>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handleSearch}
          disabled={searching}
          className="h-9"
        >
          {searching ? "…" : "Search"}
        </Button>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handleLocate}
          disabled={locating}
          className="h-9 gap-1.5"
        >
          <LocateFixed className="h-3.5 w-3.5" />
          {locating ? "Locating…" : "My location"}
        </Button>
      </div>

      <div
        ref={containerRef}
        className="w-full rounded-xl overflow-hidden border border-emerald-200 ring-1 ring-emerald-900/5"
        style={{ height }}
      />

      <div className="flex items-center gap-2 text-xs text-muted-foreground">
        <MapPin className="h-3.5 w-3.5 text-emerald-700" />
        {typeof latitude === "number" && typeof longitude === "number" ? (
          <>
            <span className="font-mono">
              {latitude.toFixed(5)}°, {longitude.toFixed(5)}°
            </span>
            <span className="text-muted-foreground/60">
              · click or drag the marker to fine-tune
            </span>
          </>
        ) : (
          <span>Click the map to drop a pin and geo-tag this farm.</span>
        )}
      </div>
    </div>
  );
}
