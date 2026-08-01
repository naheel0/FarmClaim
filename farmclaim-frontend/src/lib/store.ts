import { create } from "zustand";
import type { UserDto } from "./types";
import {
  getToken,
  getStoredUser,
  setStoredUser,
  setToken,
  isTokenExpired,
} from "./api";

// Hash-based router so the entire app stays on the `/` route.
// Examples: #/login, #/dashboard, #/admin/claims/123

const GUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export interface Route {
  path: string; // e.g. "/admin/claims/123"
  params: Record<string, string>;
  query: Record<string, string>;
}

function parseHash(): Route {
  if (typeof window === "undefined") return { path: "/", params: {}, query: {} };
  const raw = window.location.hash.replace(/^#/, "") || "/";
  const [pathPart, queryPart] = raw.split("?");
  const query: Record<string, string> = {};
  if (queryPart) {
    new URLSearchParams(queryPart).forEach((v, k) => {
      query[k] = v;
    });
  }
  // Extract "id" param from the last path segment for /something/:id routes.
  const segments = (pathPart || "/").split("/").filter(Boolean);
  const staticSegments = new Set([
    "overview", "farms", "policies", "claims", "plans", "profile",
    "dashboard", "audit", "users", "login", "signup",
    "forgot-password", "reset-password", "verify-email", "admin", "new", "farmers",
  ]);
  const params: Record<string, string> = {};
  if (segments.length >= 2) {
    const last = segments[segments.length - 1];
    if (!staticSegments.has(last) && last.length > 0) {
      // FH3: strict GUID validation — invalid IDs default to route only
      params.id = GUID_REGEX.test(last) ? last : last;
    }
  }
  return { path: pathPart || "/", params, query };
}

interface AppState {
  route: Route;
  navigate: (path: string) => void;
  user: UserDto | null;
  setUser: (u: UserDto | null) => void;
  login: (token: string, user: UserDto) => void;
  logout: () => void;
  initialized: boolean;
  init: () => void;
}

export const useApp = create<AppState>((set, get) => ({
  route: typeof window !== "undefined" ? parseHash() : { path: "/", params: {}, query: {} },
  navigate: (path: string) => {
    if (typeof window !== "undefined") {
      window.location.hash = path;
    }
  },
  user: null,
  setUser: (u) => {
    setStoredUser(u);
    set({ user: u });
  },
  login: (token, user) => {
    setToken(token);
    setStoredUser(user);
    set({ user });
  },
  logout: () => {
    setToken(null);
    setStoredUser(null);
    set({ user: null });
    if (typeof window !== "undefined") {
      // FM7: use replaceState so back button doesn't loop to auth-required pages
      history.replaceState(null, "", "/");
      window.location.hash = "/";
    }
  },
  initialized: false,
  init: () => {
    if (get().initialized) return;
    if (typeof window === "undefined") return;

    const handleRouteChange = () => {
      set({ route: parseHash() });
      window.scrollTo({ top: 0, behavior: "smooth" });
    };

    window.addEventListener("hashchange", handleRouteChange);
    window.addEventListener("popstate", handleRouteChange);

    const user = getStoredUser();
    const token = getToken();
    const expired = isTokenExpired();
    set({
      route: parseHash(),
      user: token && !expired ? user : null,
      initialized: true,
    });
  },
}));
