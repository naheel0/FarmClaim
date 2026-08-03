import type {
  AdminDashboardDto,
  AuditLogDto,
  ClaimResponseDto,
  ClaimTimelineEntryDto,
  CreateClaimRequestDto,
  CreateFarmRequestDto,
  CreatePlanRequestDto,
  CreatePolicyRequestDto,
  FarmerProfileDto,
  FarmResponseDto,
  IncidentType,
  InsurancePlanDto,
  LoginRequestDto,
  PagedResult,
  PolicyResponseDto,
  RegisterRequestDto,
  UpdateProfileRequestDto,
  UserDto,
} from "./types";

export const API_BASE = (typeof process !== "undefined" && process.env.NEXT_PUBLIC_API_BASE_URL) || "https://localhost:7251";

const TOKEN_KEY = "farmclaim.token";
const USER_KEY = "farmclaim.user";

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}
export function setToken(t: string | null) {
  if (typeof window === "undefined") return;
  if (t) localStorage.setItem(TOKEN_KEY, t);
  else localStorage.removeItem(TOKEN_KEY);
}
export function getStoredUser(): UserDto | null {
  if (typeof window === "undefined") return null;
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as UserDto;
  } catch {
    return null;
  }
}
export function setStoredUser(u: UserDto | null) {
  if (typeof window === "undefined") return;
  if (u) localStorage.setItem(USER_KEY, JSON.stringify(u));
  else localStorage.removeItem(USER_KEY);
}

function getTokenExpiry(token: string): number | null {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return payload.exp ? payload.exp * 1000 : null;
  } catch {
    return null;
  }
}

export function isTokenExpired(): boolean {
  const token = getToken();
  if (!token) return true;
  const expiry = getTokenExpiry(token);
  if (!expiry) return true;   // treat un-parseable tokens as expired
  return Date.now() >= expiry - 60000;
}

class ApiError extends Error {
  status: number;
  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

let isRefreshing = false;
let refreshPromise: Promise<string> | null = null;

function clearAuth() {
  setToken(null);
  setStoredUser(null);
  if (typeof window !== "undefined") {
    window.location.hash = "/login";
  }
}

async function doRefresh(): Promise<string> {
  const res = await fetch(`${API_BASE}/api/v1/Auth/refresh`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
  });
  if (!res.ok) throw new Error("Refresh failed");
  const data = await res.json();
  setToken(data.accessToken);
  setStoredUser(data.user);
  return data.accessToken;
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string>),
  };
  const method = (options.method || "GET").toUpperCase();
  if (options.body && method !== "GET") {
    headers["Content-Type"] = "application/json";
  }
  if (token) headers["Authorization"] = `Bearer ${token}`;

  let res = await fetch(`${API_BASE}${path}`, { ...options, headers });

  if (res.status === 401 && !path.includes("/Auth/")) {
    try {
      if (!isRefreshing) {
        isRefreshing = true;
        refreshPromise = doRefresh();
      }
      const newToken = await refreshPromise;
      headers["Authorization"] = `Bearer ${newToken}`;
      res = await fetch(`${API_BASE}${path}`, { ...options, headers });
    } catch {
      clearAuth();
      throw new ApiError("Session expired. Please sign in.", 401);
    } finally {
      isRefreshing = false;
      refreshPromise = null;
    }
  }

  // C2 FIX: Only retry on safe methods (GET/HEAD). Retrying POST/PUT/DELETE causes
  // duplicate policies, double-filed claims, and double-charged payments.
  if (!res.ok && res.status >= 500 && res.status < 600 && (method === "GET" || method === "HEAD")) {
    await new Promise((r) => setTimeout(r, 1500));
    res = await fetch(`${API_BASE}${path}`, { ...options, headers });
  }

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    // M24 FIX: sanitize error messages — strip HTML, stack traces, and JSON payloads
    const safe = text
      .replace(/<[^>]+>/g, "")     // strip HTML tags
      .replace(/\{[\s\S]*\}/g, "") // strip JSON objects
      .replace(/\[[\s\S]*\]/g, "") // strip JSON arrays
      .replace(/at\s+.*?\n/g, "")  // strip stack trace lines
      .replace(/\s{2,}/g, " ")     // collapse whitespace
      .trim()
      .slice(0, 200);              // cap length
    throw new ApiError(safe || res.statusText || "Something went wrong", res.status);
  }
  if (res.status === 204) return undefined as T;
  const ct = res.headers.get("content-type") || "";
  if (ct.includes("application/json")) return (await res.json()) as T;
  return undefined as T;
}

