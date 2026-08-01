"use client";

import { useEffect, useState, useRef } from "react";
import { useApp } from "@/lib/store";
import { claimsApi, farmsApi, policiesApi } from "@/lib/api";
import type {
  ClaimResponseDto,
  ClaimTimelineEntryDto,
  FarmResponseDto,
  IncidentType,
  PolicyResponseDto,
} from "@/lib/types";
import { PageHeader } from "@/components/layout/DashboardShell";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { StatusBadge, IncidentBadge } from "@/components/shared/badges";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
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
import {
  Plus,
  ClipboardList,
  Clock,
  IndianRupee,
  Loader2,
  ArrowLeft,
  X,
  Camera,
  Sparkles,
  Brain,
  CloudRain,
  Satellite,
  Edit2,
  Trash2,
  AlertTriangle,
  Thermometer,
  Wind,
  Droplets,
  Sun,
  Cloud,
  CloudSnow,
  CloudLightning,
  CloudDrizzle,
  MapPin,
  Upload,
} from "lucide-react";
import { formatDate, formatINR, formatRelative, cn } from "@/lib/utils";
import { toast } from "sonner";
import { motion } from "framer-motion";

const incidentTypes: { value: IncidentType; label: string; emoji: string }[] = [
  { value: "Flood", label: "Flood", emoji: "🌊" },
  { value: "Drought", label: "Drought", emoji: "🏜️" },
  { value: "HeavyRain", label: "Heavy Rain", emoji: "🌧️" },
  { value: "Hail", label: "Hail", emoji: "❄️" },
  { value: "Frost", label: "Frost", emoji: "🥶" },
  { value: "PestInfestation", label: "Pest Infestation", emoji: "🐛" },
  { value: "Fire", label: "Fire", emoji: "🔥" },
  { value: "Windstorm", label: "Windstorm", emoji: "💨" },
  { value: "Other", label: "Other", emoji: "⚠️" },
];

