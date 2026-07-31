"use client";

import { useEffect, useState, useRef } from "react";
import { useApp } from "@/lib/store";
import {
  LayoutDashboard,
  ClipboardList,
  FileText,
  Search,
  Users,
  ScrollText,
  Shield,
  Plus,
  TrendingUp,
  Wallet,
  Sprout,
  IndianRupee,
  Clock,
  CheckCircle2,
  XCircle,
  Loader2,
  Edit2,
  Trash2,
  Power,
  AlertTriangle,
  Eye,
  Brain,
  Globe,
  User,
  ArrowRight,
  Link,
  Code,
} from "lucide-react";
import {
  DashboardShell,
  PageHeader,
  CardStat,
  type NavItem,
} from "@/components/layout/DashboardShell";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
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
import { StatusBadge, IncidentBadge } from "@/components/shared/badges";
import {
  adminApi,
  plansApi,
  policiesApi,
} from "@/lib/api";
import type {
  AdminDashboardDto,
  AuditLogDto,
  ClaimResponseDto,
  CreatePlanRequestDto,
  InsurancePlanDto,
  PolicyResponseDto,
  FarmerProfileDto,
  UserDto,
} from "@/lib/types";
import { cn, formatDate, formatINR, formatNumber, formatRelative, initials } from "@/lib/utils";
import { toast } from "sonner";
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
  Legend,
} from "recharts";

const adminNav: NavItem[] = [
  { label: "Dashboard", path: "/admin/dashboard", icon: LayoutDashboard },
  { label: "Claims Review", path: "/admin/claims", icon: ClipboardList },
  { label: "Policies", path: "/admin/policies", icon: FileText },
  { label: "Insurance Plans", path: "/admin/plans", icon: Sprout },
  { label: "Users", path: "/admin/users", icon: Users },
  { label: "Audit Logs", path: "/admin/audit", icon: ScrollText },
];

const PIE_COLORS = ["#16a34a", "#f59e0b", "#0ea5e9", "#ef4444", "#22c55e", "#78716c"];

export function AdminDashboard() {
  return (
    <DashboardShell
      navItems={adminNav}
      brandLabel="FarmClaim"
      brandSub="Admin Console"
    >
      <AdminRouter />
    </DashboardShell>
  );
}

function AdminRouter() {
  const route = useApp((s) => s.route);
  const path = route.path;
  if (path.startsWith("/admin/claims")) return <AdminClaimsPage />;
  if (path.startsWith("/admin/plans")) return <AdminPlansPage />;
  if (path.startsWith("/admin/users")) return <AdminUsersPage />;
  if (path.startsWith("/admin/audit")) return <AdminAuditPage />;
  if (path.startsWith("/admin/policies")) return <AdminPoliciesPage />;
  return <AdminOverviewPage />;
}

