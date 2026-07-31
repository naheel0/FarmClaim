"use client";

import { useEffect, useState } from "react";
import { useApp } from "@/lib/store";
import { farmsApi, plansApi, policiesApi, paymentsApi } from "@/lib/api";
import type {
  FarmResponseDto,
  InsurancePlanDto,
  PolicyResponseDto,
} from "@/lib/types";
import { PageHeader } from "@/components/layout/DashboardShell";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogFooter,
} from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { StatusBadge } from "@/components/shared/badges";
import { Badge } from "@/components/ui/badge";
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
  FileText,
  Sprout,
  Calendar,
  IndianRupee,
  Shield,
  Loader2,
  CreditCard,
  CheckCircle2,
  Edit2,
  Trash2,
} from "lucide-react";
import { formatDate, formatINR } from "@/lib/utils";
import { toast } from "sonner";

export function PoliciesPage() {
  const navigate = useApp((s) => s.navigate);
  const route = useApp((s) => s.route);
  const [policies, setPolicies] = useState<PolicyResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [createOpen, setCreateOpen] = useState(false);

  const load = () => {
    setLoading(true);
    policiesApi.list().then(setPolicies).finally(() => setLoading(false));
  };
  useEffect(load, []);

  // Detail view if route is /dashboard/policies/:id
  const detailId = route.params.id;
  if (detailId) {
    return <PolicyDetail id={detailId} />;
  }

  return (
    <div>
      <PageHeader
        title="My Policies"
        subtitle="All crop insurance policies you've purchased."
        actions={
          <Dialog open={createOpen} onOpenChange={setCreateOpen}>
            <DialogTrigger asChild>
              <Button className="bg-emerald-700 hover:bg-emerald-800 text-white gap-1.5">
                <Plus className="h-4 w-4" /> Buy policy
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-lg">
              <DialogHeader>
                <DialogTitle>Buy a new policy</DialogTitle>
              </DialogHeader>
              <BuyPolicyForm
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
        <div className="grid sm:grid-cols-2 gap-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-44 rounded-xl" />
          ))}
        </div>
      ) : (
        <div className="grid sm:grid-cols-2 gap-4">
          {policies.map((p) => (
            <Card
              key={p.id}
              className="hover:shadow-lg transition-shadow cursor-pointer"
              onClick={() => navigate(`/dashboard/policies/${p.id}`)}
            >
              <CardContent className="p-5">
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-3">
                    <div className="h-10 w-10 rounded-lg bg-emerald-100 grid place-items-center">
                      <Sprout className="h-5 w-5 text-emerald-700" />
                    </div>
                    <div>
                      <div className="font-semibold">{p.cropType}</div>
                      <div className="text-xs text-muted-foreground">{p.provider}</div>
                    </div>
                  </div>
                  <StatusBadge status={p.status} />
                </div>
                <div className="space-y-1.5 text-sm">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Policy #</span>
                    <span className="font-mono text-xs">{p.policyNumber}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Farm</span>
                    <span className="font-medium">{p.farmName}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Sum insured</span>
                    <span className="font-semibold text-emerald-700">
                      {formatINR(p.sumInsured)}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Premium</span>
                    <span className="font-semibold">{formatINR(p.premium)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Validity</span>
                    <span className="text-xs">
                      {formatDate(p.startDate)} → {formatDate(p.endDate)}
                    </span>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}

function PolicyDetail({ id }: { id: string }) {
  const navigate = useApp((s) => s.navigate);
  const [policy, setPolicy] = useState<PolicyResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [busy, setBusy] = useState(false);
  const [payments, setPayments] = useState<any[]>([]);
  const [paymentsLoading, setPaymentsLoading] = useState(true);

  useEffect(() => {
    policiesApi
      .get(id)
      .then(setPolicy)
      .finally(() => setLoading(false));
  }, [id]);

  useEffect(() => {
    paymentsApi.getByPolicy(id).then(setPayments).finally(() => setPaymentsLoading(false));
  }, [id]);

  const handleDelete = async () => {
    setBusy(true);
    try {
      await policiesApi.delete(id);
      toast.success("Policy deleted");
      navigate("/dashboard/policies");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Delete failed");
    } finally {
      setBusy(false);
      setDeleting(false);
    }
  };

  if (loading) return <Skeleton className="h-96 rounded-xl" />;
  if (!policy) return <div>Policy not found.</div>;

  const canEdit = policy.status === "Pending";
  const canDelete = policy.status === "Pending" || policy.status === "PaymentReceived";

  return (
    <div>
      <Button
        variant="ghost"
        size="sm"
        onClick={() => navigate("/dashboard/policies")}
        className="mb-4"
      >
        ← Back to policies
      </Button>
      <PageHeader
        title={`Policy ${policy.policyNumber}`}
        subtitle={`${policy.cropType} insurance · ${policy.provider}`}
        actions={
          (canEdit || canDelete) ? (
            <div className="flex gap-2">
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
          ) : undefined
        }
      />
      <div className="grid lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2">
          <CardContent className="p-6">
            <div className="flex items-center justify-between mb-6">
              <StatusBadge status={policy.status} />
              <div className="text-sm text-muted-foreground">
                Issued {formatDate(policy.createdAt)}
              </div>
            </div>
            <div className="grid sm:grid-cols-2 gap-4">
              <DetailItem label="Farm" value={policy.farmName ?? "—"} />
              <DetailItem label="Crop type" value={policy.cropType ?? "—"} />
              <DetailItem
                label="Sum insured"
                value={formatINR(policy.sumInsured)}
                accent="emerald"
              />
              <DetailItem label="Premium" value={formatINR(policy.premium)} />
              <DetailItem label="Coverage amount" value={formatINR(policy.coverageAmount)} />
              <DetailItem label="Claims filed" value={`${policy.claimsCount}`} />
              <DetailItem label="Start date" value={formatDate(policy.startDate)} />
              <DetailItem label="End date" value={formatDate(policy.endDate)} />
              {policy.approvedByName && (
                <DetailItem label="Approved by" value={policy.approvedByName} />
              )}
              {policy.approvedAt && (
                <DetailItem label="Approved at" value={formatDate(policy.approvedAt)} />
              )}
              {policy.rejectionReason && (
                <div className="sm:col-span-2 p-3 rounded-lg bg-rose-50 text-rose-900 text-sm border border-rose-200">
                  <span className="font-semibold">Rejection reason: </span>
                  {policy.rejectionReason}
                </div>
              )}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6">
            <h3 className="font-serif text-lg font-semibold mb-4">Quick actions</h3>
            <div className="space-y-2">
              <Button
                variant="outline"
                className="w-full justify-start gap-2"
                onClick={() => navigate("/dashboard/claims/new")}
              >
                <FileText className="h-4 w-4" /> File a claim on this policy
              </Button>
              <Button
                variant="outline"
                className="w-full justify-start gap-2"
                onClick={() => navigate("/dashboard/farms")}
              >
                <Shield className="h-4 w-4" /> View farm details
              </Button>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6">
            <h3 className="font-serif text-lg font-semibold mb-4">Payments</h3>
            {paymentsLoading ? (
              <Skeleton className="h-24" />
            ) : payments.length === 0 ? (
              <p className="text-sm text-muted-foreground">No payments recorded.</p>
            ) : (
              <div className="space-y-3">
                {payments.map((pmt: any) => (
                  <div key={pmt.id} className="flex items-center justify-between text-sm border-b pb-2 last:border-0">
                    <div>
                      <div className="font-medium">{formatINR(pmt.amount)}</div>
                      <div className="text-xs text-muted-foreground">{formatDate(pmt.paymentDate)}</div>
                    </div>
                    <Badge
                      className={
                        pmt.status === "Completed" || pmt.status === "Confirmed"
                          ? "bg-emerald-100 text-emerald-800 border-0"
                          : pmt.status === "Pending" || pmt.status === "Processing"
                            ? "bg-amber-100 text-amber-800 border-0"
                            : "bg-rose-100 text-rose-800 border-0"
                      }
                    >
                      {pmt.status ?? pmt.paymentStatus ?? "N/A"}
                    </Badge>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <Dialog open={editing} onOpenChange={setEditing}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Edit policy</DialogTitle>
          </DialogHeader>
          <PolicyEditForm
            policy={policy}
            onSaved={(updated) => {
              setPolicy(updated);
              setEditing(false);
              toast.success("Policy updated");
            }}
            onCancel={() => setEditing(false)}
          />
        </DialogContent>
      </Dialog>

      <AlertDialog open={deleting} onOpenChange={setDeleting}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this policy?</AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone. Policy {policy.policyNumber} will be permanently deleted.
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

function PolicyEditForm({
  policy,
  onSaved,
  onCancel,
}: {
  policy: PolicyResponseDto;
  onSaved: (p: PolicyResponseDto) => void;
  onCancel: () => void;
}) {
  const [startDate, setStartDate] = useState(policy.startDate.split("T")[0]);
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const updated = await policiesApi.update(policy.id, {
        startDate: new Date(startDate).toISOString(),
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
        <Label>Start date</Label>
        <Input
          type="date"
          value={startDate}
          onChange={(e) => setStartDate(e.target.value)}
          required
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

function DetailItem({
  label,
  value,
  accent,
}: {
  label: string;
  value: string;
  accent?: "emerald";
}) {
  return (
    <div>
      <div className="text-xs text-muted-foreground uppercase tracking-wide">
        {label}
      </div>
      <div
        className={`font-semibold mt-1 ${
          accent === "emerald" ? "text-emerald-700" : "text-foreground"
        }`}
      >
        {value}
      </div>
    </div>
  );
}

function BuyPolicyForm({ onSaved }: { onSaved: () => void }) {
  const user = useApp((s) => s.user);
  const [farms, setFarms] = useState<FarmResponseDto[]>([]);
  const [plans, setPlans] = useState<InsurancePlanDto[]>([]);
  const [farmId, setFarmId] = useState("");
  const [planId, setPlanId] = useState("");
  const [startDate, setStartDate] = useState(
    new Date().toISOString().slice(0, 10)
  );
  const [saving, setSaving] = useState(false);
  const [paying, setPaying] = useState(false);
  type PayStep = "idle" | "creating" | "checkout" | "verifying" | "done";
  const [payStep, setPayStep] = useState<PayStep>("idle");

  useEffect(() => {
    Promise.all([farmsApi.list(), plansApi.list()]).then(([f, pl]) => {
      setFarms(f);
      setPlans(pl.filter((p) => p.isActive));
    });
  }, []);

  const selectedPlan = plans.find((p) => p.id === planId);
  const selectedFarm = farms.find((f) => f.id === farmId);
  const premium = selectedPlan && selectedFarm
    ? selectedPlan.premiumRatePerHectare * selectedFarm.areaInHectares
    : 0;
  const sumInsured = selectedPlan && selectedFarm
    ? selectedPlan.sumInsuredPerHectare * selectedFarm.areaInHectares
    : 0;

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setPayStep("creating");
    try {
      // 1. Create the policy (status: Pending)
      const policy = await policiesApi.create({
        farmId,
        insurancePlanId: planId,
        startDate: new Date(startDate).toISOString(),
      });

      // 2. Open Razorpay checkout for the premium
      setPayStep("checkout");
      setPaying(true);
      const result = await paymentsApi.checkout(policy.id, {
        name: user ? `${user.firstName} ${user.lastName}` : undefined,
        email: user?.email ?? undefined,
        phone: user?.phoneNumber ?? undefined,
      });
      setPaying(false);

      if (!result.ok) {
        toast.error(
          result.error
            ? `Payment failed: ${result.error}`
            : "Payment failed — policy remains pending"
        );
        // Policy was still created, just unpaid. Refresh list.
        onSaved();
        return;
      }

      // 3. Verify signature server-side
      setPayStep("verifying");
      if (result.verified) {
        toast.success(`Payment verified · ${result.paymentId}`, {
          description: "Policy is now active pending admin approval.",
        });
      } else {
        toast.warning("Payment completed but verification pending", {
          description:
            "Your policy is created. Our team will reconcile the payment manually if needed.",
        });
      }
      setPayStep("done");
      onSaved();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Purchase failed");
      setPayStep("idle");
    } finally {
      setSaving(false);
      setPaying(false);
    }
  };

  const stepLabel: Record<PayStep, string> = {
    idle: "Buy policy",
    creating: "Creating policy…",
    checkout: "Opening Razorpay…",
    verifying: "Verifying payment…",
    done: "Done",
  };

  return (
    <form onSubmit={onSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label>Choose a farm</Label>
        <Select value={farmId} onValueChange={setFarmId} required>
          <SelectTrigger><SelectValue placeholder="Select farm" /></SelectTrigger>
          <SelectContent>
            {farms.map((f) => (
              <SelectItem key={f.id} value={f.id}>
                {f.name} · {f.areaInHectares} ha
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="space-y-2">
        <Label>Choose a plan</Label>
        <Select value={planId} onValueChange={setPlanId} required>
          <SelectTrigger><SelectValue placeholder="Select plan" /></SelectTrigger>
          <SelectContent>
            {plans.map((p) => (
              <SelectItem key={p.id} value={p.id}>
                {p.name} · {p.cropType} · ₹{p.premiumRatePerHectare}/ha
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="space-y-2">
        <Label htmlFor="startDate">Start date</Label>
        <Input
          id="startDate"
          type="date"
          value={startDate}
          onChange={(e) => setStartDate(e.target.value)}
          required
        />
      </div>

      {selectedPlan && selectedFarm && (
        <div className="rounded-lg bg-emerald-50 border border-emerald-200 p-4 space-y-2 text-sm">
          <div className="font-semibold text-emerald-900 flex items-center gap-1.5">
            <CreditCard className="h-3.5 w-3.5" /> Payment summary
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Area</span>
            <span>{selectedFarm.areaInHectares} ha</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Premium (Razorpay)</span>
            <span className="font-semibold">{formatINR(premium)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Sum insured</span>
            <span className="font-semibold text-emerald-700">{formatINR(sumInsured)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Duration</span>
            <span>{selectedPlan.policyDurationMonths} months</span>
          </div>
          {payStep !== "idle" && (
            <div className="mt-2 pt-2 border-t border-emerald-200 flex items-center gap-2 text-xs text-emerald-900">
              {payStep === "done" ? (
                <CheckCircle2 className="h-3.5 w-3.5 text-emerald-700" />
              ) : (
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
              )}
              <span>{stepLabel[payStep]}</span>
            </div>
          )}
        </div>
      )}

      <DialogFooter>
        <Button
          type="submit"
          disabled={saving || paying || !farmId || !planId}
          className="bg-emerald-700 hover:bg-emerald-800 text-white"
        >
          {saving || paying ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <CreditCard className="h-4 w-4" />
          )}
          {saving || paying ? stepLabel[payStep] : `Pay ${formatINR(premium)} & buy`}
        </Button>
      </DialogFooter>
    </form>
  );
}
