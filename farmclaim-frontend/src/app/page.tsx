"use client";

import { Component, type ReactNode, useEffect } from "react";
import { useApp } from "@/lib/store";
import { LandingPage } from "@/components/landing/LandingPage";
import { LoginPage } from "@/components/auth/LoginPage";
import { SignupPage } from "@/components/auth/SignupPage";
import { ForgotPasswordPage, ResetPasswordPage } from "@/components/auth/ForgotPasswordPage";
import { VerifyEmailPage } from "@/components/auth/VerifyEmailPage";
import { FarmerDashboard } from "@/components/farmer/FarmerDashboard";
import { AdminDashboard } from "@/components/admin/AdminDashboard";
import { Button } from "@/components/ui/button";
import { Leaf } from "lucide-react";

// FL2: Error boundary to prevent white-screen crashes
class ErrorBoundary extends Component<
  { children: ReactNode; fallback?: ReactNode },
  { hasError: boolean; error: Error | null }
> {
  state = { hasError: false, error: null as Error | null };
  static getDerivedStateFromError(error: Error) { return { hasError: true, error }; }
  render() {
    if (this.state.hasError) {
      return (
        this.props.fallback ?? (
          <div className="min-h-screen grid place-items-center bg-background px-4">
            <div className="text-center max-w-md">
              <div className="h-12 w-12 rounded-2xl bg-red-100 grid place-items-center mx-auto mb-4">
                <Leaf className="h-6 w-6 text-red-700" />
              </div>
              <h1 className="font-serif text-2xl font-semibold">Something went wrong</h1>
              <p className="text-muted-foreground mt-2">
                An unexpected error occurred. Please try refreshing the page.
              </p>
              <Button
                onClick={() => { this.setState({ hasError: false, error: null }); window.location.reload(); }}
                className="mt-6 bg-emerald-700 hover:bg-emerald-800 text-white"
              >
                Reload page
              </Button>
            </div>
          </div>
        )
      );
    }
    return this.props.children;
  }
}

const PAGE_TITLES: Record<string, string> = {
  "/": "FarmClaim",
  "/login": "Sign In — FarmClaim",
  "/signup": "Create Account — FarmClaim",
  "/forgot-password": "Reset Password — FarmClaim",
  "/reset-password": "Reset Password — FarmClaim",
  "/verify-email": "Verify Email — FarmClaim",
};

function getTitle(path: string): string {
  if (path.startsWith("/admin")) return "Admin — FarmClaim";
  if (path.startsWith("/dashboard/claims")) return "My Claims — FarmClaim";
  if (path.startsWith("/dashboard/policies")) return "My Policies — FarmClaim";
  if (path.startsWith("/dashboard/farms")) return "My Farms — FarmClaim";
  if (path.startsWith("/dashboard/plans")) return "Browse Plans — FarmClaim";
  if (path.startsWith("/dashboard/profile")) return "Profile — FarmClaim";
  if (path.startsWith("/dashboard")) return "Dashboard — FarmClaim";
  return PAGE_TITLES[path] ?? "FarmClaim";
}

export default function PageWithBoundary() {
  return (
    <ErrorBoundary>
      <Home />
    </ErrorBoundary>
  );
}

function Home() {
  const { init, route, user, navigate } = useApp();

  useEffect(() => {
    init();
  }, [init]);

  // FM2: update document.title on every route change
  useEffect(() => {
    document.title = getTitle(route.path);
  }, [route.path]);

  if (!useApp.getState().initialized) {
    return (
      <div className="min-h-screen grid place-items-center bg-background">
        <div className="flex flex-col items-center gap-3">
          <div className="h-12 w-12 rounded-2xl bg-gradient-to-br from-emerald-600 to-green-700 grid place-items-center shadow-lg shadow-emerald-600/30 animate-pulse">
            <Leaf className="h-6 w-6 text-white" strokeWidth={2.5} />
          </div>
          <div className="text-sm text-muted-foreground">Loading FarmClaim…</div>
        </div>
      </div>
    );
  }

  const path = route.path;

  // Public routes
  if (path === "/" || path === "") return <LandingPage />;
  if (path === "/login") return <LoginPage />;
  if (path === "/signup") return <SignupPage />;
  if (path === "/verify-email") return <VerifyEmailPage />;
  if (path === "/forgot-password") return <ForgotPasswordPage />;
  if (path === "/reset-password") return <ResetPasswordPage />;

  // Protected: dashboard (farmer)
  if (path.startsWith("/dashboard")) {
    if (!user) {
      return <RedirectToLogin role="Farmer" />;
    }
    if (user.role !== "Farmer") {
      return <WrongRole role={user.role ?? "Farmer"} />;
    }
    return <FarmerDashboard />;
  }

  // Protected: admin
  if (path.startsWith("/admin")) {
    if (!user) {
      return <RedirectToLogin role="Admin" />;
    }
    if (user.role !== "Admin") {
      return <WrongRole role={user.role ?? "Admin"} />;
    }
    return <AdminDashboard />;
  }

  // 404
  return (
    <div className="min-h-screen grid place-items-center bg-background px-4">
      <div className="text-center max-w-md">
        <div className="font-serif text-7xl font-bold gradient-text">404</div>
        <h1 className="font-serif text-2xl font-semibold mt-4">Page not found</h1>
        <p className="text-muted-foreground mt-2">
          The page you&apos;re looking for doesn&apos;t exist or has been moved.
        </p>
        <Button
          onClick={() => navigate("/")}
          className="mt-6 bg-emerald-700 hover:bg-emerald-800 text-white"
        >
          Back to home
        </Button>
      </div>
    </div>
  );
}

function RedirectToLogin({ role }: { role: string }) {
  const navigate = useApp((s) => s.navigate);
  useEffect(() => {
    const t = setTimeout(() => navigate("/login"), 1500);
    return () => clearTimeout(t);
  }, [navigate]);
  return (
    <div className="min-h-screen grid place-items-center bg-background px-4">
      <div className="text-center max-w-md">
        <div className="h-12 w-12 rounded-2xl bg-emerald-100 grid place-items-center mx-auto mb-4">
          <Leaf className="h-6 w-6 text-emerald-700" />
        </div>
        <h1 className="font-serif text-2xl font-semibold">Sign in required</h1>
        <p className="text-muted-foreground mt-2">
          Redirecting you to the {role} sign-in page…
        </p>
      </div>
    </div>
  );
}

function WrongRole({ role }: { role: string }) {
  const navigate = useApp((s) => s.navigate);
  const logout = useApp((s) => s.logout);
  return (
    <div className="min-h-screen grid place-items-center bg-background px-4">
      <div className="text-center max-w-md">
        <div className="h-12 w-12 rounded-2xl bg-amber-100 grid place-items-center mx-auto mb-4">
          <Leaf className="h-6 w-6 text-amber-700" />
        </div>
        <h1 className="font-serif text-2xl font-semibold">Wrong account type</h1>
        <p className="text-muted-foreground mt-2">
          You&apos;re signed in as <span className="font-semibold text-foreground">{role}</span>,
          but this area requires a different role. Please sign out and sign back in with the
          correct account.
        </p>
        <Button onClick={logout} className="mt-6 bg-emerald-700 hover:bg-emerald-800 text-white">
          Sign out
        </Button>
      </div>
    </div>
  );
}
