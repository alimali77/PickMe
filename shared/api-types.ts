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
