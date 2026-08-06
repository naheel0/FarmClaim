"use client";

import { useEffect, useState } from "react";
import { useApp } from "@/lib/store";
import { Button } from "@/components/ui/button";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Sheet, SheetContent, SheetTrigger, SheetTitle, SheetHeader } from "@/components/ui/sheet";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn, initials } from "@/lib/utils";
import { useNotifications } from "@/lib/notifications";
import { Leaf, Menu, LogOut, Bell, ExternalLink, ChevronRight, CheckCircle2, AlertTriangle, Cloud, Brain } from "lucide-react";
import { GlobalSearch } from "./GlobalSearch";

export interface NavItem {
  label: string;
  path: string;
  icon: React.ElementType;
  badge?: number;
}

export function DashboardShell({
  navItems,
  brandLabel,
  brandSub,
  children,
}: {
  navItems: NavItem[];
  brandLabel: string;
  brandSub: string;
  children: React.ReactNode;
}) {
  const { user, logout, route, navigate } = useApp();
  const { notifications, unreadCount, markAsRead, markAllRead } = useNotifications();
  const [open, setOpen] = useState(false);

  const currentPath = route.path;
  const activeItem = navItems.find(
    (n) => currentPath === n.path || (n.path !== "/" && currentPath.startsWith(n.path))
  );

  // Auto-redirect to first nav item if route is the bare prefix
  useEffect(() => {
    if (currentPath === "/dashboard") {
      navigate("/dashboard/overview");
    } else if (currentPath === "/admin") {
      navigate("/admin/dashboard");
    }
  }, [currentPath, navigate]);

  return (
    <div className="min-h-screen flex bg-muted/30">
      {/* Sidebar — desktop */}
      <aside className="hidden lg:flex w-64 shrink-0 border-r bg-card flex-col fixed inset-y-0 left-0">
        <SidebarContent
          navItems={navItems}
          brandLabel={brandLabel}
          brandSub={brandSub}
          currentPath={currentPath}
          onNavigate={(p) => navigate(p)}
          user={user}
          onLogout={logout}
        />
      </aside>

      {/* Sidebar — mobile */}
      <Sheet open={open} onOpenChange={setOpen}>
        <SheetTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="lg:hidden fixed top-4 left-4 z-40 bg-card shadow-md h-10 w-10"
          >
            <Menu className="h-5 w-5" />
          </Button>
        </SheetTrigger>
        <SheetContent side="left" className="w-72 p-0">
          <SheetHeader className="sr-only">
            <SheetTitle>Navigation</SheetTitle>
          </SheetHeader>
          <SidebarContent
            navItems={navItems}
            brandLabel={brandLabel}
            brandSub={brandSub}
            currentPath={currentPath}
            onNavigate={(p) => {
              navigate(p);
              setOpen(false);
            }}
            user={user}
            onLogout={logout}
          />
        </SheetContent>
      </Sheet>

      {/* Main content */}
      <div className="flex-1 lg:pl-64 flex flex-col min-w-0">
        {/* Topbar */}
        <header className="sticky top-0 z-30 glass border-b border-border/60">
          <div className="flex items-center gap-3 px-4 lg:px-8 py-3">
            <div className="lg:hidden w-10" />
            <div className="flex-1 max-w-md">
              <GlobalSearch />
            </div>

            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon" className="relative">
                  <Bell className="h-5 w-5" />
                  {unreadCount > 0 && (
                    <span className="absolute top-1 right-1 h-4 w-4 rounded-full bg-rose-500 text-[10px] font-bold text-white grid place-items-center ring-2 ring-card">
                      {unreadCount > 9 ? "9+" : unreadCount}
                    </span>
                  )}
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-80 max-h-96 overflow-y-auto">
                <DropdownMenuLabel className="flex items-center justify-between">
                  <span>Notifications</span>
                  {unreadCount > 0 && (
                    <button onClick={markAllRead} className="text-xs text-emerald-700 hover:text-emerald-800 font-medium">
                      Mark all read
                    </button>
                  )}
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                {notifications.length === 0 ? (
                  <div className="px-4 py-8 text-center text-sm text-muted-foreground">
                    No notifications yet
                  </div>
                ) : (
                  notifications.slice(0, 20).map((n, i) => (
                    <DropdownMenuItem
                      key={`${n.claimId}-${n.timestamp}-${i}`}
                      onClick={() => markAsRead(i)}
                      className={cn(
                        "flex items-start gap-3 py-3 cursor-pointer",
                        !n.read && "bg-emerald-50/50"
                      )}
                    >
                      <div className="h-8 w-8 rounded-lg grid place-items-center shrink-0 mt-0.5">
                        {n.notificationType === "StatusChanged" && <CheckCircle2 className="h-4 w-4 text-emerald-600" />}
                        {n.notificationType === "PolicyStatusChanged" && <CheckCircle2 className="h-4 w-4 text-blue-600" />}
                        {n.notificationType === "WeatherComplete" && <Cloud className="h-4 w-4 text-sky-600" />}
                        {n.notificationType === "AIComplete" && <Brain className="h-4 w-4 text-purple-600" />}
                        {n.notificationType === "PolicyExpiryReminder" && <AlertTriangle className="h-4 w-4 text-amber-600" />}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="text-sm font-medium leading-snug">{n.title}</div>
                        <div className="text-xs text-muted-foreground mt-0.5 line-clamp-2">{n.message}</div>
                        <div className="text-[10px] text-muted-foreground mt-1">
                          {new Date(n.timestamp).toLocaleString()}
                        </div>
                      </div>
                      {!n.read && (
                        <span className="h-2 w-2 rounded-full bg-emerald-500 shrink-0 mt-2" />
                      )}
                    </DropdownMenuItem>
                  ))
                )}
              </DropdownMenuContent>
            </DropdownMenu>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => navigate("/")}
              className="hidden sm:flex gap-2 text-muted-foreground"
            >
              <ExternalLink className="h-3.5 w-3.5" />
              Home
            </Button>
            <Button
              variant="ghost"
              size="icon"
              onClick={logout}
              title="Sign out"
              className="lg:hidden"
            >
              <LogOut className="h-4 w-4" />
            </Button>
          </div>
        </header>

        {/* Breadcrumb */}
        <div className="px-4 lg:px-8 py-3 border-b bg-card/50">
          <div className="flex items-center gap-1.5 text-sm">
            <span className="text-muted-foreground">{brandLabel}</span>
            {activeItem && activeItem.path !== currentPath && (
              <>
                <ChevronRight className="h-3.5 w-3.5 text-muted-foreground" />
                <span className="font-medium text-foreground">
                  {activeItem.label}
                </span>
              </>
            )}
            {activeItem && activeItem.path === currentPath && (
              <span className="font-medium text-foreground">{activeItem.label}</span>
            )}
          </div>
        </div>

        <main className="flex-1 p-4 lg:p-8">{children}</main>
      </div>
    </div>
  );
}

