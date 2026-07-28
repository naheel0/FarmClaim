"use client";

import { useEffect, useState } from "react";
import { useApp } from "@/lib/store";
import { Button } from "@/components/ui/button";
import {
  Sheet,
  SheetContent,
  SheetTrigger,
  SheetTitle,
  SheetHeader,
} from "@/components/ui/sheet";
import { Leaf, Menu, LogOut, LayoutDashboard, Shield } from "lucide-react";
import { cn } from "@/lib/utils";

const navLinks = [
  { href: "#features", label: "Features" },
  { href: "#how", label: "How it works" },
  { href: "#plans", label: "Insurance Plans" },
  { href: "#testimonials", label: "Farmers" },
  { href: "#faq", label: "FAQ" },
];

export function Navbar() {
  const navigate = useApp((s) => s.navigate);
  const user = useApp((s) => s.user);
  const logout = useApp((s) => s.logout);
  const [scrolled, setScrolled] = useState(false);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 12);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <header
      className={cn(
        "fixed top-0 left-0 right-0 z-50 transition-all duration-300",
        scrolled ? "py-2" : "py-4"
      )}
    >
      <div className="container mx-auto px-4">
        <div
          className={cn(
            "flex items-center justify-between rounded-2xl px-4 py-2.5 transition-all duration-300",
            scrolled ? "glass shadow-lg shadow-emerald-900/5" : "bg-transparent"
          )}
        >
          <button
            onClick={() => navigate("/")}
            className="flex items-center gap-2.5 group"
          >
            <div className="relative h-9 w-9 rounded-xl bg-gradient-to-br from-emerald-600 to-green-700 grid place-items-center shadow-md shadow-emerald-600/20 transition-transform group-hover:scale-105">
              <Leaf className="h-5 w-5 text-white" strokeWidth={2.5} />
              <div className="absolute -top-1 -right-1 h-3 w-3 rounded-full bg-amber-400 ring-2 ring-white" />
            </div>
            <div className="flex flex-col items-start leading-none">
              <span className="font-serif text-lg font-semibold text-foreground">
                FarmClaim
              </span>
              <span className="text-[10px] uppercase tracking-widest text-muted-foreground">
                AI Crop Insurance
              </span>
            </div>
          </button>

          {/* Desktop nav */}
          <nav className="hidden lg:flex items-center gap-1">
            {navLinks.map((link) => (
              <a
                key={link.href}
                href={link.href}
                className="px-3 py-2 text-sm font-medium text-muted-foreground hover:text-foreground hover:bg-foreground/5 rounded-lg transition-colors"
              >
                {link.label}
              </a>
            ))}
          </nav>

          <div className="hidden lg:flex items-center gap-2">
            {user ? (
              <>
                <Button
                  variant="ghost"
                  onClick={() =>
                    navigate(user.role === "Admin" ? "/admin" : "/dashboard")
                  }
                  className="gap-2"
                >
                  {user.role === "Admin" ? (
                    <Shield className="h-4 w-4" />
                  ) : (
                    <LayoutDashboard className="h-4 w-4" />
                  )}
                  {user.role === "Admin" ? "Admin Console" : "Dashboard"}
                </Button>
                <Button variant="outline" size="icon" onClick={logout}>
                  <LogOut className="h-4 w-4" />
                </Button>
              </>
            ) : (
              <>
                <Button
                  variant="ghost"
                  onClick={() => navigate("/login")}
                  className="text-foreground"
                >
                  Sign in
                </Button>
                <Button
                  onClick={() => navigate("/signup")}
                  className="bg-emerald-700 hover:bg-emerald-800 text-white gap-1.5 shadow-md shadow-emerald-700/20"
                >
                  Get Started
                </Button>
              </>
            )}
          </div>

          {/* Mobile menu */}
          <Sheet open={open} onOpenChange={setOpen}>
            <SheetTrigger asChild>
              <Button variant="ghost" size="icon" className="lg:hidden">
                <Menu className="h-5 w-5" />
              </Button>
            </SheetTrigger>
            <SheetContent className="w-[280px]">
              <SheetHeader>
                <SheetTitle className="flex items-center gap-2">
                  <div className="h-8 w-8 rounded-lg bg-gradient-to-br from-emerald-600 to-green-700 grid place-items-center">
                    <Leaf className="h-4 w-4 text-white" />
                  </div>
                  FarmClaim
                </SheetTitle>
              </SheetHeader>
              <div className="mt-6 flex flex-col gap-1">
                {navLinks.map((link) => (
                  <a
                    key={link.href}
                    href={link.href}
                    onClick={() => setOpen(false)}
                    className="px-3 py-2.5 text-sm font-medium text-foreground hover:bg-foreground/5 rounded-lg"
                  >
                    {link.label}
                  </a>
                ))}
                <div className="h-px bg-border my-2" />
                {user ? (
                  <>
                    <Button
                      variant="outline"
                      onClick={() => {
                        setOpen(false);
                        navigate(
                          user.role === "Admin" ? "/admin" : "/dashboard"
                        );
                      }}
                      className="justify-start"
                    >
                      {user.role === "Admin" ? "Admin Console" : "Dashboard"}
                    </Button>
                    <Button
                      variant="ghost"
                      onClick={() => {
                        setOpen(false);
                        logout();
                      }}
                      className="justify-start text-rose-600"
                    >
                      Sign out
                    </Button>
                  </>
                ) : (
                  <>
                    <Button
                      variant="outline"
                      onClick={() => {
                        setOpen(false);
                        navigate("/login");
                      }}
                    >
                      Sign in
                    </Button>
                    <Button
                      onClick={() => {
                        setOpen(false);
                        navigate("/signup");
                      }}
                      className="bg-emerald-700 hover:bg-emerald-800 text-white"
                    >
                      Get Started
                    </Button>
                  </>
                )}
              </div>
            </SheetContent>
          </Sheet>
        </div>
      </div>
    </header>
  );
}
