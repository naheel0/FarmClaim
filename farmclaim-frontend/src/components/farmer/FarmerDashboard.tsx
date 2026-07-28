"use client";

import { useEffect, useState } from "react";
import { useApp } from "@/lib/store";
import {
  LayoutDashboard,
  Tractor,
  FileText,
  ClipboardList,
  Search,
  User,
  Plus,
  ArrowRight,
  TrendingUp,
  Wallet,
  Sprout,
  CloudRain,
  Activity,
  Clock,
} from "lucide-react";
import {
  DashboardShell,
  PageHeader,
  CardStat,
  type NavItem,
} from "@/components/layout/DashboardShell";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { StatusBadge, IncidentBadge } from "@/components/shared/badges";
import { Skeleton } from "@/components/ui/skeleton";
import {
  farmerApi,
  farmsApi,
  policiesApi,
  claimsApi,
  plansApi,
} from "@/lib/api";
import type {
  ClaimResponseDto,
  FarmerProfileDto,
  FarmResponseDto,
  InsurancePlanDto,
  PolicyResponseDto,
} from "@/lib/types";
import { cn, formatDate, formatINR, formatRelative } from "@/lib/utils";
import {
  Area,
  AreaChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
  CartesianGrid,
} from "recharts";

const farmerNav: NavItem[] = [
  { label: "Overview", path: "/dashboard/overview", icon: LayoutDashboard },
  { label: "My Farms", path: "/dashboard/farms", icon: Tractor },
  { label: "Policies", path: "/dashboard/policies", icon: FileText },
  { label: "Claims", path: "/dashboard/claims", icon: ClipboardList },
  { label: "Browse Plans", path: "/dashboard/plans", icon: Search },
  { label: "Profile", path: "/dashboard/profile", icon: User },
];

export function FarmerDashboard() {
  return (
    <DashboardShell
      navItems={farmerNav}
      brandLabel="FarmClaim"
      brandSub="Farmer Console"
    >
      <FarmerRouter />
    </DashboardShell>
  );
}

function FarmerRouter() {
  const route = useApp((s) => s.route);
  const path = route.path;
  if (path.startsWith("/dashboard/farms")) return <FarmsPage />;
  if (path.startsWith("/dashboard/policies")) return <PoliciesPage />;
  if (path.startsWith("/dashboard/claims")) return <ClaimsPage />;
  if (path.startsWith("/dashboard/plans")) return <PlansBrowsePage />;
  if (path.startsWith("/dashboard/profile")) return <ProfilePage />;
  return <OverviewPage />;
}

