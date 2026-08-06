// Domain types — mirror FarmClaim API DTOs

export type UserRole = "Farmer" | "Admin";
export type UserStatus =
  | "Active"
  | "Suspended"
  | "Blocked"
  | "PendingVerification";

export type PolicyStatus =
  | "Pending"
  | "Active"
  | "Expired"
  | "Rejected"
  | "Cancelled"
  | "PaymentReceived";

export type ClaimStatus =
  | "Pending"
  | "UnderReview"
  | "Approved"
  | "Rejected"
  | "Paid";

export type PaymentStatus =
  | "Created"
  | "Attempted"
  | "Captured"
  | "Failed"
  | "Refunded"
  | "Expired";

export type InstallmentFrequency = "Monthly" | "Quarterly" | "Annually";

export type PremiumScheduleStatus =
  | "Pending"
  | "Paid"
  | "Overdue"
  | "Waived";

export type IncidentType =
  | "Flood"
  | "Drought"
  | "HeavyRain"
  | "Hail"
  | "Frost"
  | "PestInfestation"
  | "Fire"
  | "Windstorm"
  | "Other";

export interface UserDto {
  id: string;
  email: string | null;
  role: string | null;
  firstName: string | null;
  lastName: string | null;
  phoneNumber: string | null;
  status?: UserStatus;
  createdAt?: string;
}

export interface FarmerProfileDto {
  id: string;
  email: string | null;
  role: string | null;
  firstName: string | null;
  lastName: string | null;
  phoneNumber: string | null;
  createdAt: string;
  lastLoginAt: string | null;
  totalFarms: number;
  totalPolicies: number;
  totalClaims: number;
}

export interface FarmResponseDto {
  id: string;
  userId: string;
  name: string | null;
  areaInHectares: number;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  locationGeoJson: string | null;
  createdAt: string;
  updatedAt: string | null;
  isActive: boolean;
  policiesCount: number;
  claimsCount: number;
}

export interface FarmListDto {
  id: string;
  name: string | null;
  areaInHectares: number;
  address: string | null;
  createdAt: string;
  policiesCount: number;
  claimsCount: number;
  isActive: boolean;
}

export interface InsurancePlanDto {
  id: string;
  name: string;
  description: string | null;
  cropType: string;
  provider: string;
  premiumRatePerHectare: number;
  sumInsuredPerHectare: number;
  coveragePercentage: number;
  minAreaInHectares: number | null;
  maxAreaInHectares: number | null;
  policyDurationMonths: number;
  isActive: boolean;
  createdAt: string;
  supportsInstallments: boolean;
  installmentCount: number | null;
  installmentFrequency: InstallmentFrequency | null;
}

export interface PolicyResponseDto {
  id: string;
  farmId: string;
  userId: string;
  farmName: string | null;
  policyNumber: string | null;
  provider: string | null;
  cropType: string | null;
  coverageAmount: number;
  premium: number;
  sumInsured: number;
  startDate: string;
  endDate: string;
  status: PolicyStatus;
  approvedAt: string | null;
  approvedByName: string | null;
  rejectedAt: string | null;
  rejectionReason: string | null;
  cancelledAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  claimsCount: number;
  currentInstallmentNumber: number | null;
  nextInstallmentDueDate: string | null;
  installmentAmount: number | null;
  premiumSchedules: PremiumScheduleDto[] | null;
}

export interface PolicyListDto {
  id: string;
  policyNumber: string | null;
  provider: string | null;
  cropType: string | null;
  coverageAmount: number;
  premium: number;
  sumInsured: number;
  startDate: string;
  endDate: string;
  status: PolicyStatus;
  rejectionReason: string | null;
  farmName: string | null;
  claimsCount: number;
}

export interface PremiumScheduleDto {
  id: string;
  policyId: string;
  installmentNumber: number;
  dueDate: string;
  amountDue: number;
  paymentId: string | null;
  status: PremiumScheduleStatus;
  paidAt: string | null;
}

export interface ClaimTimelineEntryDto {
  timestamp: string;
  action: string;
  description: string | null;
  oldValues: any | null;
  newValues: any | null;
  changedColumns: string | null;
}

export interface ClaimImageDto {
  id: string;
  claimId: string;
  imageUrl: string;
  uploadedAt: string;
}

export interface ClaimResponseDto {
  id: string;
  policyId: string;
  farmId: string;
  userId: string;
  policyNumber: string | null;
  farmName: string | null;
  incidentDate: string;
  incidentType: IncidentType;
  description: string | null;
  damageDescription: string | null;
  status: ClaimStatus;
  approvedAmount: number | null;
  reviewedBy: string | null;
  reviewedAt: string | null;
  rejectionReason: string | null;
  reviewedByName: string | null;
  paidAt: string | null;
  paymentReference: string | null;
  weatherSnapshot: string | null;
  aiAnalysisResult: string | null;
  weatherStatus: string | null;
  weatherErrorMessage: string | null;
  aiAnalysisStatus: string | null;
  aiErrorMessage: string | null;
  createdAt: string;
  updatedAt: string | null;
  images: ClaimImageDto[] | null;
}

export interface ClaimListDto {
  id: string;
  policyId: string;
  farmId: string;
  policyNumber: string | null;
  farmName: string | null;
  incidentDate: string;
  incidentType: IncidentType;
  status: ClaimStatus;
  approvedAmount: number | null;
  createdAt: string;
  imageCount: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface AdminDashboardDto {
  totalUsers: number;
  totalFarmers: number;
  totalFarms: number;
  totalPolicies: number;
  totalClaims: number;
  pendingClaims: number;
  pendingPolicies: number;
  totalPremiumCollected: number;
  totalClaimsPaid: number;
  claimsByStatus: Record<string, number>;
  policiesByStatus: Record<string, number>;
  claimsByIncidentType: Record<string, number>;
  topFarms: { farmName: string; farmerName: string; claimCount: number; totalClaimed: number }[];
  premiumTrend: { month: string; premium: number; claims: number }[];
}

export interface AuditLogDto {
  id: string;
  userId: string;
  userName: string;
  userRole: string | null;
  action: string;
  resourceType: string;
  resourceId: string | null;
  details: string | null;
  ipAddress: string | null;
  timestamp: string;
  oldValues: string | null;
  newValues: string | null;
  changedColumns: string | null;
  userAgent: string | null;
  correlationId: string | null;
  httpMethod: string | null;
  httpPath: string | null;
}

// ----- Request DTOs -----

export interface RegisterRequestDto {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
}

export interface LoginRequestDto {
  email: string;
  password: string;
}

export interface UpdateProfileRequestDto {
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
}

export interface CreateFarmRequestDto {
  name: string;
  areaInHectares?: number;
  address?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  locationGeoJson?: string | null;
}

export interface CreatePolicyRequestDto {
  farmId: string;
  insurancePlanId: string;
  startDate: string;
  endDate?: string | null;
  policyNumber?: string | null;
}

export interface CreateClaimRequestDto {
  policyId: string;
  farmId: string;
  incidentDate: string;
  incidentType: IncidentType;
  description?: string | null;
  damageDescription?: string | null;
  imageUrls?: string[] | null;
}

export interface CreatePlanRequestDto {
  name: string;
  description?: string | null;
  cropType: string;
  provider: string;
  premiumRatePerHectare: number;
  sumInsuredPerHectare: number;
  coveragePercentage: number;
  minAreaInHectares?: number | null;
  maxAreaInHectares?: number | null;
  policyDurationMonths: number;
  isActive: boolean;
}

export interface AuthState {
  token: string | null;
  refreshToken: string | null;
  user: UserDto | null;
}