// Extract items from a paginated response, or return the value as-is if it's already an array.
function extractItems<T>(data: any): T[] {
  if (Array.isArray(data)) return data as T[];
  if (data && typeof data === "object" && Array.isArray(data.items)) return data.items as T[];
  return [];
}

// ---- AUTH ----
export const authApi = {
  register: (dto: RegisterRequestDto) =>
    request<{ userId: string; requiresEmailVerification: boolean }>(
      "/api/v1/Auth/register",
      { method: "POST", body: JSON.stringify(dto) }
    ),
  verifyEmail: (email: string, otp: string) =>
    request<any>(
      "/api/v1/Auth/verify-email",
      { method: "POST", body: JSON.stringify({ email, otp }) }
    ).then((res) => ({ verified: !!res.accessToken })),
  resendOtp: (email: string) =>
    request<any>(
      "/api/v1/Auth/resend-otp",
      { method: "POST", body: JSON.stringify({ email }) }
    ),
  login: (dto: LoginRequestDto) =>
    request<{ accessToken: string; expiresIn: number; user: UserDto }>(
      "/api/v1/Auth/login",
      { method: "POST", body: JSON.stringify(dto) }
    ).then((res) => ({ token: res.accessToken, expiresIn: res.expiresIn, user: res.user })),
  logout: () =>
    request<void>("/api/v1/Auth/logout", { method: "POST" }),
  forgotPassword: (email: string) =>
    request<{ message: string }>(
      "/api/v1/Auth/forgot-password",
      { method: "POST", body: JSON.stringify({ email }) }
    ),
  resetPassword: (email: string, token: string, newPassword: string) =>
    request<{ message: string }>(
      "/api/v1/Auth/reset-password",
      {
        method: "POST",
        body: JSON.stringify({ email, token, newPassword, confirmPassword: newPassword }),
      }
    ),
  changeEmail: (newEmail: string, currentPassword: string) =>
    request<{ message: string }>(
      "/api/v1/Auth/change-email",
      { method: "POST", body: JSON.stringify({ newEmail, currentPassword }) }
    ),
  confirmEmailChange: (token: string, newEmail: string) =>
    request<{ message: string }>(
      "/api/v1/Auth/confirm-email-change",
      { method: "POST", body: JSON.stringify({ token, newEmail }) }
    ),
};

// ---- FARMER ----
export const farmerApi = {
  me: () =>
    request<FarmerProfileDto>("/api/v1/Farmers/me", { method: "GET" }),
  updateProfile: (dto: UpdateProfileRequestDto) =>
    request<FarmerProfileDto>(
      "/api/v1/Farmers/me",
      { method: "PUT", body: JSON.stringify(dto) }
    ),
};

// ---- FARMS ----
export const farmsApi = {
  list: () =>
    request<PagedResult<FarmResponseDto> | FarmResponseDto[]>(
      "/api/v1/Farms",
      { method: "GET" }
    ).then((data) => extractItems<FarmResponseDto>(data)),
  get: (id: string) =>
    request<FarmResponseDto>(`/api/v1/Farms/${id}`, { method: "GET" }),
  create: (dto: CreateFarmRequestDto) =>
    request<FarmResponseDto>(
      "/api/v1/Farms",
      { method: "POST", body: JSON.stringify(dto) }
    ),
  update: (id: string, dto: Partial<CreateFarmRequestDto>) =>
    request<FarmResponseDto>(
      `/api/v1/Farms/${id}`,
      { method: "PUT", body: JSON.stringify(dto) }
    ),
  delete: (id: string) =>
    request<void>(`/api/v1/Farms/${id}`, { method: "DELETE" }),
};

