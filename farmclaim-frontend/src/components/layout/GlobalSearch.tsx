"use client";

import { useEffect, useRef, useState } from "react";
import { useApp } from "@/lib/store";
import { farmsApi, policiesApi, claimsApi, adminApi } from "@/lib/api";
import { Input } from "@/components/ui/input";
import { Search, Sprout, FileText, MessageSquare, Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";
import type { FarmListDto, PolicyListDto, ClaimListDto } from "@/lib/types";

interface ResultItem {
  kind: "farm" | "policy" | "claim";
  id: string;
  title: string;
  subtitle: string;
  path: string;
}

export function GlobalSearch() {
  const { user, navigate, route } = useApp();
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState<ResultItem[]>([]);
  const boxRef = useRef<HTMLDivElement>(null);
  const isAdmin = user?.role === "Admin";
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const onClick = (e: MouseEvent) => {
      if (boxRef.current && !boxRef.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  useEffect(() => {
    const t = setTimeout(() => {
      setOpen(false);
      setQuery("");
    }, 0);
    return () => clearTimeout(t);
  }, [route.path]);

  useEffect(() => {
    if (timer.current) clearTimeout(timer.current);
    const q = query.trim().toLowerCase();
    if (!q || q.length < 2) return;
    timer.current = setTimeout(async () => {
      setLoading(true);
      try {
        const found = await search(q, isAdmin);
        setResults(found);
        setOpen(true);
      } catch {
        setResults([]);
      } finally {
        setLoading(false);
      }
    }, 250);
    return () => {
      if (timer.current) clearTimeout(timer.current);
    };
  }, [query, isAdmin]);

  const onChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setQuery(value);
    if (value.trim().length < 2) {
      setResults([]);
      setOpen(false);
    }
  };

  const go = (item: ResultItem) => {
    setOpen(false);
    setQuery("");
    navigate(item.path);
  };

  return (
    <div ref={boxRef} className="relative w-full">
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          value={query}
          onChange={onChange}
          onFocus={() => query.trim().length >= 2 && setOpen(true)}
          placeholder="Search farms, policies, claims…"
          className="pl-9 h-10 bg-background/60 pr-9"
        />
        {loading && (
          <Loader2 className="absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground animate-spin" />
        )}
      </div>

      {open && (
        <div className="absolute top-full left-0 right-0 mt-2 rounded-xl border bg-card shadow-lg z-50 overflow-hidden">
          {results.length === 0 ? (
            <div className="px-4 py-6 text-center text-sm text-muted-foreground">No matches found</div>
          ) : (
            <ul className="max-h-80 overflow-y-auto py-1">
              {results.map((item) => (
                <li key={`${item.kind}-${item.id}`}>
                  <button
                    onClick={() => go(item)}
                    className="w-full flex items-start gap-3 px-3 py-2.5 hover:bg-accent text-left"
                  >
                    <span
                      className={cn(
                        "mt-0.5 h-8 w-8 shrink-0 rounded-lg grid place-items-center",
                        item.kind === "farm" && "bg-emerald-100 text-emerald-700",
                        item.kind === "policy" && "bg-blue-100 text-blue-700",
                        item.kind === "claim" && "bg-amber-100 text-amber-700"
                      )}
                    >
                      {item.kind === "farm" ? (
                        <Sprout className="h-4 w-4" />
                      ) : item.kind === "policy" ? (
                        <FileText className="h-4 w-4" />
                      ) : (
                        <MessageSquare className="h-4 w-4" />
                      )}
                    </span>
                    <span className="flex-1 min-w-0 text-left">
                      <span className="block text-sm font-medium truncate">{item.title}</span>
                      <span className="block text-xs text-muted-foreground truncate">{item.subtitle}</span>
                    </span>
                    <span className="shrink-0 text-[10px] uppercase tracking-wide text-muted-foreground mt-1">
                      {item.kind}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}

async function search(q: string, isAdmin: boolean): Promise<ResultItem[]> {
  const farms = await farmsApi.list();
  const policies = await policiesApi.list();
  const claims = await claimsApi.list();

  const result: ResultItem[] = [];

  for (const f of farms) {
    if ((f.name ?? "").toLowerCase().includes(q) || (f.address ?? "").toLowerCase().includes(q)) {
      result.push({
        kind: "farm",
        id: f.id,
        title: f.name ?? "Unnamed farm",
        subtitle: `${f.areaInHectares ?? 0} ha · ${f.address ?? "No address"}`,
        path: isAdmin ? "#" : `/dashboard/farms/${f.id}`,
      });
    }
  }

  for (const p of policies) {
    if (
      (p.policyNumber ?? "").toLowerCase().includes(q) ||
      (p.cropType ?? "").toLowerCase().includes(q) ||
      (p.farmName ?? "").toLowerCase().includes(q)
    ) {
      result.push({
        kind: "policy",
        id: p.id,
        title: `${p.policyNumber ?? "Policy"} · ${p.cropType ?? "Crop"}`,
        subtitle: `${p.farmName ?? ""} · ${p.status ?? ""}`,
        path: isAdmin ? `/admin/policies` : `/dashboard/policies/${p.id}`,
      });
    }
  }

  for (const c of claims) {
    if (c.status.toLowerCase().includes(q) || c.incidentType.toLowerCase().includes(q)) {
      result.push({
        kind: "claim",
        id: c.id,
        title: `${c.incidentType} claim`,
        subtitle: `${c.farmName ?? ""} · ${c.status ?? ""}`,
        path: isAdmin ? `/admin/claims/${c.id}` : `/dashboard/claims/${c.id}`,
      });
    }
  }

  return result;
}