export function ClaimsPage() {
  const navigate = useApp((s) => s.navigate);
  const route = useApp((s) => s.route);
  const [claims, setClaims] = useState<ClaimResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<string>("All");

  const load = () => {
    setLoading(true);
    claimsApi.list().then(setClaims).finally(() => setLoading(false));
  };
  useEffect(load, []);

  // New claim form
  if (route.path === "/dashboard/claims/new") {
    return <NewClaimForm onSaved={load} />;
  }
  // Detail view
  const detailId = route.params.id;
  if (detailId) {
    return <ClaimDetail id={detailId} />;
  }

  const filtered = filter === "All" ? claims : claims.filter((c) => c.status === filter);

  return (
    <div>
      <PageHeader
        title="My Claims"
        subtitle="Track every claim from submission to payout."
        actions={
          <Button
            onClick={() => navigate("/dashboard/claims/new")}
            className="bg-emerald-700 hover:bg-emerald-800 text-white gap-1.5"
          >
            <Plus className="h-4 w-4" /> File new claim
          </Button>
        }
      />

      {/* Filter tabs */}
      <div className="flex gap-1.5 mb-5 overflow-x-auto pb-1">
        {["All", "Pending", "UnderReview", "Approved", "Rejected", "Paid"].map((s) => (
          <button
            key={s}
            onClick={() => setFilter(s)}
            className={cn(
              "px-3.5 py-1.5 rounded-full text-sm font-medium whitespace-nowrap transition-colors",
              filter === s
                ? "bg-emerald-700 text-white"
                : "bg-card text-muted-foreground hover:bg-foreground/5"
            )}
          >
            {s === "UnderReview" ? "Under Review" : s}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-24 rounded-xl" />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <Card>
          <CardContent className="py-16 text-center">
            <div className="h-14 w-14 rounded-full bg-emerald-100 mx-auto grid place-items-center mb-4">
              <ClipboardList className="h-7 w-7 text-emerald-700" />
            </div>
            <h3 className="font-serif text-xl font-semibold">No claims yet</h3>
            <p className="text-muted-foreground mt-1">
              When disaster strikes, file your first claim in minutes.
            </p>
            <Button
              onClick={() => navigate("/dashboard/claims/new")}
              className="mt-5 bg-emerald-700 hover:bg-emerald-800 text-white gap-1.5"
            >
              <Plus className="h-4 w-4" /> File your first claim
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {filtered.map((c) => (
            <Card
              key={c.id}
              className="hover:shadow-md transition-shadow cursor-pointer"
              onClick={() => navigate(`/dashboard/claims/${c.id}`)}
            >
              <CardContent className="p-5 flex items-center gap-4">
                <div className="h-12 w-12 rounded-xl bg-emerald-100 grid place-items-center shrink-0">
                  <span className="text-2xl">
                    {incidentTypes.find((t) => t.value === c.incidentType)?.emoji}
                  </span>
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="font-semibold">{c.farmName}</span>
                    <IncidentBadge type={c.incidentType} />
                    <StatusBadge status={c.status} />
                  </div>
                  <div className="text-sm text-muted-foreground mt-1 flex items-center gap-3 flex-wrap">
                    <span>{c.policyNumber}</span>
                    <span className="flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      Filed {formatRelative(c.createdAt)}
                    </span>
                    <span>Incident on {formatDate(c.incidentDate)}</span>
                  </div>
                </div>
                {c.approvedAmount && (
                  <div className="text-right shrink-0">
                    <div className="text-xs text-muted-foreground">Payout</div>
                    <div className="font-bold text-emerald-700">
                      {formatINR(c.approvedAmount)}
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}

function NewClaimForm({ onSaved }: { onSaved: () => void }) {
  const navigate = useApp((s) => s.navigate);
  const [farms, setFarms] = useState<FarmResponseDto[]>([]);
  const [policies, setPolicies] = useState<PolicyResponseDto[]>([]);
  const [farmId, setFarmId] = useState("");
  const [policyId, setPolicyId] = useState("");
  const [incidentType, setIncidentType] = useState<IncidentType>("Flood");
  const [incidentDate, setIncidentDate] = useState(
    new Date().toISOString().slice(0, 10)
  );
  const [description, setDescription] = useState("");
  const [damageDescription, setDamageDescription] = useState("");
  const [images, setImages] = useState<{ url: string; file: File }[]>([]);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    Promise.all([farmsApi.list(), policiesApi.list()]).then(([f, p]) => {
      setFarms(f);
      setPolicies(p.filter((pol) => pol.status === "Active"));
    });
  }, []);

  const availablePolicies = policies.filter((p) => p.farmId === farmId);

  const handleFiles = (files: FileList | null) => {
    if (!files) return;
    Array.from(files).slice(0, 6).forEach((file) => {
      const url = URL.createObjectURL(file);
      setImages((prev) => [...prev, { url, file }]);
    });
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const claim = await claimsApi.create({
        policyId,
        farmId,
        incidentType,
        incidentDate: new Date(incidentDate).toISOString(),
        description: description || null,
        damageDescription: damageDescription || null,
      });
      // Upload images after claim creation
      for (const img of images) {
        try {
          await claimsApi.uploadImage(claim.id, img.file);
        } catch {
          // Continue with remaining images
        }
      }
      toast.success("Claim submitted! AI assessment in progress.");
      navigate(`/dashboard/claims/${claim.id}`);
      onSaved();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Submission failed");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="max-w-3xl mx-auto">
      <Button
        variant="ghost"
        size="sm"
        onClick={() => navigate("/dashboard/claims")}
        className="mb-4"
      >
        <ArrowLeft className="h-4 w-4 mr-1" /> Back to claims
      </Button>
      <PageHeader
        title="File a new claim"
        subtitle="Tell us what happened. Our AI will verify damage using satellite + weather data."
      />

      <form onSubmit={onSubmit} className="space-y-6">
        <Card>
          <CardContent className="p-6 space-y-4">
            <h3 className="font-serif text-lg font-semibold">Incident details</h3>
            <div className="grid sm:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Farm</Label>
                <Select value={farmId} onValueChange={(v) => { setFarmId(v); setPolicyId(""); }} required>
                  <SelectTrigger><SelectValue placeholder="Select farm" /></SelectTrigger>
                  <SelectContent>
                    {farms.map((f) => (
                      <SelectItem key={f.id} value={f.id}>{f.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Policy</Label>
                <Select value={policyId} onValueChange={setPolicyId} required disabled={!farmId}>
                  <SelectTrigger><SelectValue placeholder={farmId ? "Select policy" : "Select farm first"} /></SelectTrigger>
                  <SelectContent>
                    {availablePolicies.map((p) => (
                      <SelectItem key={p.id} value={p.id}>
                        {p.policyNumber} · {p.cropType}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="grid sm:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Incident type</Label>
                <div className="grid grid-cols-3 gap-2">
                  {incidentTypes.map((t) => (
                    <button
                      key={t.value}
                      type="button"
                      onClick={() => setIncidentType(t.value)}
                      className={cn(
                        "p-2.5 rounded-lg border text-sm transition-all flex flex-col items-center gap-1",
                        incidentType === t.value
                          ? "border-emerald-600 bg-emerald-50 text-emerald-700"
                          : "border-border hover:bg-foreground/5"
                      )}
                    >
                      <span className="text-lg">{t.emoji}</span>
                      <span className="text-[11px]">{t.label}</span>
                    </button>
                  ))}
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="date">Incident date</Label>
                <Input
                  id="date"
                  type="date"
                  value={incidentDate}
                  onChange={(e) => setIncidentDate(e.target.value)}
                  required
                  max={new Date().toISOString().slice(0, 10)}
                />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6 space-y-4">
            <h3 className="font-serif text-lg font-semibold">Description</h3>
            <div className="space-y-2">
              <Label htmlFor="desc">What happened?</Label>
              <Textarea
                id="desc"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                maxLength={1000}
                rows={3}
                placeholder="Heavy monsoon rains caused waterlogging across 4 hectares..."
              />
              <div className="text-xs text-muted-foreground text-right">
                {description.length}/1000
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="damage">Damage description</Label>
              <Textarea
                id="damage"
                value={damageDescription}
                onChange={(e) => setDamageDescription(e.target.value)}
                maxLength={2000}
                rows={4}
                placeholder="Approximately 35% of the standing crop submerged for 48 hours..."
              />
              <div className="text-xs text-muted-foreground text-right">
                {damageDescription.length}/2000
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6 space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="font-serif text-lg font-semibold flex items-center gap-2">
                <Camera className="h-5 w-5 text-emerald-700" />
                Upload photos
              </h3>
              <span className="text-xs text-muted-foreground">{images.length}/10 photos</span>
            </div>
            <div className="grid grid-cols-3 sm:grid-cols-4 gap-3">
              {images.map((img, i) => (
                <div key={i} className="relative aspect-square rounded-lg overflow-hidden group">
                  <img src={img.url} alt={`Upload ${i + 1}`} className="w-full h-full object-cover" />
                  <button
                    type="button"
                    onClick={() => setImages((prev) => prev.filter((_, idx) => idx !== i))}
                    className="absolute top-1 right-1 h-6 w-6 rounded-full bg-rose-600 text-white grid place-items-center opacity-0 group-hover:opacity-100 transition-opacity"
                  >
                    <X className="h-3.5 w-3.5" />
                  </button>
                </div>
              ))}
              {images.length < 10 && (
                <label className="aspect-square rounded-lg border-2 border-dashed border-emerald-300 hover:border-emerald-500 hover:bg-emerald-50 grid place-items-center cursor-pointer transition-colors">
                  <input
                    type="file"
                    accept="image/*"
                    multiple
                    className="hidden"
                    onChange={(e) => handleFiles(e.target.files)}
                  />
                  <Upload className="h-6 w-6 text-emerald-600" />
                </label>
              )}
            </div>
            <p className="text-xs text-muted-foreground">
                Upload up to 10 photos of the damage. Our AI vision model will analyse them.
            </p>
          </CardContent>
        </Card>

        <div className="flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate("/dashboard/claims")}
          >
            Cancel
          </Button>
          <Button
            type="submit"
            disabled={saving || !farmId || !policyId}
            className="bg-emerald-700 hover:bg-emerald-800 text-white gap-1.5"
          >
            {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />}
            {saving ? "Submitting…" : "Submit claim"}
          </Button>
        </div>
      </form>
    </div>
  );
}

function ClaimDetail({ id }: { id: string }) {
  const navigate = useApp((s) => s.navigate);
  const [claim, setClaim] = useState<ClaimResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [busy, setBusy] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);

  useEffect(() => {
    claimsApi
      .get(id)
      .then(setClaim)
      .finally(() => setLoading(false));
  }, [id]);

  const handleDelete = async () => {
    setBusy(true);
    try {
      await claimsApi.delete(id);
      toast.success("Claim deleted");
      navigate("/dashboard/claims");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Delete failed");
    } finally {
      setBusy(false);
      setDeleting(false);
    }
  };

  const handleDeleteImage = async (imageId: string) => {
    if (!claim) return;
    setBusy(true);
    try {
      await claimsApi.deleteImage(claim.id, imageId);
      setClaim({ ...claim, images: claim.images?.filter((i) => i.id !== imageId) ?? null });
      toast.success("Image removed");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Failed to remove image");
    } finally {
      setBusy(false);
    }
  };

  const handleUploadImages = async (files: FileList | null) => {
    if (!files || !claim) return;
    setUploading(true);
    let uploaded = 0;
    for (const file of Array.from(files)) {
      try {
        await claimsApi.uploadImage(claim.id, file);
        uploaded++;
      } catch {
        // skip failed
      }
    }
    if (uploaded > 0) {
      const updated = await claimsApi.get(claim.id);
      setClaim(updated);
      toast.success(`${uploaded} image(s) uploaded`);
    }
    setUploading(false);
  };

  if (loading)
    return (
      <div>
        <Skeleton className="h-8 w-32 mb-4" />
        <Skeleton className="h-96 rounded-xl" />
      </div>
    );
  if (!claim) return <div>Claim not found.</div>;

  const canEdit = claim.status === "Pending";
  const canDelete = claim.status === "Pending";

  return (
    <div className="max-w-4xl mx-auto">
      <Button
        variant="ghost"
        size="sm"
        onClick={() => navigate("/dashboard/claims")}
        className="mb-4"
      >
        <ArrowLeft className="h-4 w-4 mr-1" /> Back to claims
      </Button>

      <PageHeader
        title={`Claim — ${claim.incidentType}`}
        subtitle={`${claim.farmName} · Filed ${formatDate(claim.createdAt)}`}
        actions={
          <div className="flex items-center gap-2">
            <StatusBadge status={claim.status} />
            {canEdit && (
              <Button variant="outline" size="sm" onClick={() => setEditing(true)} className="gap-1.5">
                <Edit2 className="h-3.5 w-3.5" /> Edit
              </Button>
            )}
            {canDelete && (
              <Button variant="outline" size="sm" onClick={() => setDeleting(true)} className="gap-1.5 text-rose-600 hover:bg-rose-50 border-rose-200">
                <Trash2 className="h-3.5 w-3.5" /> Delete
              </Button>
            )}
          </div>
        }
      />

      <div className="grid lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="font-serif text-lg font-semibold">Damage photos</h3>
                <span className="text-xs text-muted-foreground">
                  {(claim.images ?? []).length}/10
                </span>
              </div>
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                {(claim.images ?? []).map((img) => (
                  <motion.div
                    key={img.id}
                    initial={{ opacity: 0, scale: 0.95 }}
                    animate={{ opacity: 1, scale: 1 }}
                    className="relative aspect-square rounded-lg overflow-hidden bg-muted group"
                  >
                    <img
                      src={img.imageUrl}
                      alt="Damage"
                      className="w-full h-full object-cover hover:scale-105 transition-transform"
                    />
                    {canDelete && (
                      <button
                        onClick={() => handleDeleteImage(img.id)}
                        disabled={busy}
                        className="absolute top-1.5 right-1.5 h-6 w-6 rounded-full bg-rose-600/90 text-white grid place-items-center opacity-0 group-hover:opacity-100 transition-opacity hover:bg-rose-700"
                      >
                        <X className="h-3.5 w-3.5" />
                      </button>
                    )}
                  </motion.div>
                ))}
                {canDelete && (claim.images ?? []).length < 10 && (
                  <label className="aspect-square rounded-lg border-2 border-dashed border-emerald-300 hover:border-emerald-500 hover:bg-emerald-50 grid place-items-center cursor-pointer transition-colors">
                    <input
                      ref={fileInputRef}
                      type="file"
                      accept="image/*"
                      multiple
                      className="hidden"
                      disabled={uploading}
                      onChange={(e) => {
                        handleUploadImages(e.target.files);
                        e.target.value = "";
                      }}
                    />
                    {uploading ? (
                      <Loader2 className="h-6 w-6 text-emerald-600 animate-spin" />
                    ) : (
                      <Upload className="h-6 w-6 text-emerald-600" />
                    )}
                  </label>
                )}
              </div>
              {canDelete && (claim.images ?? []).length > 0 && (
                <p className="text-xs text-muted-foreground mt-3">
                  Click the dashed box to add more photos. Max 10 images per claim.
                </p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6 space-y-4">
              <h3 className="font-serif text-lg font-semibold">Description</h3>
              <p className="text-foreground/90 leading-relaxed">
                {claim.description ?? "No description provided."}
              </p>
              {claim.damageDescription && (
                <>
                  <h4 className="font-medium text-sm text-muted-foreground uppercase tracking-wide">
                    Damage description
                  </h4>
                  <p className="text-foreground/90 leading-relaxed">
                    {claim.damageDescription}
                  </p>
                </>
              )}
            </CardContent>
          </Card>

          {/* AI Analysis */}
          {claim.aiAnalysisResult && (
            <Card className="border-emerald-200 bg-gradient-to-br from-emerald-50/50 to-amber-50/30">
              <CardContent className="p-6">
                <div className="flex items-center gap-2 mb-3">
                  <div className="h-9 w-9 rounded-lg bg-emerald-600 text-white grid place-items-center">
                    <Brain className="h-5 w-5" />
                  </div>
                  <div>
                    <div className="font-serif text-lg font-semibold">AI Analysis Result</div>
                    <div className="text-xs text-muted-foreground">
                      Auto-generated · Reviewed by {claim.reviewedByName ?? "admin"}
                    </div>
                  </div>
                </div>
                {/* C6 FIX: Parse the JSON analysis result instead of showing fabricated stats */}
                <div>
                {(() => {
                  let parsed: any = null;
                  try {
                    // Try to extract JSON from the string (may have surrounding text)
                    const jsonStart = claim.aiAnalysisResult.indexOf("{");
                    const jsonEnd = claim.aiAnalysisResult.lastIndexOf("}");
                    if (jsonStart >= 0 && jsonEnd > jsonStart) {
                      parsed = JSON.parse(claim.aiAnalysisResult.substring(jsonStart, jsonEnd + 1));
                    }
                  } catch {
                    // Not JSON — show as plain text description
                  }
                  const damagePct = parsed?.damagePercentage;
                  const confidence = parsed?.confidence ?? null;
                  const description = parsed?.damageDescription ?? claim.aiAnalysisResult;
                  return (
                    <>
                      <p className="text-foreground/90 leading-relaxed">{description}</p>
                      <div className="grid grid-cols-3 gap-3 mt-4">
                        {damagePct != null && (
                          <AIStat
                            icon={Satellite}
                            label="Damage estimate"
                            value={`${Math.round(damagePct)}%`}
                          />
                        )}
                        {confidence && (
                          <AIStat
                            icon={CloudRain}
                            label="Confidence"
                            value={confidence}
                          />
                        )}
                        {claim.weatherSnapshot && (
                          <AIStat
                            icon={Brain}
                            label="Weather data"
                            value="Verified"
                          />
                        )}
                      </div>
                    </>
                  );
                })()}
                </div>
              </CardContent>
            </Card>
          )}

          {claim.weatherSnapshot && (
            <WeatherSnapshotCard json={claim.weatherSnapshot} />
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          <Card>
            <CardContent className="p-6 space-y-3">
              <h3 className="font-serif text-lg font-semibold">Timeline</h3>
              <Timeline claim={claim} />
            </CardContent>
          </Card>

          {claim.approvedAmount && (
            <Card className="bg-emerald-50 border-emerald-200">
              <CardContent className="p-6 text-center">
                <div className="text-xs uppercase tracking-wide text-emerald-700">
                  {claim.status === "Paid" ? "Amount paid" : "Amount approved"}
                </div>
                <div className="font-serif text-3xl font-bold text-emerald-700 mt-1">
                  {formatINR(claim.approvedAmount)}
                </div>
                {claim.paymentReference && (
                  <div className="text-xs text-muted-foreground mt-2 font-mono">
                    Ref: {claim.paymentReference}
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {claim.rejectionReason && (
            <Card className="bg-rose-50 border-rose-200">
              <CardContent className="p-6">
                <div className="font-semibold text-rose-900 mb-1">Claim rejected</div>
                <p className="text-sm text-rose-900/80">{claim.rejectionReason}</p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      <Dialog open={editing} onOpenChange={setEditing}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Edit claim</DialogTitle>
          </DialogHeader>
          <ClaimEditForm
            claim={claim}
            onSaved={(updated) => {
              setClaim(updated);
              setEditing(false);
              toast.success("Claim updated");
            }}
            onCancel={() => setEditing(false)}
          />
        </DialogContent>
      </Dialog>

      <AlertDialog open={deleting} onOpenChange={setDeleting}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this claim?</AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone. The claim and all its images will be permanently deleted.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              disabled={busy}
              className="bg-rose-600 hover:bg-rose-700"
            >
              {busy ? <Loader2 className="h-4 w-4 animate-spin mr-1" /> : null}
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

function ClaimEditForm({
  claim,
  onSaved,
  onCancel,
}: {
  claim: ClaimResponseDto;
  onSaved: (c: ClaimResponseDto) => void;
  onCancel: () => void;
}) {
  const [incidentType, setIncidentType] = useState<IncidentType>(claim.incidentType);
  const [description, setDescription] = useState(claim.description ?? "");
  const [damageDescription, setDamageDescription] = useState(claim.damageDescription ?? "");
  const [incidentDate, setIncidentDate] = useState(claim.incidentDate.split("T")[0]);
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const updated = await claimsApi.update(claim.id, {
        incidentType,
        description: description || null,
        damageDescription: damageDescription || null,
        incidentDate: new Date(incidentDate).toISOString(),
      });
      onSaved(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Update failed");
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label>Incident type</Label>
        <div className="grid grid-cols-3 gap-2">
          {incidentTypes.map((t) => (
            <button
              key={t.value}
              type="button"
              onClick={() => setIncidentType(t.value)}
              className={`flex items-center gap-2 p-2 rounded-lg border text-sm transition-colors ${
                incidentType === t.value
                  ? "border-emerald-600 bg-emerald-50 text-emerald-800"
                  : "border-border hover:bg-muted"
              }`}
            >
              <span>{t.emoji}</span>
              <span>{t.label}</span>
            </button>
          ))}
        </div>
      </div>
      <div className="space-y-2">
        <Label>Incident date</Label>
        <Input
          type="date"
          value={incidentDate}
          onChange={(e) => setIncidentDate(e.target.value)}
          required
        />
      </div>
      <div className="space-y-2">
        <Label>Description</Label>
        <Textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          rows={3}
          maxLength={1000}
          placeholder="What happened?"
        />
      </div>
      <div className="space-y-2">
        <Label>Damage description</Label>
        <Textarea
          value={damageDescription}
          onChange={(e) => setDamageDescription(e.target.value)}
          rows={3}
          maxLength={2000}
          placeholder="Describe the damage..."
        />
      </div>
      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={saving} className="bg-emerald-700 hover:bg-emerald-800 text-white">
          {saving ? <Loader2 className="h-4 w-4 animate-spin mr-1" /> : null}
          Save changes
        </Button>
      </div>
    </form>
  );
}

interface WeatherData {
  date?: string;
  temperatureCelsius?: number;
  rainfallMm?: number;
  windSpeedKmh?: number;
  weatherCondition?: string;
  source?: string;
}

function getWeatherIcon(condition: string) {
  const c = condition.toLowerCase();
  if (c.includes("thunder") || c.includes("storm")) return CloudLightning;
  if (c.includes("drizzle")) return CloudDrizzle;
  if (c.includes("rain") || c.includes("shower")) return CloudRain;
  if (c.includes("snow") || c.includes("sleet") || c.includes("blizzard")) return CloudSnow;
  if (c.includes("cloud") || c.includes("overcast") || c.includes("fog") || c.includes("mist")) return Cloud;
  if (c.includes("clear") || c.includes("sunny")) return Sun;
  return Cloud;
}

function getWeatherGradient(condition: string): string {
  const c = condition.toLowerCase();
  if (c.includes("rain") || c.includes("shower") || c.includes("drizzle"))
    return "from-blue-500/10 via-blue-400/5 to-sky-100/30";
  if (c.includes("thunder") || c.includes("storm"))
    return "from-indigo-500/10 via-purple-400/5 to-slate-100/30";
  if (c.includes("snow") || c.includes("blizzard"))
    return "from-slate-300/20 via-blue-200/10 to-white/40";
  if (c.includes("cloud") || c.includes("overcast"))
    return "from-gray-400/10 via-gray-300/5 to-slate-100/30";
  if (c.includes("clear") || c.includes("sunny"))
    return "from-amber-400/10 via-orange-300/5 to-yellow-100/30";
  return "from-blue-400/10 via-sky-300/5 to-cyan-100/30";
}

function getWeatherAccent(condition: string): string {
  const c = condition.toLowerCase();
  if (c.includes("rain") || c.includes("shower") || c.includes("drizzle")) return "text-blue-600";
  if (c.includes("thunder") || c.includes("storm")) return "text-indigo-600";
  if (c.includes("snow") || c.includes("blizzard")) return "text-slate-500";
  if (c.includes("cloud") || c.includes("overcast")) return "text-gray-500";
  if (c.includes("clear") || c.includes("sunny")) return "text-amber-600";
  return "text-blue-600";
}

function WeatherSnapshotCard({ json }: { json: string }) {
  let data: WeatherData = {};
  try {
    data = JSON.parse(json);
  } catch {
    return (
      <Card className="border-blue-200 bg-gradient-to-br from-blue-50/50 to-sky-50/30">
        <CardContent className="p-6">
          <div className="flex items-center gap-2 mb-3">
            <div className="h-9 w-9 rounded-lg bg-blue-600 text-white grid place-items-center">
              <CloudRain className="h-5 w-5" />
            </div>
            <div>
              <div className="font-serif text-lg font-semibold">Weather snapshot</div>
              <div className="text-xs text-muted-foreground">At time of incident</div>
            </div>
          </div>
          <p className="text-sm text-foreground/80 leading-relaxed whitespace-pre-line">{json}</p>
        </CardContent>
      </Card>
    );
  }

  const condition = data.weatherCondition ?? "Unknown";
  const WeatherIcon = getWeatherIcon(condition);
  const gradient = getWeatherGradient(condition);
  const accent = getWeatherAccent(condition);

  return (
    <Card className={`border-blue-200/60 bg-gradient-to-br ${gradient} overflow-hidden`}>
      <CardContent className="p-6">
        <div className="flex items-start justify-between mb-5">
          <div className="flex items-center gap-3">
            <div className={`h-11 w-11 rounded-xl bg-white/80 backdrop-blur grid place-items-center shadow-sm ${accent}`}>
              <WeatherIcon className="h-6 w-6" />
            </div>
            <div>
              <div className="font-serif text-lg font-semibold">Weather snapshot</div>
              <div className="text-xs text-muted-foreground">At time of incident</div>
            </div>
          </div>
          <div className="text-right">
            <div className="font-serif text-2xl font-bold tabular-nums">
              {data.temperatureCelsius != null ? `${data.temperatureCelsius.toFixed(1)}°` : "—"}
            </div>
            <div className={`text-xs font-medium capitalize ${accent}`}>
              {condition}
            </div>
          </div>
        </div>

        <div className="grid grid-cols-3 gap-3">
          <div className="rounded-xl bg-white/60 backdrop-blur p-3 border border-white/80">
            <div className="flex items-center gap-1.5 mb-1.5">
              <Thermometer className="h-3.5 w-3.5 text-orange-500" />
              <span className="text-[10px] uppercase tracking-wide text-muted-foreground">Temp</span>
            </div>
            <div className="font-semibold text-sm tabular-nums">
              {data.temperatureCelsius != null ? `${data.temperatureCelsius.toFixed(1)}°C` : "—"}
            </div>
          </div>
          <div className="rounded-xl bg-white/60 backdrop-blur p-3 border border-white/80">
            <div className="flex items-center gap-1.5 mb-1.5">
              <Droplets className="h-3.5 w-3.5 text-blue-500" />
              <span className="text-[10px] uppercase tracking-wide text-muted-foreground">Rain</span>
            </div>
            <div className="font-semibold text-sm tabular-nums">
              {data.rainfallMm != null ? `${data.rainfallMm.toFixed(1)} mm` : "—"}
            </div>
          </div>
          <div className="rounded-xl bg-white/60 backdrop-blur p-3 border border-white/80">
            <div className="flex items-center gap-1.5 mb-1.5">
              <Wind className="h-3.5 w-3.5 text-teal-500" />
              <span className="text-[10px] uppercase tracking-wide text-muted-foreground">Wind</span>
            </div>
            <div className="font-semibold text-sm tabular-nums">
              {data.windSpeedKmh != null ? `${data.windSpeedKmh.toFixed(1)} km/h` : "—"}
            </div>
          </div>
        </div>

        {data.date && (
          <div className="mt-4 pt-3 border-t border-white/60 flex items-center justify-between text-xs text-muted-foreground">
            <span>{new Date(data.date).toLocaleDateString("en-IN", { weekday: "long", day: "numeric", month: "short", year: "numeric" })}</span>
            {data.source && <span className="font-mono opacity-60">{data.source}</span>}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function AIStat({
  icon: Icon,
  label,
  value,
}: {
  icon: React.ElementType;
  label: string;
  value: string;
}) {
  return (
    <div className="rounded-lg bg-card p-3 border border-emerald-100">
      <Icon className="h-4 w-4 text-emerald-700 mb-1.5" />
      <div className="text-[10px] uppercase tracking-wide text-muted-foreground">
        {label}
      </div>
      <div className="font-semibold text-sm">{value}</div>
    </div>
  );
}

function Timeline({ claim }: { claim: ClaimResponseDto }) {
  const [entries, setEntries] = useState<ClaimTimelineEntryDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    claimsApi
      .getTimeline(claim.id)
      .then(setEntries)
      .catch(() => {
        // Fallback to basic timeline if API fails
        setEntries([]);
      })
      .finally(() => setLoading(false));
  }, [claim.id]);

  if (loading) {
    return (
      <div className="space-y-4">
        {[1, 2, 3].map((i) => (
          <div key={i} className="flex gap-3">
            <Skeleton className="h-2.5 w-2.5 rounded-full" />
            <div className="space-y-1 flex-1">
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-3 w-1/2" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  // Build timeline events from API entries or fallback to basic timeline
  const events = entries.length > 0
    ? entries.map((entry) => ({
        label: formatTimelineAction(entry.action),
        description: entry.description,
        date: entry.timestamp,
        done: true,
        isReject: entry.action.toLowerCase().includes("reject"),
      }))
    : [
        {
          label: "Claim submitted",
          date: claim.createdAt,
          done: true,
        },
        {
          label: "Under review",
          date: claim.reviewedAt ?? null,
          done: ["UnderReview", "Approved", "Rejected", "Paid"].includes(claim.status),
        },
        {
          label:
            claim.status === "Rejected"
              ? `Rejected${claim.rejectionReason ? `: ${claim.rejectionReason}` : ""}`
              : "Approved",
          date: claim.reviewedAt ?? null,
          done: ["Approved", "Rejected", "Paid"].includes(claim.status),
          isReject: claim.status === "Rejected",
        },
        {
          label: "Payment disbursed",
          date: claim.paidAt ?? null,
          done: claim.status === "Paid",
        },
      ];

  return (
    <div className="space-y-4">
      {events.map((e, i) => (
        <div key={i} className="flex gap-3">
          <div className="flex flex-col items-center">
            <div
              className={cn(
                "h-2.5 w-2.5 rounded-full ring-4",
                e.done
                  ? e.isReject
                    ? "bg-rose-500 ring-rose-100"
                    : "bg-emerald-500 ring-emerald-100"
                  : "bg-muted-foreground/30 ring-muted"
              )}
            />
            {i < events.length - 1 && (
              <div className={cn("w-px flex-1 mt-1", e.done ? "bg-emerald-200" : "bg-muted")} />
            )}
          </div>
          <div className="pb-1">
            <div
              className={cn(
                "text-sm font-medium",
                e.done && !e.isReject && "text-foreground",
                e.isReject && "text-rose-700",
                !e.done && "text-muted-foreground"
              )}
            >
              {e.label}
            </div>
            {e.description && (
              <div className="text-xs text-muted-foreground mt-0.5">{e.description}</div>
            )}
            {e.date && (
              <div className="text-xs text-muted-foreground mt-0.5">
                {formatDate(e.date, { dateStyle: "medium", timeStyle: "short" } as Intl.DateTimeFormatOptions)}
              </div>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}

function formatTimelineAction(action: string): string {
  const actionMap: Record<string, string> = {
    "ClaimSubmitted": "Claim submitted",
    "ClaimCreated": "Claim submitted",
    "ClaimReviewed": "Under review",
    "ClaimApproved": "Approved",
    "ClaimRejected": "Rejected",
    "ClaimPaid": "Payment disbursed",
    "ClaimDeleted": "Claim deleted",
    "ImageUploaded": "Evidence uploaded",
    "ImageDeleted": "Evidence removed",
  };
  return actionMap[action] ?? action.replace(/([A-Z])/g, " $1").trim();
}