// ---- INSURANCE PLANS (read-only for farmers, CUD goes through adminApi) ----
export const plansApi = {
  list: () =>
    request<PagedResult<InsurancePlanDto> | InsurancePlanDto[]>(
      "/api/v1/InsurancePlans",
      { method: "GET" }
    ).then((data) => extractItems<InsurancePlanDto>(data)),
  get: (id: string) =>
    request<InsurancePlanDto>(
      `/api/v1/InsurancePlans/${id}`,
      { method: "GET" }
    ),
  create: (dto: CreatePlanRequestDto) =>
    request<InsurancePlanDto>(
      "/api/v1/Admin/Plans",
      { method: "POST", body: JSON.stringify(dto) }
    ),
  update: (id: string, dto: Partial<CreatePlanRequestDto>) =>
    request<InsurancePlanDto>(
      `/api/v1/Admin/Plans/${id}`,
      { method: "PUT", body: JSON.stringify(dto) }
    ),
  delete: (id: string) =>
    request<void>(`/api/v1/Admin/Plans/${id}`, { method: "DELETE" }),
  activate: (id: string) =>
    request<void>(
      `/api/v1/Admin/Plans/${id}/activate`,
      { method: "PATCH" }
    ),
  deactivate: (id: string) =>
    request<void>(
      `/api/v1/Admin/Plans/${id}/deactivate`,
      { method: "PATCH" }
    ),
};

// ---- POLICIES ----
export const policiesApi = {
  list: () =>
    request<PagedResult<PolicyResponseDto> | PolicyResponseDto[]>(
      "/api/v1/Policies",
      { method: "GET" }
    ).then((data) => extractItems<PolicyResponseDto>(data)),
  get: (id: string) =>
    request<PolicyResponseDto>(
      `/api/v1/Policies/${id}`,
      { method: "GET" }
    ),
  create: (dto: CreatePolicyRequestDto) =>
    request<PolicyResponseDto>(
      "/api/v1/Policies",
      { method: "POST", body: JSON.stringify(dto) }
    ),
  update: (id: string, dto: Partial<CreatePolicyRequestDto>) =>
    request<PolicyResponseDto>(
      `/api/v1/Policies/${id}`,
      { method: "PUT", body: JSON.stringify(dto) }
    ),
  delete: (id: string) =>
    request<void>(`/api/v1/Policies/${id}`, { method: "DELETE" }),
  renew: (policyId: string, startDate?: string) => {
    const qs = startDate ? `?startDate=${encodeURIComponent(startDate)}` : "";
    return request<PolicyResponseDto>(
      `/api/v1/Policies/${policyId}/renew${qs}`,
      { method: "POST" }
    );
  },
};