function SidebarContent({
  navItems,
  brandLabel,
  brandSub,
  currentPath,
  onNavigate,
  user,
  onLogout,
}: {
  navItems: NavItem[];
  brandLabel: string;
  brandSub: string;
  currentPath: string;
  onNavigate: (p: string) => void;
  user: ReturnType<typeof useApp.getState>["user"];
  onLogout: () => void;
}) {
  return (
    <div className="flex flex-col h-full">
      {/* Brand */}
      <div className="p-5 border-b">
        <button
          onClick={() => onNavigate("/")}
          className="flex items-center gap-2.5 group"
        >
          <div className="h-9 w-9 rounded-xl bg-gradient-to-br from-emerald-600 to-green-700 grid place-items-center shadow-md shadow-emerald-600/20 transition-transform group-hover:scale-105">
            <Leaf className="h-5 w-5 text-white" strokeWidth={2.5} />
          </div>
          <div className="flex flex-col items-start leading-none">
            <span className="font-serif text-base font-semibold">{brandLabel}</span>
            <span className="text-[10px] uppercase tracking-widest text-muted-foreground">
              {brandSub}
            </span>
          </div>
        </button>
      </div>

      {/* Nav */}
      <nav className="flex-1 overflow-y-auto p-3 space-y-1">
        {navItems.map((item) => {
          const isActive =
            currentPath === item.path ||
            (item.path !== "/" && currentPath.startsWith(item.path + "/"));
          return (
            <button
              key={item.path}
              onClick={() => onNavigate(item.path)}
              className={cn(
                "w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all group",
                isActive
                  ? "bg-emerald-50 text-emerald-700 shadow-sm"
                  : "text-muted-foreground hover:bg-foreground/5 hover:text-foreground"
              )}
            >
              <item.icon
                className={cn(
                  "h-4.5 w-4.5 transition-colors",
                  isActive ? "text-emerald-700" : "text-muted-foreground group-hover:text-foreground"
                )}
                style={{ width: "1.125rem", height: "1.125rem" }}
              />
              <span className="flex-1 text-left">{item.label}</span>
              {item.badge ? (
                <span className="text-[10px] font-semibold bg-rose-100 text-rose-700 rounded-full px-1.5 py-0.5 min-w-[18px] text-center">
                  {item.badge}
                </span>
              ) : null}
            </button>
          );
        })}
      </nav>

      {/* User card */}
      <div className="p-3 border-t">
        <button
          onClick={() => onNavigate(user?.role === "Admin" ? "/admin/profile" : "/dashboard/profile")}
          className="w-full flex items-center gap-3 p-2 rounded-lg hover:bg-foreground/5 transition-colors"
        >
          <Avatar className="h-9 w-9 bg-gradient-to-br from-emerald-500 to-green-700 text-white">
            <AvatarFallback className="bg-transparent text-white text-xs font-semibold">
              {initials(user?.firstName, user?.lastName)}
            </AvatarFallback>
          </Avatar>
          <div className="flex-1 text-left min-w-0">
            <div className="text-sm font-medium truncate">
              {user?.firstName} {user?.lastName}
            </div>
            <div className="text-xs text-muted-foreground truncate">{user?.email}</div>
          </div>
        </button>
        <Button
          variant="ghost"
          size="sm"
          onClick={onLogout}
          className="w-full justify-start text-rose-600 hover:text-rose-700 hover:bg-rose-50 mt-1"
        >
          <LogOut className="h-4 w-4 mr-2" />
          Sign out
        </Button>
      </div>
    </div>
  );
}

