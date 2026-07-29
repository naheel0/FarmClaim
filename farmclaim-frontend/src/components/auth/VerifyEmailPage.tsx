"use client";

import { useState, useEffect } from "react";
import { useApp } from "@/lib/store";
import { authApi } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2, AlertCircle, Mail } from "lucide-react";
import { toast } from "sonner";
import { AuthShell } from "./LoginPage";

export function VerifyEmailPage() {
  const navigate = useApp((s) => s.navigate);
  const route = useApp((s) => s.route);
  const emailFromUrl = route.query.email || "";
  const [email, setEmail] = useState(emailFromUrl);
  const [otp, setOtp] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (emailFromUrl) setEmail(emailFromUrl);
  }, [emailFromUrl]);

  const onVerify = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
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

  const onResend = async () => {
    try {
      await authApi.resendOtp(email);
      toast.success("OTP resent! Check your email.");
    } catch (err) {
      toast.error("Failed to resend OTP");
    }
  };

  return (
    <AuthShell
      title="Verify your email"
      subtitle={email ? `We sent a 6-digit code to ${email}` : "Enter your email and the verification code"}
      footerText="Already verified?"
      footerLink="/login"
      footerLabel="Sign in"
    >
      <form onSubmit={onVerify} className="space-y-5">
        {error && (
          <div className="flex items-start gap-2 p-3 rounded-lg bg-rose-50 text-rose-900 text-sm border border-rose-200">
            <AlertCircle className="h-4 w-4 shrink-0 mt-0.5" />
            {error}
          </div>
        )}

        {!emailFromUrl && (
          <div className="space-y-2">
            <Label htmlFor="email" className="flex items-center gap-1.5">
              <Mail className="h-3.5 w-3.5" />
              Email address
            </Label>
            <Input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
              required
              autoFocus
              className="h-11"
            />
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
            inputMode="numeric"
            className="h-12 text-center text-2xl tracking-[0.5em] font-mono"
          />
        </div>

        <Button
          type="submit"
          disabled={loading || otp.length !== 6 || !email}
          className="w-full h-11 bg-emerald-700 hover:bg-emerald-800 text-white"
        >
          {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : "Verify email"}
        </Button>

        <button
          type="button"
          onClick={onResend}
          disabled={!email}
          className="w-full text-sm text-emerald-700 hover:text-emerald-800 font-medium"
        >
          Resend code
        </button>
      </form>
    </AuthShell>
  );
}