// ---- CLAIMS ----
export const claimsApi = {
  list: () =>
    request<PagedResult<ClaimResponseDto> | ClaimResponseDto[]>(
      "/api/v1/Claims",
      { method: "GET" }
    ).then((data) => extractItems<ClaimResponseDto>(data)),
  get: (id: string) =>
    request<ClaimResponseDto>(
      `/api/v1/Claims/${id}`,
      { method: "GET" }
    ),
  create: (dto: CreateClaimRequestDto) =>
    request<ClaimResponseDto>(
      "/api/v1/Claims",
      { method: "POST", body: JSON.stringify(dto) }
    ),
  update: (id: string, dto: Partial<{
    incidentType: IncidentType;
    description: string | null;
    damageDescription: string | null;
    incidentDate: string;
  }>) =>
    request<{ message: string; claim: ClaimResponseDto }>(
      `/api/v1/Claims/${id}`,
      { method: "PUT", body: JSON.stringify(dto) }
    ).then((res) => res.claim),
  delete: (id: string) =>
    request<void>(`/api/v1/Claims/${id}`, { method: "DELETE" }),
  uploadImage: (claimId: string, file: File) => {
    const token = getToken();
    const formData = new FormData();
    formData.append("images", file);
    return fetch(`${API_BASE}/api/v1/Claims/${claimId}/images`, {
      method: "POST",
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: formData,
    }).then(async (res) => {
      if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new ApiError(text || res.statusText, res.status);
      }
      const data = await res.json();
      return { imageUrl: data.images?.[0]?.imageUrl ?? "" };
    });
  },
  uploadImages: (claimId: string, files: File[]) => {
    const token = getToken();
    const formData = new FormData();
    for (const file of files) {
      formData.append("images", file);
    }
    return fetch(`${API_BASE}/api/v1/Claims/${claimId}/images`, {
      method: "POST",
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: formData,
    }).then(async (res) => {
      if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new ApiError(text || res.statusText, res.status);
      }
      return res.json();
    });
  },
  deleteImage: (claimId: string, imageId: string) =>
    request<void>(
      `/api/v1/Claims/${claimId}/images/${imageId}`,
      { method: "DELETE" }
    ),
  getTimeline: (claimId: string) =>
    request<ClaimTimelineEntryDto[]>(
      `/api/v1/Claims/${claimId}/timeline`,
      { method: "GET" }
    ),
};

// ---- PAYMENTS ----
let cachedRazorpayKey: string | null = null;

async function getRazorpayKeyId(): Promise<string> {
  if (cachedRazorpayKey) return cachedRazorpayKey;
  const res = await fetch(`${API_BASE}/api/v1/config/razorpay-key`);
  if (!res.ok) throw new Error("Payment not configured. Please try again later.");
  const data = await res.json();
  if (!data.keyId) throw new Error("Payment key not found.");
  cachedRazorpayKey = data.keyId;
  return data.keyId;
}

let razorpayLoader: Promise<void> | null = null;
function loadRazorpay(): Promise<void> {
  if (typeof window === "undefined") return Promise.reject(new Error("SSR"));
  if ((window as any).Razorpay) return Promise.resolve();
  if (razorpayLoader) return razorpayLoader;
  razorpayLoader = new Promise<void>((resolve, reject) => {
    const s = document.createElement("script");
    s.src = "https://checkout.razorpay.com/v1/checkout.js";
    s.async = true;
    s.onload = () => resolve();
    s.onerror = () => reject(new Error("Failed to load Razorpay SDK"));
    document.body.appendChild(s);
  });
  return razorpayLoader;
}

export interface RazorpayCheckoutResult {
  ok: boolean;
  paymentId?: string;
  orderId?: string;
  signature?: string;
  verified?: boolean;
  error?: string;
}

