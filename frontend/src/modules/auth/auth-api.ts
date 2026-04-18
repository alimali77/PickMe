import { apiClient } from '@/lib/api-client';
import type { ApiResponse, AuthTokens, CurrentUserDto } from '@pickme/shared/api-types';
import type {
  RegisterInput,
  LoginInput,
  ForgotPasswordInput,
  ResetPasswordInput,
  ChangePasswordInput,
  UpdateProfileInput,
} from '@pickme/shared/validation';

async function unwrap<T>(promise: Promise<{ data: ApiResponse<T> }>): Promise<T> {
  const res = await promise;
  if (!res.data.success) {
    const err = new Error(res.data.message ?? 'İşlem başarısız.') as Error & { errors?: Record<string, string[]>; code?: string };
    err.errors = res.data.errors;
    err.code = res.data.code;
    throw err;
  }
  return res.data.data as T;
}

export const authApi = {
  register: (input: RegisterInput) =>
    unwrap(apiClient.post<ApiResponse<void>>('/auth/register', input)),
  verifyEmail: (token: string) =>
    unwrap(apiClient.post<ApiResponse<void>>('/auth/verify-email', { token })),
  resendVerification: (email: string) =>
    unwrap(apiClient.post<ApiResponse<void>>('/auth/resend-verification', { email })),
  login: (input: LoginInput) =>
    unwrap(apiClient.post<ApiResponse<AuthTokens>>('/auth/login', input)),
  refresh: (refreshToken: string) =>
    unwrap(apiClient.post<ApiResponse<AuthTokens>>('/auth/refresh', { refreshToken })),
  logout: (refreshToken: string) =>
    unwrap(apiClient.post<ApiResponse<void>>('/auth/logout', { refreshToken })),
  forgotPassword: (input: ForgotPasswordInput) =>
    unwrap(apiClient.post<ApiResponse<void>>('/auth/forgot-password', input)),
  resetPassword: (input: ResetPasswordInput) =>
    unwrap(apiClient.post<ApiResponse<void>>('/auth/reset-password', input)),
  me: () => unwrap(apiClient.get<ApiResponse<CurrentUserDto>>('/auth/me')),
  updateMe: (input: UpdateProfileInput) =>
    unwrap(apiClient.patch<ApiResponse<CurrentUserDto>>('/auth/me', input)),
  changePassword: (input: ChangePasswordInput) =>
    unwrap(apiClient.patch<ApiResponse<void>>('/auth/me/password', input)),
};
