"use client";

import { useEffect } from "react";
import { useApp } from "@/lib/store";
import { LandingPage } from "@/components/landing/LandingPage";
import { LoginPage } from "@/components/auth/LoginPage";
import { SignupPage } from "@/components/auth/SignupPage";
import { ForgotPasswordPage, ResetPasswordPage } from "@/components/auth/ForgotPasswordPage";
import { FarmerDashboard } from "@/components/farmer/FarmerDashboard";
import { AdminDashboard } from "@/components/admin/AdminDashboard";
import { Button } from "@/components/ui/button";
import { Leaf } from "lucide-react";

export default function Home() {
  const { init, route, user, navigate } = useApp();

  useEffect(() => {
    init();
  }, [init]);

  // Wait for init
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