export const paymentsApi = {
  createOrder: (policyId: string, premiumScheduleId?: string) => {
    const qs = premiumScheduleId ? `?premiumScheduleId=${premiumScheduleId}` : "";
    return request<{ orderId: string; amountInRupees: number; currency: string; razorpayKeyId?: string }>(
      `/api/v1/Payments/create-order/${policyId}${qs}`,
      { method: "POST" }
    );
  },
  verify: (orderId: string, paymentId: string, signature: string) =>
    request<{ success: boolean }>(
      "/api/v1/Payments/verify",
      { method: "POST", body: JSON.stringify({ razorpayOrderId: orderId, razorpayPaymentId: paymentId, razorpaySignature: signature }) }
    ),
  getByPolicy: (policyId: string) =>
    request<any>(
      `/api/v1/Payments/policy/${policyId}`,
      { method: "GET" }
    ),

  checkout: async (
    policyId: string,
    user?: { name?: string; email?: string; phone?: string },
    premiumScheduleId?: string
  ): Promise<RazorpayCheckoutResult> => {
    try {
      const [order, razorpayKey] = await Promise.all([
        paymentsApi.createOrder(policyId, premiumScheduleId),
        getRazorpayKeyId(),
      ]);

      await loadRazorpay();
      const Razorpay = (window as any).Razorpay;
      if (!Razorpay) throw new Error("Razorpay SDK not loaded");

      const options = {
        key: razorpayKey,
        amount: order.amountInRupees * 100,
        currency: order.currency,
        order_id: order.orderId,
        name: "FarmClaim",
        description: premiumScheduleId ? "Crop insurance installment" : "Crop insurance premium",
        image: "/favicon.svg",
        prefill: {
          name: user?.name ?? "",
          email: user?.email ?? "",
          contact: user?.phone ?? "",
        },
        theme: { color: "#047857" },
        handler: () => {},
        modal: { escape: false, backdropclose: false },
        redirect: false,
      };

      return await new Promise<RazorpayCheckoutResult>((resolve) => {
        const rzp = new Razorpay({
          ...options,
          handler: (response: any) => {
            const { razorpay_payment_id, razorpay_order_id, razorpay_signature } = response;
            paymentsApi
              .verify(razorpay_order_id, razorpay_payment_id, razorpay_signature)
              .then((r) =>
                resolve({
                  ok: true,
                  paymentId: razorpay_payment_id,
                  orderId: razorpay_order_id,
                  signature: razorpay_signature,
                  verified: r.success,
                })
              )
              .catch(() =>
                resolve({
                  ok: true,
                  paymentId: razorpay_payment_id,
                  orderId: razorpay_order_id,
                  signature: razorpay_signature,
                  verified: false,
                })
              );
          },
        });
        rzp.on("payment.failed", (resp: any) => {
          resolve({
            ok: false,
            error: resp?.error?.description ?? "Payment failed",
          });
        });
        // C3 FIX: Handle modal dismiss — without this, closing the Razorpay modal
        // leaves the Promise hanging and the UI spinner locked forever.
        rzp.on("modal.ondismiss", () => {
          resolve({
            ok: false,
            error: "Payment cancelled",
          });
        });
        rzp.open();
      });
    } catch (err) {
      return {
        ok: false,
        error: err instanceof Error ? err.message : "Checkout failed",
      };
    }
  },
};

// ---- ADMIN ----
function mapAuditLog(raw: any): AuditLogDto {
  return {
    id: raw.id,
    userId: raw.userId ?? "",
    userName: raw.userEmail ?? raw.userName ?? "",
    userRole: raw.userRole ?? null,
    action: raw.action ?? "",
    resourceType: raw.entityType ?? raw.resourceType ?? "",
    resourceId: raw.entityId ?? raw.resourceId ?? null,
    details: raw.description ?? raw.details ?? null,
    ipAddress: raw.ipAddress ?? null,
    timestamp: raw.timestamp ?? "",
    oldValues: raw.oldValues ?? null,
    newValues: raw.newValues ?? null,
    changedColumns: raw.changedColumns ?? null,
    userAgent: raw.userAgent ?? null,
    correlationId: raw.correlationId ?? null,
    httpMethod: raw.httpMethod ?? null,
    httpPath: raw.httpPath ?? null,
  };
}

