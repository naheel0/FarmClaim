"use client";

import { useState } from "react";
import { useApp } from "@/lib/store";
import { authApi } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Eye, EyeOff, Leaf, ArrowLeft, Loader2, AlertCircle } from "lucide-react";
import { toast } from "sonner";

const HERO_IMG =
  "https://images.unsplash.com/photo-1500382017468-9049fed747ef?ixlib=rb-4.0.3&auto=format&fit=crop&w=1600&q=80";

export function LoginPage() {
  const navigate = useApp((s) => s.navigate);
  const login = useApp((s) => s.login);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPwd, setShowPwd] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const res = await authApi.login({ email, password });
      login(res.token, res.user);
      toast.success(`Welcome back, ${res.user.firstName}!`);
      navigate(res.user.role === "Admin" ? "/admin" : "/dashboard");
    } catch (err) {
      const msg = err instanceof Error ? err.message : "Login failed";
      setError(msg);
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthShell
      title="Welcome back"
      subtitle="Sign in to manage your farm, policies and claims."
      footerText="New to FarmClaim?"
      footerLink="/signup"
      footerLabel="Create an account"
    >
      <form onSubmit={onSubmit} className="space-y-5">
        {error && (
          <div className="flex items-start gap-2 p-3 rounded-lg bg-rose-50 text-rose-900 text-sm border border-rose-200">
            <AlertCircle className="h-4 w-4 shrink-0 mt-0.5" />
            {error}
          </div>
        )}

        <div className="space-y-2">
          <Label htmlFor="email">Email address</Label>
          <Input
            id="email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="you@example.com"
            required
            autoComplete="email"
            autoFocus
            className="h-11"
          />
        </div>

        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label htmlFor="password">Password</Label>
            <button
              type="button"
              onClick={() => navigate("/forgot-password")}
              className="text-xs text-emerald-700 hover:text-emerald-800 font-medium"
            >
              Forgot password?
            </button>
          </div>
          <div className="relative">
            <Input
              id="password"
              type={showPwd ? "text" : "password"}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
              autoComplete="current-password"
              className="h-11 pr-11"
            />
            <button
              type="button"
              onClick={() => setShowPwd((v) => !v)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
              tabIndex={-1}
            >
              {showPwd ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Checkbox id="remember" defaultChecked />
          <Label htmlFor="remember" className="text-sm text-muted-foreground font-normal cursor-pointer">
            Keep me signed in for 30 days
          </Label>
        </div>

        <Button
          type="submit"
          disabled={loading}
          className="w-full h-11 bg-emerald-700 hover:bg-emerald-800 text-white"
        >
          {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : "Sign in"}
        </Button>
      </form>
    </AuthShell>
  );
}

export function AuthShell({
  title,
  subtitle,
  children,
  footerText,
  footerLink,
  footerLabel,
}: {
  title: string;
  subtitle: string;
  children: React.ReactNode;
  footerText: string;
  footerLink: string;
  footerLabel: string;
}) {
  const navigate = useApp((s) => s.navigate);
  return (
    <div className="min-h-screen grid lg:grid-cols-2">
      {/* Left: form */}
      <div className="flex flex-col px-6 sm:px-12 lg:px-20 py-8">
        <button
          onClick={() => navigate("/")}
          className="flex items-center gap-2.5 self-start group"
        >
          <div className="h-9 w-9 rounded-xl bg-gradient-to-br from-emerald-600 to-green-700 grid place-items-center shadow-md shadow-emerald-600/20 transition-transform group-hover:scale-105">
            <Leaf className="h-5 w-5 text-white" strokeWidth={2.5} />
          </div>
          <span className="font-serif text-lg font-semibold">FarmClaim</span>
        </button>

        <div className="flex-1 flex flex-col justify-center max-w-md w-full mx-auto py-10">
          <button
            onClick={() => navigate("/")}
            className="flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground mb-6 self-start"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            Back to home
          </button>
          <h1 className="font-serif text-3xl lg:text-4xl font-semibold tracking-tight text-balance">
            {title}
          </h1>
          <p className="mt-2 text-muted-foreground">{subtitle}</p>

          <div className="mt-8">{children}</div>

          <p className="mt-8 text-sm text-muted-foreground text-center">
            {footerText}{" "}
            <button
              onClick={() => navigate(footerLink)}
              className="text-emerald-700 hover:text-emerald-800 font-semibold"
            >
              {footerLabel}
            </button>
          </p>
        </div>

        <div className="text-xs text-muted-foreground text-center">
          © {new Date().getFullYear()} FarmClaim · Secured with 256-bit encryption
        </div>
      </div>

      {/* Right: hero image */}
      <div className="hidden lg:block relative overflow-hidden">
        <img
          src={HERO_IMG}
          alt="Sunlit green farmland"
          className="absolute inset-0 w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-emerald-950/80 via-emerald-900/40 to-emerald-900/20" />
        <div className="absolute bottom-0 left-0 right-0 p-12 text-white">
          <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-white/10 backdrop-blur-md border border-white/20 text-xs uppercase tracking-widest mb-4">
            <Leaf className="h-3.5 w-3.5" />
            Farmer testimonial
          </div>
          <blockquote className="font-serif text-2xl leading-snug max-w-md text-balance">
            &ldquo;Filed a flood claim at 8pm. Money was in my bank by 4pm next day.
            This is how insurance should always have worked.&rdquo;
          </blockquote>
          <div className="mt-4 text-emerald-100/80 text-sm">
            Amara Singh · Paddy farmer · Krishna District
          </div>
        </div>
      </div>
    </div>
  );
}
