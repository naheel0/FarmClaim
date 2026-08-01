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
      // FH3: strict GUID validation — invalid IDs fall back to route without id
      params.id = GUID_REGEX.test(last) ? last : "";
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

// H7 FIX: Store listener references so they can be cleaned up on unmount
let _hashChangeHandler: (() => void) | null = null;
let _popStateHandler: (() => void) | null = null;
let _storageHandler: ((e: StorageEvent) => void) | null = null;

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
      // H4 FIX: Use location.replace to fully replace current history entry
      // instead of history.replaceState + setting hash separately.
      // The old approach left stale hash entries causing infinite Back-button loops.
      window.location.replace(window.location.pathname + "#/");
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

    // H7 FIX: Store references so we can clean up
    _hashChangeHandler = handleRouteChange;
    _popStateHandler = handleRouteChange;
    window.addEventListener("hashchange", _hashChangeHandler);
    window.addEventListener("popstate", _popStateHandler);

    // H6 FIX: Listen for cross-tab storage changes.
    // When user logs out in Tab A, Tab B immediately syncs.
    _storageHandler = (e: StorageEvent) => {
      if (e.key === "farmclaim.token" && e.newValue === null) {
        // Token removed in another tab — sync logout
        set({ user: null });
        if (typeof window !== "undefined") {
          window.location.replace(window.location.pathname + "#/");
        }
      } else if (e.key === "farmclaim.user" && e.newValue) {
        try {
          const user = JSON.parse(e.newValue) as UserDto;
          set({ user });
        } catch { /* ignore malformed */ }
      }
    };
    window.addEventListener("storage", _storageHandler);

    const user = getStoredUser();
    const token = getToken();
    const expired = isTokenExpired();

    // H5 FIX: Purge stale token from localStorage if expired
    if (token && expired) {
      setToken(null);
      setStoredUser(null);
    }

    set({
      route: parseHash(),
      user: token && !expired ? user : null,
      initialized: true,
    });
  },
}));