function mapDashboardStats(raw: any): AdminDashboardDto {
  return {
    totalUsers: (raw.totalFarmers ?? 0) + 1,
    totalFarmers: raw.totalFarmers ?? 0,
    totalFarms: raw.totalFarms ?? 0,
    totalPolicies: raw.totalPolicies ?? 0,
    totalClaims: raw.totalClaims ?? 0,
    pendingClaims: raw.pendingClaims ?? 0,
    pendingPolicies: raw.pendingPolicies ?? 0,
    // C5 FIX: Use correct backend field names (DashboardStatsDto uses PascalCase from .NET)
    totalPremiumCollected: raw.totalPayoutAmount ?? raw.TotalPayoutAmount ?? 0,
    totalClaimsPaid: raw.paidClaims ?? raw.PaidClaims ?? 0,
    claimsByStatus: {
      Pending: raw.pendingClaims ?? raw.PendingClaims ?? 0,
      UnderReview: raw.underReviewClaims ?? raw.UnderReviewClaims ?? 0,
      Approved: raw.approvedClaims ?? raw.ApprovedClaims ?? 0,
      Rejected: raw.rejectedClaims ?? raw.RejectedClaims ?? 0,
      Paid: raw.paidClaims ?? raw.PaidClaims ?? 0,
    },
    policiesByStatus: {
      Pending: raw.pendingPolicies ?? raw.PendingPolicies ?? 0,
      Active: raw.activePolicies ?? raw.ActivePolicies ?? 0,
      Rejected: raw.rejectedPolicies ?? raw.RejectedPolicies ?? 0,
      Expired: raw.expiredPolicies ?? raw.ExpiredPolicies ?? 0,
    },
    claimsByIncidentType: Object.fromEntries(
      (raw.incidentBreakdown ?? raw.IncidentBreakdown ?? []).map((i: any) => [i.incidentType, i.count])
    ),
    // C5 FIX: Backend returns TopFarms — only use for "top farms" section, NOT as recentClaims
    recentClaims: [],
    premiumTrend: (raw.monthlyTrends ?? raw.MonthlyTrends ?? []).map((m: any) => ({
      month: m.month,
      premium: m.amount ?? 0,
      claims: m.claims ?? 0,
    })),
  };
}