// ============== OVERVIEW ==============
function OverviewPage() {
  const navigate = useApp((s) => s.navigate);
  const [profile, setProfile] = useState<FarmerProfileDto | null>(null);
  const [farms, setFarms] = useState<FarmResponseDto[]>([]);
  const [policies, setPolicies] = useState<PolicyResponseDto[]>([]);
  const [claims, setClaims] = useState<ClaimResponseDto[]>([]);
  const [plans, setPlans] = useState<InsurancePlanDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      farmerApi.me(),
      farmsApi.list(),
      policiesApi.list(),
      claimsApi.list(),
      plansApi.list(),
    ])
      .then(([p, f, pol, c, pl]) => {
        setProfile(p);
        setFarms(f);
        setPolicies(pol);
        setClaims(c);
        setPlans(pl);
      })
      .finally(() => setLoading(false));
  }, []);

  // Build a small claims-per-month chart
  const monthlyData = ((): { month: string; claims: number; payout: number }[] => {
    const months = ["Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    return months.map((m, i) => ({
      month: m,
      claims: (i * 7 + 11) % 9,
      payout: ((i * 7 + 11) % 9) * 35000 + i * 5000,
    }));
  })();

  return (
    <div>
      <PageHeader
        title={`Welcome back, ${profile?.firstName ?? "Farmer"} 👋`}
        subtitle="Here's what's happening across your farms today."
        actions={
          <Button
            onClick={() => navigate("/dashboard/claims/new")}
            className="bg-emerald-700 hover:bg-emerald-800 text-white gap-1.5"
          >
            <Plus className="h-4 w-4" />
            File a claim
          </Button>
        }
      />

      {loading ? (
        <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-32 rounded-xl" />
          ))}
        </div>
      ) : (
        <>
          <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <CardStat
              label="Total farms"
              value={profile?.totalFarms ?? 0}
              icon={Tractor}
              delta={{ value: "1 new", up: true }}
            />
            <CardStat
              label="Active policies"
              value={policies.filter((p) => p.status === "Active").length}
              icon={FileText}
              accent="amber"
              delta={{ value: "2 active", up: true }}
            />
            <CardStat
              label="Total claims"
              value={profile?.totalClaims ?? 0}
              icon={ClipboardList}
              accent="blue"
              delta={{ value: "1 pending", up: false }}
            />
            <CardStat
              label="Total received"
              value={formatINR(
                claims
                  .filter((c) => c.status === "Paid")
                  .reduce((sum, c) => sum + (c.approvedAmount ?? 0), 0)
              )}
              icon={Wallet}
              accent="rose"
              delta={{ value: "₹2.3L", up: true }}
            />
          </div>

          <div className="grid lg:grid-cols-3 gap-6 mt-6">
            {/* Chart */}
            <Card className="lg:col-span-2">
              <CardHeader className="flex-row items-center justify-between pb-2">
                <div>
                  <CardTitle className="font-serif">Claims &amp; payouts</CardTitle>
                  <p className="text-xs text-muted-foreground mt-1">Last 6 months</p>
                </div>
                <Badge variant="outline" className="gap-1.5">
                  <Activity className="h-3 w-3" />
                  Live
                </Badge>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={280}>
                  <AreaChart data={monthlyData}>
                    <defs>
                      <linearGradient id="g1" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#16a34a" stopOpacity={0.4} />
                        <stop offset="95%" stopColor="#16a34a" stopOpacity={0} />
                      </linearGradient>
                      <linearGradient id="g2" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#f59e0b" stopOpacity={0.4} />
                        <stop offset="95%" stopColor="#f59e0b" stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e7e5e4" vertical={false} />
                    <XAxis dataKey="month" stroke="#78716c" fontSize={12} tickLine={false} axisLine={false} />
                    <YAxis stroke="#78716c" fontSize={12} tickLine={false} axisLine={false} />
                    <Tooltip
                      contentStyle={{
                        background: "white",
                        border: "1px solid #e7e5e4",
                        borderRadius: 12,
                        fontSize: 12,
                      }}
                    />
                    <Area
                      type="monotone"
                      dataKey="payout"
                      stroke="#16a34a"
                      strokeWidth={2}
                      fill="url(#g1)"
                      name="Payout (₹)"
                    />
                    <Area
                      type="monotone"
                      dataKey="claims"
                      stroke="#f59e0b"
                      strokeWidth={2}
                      fill="url(#g2)"
                      name="Claims"
                    />
                  </AreaChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            {/* Weather widget */}
            <WeatherWidget />
          </div>

          <div className="grid lg:grid-cols-2 gap-6 mt-6">
            {/* Recent claims */}
            <Card>
              <CardHeader className="flex-row items-center justify-between pb-2">
                <CardTitle className="font-serif">Recent claims</CardTitle>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => navigate("/dashboard/claims")}
                  className="text-emerald-700"
                >
                  View all <ArrowRight className="h-3.5 w-3.5 ml-1" />
                </Button>
              </CardHeader>
              <CardContent className="space-y-3">
                {claims.slice(0, 4).map((c) => (
                  <button
                    key={c.id}
                    onClick={() => navigate(`/dashboard/claims/${c.id}`)}
                    className="w-full flex items-center gap-3 p-3 rounded-lg hover:bg-muted/60 transition-colors text-left"
                  >
                    <div className="h-10 w-10 rounded-lg bg-emerald-100 grid place-items-center shrink-0">
                      <CloudRain className="h-5 w-5 text-emerald-700" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="font-medium text-sm truncate">
                          {c.farmName} · {c.incidentType}
                        </span>
                      </div>
                      <div className="text-xs text-muted-foreground flex items-center gap-1.5 mt-0.5">
                        <Clock className="h-3 w-3" />
                        {formatRelative(c.createdAt)}
                      </div>
                    </div>
                    <div className="flex flex-col items-end gap-1">
                      <StatusBadge status={c.status} />
                      {c.approvedAmount && (
                        <span className="text-xs font-semibold text-emerald-700">
                          {formatINR(c.approvedAmount)}
                        </span>
                      )}
                    </div>
                  </button>
                ))}
                {claims.length === 0 && (
                  <EmptyState
                    title="No claims yet"
                    desc="When you file your first claim, it'll show up here."
                    action={
                      <Button
                        onClick={() => navigate("/dashboard/claims/new")}
                        className="bg-emerald-700 hover:bg-emerald-800 text-white"
                      >
                        File a claim
                      </Button>
                    }
                  />
                )}
              </CardContent>
            </Card>

            {/* Active policies */}
            <Card>
              <CardHeader className="flex-row items-center justify-between pb-2">
                <CardTitle className="font-serif">Active policies</CardTitle>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => navigate("/dashboard/policies")}
                  className="text-emerald-700"
                >
                  View all <ArrowRight className="h-3.5 w-3.5 ml-1" />
                </Button>
              </CardHeader>
              <CardContent className="space-y-3">
                {policies.slice(0, 4).map((p) => (
                  <button
                    key={p.id}
                    onClick={() => navigate(`/dashboard/policies/${p.id}`)}
                    className="w-full flex items-center gap-3 p-3 rounded-lg hover:bg-muted/60 transition-colors text-left"
                  >
                    <div className="h-10 w-10 rounded-lg bg-amber-100 grid place-items-center shrink-0">
                      <Sprout className="h-5 w-5 text-amber-700" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="font-medium text-sm truncate">
                        {p.cropType} · {p.provider}
                      </div>
                      <div className="text-xs text-muted-foreground truncate">
                        {p.policyNumber} · {p.farmName}
                      </div>
                    </div>
                    <div className="flex flex-col items-end gap-1">
                      <StatusBadge status={p.status} />
                      <span className="text-xs text-muted-foreground">
                        {formatINR(p.sumInsured)}
                      </span>
                    </div>
                  </button>
                ))}
              </CardContent>
            </Card>
          </div>
        </>
      )}
    </div>
  );
}

