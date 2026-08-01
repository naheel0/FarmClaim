"use client";

import { useEffect, useState } from "react";
import { useApp } from "@/lib/store";
import { plansApi, farmsApi, policiesApi, paymentsApi } from "@/lib/api";
import type { InsurancePlanDto, FarmResponseDto } from "@/lib/types";
import { PageHeader } from "@/components/layout/DashboardShell";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Sprout,
  Search,
  Calendar,
  Shield,
  IndianRupee,
  Loader2,
  CheckCircle2,
  CreditCard,
} from "lucide-react";
import { formatINR } from "@/lib/utils";
import { toast } from "sonner";

const cropEmojis: Record<string, string> = {
  Paddy: "🌾",
  Wheat: "🌾",
  Cotton: "🌱",
  Sugarcane: "🎋",
  Horticulture: "🍎",
  Pulses: "🫘",
};

export function PlansBrowsePage() {
  const navigate = useApp((s) => s.navigate);
  const [plans, setPlans] = useState<InsurancePlanDto[]>([]);
  const [farms, setFarms] = useState<FarmResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [cropFilter, setCropFilter] = useState("All");
  const [buyingPlan, setBuyingPlan] = useState<InsurancePlanDto | null>(null);
  const [detailPlanId, setDetailPlanId] = useState<string | null>(null);
  const [detailPlan, setDetailPlan] = useState<InsurancePlanDto | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  useEffect(() => {
    Promise.all([plansApi.list(), farmsApi.list()])
      .then(([p, f]) => {
        setPlans(p);
        setFarms(f);
      })
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!detailPlanId) { setDetailPlan(null); return; }
    setDetailLoading(true);
    plansApi.get(detailPlanId).then(setDetailPlan).finally(() => setDetailLoading(false));
  }, [detailPlanId]);

  const crops = ["All", ...Array.from(new Set(plans.map((p) => p.cropType)))];
  const filtered = plans.filter(
    (p) =>
      p.isActive &&
      (cropFilter === "All" || p.cropType === cropFilter) &&
      (search === "" ||
        p.name.toLowerCase().includes(search.toLowerCase()) ||
        p.provider.toLowerCase().includes(search.toLowerCase()) ||
        p.cropType.toLowerCase().includes(search.toLowerCase()))
  );

  return (
    <div>
      <PageHeader
        title="Browse Insurance Plans"
        subtitle="Compare and buy crop insurance plans tailored to your region and crop."
      />

      <div className="flex flex-col sm:flex-row gap-3 mb-6">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search plans, providers, crops..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9 h-11"
          />
        </div>
        <Select value={cropFilter} onValueChange={setCropFilter}>
          <SelectTrigger className="w-full sm:w-48 h-11">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {crops.map((c) => (
              <SelectItem key={c} value={c}>{c === "All" ? "All crops" : c}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {loading ? (
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-72 rounded-xl" />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <Card>
          <CardContent className="py-16 text-center">
            <div className="h-14 w-14 rounded-full bg-emerald-100 mx-auto grid place-items-center mb-4">
              <Sprout className="h-7 w-7 text-emerald-700" />
            </div>
            <h3 className="font-serif text-xl font-semibold">No plans match your search</h3>
            <p className="text-muted-foreground mt-1">Try a different crop or keyword.</p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map((plan) => (
            <PlanBrowseCard
              key={plan.id}
              plan={plan}
              onBuy={() => setBuyingPlan(plan)}
              onDetail={() => setDetailPlanId(plan.id)}
            />
          ))}
        </div>
      )}

      {buyingPlan && (
        <BuyPlanDialog
          plan={buyingPlan}
          farms={farms}
          onClose={() => setBuyingPlan(null)}
          onBought={() => {
            setBuyingPlan(null);
            navigate("/dashboard/policies");
            toast.success("Policy purchased! Pending admin approval.");
          }}
        />
      )}

      <PlanDetailDialog
        planId={detailPlanId}
        plan={detailPlan}
        loading={detailLoading}
        onClose={() => setDetailPlanId(null)}
      />
    </div>
  );
}

function PlanBrowseCard({
  plan,
  onBuy,
  onDetail,
}: {
  plan: InsurancePlanDto;
  onBuy: () => void;
  onDetail: () => void;
}) {
  return (
    <Card
      className="overflow-hidden hover:shadow-lg transition-all hover:-translate-y-1 duration-300 flex flex-col cursor-pointer"
      onClick={onDetail}
    >
      <div className="relative h-28 bg-gradient-to-br from-emerald-600 via-emerald-700 to-green-800 p-5 text-white">
        <div className="absolute inset-0 leaf-pattern opacity-20" />
        <div className="relative flex items-start justify-between">
          <div>
            <Badge className="bg-white/20 text-white border-0">
              {cropEmojis[plan.cropType] ?? "🌱"} {plan.cropType}
            </Badge>
            <div className="font-serif text-lg font-semibold mt-2">{plan.name}</div>
          </div>
          <div className="text-right">
            <div className="text-2xl font-bold">{plan.coveragePercentage}%</div>
            <div className="text-[10px] uppercase tracking-wide opacity-80">coverage</div>
          </div>
        </div>
      </div>
      <CardContent className="p-5 flex-1 flex flex-col">
        <div className="text-sm text-muted-foreground mb-4 line-clamp-3 flex-1">
          {plan.description}
        </div>
        <div className="space-y-2 text-sm border-t pt-3">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Provider</span>
            <span className="font-medium">{plan.provider}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Premium / ha</span>
            <span className="font-semibold">{formatINR(plan.premiumRatePerHectare)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Sum insured / ha</span>
            <span className="font-semibold text-emerald-700">
              {formatINR(plan.sumInsuredPerHectare)}
            </span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Duration</span>
            <span className="font-medium">{plan.policyDurationMonths} months</span>
          </div>
          {plan.supportsInstallments && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">Installments</span>
              <span className="font-medium text-emerald-700">
                {plan.installmentCount}× {plan.installmentFrequency}
              </span>
            </div>
          )}
        </div>
        <Button
          onClick={(e) => { e.stopPropagation(); onBuy(); }}
          className="w-full mt-5 bg-emerald-700 hover:bg-emerald-800 text-white"
        >
          Buy this plan
        </Button>
      </CardContent>
    </Card>
  );
}

function PlanDetailDialog({
  planId,
  plan,
  loading,
  onClose,
}: {
  planId: string | null;
  plan: InsurancePlanDto | null;
  loading: boolean;
  onClose: () => void;
}) {
  if (!planId) return null;

  return (
    <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 grid place-items-center p-4" onClick={onClose}>
      <Card className="w-full max-w-lg max-h-[85vh] overflow-y-auto" onClick={(e) => e.stopPropagation()}>
        <CardContent className="p-6">
          {loading ? (
            <div className="space-y-4">
              <Skeleton className="h-6 w-3/4" />
              <Skeleton className="h-4 w-1/2" />
              <Skeleton className="h-32 w-full" />
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-3/4" />
            </div>
          ) : plan ? (
            <>
              <div className="flex items-start justify-between mb-5">
                <div>
                  <Badge className="bg-emerald-100 text-emerald-800 border-0 mb-2">
                    {cropEmojis[plan.cropType] ?? "🌱"} {plan.cropType}
                  </Badge>
                  <h3 className="font-serif text-xl font-semibold">{plan.name}</h3>
                  <p className="text-sm text-muted-foreground">by {plan.provider}</p>
                </div>
                <div className="text-right">
                  <div className="text-3xl font-bold font-serif text-emerald-700">{plan.coveragePercentage}%</div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">coverage</div>
                </div>
              </div>

              <div className="text-sm text-muted-foreground mb-5 leading-relaxed">
                {plan.description}
              </div>

              <div className="grid grid-cols-2 gap-4 text-sm border-t pt-5">
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Premium rate / ha</div>
                  <div className="font-semibold mt-1">{formatINR(plan.premiumRatePerHectare)}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Sum insured / ha</div>
                  <div className="font-semibold mt-1 text-emerald-700">{formatINR(plan.sumInsuredPerHectare)}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Policy duration</div>
                  <div className="font-medium mt-1">{plan.policyDurationMonths} months</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Status</div>
                  <div className="mt-1">
                    <Badge variant={plan.isActive ? "default" : "secondary"} className={plan.isActive ? "bg-emerald-700 text-white" : ""}>
                      {plan.isActive ? "Active" : "Inactive"}
                    </Badge>
                  </div>
                </div>
                {plan.supportsInstallments && (
                  <div className="col-span-2">
                    <div className="text-xs text-muted-foreground uppercase tracking-wide">Installment plan</div>
                    <div className="font-medium mt-1 text-emerald-700">
                      {plan.installmentCount} payments of {formatINR(plan.premiumRatePerHectare / (plan.installmentCount ?? 1))}/ha · {plan.installmentFrequency}
                    </div>
                  </div>
                )}
              </div>

              <div className="flex gap-2 mt-6 pt-4 border-t">
                <Button variant="outline" onClick={onClose} className="flex-1">Close</Button>
              </div>
            </>
          ) : (
            <div className="text-center py-8 text-muted-foreground">Plan not found.</div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function BuyPlanDialog({
  plan,
  farms,
  onClose,
  onBought,
}: {
  plan: InsurancePlanDto;
  farms: FarmResponseDto[];
  onClose: () => void;
  onBought: () => void;
}) {
  const user = useApp((s) => s.user);
  const [farmId, setFarmId] = useState(farms[0]?.id ?? "");
  const [startDate, setStartDate] = useState(new Date().toISOString().slice(0, 10));
  const [saving, setSaving] = useState(false);
  const [paying, setPaying] = useState(false);
  type PayStep = "idle" | "creating" | "checkout" | "verifying" | "done";
  const [payStep, setPayStep] = useState<PayStep>("idle");

  const farm = farms.find((f) => f.id === farmId);
  const premium = farm ? plan.premiumRatePerHectare * farm.areaInHectares : 0;
  const sumInsured = farm ? plan.sumInsuredPerHectare * farm.areaInHectares : 0;

  const buy = async () => {
    setSaving(true);
    setPayStep("creating");
    try {
      // 1. Create the policy (status: Pending)
      const policy = await policiesApi.create({
        farmId,
        insurancePlanId: plan.id,
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
        onBought();
        return;
      }

      // 3. Verify signature server-side
      setPayStep("verifying");
      if (result.verified) {
        toast.success(`Payment verified · ${result.paymentId}`, {
          description: "Policy is now active.",
        });
      } else {
        toast.warning("Payment completed but verification pending", {
          description:
            "Your policy is created. Our team will reconcile the payment manually if needed.",
        });
      }
      setPayStep("done");
      onBought();
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
    <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 grid place-items-center p-4" onClick={onClose}>
      <Card className="w-full max-w-md" onClick={(e) => e.stopPropagation()}>
        <CardContent className="p-6">
          <h3 className="font-serif text-xl font-semibold mb-1">{plan.name}</h3>
          <p className="text-sm text-muted-foreground mb-5">{plan.provider}</p>

          {farms.length === 0 ? (
            <div className="text-center py-8">
              <p className="text-muted-foreground">You need to register a farm first.</p>
              <Button
                onClick={() => {
                  onClose();
                  useApp.getState().navigate("/dashboard/farms");
                }}
                className="mt-3 bg-emerald-700 hover:bg-emerald-800 text-white"
              >
                Go to farms
              </Button>
            </div>
          ) : (
            <>
              <div className="space-y-3">
                <div>
                  <label className="text-sm font-medium">Select farm</label>
                  <Select value={farmId} onValueChange={setFarmId}>
                    <SelectTrigger className="mt-1">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {farms.map((f) => (
                        <SelectItem key={f.id} value={f.id}>
                          {f.name} · {f.areaInHectares} ha
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div>
                  <label className="text-sm font-medium">Start date</label>
                  <Input
                    type="date"
                    value={startDate}
                    onChange={(e) => setStartDate(e.target.value)}
                    className="mt-1"
                  />
                </div>
              </div>

              <div className="rounded-lg bg-emerald-50 border border-emerald-200 p-4 mt-5 space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Premium</span>
                  <span className="font-semibold">{formatINR(premium)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Sum insured</span>
                  <span className="font-semibold text-emerald-700">{formatINR(sumInsured)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Coverage</span>
                  <span>{plan.coveragePercentage}% of damage</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Duration</span>
                  <span>{plan.policyDurationMonths} months</span>
                </div>
                {plan.supportsInstallments && (
                  <div className="flex justify-between text-emerald-700">
                    <span className="font-medium">Installment plan available</span>
                    <span className="font-medium">{plan.installmentCount}× {plan.installmentFrequency}</span>
                  </div>
                )}
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

              <div className="flex gap-2 mt-5">
                <Button variant="outline" onClick={onClose} className="flex-1">
                  Cancel
                </Button>
                <Button
                  onClick={buy}
                  disabled={saving || paying}
                  className="flex-1 bg-emerald-700 hover:bg-emerald-800 text-white"
                >
                  {saving || paying ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <CreditCard className="h-4 w-4" />
                  )}
                  {saving || paying ? stepLabel[payStep] : `Pay ${formatINR(premium)} & buy`}
                </Button>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