export const adminApi = {
  dashboard: () =>
    request<any>("/api/v1/Admin/Dashboard", { method: "GET" }).then(mapDashboardStats),
  listClaims: (
    page = 1,
    pageSize = 20,
    filters?: { status?: string; incidentType?: string; searchTerm?: string }
  ) =>
    request<PagedResult<ClaimResponseDto>>(
      `/api/v1/Admin/Claims?pageNumber=${page}&pageSize=${pageSize}${
        filters?.status ? `&status=${filters.status}` : ""
      }${filters?.incidentType ? `&incidentType=${filters.incidentType}` : ""}${
        filters?.searchTerm ? `&searchTerm=${encodeURIComponent(filters.searchTerm)}` : ""
      }`,
      { method: "GET" }
    ),
  getClaim: (id: string) =>
    request<ClaimResponseDto>(
      `/api/v1/Admin/Claims/${id}`,
      { method: "GET" }
    ),
  reviewClaim: (id: string) =>
    request<void>(
      `/api/v1/Admin/Claims/${id}/review`,
      { method: "PUT" }
    ),
  approveClaim: (id: string, amount: number, notes?: string) =>
    request<void>(
      `/api/v1/Admin/Claims/${id}/approve`,
      { method: "PUT", body: JSON.stringify({ approvedAmount: amount, adminNotes: notes }) }
    ),
  rejectClaim: (id: string, reason: string) =>
    request<void>(
      `/api/v1/Admin/Claims/${id}/reject`,
      { method: "PUT", body: JSON.stringify({ rejectionReason: reason }) }
    ),
  payClaim: (id: string, reference: string) =>
    request<void>(
      `/api/v1/Admin/Claims/${id}/pay`,
      { method: "PUT", body: JSON.stringify({ paymentReference: reference }) }
    ),
  listUsers: (params?: { page?: number; pageSize?: number; searchTerm?: string; role?: string; status?: string }) => {
    const query = new URLSearchParams();
    if (params?.page) query.set("pageNumber", String(params.page));
    if (params?.pageSize) query.set("pageSize", String(params.pageSize));
    if (params?.searchTerm) query.set("searchTerm", params.searchTerm);
    if (params?.role) query.set("role", params.role);
    if (params?.status) query.set("status", params.status);
    const qs = query.toString();
    return request<PagedResult<any>>(`/api/v1/Admin/Users${qs ? `?${qs}` : ""}`, { method: "GET" });
  },
  getUser: (id: string) =>
    request<UserDto & { status: string; createdAt: string }>(
      `/api/v1/Admin/Users/${id}`,
      { method: "GET" }
    ),
  suspendUser: (id: string, reason: string) =>
    request<void>(
      `/api/v1/Admin/Users/${id}/suspend`,
      { method: "PATCH", body: JSON.stringify({ reason }) }
    ),
  activateUser: (id: string, reason = "Reactivated by admin") =>
    request<void>(
      `/api/v1/Admin/Users/${id}/activate`,
      { method: "PATCH", body: JSON.stringify({ reason }) }
    ),
  blockUser: (id: string, reason: string) =>
    request<void>(
      `/api/v1/Admin/Users/${id}/block`,
      { method: "PATCH", body: JSON.stringify({ reason }) }
    ),
  approvePolicy: (id: string) =>
    request<void>(
      `/api/v1/Admin/Policies/${id}/approve`,
      { method: "PUT" }
    ),
  rejectPolicy: (id: string, reason: string) =>
    request<void>(
      `/api/v1/Admin/Policies/${id}/reject`,
      { method: "PUT", body: JSON.stringify({ reason }) }
    ),
  cancelPolicy: (id: string, reason: string) =>
    request<void>(
      `/api/v1/Admin/Policies/${id}/cancel`,
      { method: "PUT", body: JSON.stringify({ reason }) }
    ),
  listPolicies: (params?: { page?: number; pageSize?: number; status?: string; searchTerm?: string }) => {
    const query = new URLSearchParams();
    if (params?.page) query.set("pageNumber", String(params.page));
    if (params?.pageSize) query.set("pageSize", String(params.pageSize));
    if (params?.status) query.set("status", params.status);
    if (params?.searchTerm) query.set("searchTerm", params.searchTerm);
    const qs = query.toString();
    return request<PagedResult<any>>(`/api/v1/Admin/Policies${qs ? `?${qs}` : ""}`, { method: "GET" });
  },
  listPlans: () =>
    request<PagedResult<InsurancePlanDto> | InsurancePlanDto[]>(
      "/api/v1/Admin/Plans",
      { method: "GET" }
    ).then((data) => extractItems<InsurancePlanDto>(data)),
  listFarmers: (params?: { page?: number; pageSize?: number; searchTerm?: string }) => {
    const query = new URLSearchParams();
    if (params?.page) query.set("pageNumber", String(params.page));
    if (params?.pageSize) query.set("pageSize", String(params.pageSize));
    if (params?.searchTerm) query.set("searchTerm", params.searchTerm);
    const qs = query.toString();
    return request<PagedResult<any>>(`/api/v1/Farmers${qs ? `?${qs}` : ""}`, { method: "GET" });
  },
  getFarmer: (id: string) =>
    request<any>(
      `/api/v1/Farmers/${id}`,
      { method: "GET" }
    ),
  auditLogs: () =>
    request<PagedResult<any>>(
      "/api/v1/Admin/AuditLogs",
      { method: "GET" }
    ).then((data) => extractItems<any>(data).map(mapAuditLog)),
  getAuditLog: (id: string) =>
    request<any>(
      `/api/v1/Admin/AuditLogs/${id}`,
      { method: "GET" }
    ).then(mapAuditLog),
};

// ---- WEATHER ----
export interface WeatherData {
  temperatureCelsius: number;
  feelsLikeCelsius: number;
  humidity: number;
  windSpeedKmh: number;
  precipitation: number;
  weatherCondition: string;
  weatherCode: number;
  dailyMaxTemp: number;
  dailyMinTemp: number;
  dailyRainfall: number;
  dailyMaxWind: number;
  date: string;
  source: string;
}

export const weatherApi = {
  current: (lat: number, lon: number) =>
    request<WeatherData>(
      `/api/v1/Weather/current?lat=${lat}&lon=${lon}`,
      { method: "GET" }
    ),
};