export function PageHeader({
  title,
  subtitle,
  actions,
}: {
  title: string;
  subtitle?: string;
  actions?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 mb-6">
      <div>
        <h1 className="font-serif text-2xl lg:text-3xl font-semibold tracking-tight">
          {title}
        </h1>
        {subtitle && <p className="text-muted-foreground mt-1">{subtitle}</p>}
      </div>
      {actions && <div className="flex gap-2">{actions}</div>}
    </div>
  );
}

export function CardStat({
  label,
  value,
  delta,
  icon: Icon,
  accent = "emerald",
}: {
  label: string;
  value: string | number;
  delta?: { value: string; up: boolean };
  icon: React.ElementType;
  accent?: "emerald" | "amber" | "rose" | "blue";
}) {
  const colors: Record<string, string> = {
    emerald: "bg-emerald-100 text-emerald-700",
    amber: "bg-amber-100 text-amber-700",
    rose: "bg-rose-100 text-rose-700",
    blue: "bg-blue-100 text-blue-700",
  };
  return (
    <div className="rounded-xl border bg-card p-5">
      <div className="flex items-start justify-between">
        <div className={cn("h-10 w-10 rounded-lg grid place-items-center", colors[accent])}>
          <Icon className="h-5 w-5" />
        </div>
        {delta && (
          <span
            className={cn(
              "text-xs font-semibold px-2 py-0.5 rounded-full",
              delta.up
                ? "bg-emerald-100 text-emerald-700"
                : "bg-rose-100 text-rose-700"
            )}
          >
            {delta.up ? "↑" : "↓"} {delta.value}
          </span>
        )}
      </div>
      <div className="mt-4">
        <div className="text-2xl lg:text-3xl font-bold font-serif tracking-tight">
          {value}
        </div>
        <div className="text-sm text-muted-foreground mt-0.5">{label}</div>
      </div>
    </div>
  );
}


