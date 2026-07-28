"use client";

import { useState } from "react";
import { useApp } from "@/lib/store";
import { authApi } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Eye,
  EyeOff,
  Shield,
  User as UserIcon,
  Loader2,
  AlertCircle,
  CheckCircle2,
  Mail,
  KeyRound,
} from "lucide-react";
import { toast } from "sonner";
import { AuthShell } from "./LoginPage";
import type { UserRole } from "@/lib/types";

export function SignupPage() {
  const navigate = useApp((s) => s.navigate);
  const [step, setStep] = useState<"form" | "verify">("form");
  const [role, setRole] = useState<UserRole>("Farmer");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [showPwd, setShowPwd] = useState(false);
  const [agree, setAgree] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [otp, setOtp] = useState("");

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (password !== confirm) {
      setError("Passwords don't match");
      return;
    }
    if (password.length < 6) {
      setError("Password must be at least 6 characters");
      return;
    }
    if (!agree) {
      setError("Please accept the terms to continue");
      return;
    }
    setLoading(true);
    try {
      await authApi.register({
        email,
        password,
        firstName,
        lastName,
        phoneNumber: phone || null,
        role,
      });
      // In live mode the .NET backend already emailed the code.
      toast.success("Account created! Check your email for the OTP.");
      setStep("verify");
    } catch (err) {
      const msg = err instanceof Error ? err.message : "Sign up failed";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  const onVerify = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await authApi.verifyEmail(email, otp);
      toast.success("Email verified! You can now sign in.");
      navigate("/login");
    } catch (err) {
      const msg = err instanceof Error ? err.message : "Verification failed";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  if (step === "verify") {
    return (
      <AuthShell
        title="Verify your email"
        subtitle={`We sent a 6-digit code to ${email}`}
        footerText="Didn't get the email?"
        footerLink="/signup"
        footerLabel="Use a different email"
      >
        <form onSubmit={onVerify} className="space-y-5">
          {error && (
            <div className="flex items-start gap-2 p-3 rounded-lg bg-rose-50 text-rose-900 text-sm border border-rose-200">
              <AlertCircle className="h-4 w-4 shrink-0 mt-0.5" />
              {error}
            </div>
          )}
          <div className="space-y-2">
            <Label htmlFor="otp" className="flex items-center gap-1.5">
              <Mail className="h-3.5 w-3.5" />
              6-digit code
            </Label>
            <Input
              id="otp"
              value={otp}
              onChange={(e) => setOtp(e.target.value.replace(/\D/g, "").slice(0, 6))}
              placeholder="123456"
              required
              autoFocus
              inputMode="numeric"
              className="h-12 text-center text-2xl tracking-[0.5em] font-mono"
            />
          </div>
          <Button
            type="submit"
            disabled={loading || otp.length !== 6}
            className="w-full h-11 bg-emerald-700 hover:bg-emerald-800 text-white"
          >
            {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : "Verify email"}
          </Button>
          <button
            type="button"
            onClick={async () => {
              await authApi.resendOtp(email);
              toast.success("OTP resent!");
            }}
            className="w-full text-sm text-emerald-700 hover:text-emerald-800 font-medium"
          >
            Resend code
          </button>
        </form>
      </AuthShell>
    );
  }

  return (
    <AuthShell
      title="Create your account"
      subtitle="Sign up free. No paperwork, no agent visit, cancel anytime."
      footerText="Already have an account?"
      footerLink="/login"
      footerLabel="Sign in"
    >
      <form onSubmit={onSubmit} className="space-y-4">
        {/* Role toggle */}
        <div className="grid grid-cols-2 gap-2 p-1 bg-muted rounded-xl mb-2">
          <button
            type="button"
            onClick={() => setRole("Farmer")}
            className={`flex items-center justify-center gap-2 py-2.5 rounded-lg text-sm font-medium transition-all ${
              role === "Farmer"
                ? "bg-card shadow-sm text-emerald-700"
                : "text-muted-foreground"
            }`}
          >
            <UserIcon className="h-4 w-4" />
            Farmer
          </button>
          <button
            type="button"
            onClick={() => setRole("Admin")}
            className={`flex items-center justify-center gap-2 py-2.5 rounded-lg text-sm font-medium transition-all ${
              role === "Admin"
                ? "bg-card shadow-sm text-emerald-700"
                : "text-muted-foreground"
            }`}
          >
            <Shield className="h-4 w-4" />
            Admin
          </button>
        </div>

        {error && (
          <div className="flex items-start gap-2 p-3 rounded-lg bg-rose-50 text-rose-900 text-sm border border-rose-200">
            <AlertCircle className="h-4 w-4 shrink-0 mt-0.5" />
            {error}
          </div>
        )}

        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label htmlFor="firstName">First name</Label>
            <Input
              id="firstName"
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              required
              maxLength={100}
              placeholder="Amara"
              className="h-11"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="lastName">Last name</Label>
            <Input
              id="lastName"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              required
              maxLength={100}
              placeholder="Singh"
              className="h-11"
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="email">Email address</Label>
          <Input
            id="email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            placeholder="you@example.com"
            autoComplete="email"
            className="h-11"
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="phone">Phone number (optional)</Label>
          <Input
            id="phone"
            type="tel"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            placeholder="+91 98765 43210"
            className="h-11"
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="password">Password</Label>
          <div className="relative">
            <Input
              id="password"
              type={showPwd ? "text" : "password"}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={6}
              placeholder="At least 6 characters"
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
          {password && (
            <PasswordStrength password={password} />
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="confirm">Confirm password</Label>
          <div className="relative">
            <Input
              id="confirm"
              type={showPwd ? "text" : "password"}
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              required
              placeholder="Re-enter your password"
              className="h-11 pr-11"
            />
            {confirm && confirm === password && (
              <CheckCircle2 className="absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 text-emerald-600" />
            )}
          </div>
        </div>

        <div className="flex items-start gap-2 pt-1">
          <Checkbox
            id="terms"
            checked={agree}
            onCheckedChange={(v) => setAgree(v === true)}
            className="mt-1"
          />
          <Label htmlFor="terms" className="text-sm text-muted-foreground font-normal cursor-pointer leading-relaxed">
            I agree to the{" "}
            <span className="text-emerald-700 font-medium">Terms of Service</span>,{" "}
            <span className="text-emerald-700 font-medium">Privacy Policy</span> and
            the IRDAI crop insurance handbook.
          </Label>
        </div>

        <Button
          type="submit"
          disabled={loading}
          className="w-full h-11 bg-emerald-700 hover:bg-emerald-800 text-white"
        >
          {loading ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            `Create ${role} account`
          )}
        </Button>
      </form>
    </AuthShell>
  );
}

function PasswordStrength({ password }: { password: string }) {
  const checks = [
    { label: "6+ characters", ok: password.length >= 6 },
    { label: "Mixed case", ok: /[a-z]/.test(password) && /[A-Z]/.test(password) },
    { label: "Number", ok: /\d/.test(password) },
  ];
  return (
    <div className="flex flex-wrap gap-3 text-xs">
      {checks.map((c) => (
        <div
          key={c.label}
          className={`flex items-center gap-1 ${c.ok ? "text-emerald-700" : "text-muted-foreground"}`}
        >
          {c.ok ? <CheckCircle2 className="h-3 w-3" /> : <span className="h-3 w-3 rounded-full border border-current" />}
          {c.label}
        </div>
      ))}
    </div>
  );
}