function WeatherWidget() {
  // Static demo weather data
  const weather = {
    location: "Krishna District, AP",
    temp: 28,
    condition: "Partly cloudy",
    humidity: 72,
    wind: 12,
    rainfall: 0,
    forecast: [
      { day: "Mon", temp: 29, rain: 10 },
      { day: "Tue", temp: 28, rain: 65 },
      { day: "Wed", temp: 27, rain: 80 },
      { day: "Thu", temp: 28, rain: 35 },
      { day: "Fri", temp: 30, rain: 5 },
    ],
    alert: "Heavy rain expected Tuesday — secure your harvest.",
  };
  return (
    <Card className="overflow-hidden border-0 shadow-md bg-gradient-to-br from-emerald-700 via-emerald-800 to-green-900 text-white">
      <CardContent className="p-6">
        <div className="flex items-start justify-between">
          <div>
            <div className="text-xs uppercase tracking-widest text-emerald-200">
              Weather at your farm
            </div>
            <div className="text-sm text-emerald-100/80 mt-0.5">{weather.location}</div>
          </div>
          <CloudRain className="h-8 w-8 text-emerald-200" />
        </div>
        <div className="mt-4 flex items-baseline gap-2">
          <span className="text-5xl font-bold font-serif">{weather.temp}°</span>
          <span className="text-emerald-100/80">{weather.condition}</span>
        </div>
        <div className="grid grid-cols-3 gap-3 mt-5 text-sm">
          <div>
            <div className="text-emerald-200 text-xs">Humidity</div>
            <div className="font-semibold">{weather.humidity}%</div>
          </div>
          <div>
            <div className="text-emerald-200 text-xs">Wind</div>
            <div className="font-semibold">{weather.wind} km/h</div>
          </div>
          <div>
            <div className="text-emerald-200 text-xs">Rainfall</div>
            <div className="font-semibold">{weather.rainfall} mm</div>
          </div>
        </div>
        <div className="grid grid-cols-5 gap-1 mt-5 pt-4 border-t border-white/10">
          {weather.forecast.map((d) => (
            <div key={d.day} className="text-center">
              <div className="text-[10px] text-emerald-200">{d.day}</div>
              <div className="text-sm font-semibold mt-0.5">{d.temp}°</div>
              <div className="text-[10px] text-emerald-300">{d.rain}%</div>
            </div>
          ))}
        </div>
        <div className="mt-4 p-2.5 rounded-lg bg-amber-400/20 border border-amber-300/30 text-xs flex items-start gap-2">
          <span>⚠️</span>
          <span className="text-amber-100">{weather.alert}</span>
        </div>
      </CardContent>
    </Card>
  );
}

function EmptyState({
  title,
  desc,
  action,
}: {
  title: string;
  desc: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="text-center py-10 px-4">
      <div className="h-12 w-12 rounded-full bg-muted mx-auto grid place-items-center mb-3">
        <Sprout className="h-6 w-6 text-muted-foreground" />
      </div>
      <div className="font-medium text-foreground">{title}</div>
      <div className="text-sm text-muted-foreground mt-1 mb-4">{desc}</div>
      {action}
    </div>
  );
}

// Lazy-import the other farmer pages to keep file readable
import { FarmsPage } from "@/components/farmer/FarmsPage";
import { PoliciesPage } from "@/components/farmer/PoliciesPage";
import { ClaimsPage } from "@/components/farmer/ClaimsPage";
import { PlansBrowsePage } from "@/components/farmer/PlansBrowsePage";
import { ProfilePage } from "@/components/farmer/ProfilePage";
