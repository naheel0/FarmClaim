import { cn } from "@/lib/utils";
import {
  type ClaimStatus,
  type IncidentType,
  type PaymentStatus,
  type PolicyStatus,
  type UserStatus,
} from "@/lib/types";

const statusStyles: Record<string, string> = {
  Pending: "bg-amber-100 text-amber-900 border-amber-200 dark:bg-amber-900/30 dark:text-amber-200",
  UnderReview: "bg-blue-100 text-blue-900 border-blue-200 dark:bg-blue-900/30 dark:text-blue-200",
  Approved: "bg-emerald-100 text-emerald-900 border-emerald-200 dark:bg-emerald-900/30 dark:text-emerald-200",
  Rejected: "bg-rose-100 text-rose-900 border-rose-200 dark:bg-rose-900/30 dark:text-rose-200",
  Paid: "bg-green-700 text-white border-green-800 dark:bg-green-900/40 dark:text-green-100",
  Active: "bg-emerald-100 text-emerald-900 border-emerald-200 dark:bg-emerald-900/30 dark:text-emerald-200",
  Expired: "bg-stone-100 text-stone-700 border-stone-200 dark:bg-stone-700/30 dark:text-stone-300",
  Cancelled: "bg-stone-100 text-stone-700 border-stone-200 dark:bg-stone-700/30 dark:text-stone-300",
  Created: "bg-stone-100 text-stone-700 border-stone-200",
  Attempted: "bg-amber-100 text-amber-900 border-amber-200",
  Captured: "bg-emerald-100 text-emerald-900 border-emerald-200",
  Failed: "bg-rose-100 text-rose-900 border-rose-200",
  Refunded: "bg-blue-100 text-blue-900 border-blue-200",
  Suspended: "bg-orange-100 text-orange-900 border-orange-200",
  Blocked: "bg-rose-100 text-rose-900 border-rose-200",
  PendingVerification: "bg-amber-100 text-amber-900 border-amber-200",
};

export function StatusBadge({
  status,
  className,
}: {
  status: ClaimStatus | PolicyStatus | PaymentStatus | UserStatus | string;
  className?: string;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium capitalize",
        statusStyles[status] ?? "bg-stone-100 text-stone-700 border-stone-200",
        className
      )}
    >
      <span className="h-1.5 w-1.5 rounded-full bg-current opacity-70" />
      {status.replace(/([A-Z])/g, " $1").trim()}
    </span>
  );
}

const incidentStyles: Record<IncidentType, string> = {
  Flood: "bg-blue-50 text-blue-700 border-blue-200",
  Drought: "bg-amber-50 text-amber-800 border-amber-200",
  HeavyRain: "bg-cyan-50 text-cyan-700 border-cyan-200",
  Hail: "bg-slate-50 text-slate-700 border-slate-200",
  Frost: "bg-sky-50 text-sky-700 border-sky-200",
  PestInfestation: "bg-orange-50 text-orange-700 border-orange-200",
  Fire: "bg-red-50 text-red-700 border-red-200",
  Windstorm: "bg-teal-50 text-teal-700 border-teal-200",
  Other: "bg-stone-50 text-stone-700 border-stone-200",
};

const incidentIcons: Record<IncidentType, string> = {
  Flood: "🌊",
  Drought: "🏜️",
  HeavyRain: "🌧️",
  Hail: "❄️",
  Frost: "🥶",
  PestInfestation: "🐛",
  Fire: "🔥",
  Windstorm: "💨",
  Other: "⚠️",
};

export function IncidentBadge({ type }: { type: IncidentType }) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-md border px-2 py-0.5 text-xs font-medium",
        incidentStyles[type]
      )}
    >
      <span>{incidentIcons[type]}</span>
      {type.replace(/([A-Z])/g, " $1").trim()}
    </span>
  );
}
