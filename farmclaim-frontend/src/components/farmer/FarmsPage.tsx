"use client";

import { useEffect, useState } from "react";
import dynamic from "next/dynamic";
import { useApp } from "@/lib/store";
import { farmsApi } from "@/lib/api";
import type { FarmResponseDto, CreateFarmRequestDto } from "@/lib/types";
import { PageHeader } from "@/components/layout/DashboardShell";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import {
  Plus,
  MapPin,
  AreaChart,
  FileText,
  ClipboardList,
  Edit2,
  Trash2,
  Sprout,
  Loader2,
} from "lucide-react";
import { formatDate, formatINR } from "@/lib/utils";
import { toast } from "sonner";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";

// Leaflet must not run during SSR — load the picker client-only.
const MapPicker = dynamic(
  () => import("@/components/shared/MapPicker").then((m) => m.MapPicker),
  { ssr: false, loading: () => (
    <div className="h-80 rounded-xl bg-muted animate-pulse grid place-items-center text-sm text-muted-foreground">
      Loading map…
    </div>
  ) }
);

export function FarmsPage() {
  const navigate = useApp((s) => s.navigate);
  const route = useApp((s) => s.route);
  const [farms, setFarms] = useState<FarmResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<FarmResponseDto | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [deleting, setDeleting] = useState<FarmResponseDto | null>(null);

  const load = () => {
    setLoading(true);
    farmsApi.list().then(setFarms).finally(() => setLoading(false));
  };
  useEffect(() => {
    farmsApi.list().then(setFarms).finally(() => setLoading(false));
  }, []);

  const detailId = route.params.id;
  if (detailId) {
    return <FarmDetail id={detailId} />;
  }

  return (
    <div>
      <PageHeader
        title="My Farms"
        subtitle="Manage your registered farm plots and their details."
        actions={
          <Dialog open={createOpen} onOpenChange={setCreateOpen}>
            <DialogTrigger asChild>
              <Button className="bg-emerald-700 hover:bg-emerald-800 text-white gap-1.5">
                <Plus className="h-4 w-4" />
                Add farm
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl">
              <DialogHeader>
                <DialogTitle>Register a new farm</DialogTitle>
              </DialogHeader>
              <FarmForm
                onSaved={() => {
                  setCreateOpen(false);
                  load();
                }}
              />
            </DialogContent>
          </Dialog>
        }
      />

      {loading ? (
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-64 rounded-xl" />
          ))}
        </div>
      ) : farms.length === 0 ? (
        <Card>
          <CardContent className="py-16 text-center">
            <div className="h-14 w-14 rounded-full bg-emerald-100 mx-auto grid place-items-center mb-4">
              <Sprout className="h-7 w-7 text-emerald-700" />
            </div>
            <h3 className="font-serif text-xl font-semibold">No farms registered yet</h3>
            <p className="text-muted-foreground mt-1 max-w-sm mx-auto">
              Add your first farm plot to start buying crop insurance and filing claims.
            </p>
            <Button
              onClick={() => setCreateOpen(true)}
              className="mt-5 bg-emerald-700 hover:bg-emerald-800 text-white gap-1.5"
            >
              <Plus className="h-4 w-4" /> Register your first farm
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {farms.map((farm) => (
            <Card key={farm.id} className="overflow-hidden hover:shadow-lg transition-shadow group">
              <div className="relative h-32 bg-emerald-100 overflow-hidden">
                <img
                  src={`https://images.unsplash.com/photo-1500382017468-9049fed747ef?w=600&q=80&sat=-30`}
                  alt="Farm"
                  className="w-full h-full object-cover transition-transform group-hover:scale-105"
                />
                <div className="absolute inset-0 bg-gradient-to-t from-emerald-950/70 to-transparent" />
                <div className="absolute bottom-3 left-3 right-3 text-white">
                  <div className="flex items-center gap-1.5 text-xs opacity-90">
                    <MapPin className="h-3 w-3" />
                    {farm.address ?? "No address"}
                  </div>
                  <div className="font-serif text-lg font-semibold mt-0.5">
                    {farm.name}
                  </div>
                </div>
                {!farm.isActive && (
                  <div className="absolute top-3 right-3 bg-stone-700 text-white text-[10px] uppercase tracking-wide px-2 py-0.5 rounded-full">
                    Inactive
                  </div>
                )}
              </div>
              <CardContent className="p-5">
                <div className="grid grid-cols-3 gap-2 text-center">
                  <div>
                    <div className="text-lg font-bold text-emerald-700">
                      {farm.areaInHectares}
                    </div>
                    <div className="text-[10px] uppercase tracking-wide text-muted-foreground">
                      Hectares
                    </div>
                  </div>
                  <div>
                    <div className="text-lg font-bold text-amber-700">
                      {farm.policiesCount}
                    </div>
                    <div className="text-[10px] uppercase tracking-wide text-muted-foreground">
                      Policies
                    </div>
                  </div>
                  <div>
                    <div className="text-lg font-bold text-blue-700">
                      {farm.claimsCount}
                    </div>
                    <div className="text-[10px] uppercase tracking-wide text-muted-foreground">
                      Claims
                    </div>
                  </div>
                </div>
                {typeof farm.latitude === "number" && typeof farm.longitude === "number" && (
                  <a
                    href={`https://www.openstreetmap.org/?mlat=${farm.latitude}&mlon=${farm.longitude}#map=15/${farm.latitude}/${farm.longitude}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="mt-3 flex items-center justify-center gap-1 text-xs text-emerald-700 hover:underline"
                    onClick={(e) => e.stopPropagation()}
                  >
                    <MapPin className="h-3 w-3" />
                    <span className="font-mono">
                      {farm.latitude.toFixed(4)}°, {farm.longitude.toFixed(4)}°
                    </span>
                  </a>
                )}
                <div className="text-xs text-muted-foreground mt-3 text-center">
                  Registered {formatDate(farm.createdAt)}
                </div>
                <div className="flex gap-2 mt-4">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setEditing(farm)}
                    className="flex-1 gap-1.5"
                  >
                    <Edit2 className="h-3.5 w-3.5" /> Edit
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setDeleting(farm)}
                    className="text-rose-600 hover:text-rose-700 hover:bg-rose-50"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Edit dialog */}
      <Dialog open={!!editing} onOpenChange={(o) => !o && setEditing(null)}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Edit farm</DialogTitle>
          </DialogHeader>
          {editing && (
            <FarmForm
              initial={editing}
              onSaved={() => {
                setEditing(null);
                load();
              }}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Delete confirm */}
      <AlertDialog open={!!deleting} onOpenChange={(o) => !o && setDeleting(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this farm?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete &ldquo;{deleting?.name}&rdquo; and remove it from all
              future policy and claim options. This cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={async () => {
                if (!deleting) return;
                await farmsApi.delete(deleting.id);
                toast.success("Farm deleted");
                setDeleting(null);
                load();
              }}
              className="bg-rose-600 hover:bg-rose-700"
            >
              Delete farm
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

function FarmForm({
  initial,
  onSaved,
}: {
  initial?: FarmResponseDto;
  onSaved: () => void;
}) {
  const [name, setName] = useState(initial?.name ?? "");
  const [area, setArea] = useState(initial?.areaInHectares?.toString() ?? "1");
  const [address, setAddress] = useState(initial?.address ?? "");
  const [lat, setLat] = useState<number | null>(
    typeof initial?.latitude === "number" ? initial.latitude : null
  );
  const [lng, setLng] = useState<number | null>(
    typeof initial?.longitude === "number" ? initial.longitude : null
  );
  const [saving, setSaving] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const dto: CreateFarmRequestDto = {
        name,
        areaInHectares: parseFloat(area) || 1,
        address: address || null,
        latitude: lat,
        longitude: lng,
      };
      if (initial) {
        await farmsApi.update(initial.id, dto);
        toast.success("Farm updated");
      } else {
        await farmsApi.create(dto);
        toast.success("Farm registered");
      }
      onSaved();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Save failed");
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={onSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="name">Farm name</Label>
        <Input
          id="name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          maxLength={200}
          placeholder="Greenfield Acres"
        />
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-2">
          <Label htmlFor="area">Area (hectares)</Label>
          <Input
            id="area"
            type="number"
            step="0.01"
            min="0.01"
            value={area}
            onChange={(e) => setArea(e.target.value)}
            required
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="address">Address (optional)</Label>
          <Input
            id="address"
            value={address}
            onChange={(e) => setAddress(e.target.value)}
            maxLength={500}
            placeholder="Plot 14, Krishna District, AP"
          />
        </div>
      </div>

      <div className="space-y-2">
        <Label className="flex items-center gap-1.5">
          <MapPin className="h-3.5 w-3.5 text-emerald-700" />
          Geo-tag this farm on the map
        </Label>
        <p className="text-xs text-muted-foreground -mt-1">
          Click the map to drop a pin, search a place, or use your location.
          Co-ordinates power satellite-based damage detection during claims.
        </p>
        <MapPicker
          latitude={lat}
          longitude={lng}
          onChange={(lla, lnga) => {
            setLat(lla);
            setLng(lnga);
          }}
          onAddressResolved={(a) => {
            // Only auto-fill address if user hasn't typed one yet
            if (!address.trim()) setAddress(a);
          }}
        />
      </div>

      <DialogFooter>
        <Button
          type="submit"
          disabled={saving}
          className="bg-emerald-700 hover:bg-emerald-800 text-white"
        >
          {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : initial ? "Save changes" : "Register farm"}
        </Button>
      </DialogFooter>
    </form>
  );
}

function FarmDetail({ id }: { id: string }) {
  const navigate = useApp((s) => s.navigate);
  const [farm, setFarm] = useState<FarmResponseDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    farmsApi.get(id).then(setFarm).finally(() => setLoading(false));
  }, [id]);

  if (loading) return <Skeleton className="h-96 rounded-xl" />;
  if (!farm) return <div>Farm not found.</div>;

  return (
    <div className="max-w-4xl mx-auto">
      <Button
        variant="ghost"
        size="sm"
        onClick={() => navigate("/dashboard/farms")}
        className="mb-4"
      >
        ← Back to farms
      </Button>
      <PageHeader
        title={farm.name ?? "Farm details"}
        subtitle={farm.address ?? "No address"}
        actions={
          <div className="flex gap-2">
            {!farm.isActive && (
              <Badge variant="secondary" className="bg-stone-100 text-stone-700">Inactive</Badge>
            )}
          </div>
        }
      />

      <div className="grid lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          {typeof farm.latitude === "number" && typeof farm.longitude === "number" && (
            <Card>
              <CardContent className="p-6">
                <h3 className="font-serif text-lg font-semibold mb-4">Location</h3>
                <div className="aspect-video rounded-lg overflow-hidden bg-muted">
                  <iframe
                    title="Farm location"
                    width="100%"
                    height="100%"
                    frameBorder="0"
                    src={`https://www.openstreetmap.org/export/embed.html?bbox=${farm.longitude - 0.01}%2C${farm.latitude - 0.01}%2C${farm.longitude + 0.01}%2C${farm.latitude + 0.01}&layer=mapnik&marker=${farm.latitude}%2C${farm.longitude}`}
                    className="w-full h-full"
                  />
                </div>
                <div className="flex items-center gap-2 mt-3 text-sm text-muted-foreground">
                  <MapPin className="h-4 w-4 text-emerald-700" />
                  <span className="font-mono">{farm.latitude.toFixed(5)}°, {farm.longitude.toFixed(5)}°</span>
                  <a
                    href={`https://www.openstreetmap.org/?mlat=${farm.latitude}&mlon=${farm.longitude}#map=15/${farm.latitude}/${farm.longitude}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-emerald-700 hover:underline ml-auto text-xs"
                  >
                    Open in OSM →
                  </a>
                </div>
              </CardContent>
            </Card>
          )}

          <Card>
            <CardContent className="p-6 space-y-4">
              <h3 className="font-serif text-lg font-semibold">Farm details</h3>
              <div className="grid sm:grid-cols-2 gap-4 text-sm">
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Name</div>
                  <div className="font-medium mt-1">{farm.name}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Area</div>
                  <div className="font-medium mt-1">{farm.areaInHectares} hectares</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Address</div>
                  <div className="font-medium mt-1">{farm.address ?? "—"}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Registered</div>
                  <div className="font-medium mt-1">{formatDate(farm.createdAt)}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Policies</div>
                  <div className="font-medium mt-1">{farm.policiesCount}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Claims</div>
                  <div className="font-medium mt-1">{farm.claimsCount}</div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        <div className="space-y-4">
          <Card>
            <CardContent className="p-6">
              <h3 className="font-serif text-lg font-semibold mb-4">Quick actions</h3>
              <div className="space-y-2">
                <Button
                  variant="outline"
                  className="w-full justify-start gap-2"
                  onClick={() => navigate("/dashboard/policies/new")}
                >
                  <FileText className="h-4 w-4" /> Buy policy for this farm
                </Button>
                <Button
                  variant="outline"
                  className="w-full justify-start gap-2"
                  onClick={() => navigate("/dashboard/claims/new")}
                >
                  <ClipboardList className="h-4 w-4" /> File a claim
                </Button>
              </div>
            </CardContent>
          </Card>
          <Card className="bg-emerald-50 border-emerald-200">
            <CardContent className="p-6 text-center">
              <div className="text-xs uppercase tracking-wide text-emerald-700">Total area</div>
              <div className="font-serif text-3xl font-bold text-emerald-700 mt-1">
                {farm.areaInHectares} ha
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
