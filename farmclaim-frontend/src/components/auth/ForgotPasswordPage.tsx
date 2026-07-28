"use client";

import { useState } from "react";
import { useApp } from "@/lib/store";
import { authApi } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2, AlertCircle, MailCheck, ArrowLeft } from "lucide-react";
import { toast } from "sonner";
import { AuthShell } from "./LoginPage";

export function ForgotPasswordPage() {
  const navigate = useApp((s) => s.navigate);
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      await authApi.forgotPassword(email);
      setSent(true);
      toast.success("Reset link sent!");
    } catch (err) {
      const msg = err instanceof Error ? err.message : "Failed to send email";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  if (sent) {
    return (
      <AuthShell
        title="Check your inbox"
        subtitle=""
        footerText="Remembered your password?"
        footerLink="/login"
        footerLabel="Sign in"
      >
        <div className="text-center py-6">
          <div className="h-16 w-16 rounded-full bg-emerald-100 grid place-items-center mx-auto mb-5">
            <MailCheck className="h-8 w-8 text-emerald-700" />
          </div>
          <p className="text-muted-foreground">
            We&apos;ve sent a password reset link to{" "}
            <span className="font-semibold text-foreground">{email}</span>.
            Click the link in the email to set a new password.
          </p>
          <Button
            onClick={() => navigate("/reset-password")}
            className="mt-6 bg-emerald-700 hover:bg-emerald-800 text-white"
          >
            I have my code — reset password
          </Button>
        </div>
      </AuthShell>
    );
  }

  return (
    <AuthShell
      title="Forgot password?"
      subtitle="Enter your email and we'll send you a reset link."
      footerText="Remembered your password?"
      footerLink="/login"
      footerLabel="Sign in"
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
            required
            placeholder="you@example.com"
            autoFocus
            className="h-11"
          />
        </div>
        <Button
          type="submit"
          disabled={loading}
          className="w-full h-11 bg-emerald-700 hover:bg-emerald-800 text-white"
        >
          {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : "Send reset link"}
        </Button>
      </form>
    </AuthShell>
  );
}

export function ResetPasswordPage() {
  const navigate = useApp((s) => s.navigate);
  const [email, setEmail] = useState("");
  const [token, setToken] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
    setLoading(true);
    try {
      await authApi.resetPassword(email, token, password);
      toast.success("Password reset! You can sign in now.");
      navigate("/login");
    } catch (err) {
      const msg = err instanceof Error ? err.message : "Reset failed";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthShell
      title="Reset your password"
      subtitle="Enter the code from your email and choose a new password."
      footerText="Need a new code?"
      footerLink="/forgot-password"
      footerLabel="Resend"
    >
      <form onSubmit={onSubmit} className="space-y-4">
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
            required
            className="h-11"
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="token">Reset code</Label>
          <Input
            id="token"
            value={token}
            onChange={(e) => setToken(e.target.value)}
            required
            placeholder="6-digit code"
            inputMode="numeric"
            className="h-11 text-center text-lg tracking-widest font-mono"
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="password">New password</Label>
          <Input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={6}
            className="h-11"
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="confirm">Confirm new password</Label>
          <Input
            id="confirm"
            type="password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            required
            className="h-11"
          />
        </div>
        <Button
          type="submit"
          disabled={loading}
          className="w-full h-11 bg-emerald-700 hover:bg-emerald-800 text-white"
        >
          {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : "Reset password"}
        </Button>
      </form>
    </AuthShell>
  );
}
