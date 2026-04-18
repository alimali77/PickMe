/* eslint-disable */
/**
 * BU DOSYA OTOMATIK OLUŞTURULUR.
 *
 * NSwag ile backend Swagger JSON'undan `pnpm run generate:types` komutuyla üretilir.
 * Manuel düzenleme YASAK; düzenlersen CI'daki drift kontrolü build'i kıracak.
 *
 * Faz 2 sonunda NSwag pipeline'ı kurulduğunda bu dosya yeniden üretilecek.
 * Şu an için elle bakımı yapılan bir iskelet; backend endpoint eklendikçe güncellenir.
 */

export type ApiResponse<T> = {
  success: boolean;
  data?: T;
  message?: string;
  errors?: Record<string, string[]>;
  code?: string;
};

// ---------- Auth ----------
export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  password: string;
  passwordConfirm: string;
  kvkkAccepted: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

export interface CurrentUserDto {
  id: string;
  email: string;
  role: 'Customer' | 'Driver' | 'Admin';
  firstName: string;
  lastName: string;
  phone: string;
  mustChangePassword: boolean;
  emailConfirmed: boolean;
}

// ---------- Reservations ----------
export type ReservationStatusLiteral = 'Pending' | 'Assigned' | 'OnTheWay' | 'Completed' | 'Cancelled';
export type ServiceTypeLiteral = 'Driver' | 'Valet';
export type CancelledByLiteral = 'Customer' | 'Admin' | 'System';

export interface CreateReservationRequest {
  serviceType: ServiceTypeLiteral;
  reservationDateTimeUtc: string;
  address: string;
  lat: number;
  lng: number;
  note?: string;
  placeSelectedFromAutocomplete: boolean;
}

export interface ReservationSummary {
  id: string;
  status: ReservationStatusLiteral;
  serviceType: ServiceTypeLiteral;
  reservationDateTimeUtc: string;
  address: string;
  lat: number;
  lng: number;
  note?: string | null;
  createdAtUtc: string;
}

export interface ReservationDetail {
  id: string;
  status: ReservationStatusLiteral;
  serviceType: ServiceTypeLiteral;
  reservationDateTimeUtc: string;
  address: string;
  lat: number;
  lng: number;
  note?: string | null;
  customerId: string;
  customerName: string;
  customerPhone: string;
  customerEmail: string;
  driverId?: string | null;
  driverName?: string | null;
  driverPhone?: string | null;
  driverAverageRating?: number | null;
  cancellationReason?: string | null;
  cancelledBy?: CancelledByLiteral | null;
  assignedAtUtc?: string | null;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  cancelledAtUtc?: string | null;
  createdAtUtc: string;
  hasRating: boolean;
  ratingScore?: number | null;
  ratingEditable: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface DriverSummary {
  id: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  averageRating: number;
  totalTrips: number;
  status: 'Active' | 'Inactive';
}

export interface FaqDto {
  id: string;
  question: string;
  answer: string;
  displayOrder: number;
}

export interface RateReservationRequest {
  score: number;
  comment?: string;
}

export interface ContactFormRequest {
  firstName: string;
  email: string;
  phone: string;
  subject: string;
  message: string;
}

// ---------- Admin management ----------
export interface CreateDriverRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  initialPassword?: string;
}

export interface UpdateDriverRequest {
  firstName: string;
  lastName: string;
  phone: string;
}

export interface DriverListItem {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  status: 'Active' | 'Inactive';
  averageRating: number;
  totalTrips: number;
  mustChangePassword: boolean;
  createdAtUtc: string;
}

export interface RecentRatingDto {
  reservationId: string;
  score: number;
  comment?: string | null;
  createdAtUtc: string;
  isFlagged: boolean;
}

export interface DriverDetail extends DriverListItem {
  activeAssignmentCount: number;
  recentRatings: RecentRatingDto[];
}

export interface RecipientDto {
  id: string;
  email: string;
  isActive: boolean;
}

export interface FaqAdminDto {
  id: string;
  question: string;
  answer: string;
  displayOrder: number;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CreateFaqRequest {
  question: string;
  answer: string;
  displayOrder: number;
}

export interface UpdateFaqRequest extends CreateFaqRequest {
  isActive: boolean;
}

export interface CustomerListItem {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  isActive: boolean;
  reservationCount: number;
  createdAtUtc: string;
}

export interface RecentReservationDto {
  id: string;
  status: ReservationStatusLiteral;
  reservationDateTimeUtc: string;
  address: string;
}

export interface CustomerDetail extends CustomerListItem {
  emailConfirmed: boolean;
  recentReservations: RecentReservationDto[];
}

export interface ContactMessageDto {
  id: string;
  firstName: string;
  email: string;
  phone: string;
  subject: string;
  message: string;
  isRead: boolean;
  readAtUtc?: string | null;
  createdAtUtc: string;
}

export interface AdminRatingListItem {
  id: string;
  reservationId: string;
  score: number;
  comment?: string | null;
  customerName: string;
  driverName: string;
  isFlagged: boolean;
  flaggedReason?: string | null;
  createdAtUtc: string;
}

export interface AdminUserDto {
  id: string;
  fullName: string;
  email: string;
  createdAtUtc: string;
}

export interface CreateAdminRequest {
  fullName: string;
  email: string;
  password: string;
}

export interface SystemSettingDto {
  key: string;
  value: string;
  isSensitive: boolean;
}