// ============== ADMIN OVERVIEW ==============
function AdminOverviewPage() {
  const [data, setData] = useState<AdminDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    adminApi.dashboard().then(setData).catch((e) => setError(e.message)).finally(() => setLoading(false));
  }, []);

  if (loading)
    return (
      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {Array.from({ length: 8 }).map((_, i) => (
          <Skeleton key={i} className="h-32 rounded-xl" />
        ))}
      </div>
    );
  if (error) return <div className="text-rose-600 p-4 border rounded-lg bg-rose-50">Failed to load dashboard: {error}</div>;
  if (!data) return <div>No dashboard data available.</div>;

  const pieData = Object.entries(data.claimsByStatus).map(([name, value]) => ({
    name,
    value,
  }));
  const incidentData = Object.entries(data.claimsByIncidentType).map(([name, value]) => ({
    name,
    value,
  }));

  return (
    <div>
      <PageHeader
        title="Admin Dashboard"
        subtitle="Real-time overview of FarmClaim operations."
      />

      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <CardStat
          label="Total farmers"
          value={formatNumber(data.totalFarmers)}
          icon={Users}
        />
        <CardStat
          label="Active policies"
          value={formatNumber(data.policiesByStatus.Active ?? 0)}
          icon={FileText}
          accent="amber"
        />
        <CardStat
          label="Pending claims"
          value={formatNumber(data.pendingClaims)}
          icon={Clock}
          accent="rose"
        />
        <CardStat
          label="Premium collected"
          value={formatINR(data.totalPremiumCollected)}
          icon={Wallet}
          accent="blue"
        />
      </div>

      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4 mt-4">
        <CardStat label="Total farms" value={formatNumber(data.totalFarms)} icon={Sprout} />
        <CardStat
          label="Total claims"
          value={formatNumber(data.totalClaims)}
          icon={ClipboardList}
          accent="amber"
        />
        <CardStat
          label="Claims paid out"
          value={formatINR(data.totalClaimsPaid)}
          icon={IndianRupee}
          accent="rose"
        />
        <CardStat
          label="Pending policies"
          value={formatNumber(data.pendingPolicies)}
          icon={FileText}
          accent="blue"
        />
      </div>

      <div className="grid lg:grid-cols-3 gap-6 mt-6">
        {/* Premium & claims trend */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="font-serif">Premium vs Claims — 12 months</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <AreaChart data={data.premiumTrend}>
                <defs>
                  <linearGradient id="adminG1" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#16a34a" stopOpacity={0.4} />
                    <stop offset="95%" stopColor="#16a34a" stopOpacity={0} />
                  </linearGradient>
                  <linearGradient id="adminG2" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#ef4444" stopOpacity={0.4} />
                    <stop offset="95%" stopColor="#ef4444" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#e7e5e4" vertical={false} />
                <XAxis dataKey="month" stroke="#78716c" fontSize={12} tickLine={false} axisLine={false} />
                <YAxis stroke="#78716c" fontSize={12} tickLine={false} axisLine={false} tickFormatter={(v) => `₹${(v / 1000000).toFixed(0)}M`} />
                <Tooltip
                  contentStyle={{ background: "white", border: "1px solid #e7e5e4", borderRadius: 12, fontSize: 12 }}
                  formatter={(v: number) => formatINR(v)}
                />
                <Legend wrapperStyle={{ fontSize: 12 }} />
                <Area type="monotone" dataKey="premium" stroke="#16a34a" strokeWidth={2} fill="url(#adminG1)" name="Premium collected" />
                <Area type="monotone" dataKey="claims" stroke="#ef4444" strokeWidth={2} fill="url(#adminG2)" name="Claims paid" />
              </AreaChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        {/* Claims by status */}
        <Card>
          <CardHeader>
            <CardTitle className="font-serif">Claims by status</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={220}>
              <PieChart>
                <Pie data={pieData} dataKey="value" nameKey="name" cx="50%" cy="50%" innerRadius={45} outerRadius={75} paddingAngle={3}>
                  {pieData.map((_, i) => (
                    <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip contentStyle={{ background: "white", border: "1px solid #e7e5e4", borderRadius: 12, fontSize: 12 }} />
              </PieChart>
            </ResponsiveContainer>
            <div className="grid grid-cols-2 gap-2 mt-2 text-xs">
              {pieData.map((d, i) => (
                <div key={d.name} className="flex items-center gap-1.5">
                  <span className="h-2.5 w-2.5 rounded-full" style={{ background: PIE_COLORS[i % PIE_COLORS.length] }} />
                  <span className="text-muted-foreground">{d.name}:</span>
                  <span className="font-semibold">{d.value}</span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid lg:grid-cols-2 gap-6 mt-6">
        {/* Claims by incident type */}
        <Card>
          <CardHeader>
            <CardTitle className="font-serif">Claims by incident type</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={280}>
              <BarChart data={incidentData} layout="vertical" margin={{ left: 30 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e7e5e4" horizontal={false} />
                <XAxis type="number" stroke="#78716c" fontSize={12} tickLine={false} axisLine={false} />
                <YAxis type="category" dataKey="name" stroke="#78716c" fontSize={11} tickLine={false} axisLine={false} width={100} />
                <Tooltip contentStyle={{ background: "white", border: "1px solid #e7e5e4", borderRadius: 12, fontSize: 12 }} />
                <Bar dataKey="value" fill="#16a34a" radius={[0, 6, 6, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        {/* Recent claims */}
        <Card>
          <CardHeader>
            <CardTitle className="font-serif">Recent claims</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 max-h-80 overflow-y-auto">
            {data.recentClaims.map((c) => (
              <div key={c.id} className="flex items-center gap-3 p-2.5 rounded-lg hover:bg-muted/60 transition-colors">
                <div className="flex-1 min-w-0">
                  <div className="text-sm font-medium truncate">
                    {c.farmName} · {c.incidentType}
                  </div>
                  <div className="text-xs text-muted-foreground">{c.policyNumber}</div>
                </div>
                <StatusBadge status={c.status} />
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

// ============== ADMIN CLAIMS REVIEW ==============
function AdminClaimsPage() {
  const navigate = useApp((s) => s.navigate);
  const route = useApp((s) => s.route);
  const [claims, setClaims] = useState<ClaimResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState("All");
  const [search, setSearch] = useState("");

  const load = () => {
    setLoading(true);
    adminApi
      .listClaims(1, 100, {
        status: statusFilter === "All" ? undefined : statusFilter,
        searchTerm: search || undefined,
      })
      .then((r) => setClaims(r.items))
      .finally(() => setLoading(false));
  };
  useEffect(load, [statusFilter, search]);

  // Detail view
  const detailId = route.params.id;
  if (detailId) return <AdminClaimDetail id={detailId} onUpdated={load} />;

  return (
    <div>
      <PageHeader
        title="Claims Review"
        subtitle="Review, approve, reject and pay farmer claims."
      />

      <div className="flex flex-col sm:flex-row gap-3 mb-5">
        <Input
          placeholder="Search by farm, policy #, description..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="flex-1 h-11"
        />
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-full sm:w-48 h-11">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {["All", "Pending", "UnderReview", "Approved", "Rejected", "Paid"].map((s) => (
              <SelectItem key={s} value={s}>
                {s === "UnderReview" ? "Under Review" : s}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {loading ? (
        <div className="space-y-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-24 rounded-xl" />
          ))}
        </div>
      ) : claims.length === 0 ? (
        <Card>
          <CardContent className="py-16 text-center text-muted-foreground">
            No claims match your filters.
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {claims.map((c) => (
            <Card key={c.id} className="hover:shadow-md transition-shadow cursor-pointer" onClick={() => navigate(`/admin/claims/${c.id}`)}>
              <CardContent className="p-5 flex items-center gap-4">
                <div className="h-12 w-12 rounded-xl bg-emerald-100 grid place-items-center shrink-0">
                  <Brain className="h-6 w-6 text-emerald-700" />
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
                      <Clock className="h-3 w-3" /> Filed {formatRelative(c.createdAt)}
                    </span>
                    <span>Incident {formatDate(c.incidentDate)}</span>
                  </div>
                </div>
                <Button variant="outline" size="sm" className="shrink-0">
                  <Eye className="h-3.5 w-3.5 mr-1" /> Review
                </Button>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}

function AdminClaimDetail({ id, onUpdated }: { id: string; onUpdated: () => void }) {
  const navigate = useApp((s) => s.navigate);
  const [claim, setClaim] = useState<ClaimResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [approveOpen, setApproveOpen] = useState(false);
  const [rejectOpen, setRejectOpen] = useState(false);
  const [payOpen, setPayOpen] = useState(false);
  const [approveAmount, setApproveAmount] = useState("");
  const [rejectReason, setRejectReason] = useState("");
  const [payRef, setPayRef] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    adminApi
      .getClaim(id)
      .then(setClaim)
      .finally(() => setLoading(false));
  }, [id]);

  if (loading)
    return (
      <div>
        <Skeleton className="h-8 w-32 mb-4" />
        <Skeleton className="h-96 rounded-xl" />
      </div>
    );
  if (!claim) return <div>Claim not found.</div>;

  const startReview = async () => {
    setBusy(true);
    try {
      await adminApi.reviewClaim(claim.id);
      toast.success("Marked as under review");
      setClaim({ ...claim, status: "UnderReview" });
      onUpdated();
    } finally {
      setBusy(false);
    }
  };

  const approve = async () => {
    setBusy(true);
    try {
      await adminApi.approveClaim(claim.id, parseFloat(approveAmount));
      toast.success("Claim approved");
      setApproveOpen(false);
      setClaim({ ...claim, status: "Approved", approvedAmount: parseFloat(approveAmount), reviewedAt: new Date().toISOString() });
      onUpdated();
    } finally {
      setBusy(false);
    }
  };

  const reject = async () => {
    setBusy(true);
    try {
      await adminApi.rejectClaim(claim.id, rejectReason);
      toast.success("Claim rejected");
      setRejectOpen(false);
      setClaim({ ...claim, status: "Rejected", rejectionReason: rejectReason, reviewedAt: new Date().toISOString() });
      onUpdated();
    } finally {
      setBusy(false);
    }
  };

  const pay = async () => {
    setBusy(true);
    try {
      await adminApi.payClaim(claim.id, payRef);
      toast.success("Payment disbursed");
      setPayOpen(false);
      setClaim({ ...claim, status: "Paid", paidAt: new Date().toISOString(), paymentReference: payRef });
      onUpdated();
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="max-w-5xl mx-auto">
      <Button variant="ghost" size="sm" onClick={() => navigate("/admin/claims")} className="mb-4">
        ← Back to claims
      </Button>
      <PageHeader
        title={`Claim — ${claim.incidentType}`}
        subtitle={`${claim.farmName} · Filed ${formatDate(claim.createdAt)}`}
        actions={<StatusBadge status={claim.status} />}
      />

      <div className="grid lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          {claim.images && claim.images.length > 0 && (
            <Card>
              <CardContent className="p-6">
                <h3 className="font-serif text-lg font-semibold mb-4">Damage photos</h3>
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                  {claim.images.map((img) => (
                    <div key={img.id} className="aspect-square rounded-lg overflow-hidden bg-muted">
                      <img src={img.imageUrl} alt="Damage" className="w-full h-full object-cover" />
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          <Card>
            <CardContent className="p-6 space-y-4">
              <h3 className="font-serif text-lg font-semibold">Incident details</h3>
              <div className="grid sm:grid-cols-2 gap-4 text-sm">
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Farm</div>
                  <div className="font-medium mt-1">{claim.farmName}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Policy #</div>
                  <div className="font-mono text-xs mt-1">{claim.policyNumber}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Incident type</div>
                  <div className="mt-1"><IncidentBadge type={claim.incidentType} /></div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Incident date</div>
                  <div className="font-medium mt-1">{formatDate(claim.incidentDate)}</div>
                </div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground uppercase tracking-wide">Description</div>
                <p className="mt-1 text-foreground/90 leading-relaxed">
                  {claim.description ?? "—"}
                </p>
              </div>
              {claim.damageDescription && (
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Damage description</div>
                  <p className="mt-1 text-foreground/90 leading-relaxed">{claim.damageDescription}</p>
                </div>
              )}
              {claim.weatherSnapshot && (
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Weather snapshot</div>
                  <p className="mt-1 text-sm text-muted-foreground">{claim.weatherSnapshot}</p>
                </div>
              )}
              {claim.aiAnalysisResult && (
                <div className="rounded-lg bg-emerald-50 border border-emerald-200 p-4">
                  <div className="flex items-center gap-2 mb-2">
                    <Brain className="h-4 w-4 text-emerald-700" />
                    <span className="font-semibold text-emerald-900">AI Analysis</span>
                  </div>
                  <p className="text-sm text-emerald-900/90">{claim.aiAnalysisResult}</p>
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Action panel */}
        <div className="space-y-4">
          <Card>
            <CardContent className="p-6">
              <h3 className="font-serif text-lg font-semibold mb-4">Actions</h3>
              {claim.status === "Pending" && (
                <Button
                  onClick={startReview}
                  disabled={busy}
                  className="w-full bg-blue-600 hover:bg-blue-700 text-white mb-2"
                >
                  <Eye className="h-4 w-4 mr-1.5" /> Mark under review
                </Button>
              )}
              {(claim.status === "Pending" || claim.status === "UnderReview") && (
                <>
                  <Button
                    onClick={() => setApproveOpen(true)}
                    disabled={busy}
                    className="w-full bg-emerald-700 hover:bg-emerald-800 text-white mb-2"
                  >
                    <CheckCircle2 className="h-4 w-4 mr-1.5" /> Approve
                  </Button>
                  <Button
                    onClick={() => setRejectOpen(true)}
                    disabled={busy}
                    variant="outline"
                    className="w-full text-rose-600 hover:text-rose-700 hover:bg-rose-50 border-rose-200"
                  >
                    <XCircle className="h-4 w-4 mr-1.5" /> Reject
                  </Button>
                </>
              )}
              {claim.status === "Approved" && (
                <Button
                  onClick={() => setPayOpen(true)}
                  disabled={busy}
                  className="w-full bg-emerald-700 hover:bg-emerald-800 text-white"
                >
                  <Wallet className="h-4 w-4 mr-1.5" /> Disburse payment
                </Button>
              )}
              {claim.status === "Paid" && (
                <div className="text-center py-4 text-sm text-muted-foreground">
                  Payment disbursed on {formatDate(claim.paidAt)}
                  <div className="font-mono text-xs mt-1">{claim.paymentReference}</div>
                </div>
              )}
              {claim.status === "Rejected" && (
                <div className="text-center py-4 text-sm text-rose-700">
                  Rejected: {claim.rejectionReason}
                </div>
              )}
            </CardContent>
          </Card>

          {claim.approvedAmount && (
            <Card className="bg-emerald-50 border-emerald-200">
              <CardContent className="p-6 text-center">
                <div className="text-xs uppercase tracking-wide text-emerald-700">Approved amount</div>
                <div className="font-serif text-2xl font-bold text-emerald-700 mt-1">
                  {formatINR(claim.approvedAmount)}
                </div>
                {claim.reviewedByName && (
                  <div className="text-xs text-muted-foreground mt-2">
                    by {claim.reviewedByName}
                  </div>
                )}
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {/* Approve dialog */}
      <Dialog open={approveOpen} onOpenChange={setApproveOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Approve claim</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <Label>Approved payout amount (₹)</Label>
            <Input
              type="number"
              value={approveAmount}
              onChange={(e) => setApproveAmount(e.target.value)}
              placeholder="197000"
              autoFocus
            />
            <p className="text-xs text-muted-foreground">
              This amount will be disbursed to the farmer&apos;s bank account.
            </p>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setApproveOpen(false)}>Cancel</Button>
            <Button
              onClick={approve}
              disabled={busy || !approveAmount}
              className="bg-emerald-700 hover:bg-emerald-800 text-white"
            >
              {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : "Approve claim"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Reject dialog */}
      <Dialog open={rejectOpen} onOpenChange={setRejectOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reject claim</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <Label>Rejection reason</Label>
            <Textarea
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              rows={4}
              placeholder="Damage not consistent with weather records..."
              autoFocus
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setRejectOpen(false)}>Cancel</Button>
            <Button
              onClick={reject}
              disabled={busy || !rejectReason}
              className="bg-rose-600 hover:bg-rose-700 text-white"
            >
              {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : "Reject claim"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Pay dialog */}
      <Dialog open={payOpen} onOpenChange={setPayOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Disburse payment</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <div className="rounded-lg bg-emerald-50 border border-emerald-200 p-4 text-center">
              <div className="text-xs text-emerald-700 uppercase tracking-wide">Amount</div>
              <div className="font-serif text-2xl font-bold text-emerald-700">
                {formatINR(claim.approvedAmount)}
              </div>
            </div>
            <Label>Payment reference (Razorpay ID)</Label>
            <Input
              value={payRef}
              onChange={(e) => setPayRef(e.target.value)}
              placeholder="RAZP-FC-XXXXXXX"
              autoFocus
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setPayOpen(false)}>Cancel</Button>
            <Button
              onClick={pay}
              disabled={busy || !payRef}
              className="bg-emerald-700 hover:bg-emerald-800 text-white"
            >
              {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : "Confirm payment"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

// ============== ADMIN PLANS CRUD ==============
function AdminPlansPage() {
  const [plans, setPlans] = useState<InsurancePlanDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<InsurancePlanDto | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [deleting, setDeleting] = useState<InsurancePlanDto | null>(null);

  const load = () => {
    setLoading(true);
    adminApi.listPlans().then(setPlans).finally(() => setLoading(false));
  };
  useEffect(load, []);

  const toggleActive = async (plan: InsurancePlanDto) => {
    if (plan.isActive) {
      await plansApi.deactivate(plan.id);
      toast.success("Plan deactivated");
    } else {
      await plansApi.activate(plan.id);
      toast.success("Plan activated");
    }
    load();
  };

  return (
    <div>
      <PageHeader
        title="Insurance Plans"
        subtitle="Create, edit and manage crop insurance plans."
        actions={
          <Dialog open={createOpen} onOpenChange={setCreateOpen}>
            <DialogTrigger asChild>
              <Button className="bg-emerald-700 hover:bg-emerald-800 text-white gap-1.5">
                <Plus className="h-4 w-4" /> New plan
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
              <DialogHeader>
                <DialogTitle>Create insurance plan</DialogTitle>
              </DialogHeader>
              <PlanForm onSaved={() => { setCreateOpen(false); load(); }} />
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
      ) : (
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {plans.map((plan) => (
            <Card key={plan.id} className="overflow-hidden">
              <CardContent className="p-5">
                <div className="flex items-start justify-between mb-3">
                  <div>
                    <Badge variant="outline">{plan.cropType}</Badge>
                    <div className="font-serif text-lg font-semibold mt-2">{plan.name}</div>
                    <div className="text-xs text-muted-foreground">{plan.provider}</div>
                  </div>
                  <Badge variant={plan.isActive ? "default" : "secondary"} className={plan.isActive ? "bg-emerald-700 text-white" : ""}>
                    {plan.isActive ? "Active" : "Inactive"}
                  </Badge>
                </div>
                <div className="space-y-1.5 text-sm">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Premium / ha</span>
                    <span className="font-semibold">{formatINR(plan.premiumRatePerHectare)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Sum insured / ha</span>
                    <span className="font-semibold text-emerald-700">{formatINR(plan.sumInsuredPerHectare)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Coverage</span>
                    <span>{plan.coveragePercentage}%</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Duration</span>
                    <span>{plan.policyDurationMonths} months</span>
                  </div>
                </div>
                <div className="flex gap-2 mt-4">
                  <Button variant="outline" size="sm" onClick={() => setEditing(plan)} className="flex-1">
                    <Edit2 className="h-3.5 w-3.5 mr-1" /> Edit
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => toggleActive(plan)}
                    className={plan.isActive ? "text-amber-700 hover:bg-amber-50" : "text-emerald-700 hover:bg-emerald-50"}
                  >
                    <Power className="h-3.5 w-3.5" />
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setDeleting(plan)}
                    className="text-rose-600 hover:bg-rose-50"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      <Dialog open={!!editing} onOpenChange={(o) => !o && setEditing(null)}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Edit plan</DialogTitle>
          </DialogHeader>
          {editing && <PlanForm initial={editing} onSaved={() => { setEditing(null); load(); }} />}
        </DialogContent>
      </Dialog>

      <AlertDialog open={!!deleting} onOpenChange={(o) => !o && setDeleting(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete plan?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete &ldquo;{deleting?.name}&rdquo;. Existing policies bought against this plan will not be affected.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={async () => {
                if (!deleting) return;
                await plansApi.delete(deleting.id);
                toast.success("Plan deleted");
                setDeleting(null);
                load();
              }}
              className="bg-rose-600 hover:bg-rose-700"
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

function PlanForm({
  initial,
  onSaved,
}: {
  initial?: InsurancePlanDto;
  onSaved: () => void;
}) {
  const [form, setForm] = useState<CreatePlanRequestDto>({
    name: initial?.name ?? "",
    description: initial?.description ?? "",
    cropType: initial?.cropType ?? "",
    provider: initial?.provider ?? "",
    premiumRatePerHectare: initial?.premiumRatePerHectare ?? 1000,
    sumInsuredPerHectare: initial?.sumInsuredPerHectare ?? 25000,
    coveragePercentage: initial?.coveragePercentage ?? 85,
    minAreaInHectares: initial?.minAreaInHectares ?? 0.5,
    maxAreaInHectares: initial?.maxAreaInHectares ?? 100,
    policyDurationMonths: initial?.policyDurationMonths ?? 6,
    isActive: initial?.isActive ?? true,
  });
  const [saving, setSaving] = useState(false);

  const set = (k: keyof CreatePlanRequestDto, v: any) => setForm((p) => ({ ...p, [k]: v }));

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      if (initial) {
        await plansApi.update(initial.id, form);
        toast.success("Plan updated");
      } else {
        await plansApi.create(form);
        toast.success("Plan created");
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
      <div className="grid sm:grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label>Plan name</Label>
          <Input value={form.name} onChange={(e) => set("name", e.target.value)} required maxLength={200} />
        </div>
        <div className="space-y-2">
          <Label>Crop type</Label>
          <Input value={form.cropType} onChange={(e) => set("cropType", e.target.value)} required maxLength={100} placeholder="Paddy, Wheat, Cotton..." />
        </div>
      </div>
      <div className="grid sm:grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label>Provider</Label>
          <Input value={form.provider} onChange={(e) => set("provider", e.target.value)} required maxLength={200} />
        </div>
        <div className="space-y-2">
          <Label>Duration (months)</Label>
          <Input type="number" min={1} max={60} value={form.policyDurationMonths} onChange={(e) => set("policyDurationMonths", parseInt(e.target.value))} required />
        </div>
      </div>
      <div className="space-y-2">
        <Label>Description</Label>
        <Textarea value={form.description ?? ""} onChange={(e) => set("description", e.target.value)} rows={3} maxLength={2000} />
      </div>
      <div className="grid sm:grid-cols-3 gap-4">
        <div className="space-y-2">
          <Label>Premium / ha (₹)</Label>
          <Input type="number" min={0.01} step="0.01" value={form.premiumRatePerHectare} onChange={(e) => set("premiumRatePerHectare", parseFloat(e.target.value))} required />
        </div>
        <div className="space-y-2">
          <Label>Sum insured / ha (₹)</Label>
          <Input type="number" min={0.01} step="0.01" value={form.sumInsuredPerHectare} onChange={(e) => set("sumInsuredPerHectare", parseFloat(e.target.value))} required />
        </div>
        <div className="space-y-2">
          <Label>Coverage %</Label>
          <Input type="number" min={1} max={100} step="0.1" value={form.coveragePercentage} onChange={(e) => set("coveragePercentage", parseFloat(e.target.value))} required />
        </div>
      </div>
      <div className="grid sm:grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label>Min area (ha)</Label>
          <Input type="number" min={0.01} step="0.01" value={form.minAreaInHectares ?? ""} onChange={(e) => set("minAreaInHectares", e.target.value ? parseFloat(e.target.value) : null)} />
        </div>
        <div className="space-y-2">
          <Label>Max area (ha)</Label>
          <Input type="number" min={0.01} step="0.01" value={form.maxAreaInHectares ?? ""} onChange={(e) => set("maxAreaInHectares", e.target.value ? parseFloat(e.target.value) : null)} />
        </div>
      </div>
      <label className="flex items-center gap-2 cursor-pointer">
        <input
          type="checkbox"
          checked={form.isActive}
          onChange={(e) => set("isActive", e.target.checked)}
          className="h-4 w-4 rounded border-input"
        />
        <span className="text-sm">Active (visible to farmers)</span>
      </label>
      <DialogFooter>
        <Button type="submit" disabled={saving} className="bg-emerald-700 hover:bg-emerald-800 text-white">
          {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : initial ? "Save changes" : "Create plan"}
        </Button>
      </DialogFooter>
    </form>
  );
}

// ============== ADMIN USERS ==============
function AdminUsersPage() {
  const [users, setUsers] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [actionUser, setActionUser] = useState<any>(null);
  const [actionType, setActionType] = useState<"suspend" | "activate" | "block">("suspend");
  const [reason, setReason] = useState("");
  const [actionLoading, setActionLoading] = useState(false);
  const [userDetail, setUserDetail] = useState<any>(null);
  const [tab, setTab] = useState<"all" | "farmers" | "admins">("all");
  const [farmers, setFarmers] = useState<any[]>([]);
  const [farmersLoading, setFarmersLoading] = useState(true);
  const [farmerDetail, setFarmerDetail] = useState<any>(null);
  const [farmerDetailLoading, setFarmerDetailLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [farmersPage, setFarmersPage] = useState(1);
  const [farmersTotalPages, setFarmersTotalPages] = useState(1);

  const loadUsers = (p: number, searchTerm: string, role?: string) => {
    setLoading(true);
    adminApi.listUsers({ page: p, pageSize: 20, searchTerm: searchTerm || undefined, role })
      .then((res) => {
        setUsers(res.items);
        setTotalPages(res.totalPages);
      })
      .finally(() => setLoading(false));
  };

  const debounceRef = useRef<ReturnType<typeof setTimeout>>();

  const load = (immediate?: boolean) => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    const doLoad = () => {
      setPage(1);
      loadUsers(1, search, tab === "admins" ? "Admin" : undefined);
    };
    if (immediate) {
      doLoad();
    } else {
      debounceRef.current = setTimeout(doLoad, 400);
    }
  };
  useEffect(() => { load(true); }, [tab]);
  useEffect(() => { load(); }, [search]);

  const debounceRefFarmers = useRef<ReturnType<typeof setTimeout>>();

  const loadFarmers = (p: number, searchTerm: string) => {
    setFarmersLoading(true);
    adminApi.listFarmers({ page: p, pageSize: 20, searchTerm: searchTerm || undefined })
      .then((res) => {
        setFarmers(res.items);
        setFarmersTotalPages(res.totalPages);
      })
      .finally(() => setFarmersLoading(false));
  };

  useEffect(() => {
    if (debounceRefFarmers.current) clearTimeout(debounceRefFarmers.current);
    debounceRefFarmers.current = setTimeout(() => {
      setFarmersPage(1);
      loadFarmers(1, search);
    }, 400);
  }, [search]);

  const loadFarmerDetail = async (id: string) => {
    setFarmerDetailLoading(true);
    try {
      const f = await adminApi.getFarmer(id);
      setFarmerDetail(f);
    } catch {
      toast.error("Failed to load farmer details");
    } finally {
      setFarmerDetailLoading(false);
    }
  };

  const doAction = async () => {
    if (!actionUser || actionLoading) return;
    setActionLoading(true);
    try
    {
      if (actionType === "suspend") await adminApi.suspendUser(actionUser.id, reason);
      if (actionType === "activate") await adminApi.activateUser(actionUser.id);
      if (actionType === "block") await adminApi.blockUser(actionUser.id, reason);
      toast.success(`User ${actionType}d`);
      setActionUser(null);
      setReason("");
      loadUsers(page, search, tab === "admins" ? "Admin" : undefined);
    }
    catch (err: any)
    {
      toast.error(err?.message || `Failed to ${actionType} user`);
    }
    finally
    {
      setActionLoading(false);
    }
  };

  const tabs = [
    { key: "all" as const, label: "All Users" },
    { key: "farmers" as const, label: "Farmers" },
    { key: "admins" as const, label: "Admins" },
  ];

  return (
    <div>
      <PageHeader title="User Management" subtitle="Manage farmer and admin accounts." />

      <div className="flex items-center gap-2 mb-5">
        {tabs.map((t) => (
          <Button
            key={t.key}
            variant={tab === t.key ? "default" : "outline"}
            size="sm"
            onClick={() => { setTab(t.key); setPage(1); setSearch(""); }}
            className={tab === t.key ? "bg-emerald-700 hover:bg-emerald-800 text-white" : ""}
          >
            {t.label}
          </Button>
        ))}
      </div>

      <div className="mb-5">
        <Input
          placeholder={`Search ${tab === "farmers" ? "farmers" : "users"} by name or email...`}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="h-11 max-w-md"
        />
      </div>

      {tab === "farmers" ? (
        farmersLoading ? (
          <div className="space-y-2">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-16 rounded-lg" />
            ))}
          </div>
        ) : farmers.length === 0 ? (
          <div className="py-16 text-center text-muted-foreground">No farmers found.</div>
        ) : (
          <>
            <Card>
              <CardContent className="p-0">
                <div className="divide-y">
                  {farmers.map((f) => (
                    <div
                      key={f.id}
                      className="flex items-center gap-4 p-4 hover:bg-muted/40 cursor-pointer"
                      onClick={() => loadFarmerDetail(f.id)}
                    >
                      <Avatar className="h-10 w-10 bg-gradient-to-br from-emerald-500 to-green-700 text-white shrink-0">
                        <AvatarFallback className="bg-transparent text-white text-xs font-semibold">
                          {initials(f.firstName, f.lastName)}
                        </AvatarFallback>
                      </Avatar>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="font-medium">{f.firstName} {f.lastName}</span>
                          <span className="text-xs text-muted-foreground">{f.email}</span>
                        </div>
                        <div className="text-xs text-muted-foreground mt-0.5">
                          {f.phoneNumber ?? "No phone"} · {f.totalFarms} farms · {f.totalPolicies} policies · {f.totalClaims} claims
                        </div>
                      </div>
                      <Button variant="ghost" size="sm">View →</Button>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
            {farmersTotalPages > 1 && (
              <div className="flex items-center justify-between mt-4 text-sm">
                <span className="text-muted-foreground">Page {farmersPage} of {farmersTotalPages}</span>
                <div className="flex gap-2">
                  <Button size="sm" variant="outline" disabled={farmersPage <= 1} onClick={() => { setFarmersPage(p => p - 1); loadFarmers(farmersPage - 1, search); }}>Previous</Button>
                  <Button size="sm" variant="outline" disabled={farmersPage >= farmersTotalPages} onClick={() => { setFarmersPage(p => p + 1); loadFarmers(farmersPage + 1, search); }}>Next</Button>
                </div>
              </div>
            )}
          </>
        )
      ) : loading ? (
        <div className="space-y-2">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-16 rounded-lg" />
          ))}
        </div>
      ) : users.length === 0 ? (
        <div className="py-16 text-center text-muted-foreground">No users found.</div>
      ) : (
        <>
          <Card>
            <CardContent className="p-0">
              <div className="divide-y">
                {users.map((u) => (
                  <div key={u.id} className="flex items-center gap-4 p-4 hover:bg-muted/40 cursor-pointer" onClick={() => setUserDetail(u)}>
                    <Avatar className="h-10 w-10 bg-gradient-to-br from-emerald-500 to-green-700 text-white shrink-0">
                      <AvatarFallback className="bg-transparent text-white text-xs font-semibold">
                        {initials(u.firstName, u.lastName)}
                      </AvatarFallback>
                    </Avatar>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{u.firstName} {u.lastName}</span>
                        <Badge variant="outline" className={u.role === "Admin" ? "border-emerald-300 text-emerald-700" : ""}>
                          {u.role}
                        </Badge>
                        <StatusBadge status={u.status} />
                      </div>
                      <div className="text-xs text-muted-foreground truncate">
                        {u.email}
                      </div>
                    </div>
                    <div className="flex gap-1.5 shrink-0">
                      {u.status === "Active" && u.role !== "Admin" && (
                        <Button size="sm" variant="outline" onClick={(e) => { e.stopPropagation(); setActionUser(u); setActionType("suspend"); }} className="text-amber-700 hover:bg-amber-50">Suspend</Button>
                      )}
                      {u.status === "Suspended" && (
                        <Button size="sm" variant="outline" onClick={(e) => { e.stopPropagation(); setActionUser(u); setActionType("activate"); }} className="text-emerald-700 hover:bg-emerald-50">Activate</Button>
                      )}
                      {u.status !== "Blocked" && u.role !== "Admin" && (
                        <Button size="sm" variant="outline" onClick={(e) => { e.stopPropagation(); setActionUser(u); setActionType("block"); }} className="text-rose-600 hover:bg-rose-50">Block</Button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
          {totalPages > 1 && (
            <div className="flex items-center justify-between mt-4 text-sm">
              <span className="text-muted-foreground">Page {page} of {totalPages}</span>
              <div className="flex gap-2">
                <Button size="sm" variant="outline" disabled={page <= 1} onClick={() => { setPage(p => p - 1); loadUsers(page - 1, search, tab === "admins" ? "Admin" : undefined); }}>Previous</Button>
                <Button size="sm" variant="outline" disabled={page >= totalPages} onClick={() => { setPage(p => p + 1); loadUsers(page + 1, search, tab === "admins" ? "Admin" : undefined); }}>Next</Button>
              </div>
            </div>
          )}
        </>
      )}

      <Dialog open={!!userDetail} onOpenChange={(o) => { if (!o) setUserDetail(null); }}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>User Details</DialogTitle>
          </DialogHeader>
          {userDetail && (
            <div className="space-y-5">
              <div className="text-center">
                <Avatar className="h-16 w-16 bg-gradient-to-br from-emerald-500 to-green-700 text-white mx-auto">
                  <AvatarFallback className="bg-transparent text-white text-lg font-semibold">
                    {initials(userDetail.firstName, userDetail.lastName)}
                  </AvatarFallback>
                </Avatar>
                <h3 className="font-serif text-lg font-semibold mt-3">{userDetail.firstName} {userDetail.lastName}</h3>
                <p className="text-sm text-muted-foreground">{userDetail.email}</p>
                <div className="flex items-center justify-center gap-2 mt-2">
                  <Badge variant="outline" className={userDetail.role === "Admin" ? "border-emerald-300 text-emerald-700" : ""}>
                    {userDetail.role}
                  </Badge>
                  <StatusBadge status={userDetail.status} />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3 text-sm border-t pt-4">
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Phone</div>
                  <div className="font-medium mt-1">{userDetail.phoneNumber ?? "—"}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Joined</div>
                  <div className="font-medium mt-1">{formatDate(userDetail.createdAt)}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Last login</div>
                  <div className="font-medium mt-1">{userDetail.lastLoginAt ? formatRelative(userDetail.lastLoginAt) : "—"}</div>
                </div>
              </div>
              <div className="flex gap-2 pt-3 border-t">
                {userDetail.status === "Active" && userDetail.role !== "Admin" && (
                  <Button size="sm" className="flex-1 bg-amber-600 hover:bg-amber-700 text-white" onClick={() => { setUserDetail(null); setActionUser(userDetail); setActionType("suspend"); }}>
                    Suspend
                  </Button>
                )}
                {userDetail.status === "Suspended" && (
                  <Button size="sm" className="flex-1 bg-emerald-700 hover:bg-emerald-800 text-white" onClick={() => { setUserDetail(null); setActionUser(userDetail); setActionType("activate"); }}>
                    Activate
                  </Button>
                )}
                {userDetail.status !== "Blocked" && userDetail.role !== "Admin" && (
                  <Button size="sm" className="flex-1 bg-rose-600 hover:bg-rose-700 text-white" onClick={() => { setUserDetail(null); setActionUser(userDetail); setActionType("block"); }}>
                    Block
                  </Button>
                )}
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={!!actionUser} onOpenChange={(o) => !o && setActionUser(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="capitalize">{actionType} {actionUser?.firstName} {actionUser?.lastName}?</DialogTitle>
          </DialogHeader>
          {(actionType === "suspend" || actionType === "block") && (
            <div className="space-y-2">
              <Label>Reason</Label>
              <Textarea value={reason} onChange={(e) => setReason(e.target.value)} rows={3} placeholder="Reason for this action..." />
            </div>
          )}
          {actionType === "activate" && (
            <p className="text-sm text-muted-foreground">This user will regain full access to their account immediately.</p>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setActionUser(null)}>Cancel</Button>
            <Button onClick={doAction} disabled={(actionType !== "activate" && !reason) || actionLoading} className={cn(
              actionType === "activate" ? "bg-emerald-700 hover:bg-emerald-800 text-white" : "",
              actionType === "suspend" ? "bg-amber-600 hover:bg-amber-700 text-white" : "",
              actionType === "block" ? "bg-rose-600 hover:bg-rose-700 text-white" : ""
            )}>{actionLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : `Confirm ${actionType}`}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={!!farmerDetail} onOpenChange={(o) => { if (!o) setFarmerDetail(null); }}>
        <DialogContent className="max-w-lg max-h-[85vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Farmer Profile</DialogTitle>
          </DialogHeader>
          {farmerDetailLoading ? (
            <Skeleton className="h-64 rounded-xl" />
          ) : farmerDetail ? (
            <div className="space-y-6">
              <div className="text-center">
                <Avatar className="h-20 w-20 bg-gradient-to-br from-emerald-500 to-green-700 text-white mx-auto">
                  <AvatarFallback className="bg-transparent text-white text-2xl font-serif">
                    {initials(farmerDetail.firstName, farmerDetail.lastName)}
                  </AvatarFallback>
                </Avatar>
                <h3 className="font-serif text-xl font-semibold mt-3">{farmerDetail.firstName} {farmerDetail.lastName}</h3>
                <p className="text-sm text-muted-foreground">{farmerDetail.email}</p>
              </div>
              <div className="grid grid-cols-3 gap-2">
                <div className="text-center p-3 rounded-lg bg-emerald-50">
                  <div className="text-2xl font-bold font-serif text-emerald-700">{farmerDetail.totalFarms}</div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Farms</div>
                </div>
                <div className="text-center p-3 rounded-lg bg-emerald-50">
                  <div className="text-2xl font-bold font-serif text-emerald-700">{farmerDetail.totalPolicies}</div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Policies</div>
                </div>
                <div className="text-center p-3 rounded-lg bg-emerald-50">
                  <div className="text-2xl font-bold font-serif text-emerald-700">{farmerDetail.totalClaims}</div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Claims</div>
                </div>
              </div>
              <div className="grid sm:grid-cols-2 gap-3 text-sm border-t pt-4">
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Phone</div>
                  <div className="font-medium mt-1">{farmerDetail.phoneNumber ?? "—"}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Joined</div>
                  <div className="font-medium mt-1">{formatDate(farmerDetail.createdAt)}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Last login</div>
                  <div className="font-medium mt-1">{farmerDetail.lastLoginAt ? formatDate(farmerDetail.lastLoginAt) : "—"}</div>
                </div>
              </div>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground text-center py-4">Farmer not found.</p>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}

// ============== ADMIN AUDIT LOGS ==============
function AdminAuditPage() {
  const [logs, setLogs] = useState<AuditLogDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [detailLogId, setDetailLogId] = useState<string | null>(null);
  const [logDetail, setLogDetail] = useState<AuditLogDto | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  useEffect(() => {
    adminApi.auditLogs().then(setLogs).finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!detailLogId) { setLogDetail(null); return; }
    setDetailLoading(true);
    adminApi.getAuditLog(detailLogId).then(setLogDetail).finally(() => setDetailLoading(false));
  }, [detailLogId]);

  const filtered = logs.filter(
    (l) =>
      search === "" ||
      l.userName.toLowerCase().includes(search.toLowerCase()) ||
      l.action.toLowerCase().includes(search.toLowerCase()) ||
      (l.details ?? "").toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div>
      <PageHeader title="Audit Logs" subtitle="Immutable trail of all admin actions." />
      <div className="mb-5">
        <Input
          placeholder="Search by user, action or details..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="h-11 max-w-md"
        />
      </div>

      {loading ? (
        <div className="space-y-2">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-16 rounded-lg" />
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="p-0">
            <div className="divide-y">
              {filtered.map((log) => (
                <div
                  key={log.id}
                  className="flex items-start gap-4 p-4 hover:bg-muted/40 cursor-pointer"
                  onClick={() => setDetailLogId(log.id)}
                >
                  <div className="h-9 w-9 rounded-lg bg-emerald-100 grid place-items-center shrink-0">
                    <ScrollText className="h-4 w-4 text-emerald-700" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <Badge variant="outline" className="font-mono text-[10px]">
                        {log.action}
                      </Badge>
                      <span className="text-sm font-medium">{log.userName}</span>
                      {log.userRole && (
                        <Badge variant="secondary" className="text-[9px] font-mono">{log.userRole}</Badge>
                      )}
                      <span className="text-xs text-muted-foreground">
                        on {log.resourceType}
                      </span>
                    </div>
                    <p className="text-sm text-foreground/90 mt-1">{log.details}</p>
                    <div className="text-xs text-muted-foreground mt-1 flex gap-3">
                      <span>{formatDate(log.timestamp, { dateStyle: "medium", timeStyle: "short" } as Intl.DateTimeFormatOptions)}</span>
                      <span>IP: {log.ipAddress}</span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      <Dialog open={!!detailLogId} onOpenChange={(o) => { if (!o) setDetailLogId(null); }}>
        <DialogContent className="max-w-2xl max-h-[85vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <ScrollText className="h-5 w-5 text-emerald-600" />
              Audit Log Detail
            </DialogTitle>
          </DialogHeader>
          {detailLoading ? (
            <div className="space-y-4">
              <Skeleton className="h-5 w-1/3" />
              <Skeleton className="h-20 w-full" />
              <Skeleton className="h-20 w-full" />
              <Skeleton className="h-20 w-full" />
            </div>
          ) : logDetail ? (
            <div className="space-y-5">
              {/* Action badge + description */}
              <div className="flex items-start gap-3">
                <Badge variant="outline" className="font-mono text-[10px] shrink-0">{logDetail.action}</Badge>
                {logDetail.details && (
                  <p className="text-sm text-foreground/80 leading-relaxed">{logDetail.details}</p>
                )}
              </div>

              {/* WHO section */}
              <div className="rounded-lg border bg-muted/30">
                <div className="px-4 py-2.5 border-b flex items-center gap-2">
                  <User className="h-3.5 w-3.5 text-muted-foreground" />
                  <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Who</span>
                </div>
                <div className="grid grid-cols-2 gap-px bg-border/50">
                  <div className="bg-background px-4 py-3">
                    <div className="text-[11px] text-muted-foreground uppercase tracking-wide">User</div>
                    <div className="text-sm font-medium mt-1">{logDetail.userName || "—"}</div>
                  </div>
                  <div className="bg-background px-4 py-3">
                    <div className="text-[11px] text-muted-foreground uppercase tracking-wide">Role</div>
                    <div className="text-sm font-medium mt-1">
                      {logDetail.userRole ? (
                        <Badge variant="secondary" className="text-[10px] font-mono">{logDetail.userRole}</Badge>
                      ) : "—"}
                    </div>
                  </div>
                  <div className="bg-background px-4 py-3">
                    <div className="text-[11px] text-muted-foreground uppercase tracking-wide">IP Address</div>
                    <div className="text-sm font-mono mt-1">{logDetail.ipAddress || "—"}</div>
                  </div>
                  <div className="bg-background px-4 py-3">
                    <div className="text-[11px] text-muted-foreground uppercase tracking-wide">User Agent</div>
                    <div className="text-[11px] font-mono mt-1 text-muted-foreground truncate" title={logDetail.userAgent ?? ""}>
                      {logDetail.userAgent || "—"}
                    </div>
                  </div>
                </div>
              </div>

              {/* WHAT section */}
              <div className="rounded-lg border bg-muted/30">
                <div className="px-4 py-2.5 border-b flex items-center gap-2">
                  <FileText className="h-3.5 w-3.5 text-muted-foreground" />
                  <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">What</span>
                </div>
                <div className="grid grid-cols-2 gap-px bg-border/50">
                  <div className="bg-background px-4 py-3">
                    <div className="text-[11px] text-muted-foreground uppercase tracking-wide">Resource</div>
                    <div className="text-sm font-medium mt-1">{logDetail.resourceType}</div>
                  </div>
                  <div className="bg-background px-4 py-3">
                    <div className="text-[11px] text-muted-foreground uppercase tracking-wide">Resource ID</div>
                    <div className="text-sm font-mono mt-1 truncate" title={logDetail.resourceId ?? ""}>
                      {logDetail.resourceId ? (
                        <span className="text-xs">{logDetail.resourceId.length > 20 ? logDetail.resourceId.slice(0, 8) + "…" + logDetail.resourceId.slice(-6) : logDetail.resourceId}</span>
                      ) : "—"}
                    </div>
                  </div>
                  <div className="bg-background px-4 py-3 col-span-2">
                    <div className="text-[11px] text-muted-foreground uppercase tracking-wide">Timestamp</div>
                    <div className="text-sm font-medium mt-1">{formatDate(logDetail.timestamp, { dateStyle: "full", timeStyle: "medium" } as Intl.DateTimeFormatOptions)}</div>
                  </div>
                </div>
              </div>

              {/* CHANGES section — only if oldValues or newValues exist */}
              {(logDetail.oldValues || logDetail.newValues) && (() => {
                let oldVals: Record<string, any> = {};
                let newVals: Record<string, any> = {};
                try { if (logDetail.oldValues) oldVals = JSON.parse(logDetail.oldValues); } catch {}
                try { if (logDetail.newValues) newVals = JSON.parse(logDetail.newValues); } catch {}
                const allKeys = [...new Set([...Object.keys(oldVals), ...Object.keys(newVals)])];
                if (allKeys.length === 0) return null;
                return (
                  <div className="rounded-lg border bg-muted/30">
                    <div className="px-4 py-2.5 border-b flex items-center gap-2">
                      <ArrowRight className="h-3.5 w-3.5 text-muted-foreground" />
                      <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Changes</span>
                      {logDetail.changedColumns && (
                        <Badge variant="outline" className="text-[9px] font-mono ml-auto">{logDetail.changedColumns}</Badge>
                      )}
                    </div>
                    <div className="overflow-x-auto">
                      <table className="w-full text-sm">
                        <thead>
                          <tr className="border-b bg-muted/50">
                            <th className="text-left px-4 py-2 text-[11px] font-semibold text-muted-foreground uppercase tracking-wide w-1/4">Field</th>
                            <th className="text-left px-4 py-2 text-[11px] font-semibold text-muted-foreground uppercase tracking-wide w-[37.5%]">Before</th>
                            <th className="text-left px-4 py-2 text-[11px] font-semibold text-muted-foreground uppercase tracking-wide w-[37.5%]">After</th>
                          </tr>
                        </thead>
                        <tbody>
                          {allKeys.map((key) => {
                            const oldVal = oldVals[key];
                            const newVal = newVals[key];
                            const isChanged = JSON.stringify(oldVal) !== JSON.stringify(newVal);
                            return (
                              <tr key={key} className={`border-b last:border-0 ${isChanged ? "" : "opacity-50"}`}>
                                <td className="px-4 py-2 font-mono text-xs font-medium">{key}</td>
                                <td className="px-4 py-2">
                                  {oldVal !== undefined && oldVal !== null ? (
                                    <span className="text-xs text-red-600 bg-red-50 px-1.5 py-0.5 rounded line-through">
                                      {typeof oldVal === "object" ? JSON.stringify(oldVal) : String(oldVal)}
                                    </span>
                                  ) : (
                                    <span className="text-xs text-muted-foreground">—</span>
                                  )}
                                </td>
                                <td className="px-4 py-2">
                                  {newVal !== undefined && newVal !== null ? (
                                    <span className="text-xs text-emerald-700 bg-emerald-50 px-1.5 py-0.5 rounded font-medium">
                                      {typeof newVal === "object" ? JSON.stringify(newVal) : String(newVal)}
                                    </span>
                                  ) : (
                                    <span className="text-xs text-muted-foreground">—</span>
                                  )}
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                    </div>
                  </div>
                );
              })()}

              {/* REQUEST section */}
              {(logDetail.httpMethod || logDetail.httpPath || logDetail.correlationId) && (
                <div className="rounded-lg border bg-muted/30">
                  <div className="px-4 py-2.5 border-b flex items-center gap-2">
                    <Globe className="h-3.5 w-3.5 text-muted-foreground" />
                    <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Request</span>
                  </div>
                  <div className="grid grid-cols-2 gap-px bg-border/50">
                    {logDetail.httpMethod && (
                      <div className="bg-background px-4 py-3">
                        <div className="text-[11px] text-muted-foreground uppercase tracking-wide">Method</div>
                        <div className="text-sm font-mono font-bold mt-1">{logDetail.httpMethod}</div>
                      </div>
                    )}
                    {logDetail.httpPath && (
                      <div className={`bg-background px-4 py-3 ${logDetail.httpMethod ? "" : "col-span-2"}`}>
                        <div className="text-[11px] text-muted-foreground uppercase tracking-wide">Path</div>
                        <div className="text-xs font-mono mt-1 text-muted-foreground break-all">{logDetail.httpPath}</div>
                      </div>
                    )}
                    {logDetail.correlationId && (
                      <div className="bg-background px-4 py-3 col-span-2">
                        <div className="text-[11px] text-muted-foreground uppercase tracking-wide">Correlation ID</div>
                        <div className="text-xs font-mono mt-1 text-muted-foreground break-all">{logDetail.correlationId}</div>
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">Log not found.</p>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}

// ============== ADMIN POLICIES ==============
function AdminPoliciesPage() {
  const [policies, setPolicies] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [tab, setTab] = useState<"pending" | "payment_received" | "active" | "rejected" | "all">("pending");
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const [searchId, setSearchId] = useState("");
  const [lookupPolicy, setLookupPolicy] = useState<any | null>(null);
  const [lookupLoading, setLookupLoading] = useState(false);

  const [selectedPolicy, setSelectedPolicy] = useState<any | null>(null);
  const [approveOpen, setApproveOpen] = useState(false);
  const [rejectOpen, setRejectOpen] = useState(false);
  const [cancelOpen, setCancelOpen] = useState(false);
  const [rejectReason, setRejectReason] = useState("");
  const [cancelReason, setCancelReason] = useState("");
  const [busy, setBusy] = useState(false);

  const loadPolicies = async (p: number, statusFilter?: string) => {
    setLoading(true);
    try {
      const params: any = { page: p, pageSize: 15 };
      if (statusFilter && statusFilter !== "all") params.status = statusFilter;
      const result = await adminApi.listPolicies(params);
      setPolicies(result.items || []);
      setTotalPages(result.totalPages || 1);
      setTotalCount(result.totalCount || 0);
    } catch {
      toast.error("Failed to load policies");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    setPage(1);
    loadPolicies(1, tab);
  }, [tab]);

  const lookup = async () => {
    if (!searchId.trim()) return;
    setLookupLoading(true);
    setLookupPolicy(null);
    try {
      const result = await policiesApi.get(searchId.trim());
      setLookupPolicy(result);
    } catch {
      toast.error("Policy not found. Check the ID and try again.");
    } finally {
      setLookupLoading(false);
    }
  };

  const doApprove = async () => {
    if (!selectedPolicy) return;
    setBusy(true);
    try {
      await adminApi.approvePolicy(selectedPolicy.id);
      toast.success("Policy approved and activated");
      setPolicies((prev) => prev.filter((p) => p.id !== selectedPolicy.id));
      setTotalCount((c) => c - 1);
      setApproveOpen(false);
      setSelectedPolicy(null);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Failed to approve");
    } finally {
      setBusy(false);
    }
  };

  const doReject = async () => {
    if (!selectedPolicy) return;
    setBusy(true);
    try {
      await adminApi.rejectPolicy(selectedPolicy.id, rejectReason);
      toast.success("Policy rejected" + (selectedPolicy.paymentStatus === "Paid" ? " (refund initiated)" : ""));
      setPolicies((prev) => prev.filter((p) => p.id !== selectedPolicy.id));
      setTotalCount((c) => c - 1);
      setRejectOpen(false);
      setSelectedPolicy(null);
      setRejectReason("");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Failed to reject");
    } finally {
      setBusy(false);
    }
  };

  const doCancel = async () => {
    if (!selectedPolicy) return;
    setBusy(true);
    try {
      await adminApi.cancelPolicy(selectedPolicy.id, cancelReason);
      toast.success("Policy cancelled" + (selectedPolicy.paymentStatus === "Paid" ? " (refund initiated)" : ""));
      setPolicies((prev) => prev.filter((p) => p.id !== selectedPolicy.id));
      setTotalCount((c) => c - 1);
      setCancelOpen(false);
      setSelectedPolicy(null);
      setCancelReason("");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Failed to cancel");
    } finally {
      setBusy(false);
    }
  };

  const tabs = [
    { key: "pending" as const, label: "Pending", count: tab === "pending" ? totalCount : undefined },
    { key: "payment_received" as const, label: "Paid (Awaiting)" },
    { key: "active" as const, label: "Active" },
    { key: "rejected" as const, label: "Rejected" },
    { key: "all" as const, label: "All" },
  ];

  return (
    <div>
      <PageHeader
        title="Policy Management"
        subtitle="Review, approve or reject crop insurance policies."
      />

      {/* Tab filter */}
      <div className="flex items-center gap-2 mb-4">
        {tabs.map((t) => (
          <Button
            key={t.key}
            size="sm"
            variant={tab === t.key ? "default" : "outline"}
            onClick={() => setTab(t.key)}
            className={tab === t.key ? "bg-emerald-700 hover:bg-emerald-800 text-white" : ""}
          >
            {t.label}
            {t.count !== undefined && (
              <Badge className="ml-1.5 bg-white/20 text-inherit" variant="secondary">{t.count}</Badge>
            )}
          </Button>
        ))}
      </div>

      {/* Policies table */}
      <Card>
        <CardContent className="p-0">
          {loading ? (
            <div className="p-6 space-y-3">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-12 w-full" />
              ))}
            </div>
          ) : policies.length === 0 ? (
            <div className="p-12 text-center text-muted-foreground">
              No {tab === "all" ? "" : tab} policies found.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="text-left p-3 font-medium">Farmer</th>
                    <th className="text-left p-3 font-medium">Policy #</th>
                    <th className="text-left p-3 font-medium">Crop</th>
                    <th className="text-left p-3 font-medium">Premium</th>
                    <th className="text-left p-3 font-medium">Sum Insured</th>
                    <th className="text-left p-3 font-medium">Start</th>
                    <th className="text-left p-3 font-medium">Status</th>
                    <th className="text-left p-3 font-medium">Payment</th>
                    {(tab === "pending" || tab === "payment_received" || tab === "active") && (
                      <th className="text-right p-3 font-medium">Actions</th>
                    )}
                  </tr>
                </thead>
                <tbody>
                  {policies.map((p) => (
                    <tr key={p.id} className="border-b last:border-0 hover:bg-muted/30">
                      <td className="p-3">
                        <div className="font-medium">{p.farmerName}</div>
                        <div className="text-xs text-muted-foreground">{p.farmerEmail}</div>
                      </td>
                      <td className="p-3 font-mono text-xs">{p.policyNumber}</td>
                      <td className="p-3">{p.cropType}</td>
                      <td className="p-3">{formatINR(p.premium)}</td>
                      <td className="p-3 text-emerald-700 font-medium">{formatINR(p.sumInsured)}</td>
                      <td className="p-3 text-xs">{formatDate(p.startDate)}</td>
                      <td className="p-3"><StatusBadge status={p.status} /></td>
                      <td className="p-3">
                        <Badge
                          variant={p.paymentStatus === "Paid" ? "default" : "outline"}
                          className={p.paymentStatus === "Paid"
                            ? "bg-emerald-100 text-emerald-800 border-emerald-200"
                            : "text-amber-700 border-amber-200"}
                        >
                          {p.paymentStatus}
                        </Badge>
                      </td>
                      {(tab === "pending" || tab === "payment_received" || tab === "active") && (
                        <td className="p-3 text-right">
                          <div className="flex gap-1 justify-end">
                            {(tab === "pending" || tab === "payment_received") && (
                              <>
                                <Button
                                  size="sm"
                                  className="bg-emerald-700 hover:bg-emerald-800 text-white gap-1 h-8"
                                  onClick={() => { setSelectedPolicy(p); setApproveOpen(true); }}
                                >
                                  <CheckCircle2 className="h-3.5 w-3.5" /> Approve
                                </Button>
                                <Button
                                  size="sm"
                                  variant="outline"
                                  className="text-rose-600 hover:bg-rose-50 border-rose-200 gap-1 h-8"
                                  onClick={() => { setSelectedPolicy(p); setRejectOpen(true); }}
                                >
                                  <XCircle className="h-3.5 w-3.5" /> Reject
                                </Button>
                              </>
                            )}
                            {tab === "active" && (
                              <Button
                                size="sm"
                                variant="outline"
                                className="text-amber-600 hover:bg-amber-50 border-amber-200 gap-1 h-8"
                                onClick={() => { setSelectedPolicy(p); setCancelOpen(true); }}
                              >
                                <Power className="h-3.5 w-3.5" /> Cancel
                              </Button>
                            )}
                          </div>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex justify-between items-center mt-4">
          <span className="text-sm text-muted-foreground">Page {page} of {totalPages}</span>
          <div className="flex gap-2">
            <Button size="sm" variant="outline" disabled={page <= 1} onClick={() => { setPage(page - 1); loadPolicies(page - 1, tab); }}>Previous</Button>
            <Button size="sm" variant="outline" disabled={page >= totalPages} onClick={() => { setPage(page + 1); loadPolicies(page + 1, tab); }}>Next</Button>
          </div>
        </div>
      )}

      {/* Manual lookup section */}
      <div className="mt-8">
        <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide mb-3">Look up by Policy ID</h3>
        <div className="flex gap-3">
          <div className="relative flex-1 max-w-md">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Enter policy ID..."
              value={searchId}
              onChange={(e) => setSearchId(e.target.value)}
              className="pl-9 h-11"
              onKeyDown={(e) => { if (e.key === "Enter") lookup(); }}
            />
          </div>
          <Button onClick={lookup} disabled={lookupLoading || !searchId.trim()} className="h-11">
            {lookupLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : "Search"}
          </Button>
        </div>

        {lookupPolicy && (
          <Card className="mt-4">
            <CardContent className="p-6">
              <div className="flex items-start justify-between mb-4">
                <div>
                  <h4 className="font-serif text-lg font-semibold">{lookupPolicy.cropType} · {lookupPolicy.provider}</h4>
                  <p className="text-sm text-muted-foreground">Policy #{lookupPolicy.policyNumber} · Farm: {lookupPolicy.farmName}</p>
                </div>
                <div className="flex items-center gap-2">
                  <Badge
                    variant={lookupPolicy.paymentStatus === "Paid" ? "default" : "outline"}
                    className={lookupPolicy.paymentStatus === "Paid"
                      ? "bg-emerald-100 text-emerald-800 border-emerald-200"
                      : "text-amber-700 border-amber-200"}
                  >
                    {lookupPolicy.paymentStatus}
                  </Badge>
                  <StatusBadge status={lookupPolicy.status} />
                </div>
              </div>
              <div className="grid sm:grid-cols-3 gap-4 text-sm mb-4">
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Coverage</div>
                  <div className="font-semibold mt-1">{formatINR(lookupPolicy.coverageAmount)}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Premium</div>
                  <div className="font-semibold mt-1">{formatINR(lookupPolicy.premium)}</div>
                </div>
                <div>
                  <div className="text-xs text-muted-foreground uppercase tracking-wide">Sum insured</div>
                  <div className="font-semibold mt-1 text-emerald-700">{formatINR(lookupPolicy.sumInsured)}</div>
                </div>
              </div>
              {(lookupPolicy.status === "Pending" || lookupPolicy.status === "PaymentReceived") && (
                <div className="flex gap-2 pt-4 border-t">
                  <Button
                    onClick={() => { setSelectedPolicy(lookupPolicy); setApproveOpen(true); }}
                    className="bg-emerald-700 hover:bg-emerald-800 text-white gap-1.5"
                  >
                    <CheckCircle2 className="h-4 w-4" /> Approve policy
                  </Button>
                  <Button
                    onClick={() => { setSelectedPolicy(lookupPolicy); setRejectOpen(true); }}
                    variant="outline"
                    className="text-rose-600 hover:bg-rose-50 border-rose-200 gap-1.5"
                  >
                    <XCircle className="h-4 w-4" /> Reject policy
                  </Button>
                </div>
              )}
              {lookupPolicy.status === "Active" && (
                <div className="flex gap-2 pt-4 border-t">
                  {lookupPolicy.approvedByName && (
                    <span className="text-sm text-muted-foreground mr-auto">
                      Approved by {lookupPolicy.approvedByName} on {formatDate(lookupPolicy.approvedAt)}
                    </span>
                  )}
                  <Button
                    onClick={() => { setSelectedPolicy(lookupPolicy); setCancelOpen(true); }}
                    variant="outline"
                    className="text-amber-600 hover:bg-amber-50 border-amber-200 gap-1.5"
                  >
                    <Power className="h-4 w-4" /> Cancel policy
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>
        )}
      </div>

      {/* Approve dialog */}
      <Dialog open={approveOpen} onOpenChange={setApproveOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Approve policy</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            This will activate policy <strong>#{selectedPolicy?.policyNumber}</strong> for {selectedPolicy?.farmName}.
          </p>
          <DialogFooter>
            <Button variant="outline" onClick={() => setApproveOpen(false)}>Cancel</Button>
            <Button onClick={doApprove} disabled={busy} className="bg-emerald-700 hover:bg-emerald-800 text-white">
              {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : "Confirm approve"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Reject dialog */}
      <Dialog open={rejectOpen} onOpenChange={setRejectOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reject policy</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <Label>Rejection reason</Label>
            <Textarea
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              rows={3}
              placeholder="Enter reason for rejection..."
              autoFocus
            />
            {selectedPolicy?.paymentStatus === "Paid" && (
              <p className="text-xs text-amber-600 bg-amber-50 p-2 rounded">
                This policy has a confirmed payment. A refund will be initiated automatically.
              </p>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setRejectOpen(false)}>Cancel</Button>
            <Button onClick={doReject} disabled={busy || !rejectReason} className="bg-rose-600 hover:bg-rose-700 text-white">
              {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : "Confirm reject"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Cancel dialog */}
      <Dialog open={cancelOpen} onOpenChange={setCancelOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Cancel active policy</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <p className="text-sm text-muted-foreground">
              This will cancel policy <strong>#{selectedPolicy?.policyNumber}</strong> and deactivate it.
            </p>
            <Label>Cancellation reason</Label>
            <Textarea
              value={cancelReason}
              onChange={(e) => setCancelReason(e.target.value)}
              rows={3}
              placeholder="Enter reason for cancellation..."
              autoFocus
            />
            {selectedPolicy?.paymentStatus === "Paid" && (
              <p className="text-xs text-amber-600 bg-amber-50 p-2 rounded">
                This policy has a confirmed payment. A refund will be initiated automatically.
              </p>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCancelOpen(false)}>Cancel</Button>
            <Button onClick={doCancel} disabled={busy || !cancelReason} className="bg-amber-600 hover:bg-amber-700 text-white">
              {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : "Confirm cancel policy"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

