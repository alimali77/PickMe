import { apiClient } from '@/lib/api-client';
import type {
  AdminRatingListItem,
  AdminUserDto,
  ApiResponse,
  ContactMessageDto,
  CreateAdminRequest,
  CreateDriverRequest,
  CreateFaqRequest,
  CustomerDetail,
  CustomerListItem,
  DriverDetail,
  DriverListItem,
  FaqAdminDto,
  PagedResult,
  RecipientDto,
  SystemSettingDto,
  UpdateDriverRequest,
  UpdateFaqRequest,
} from '@pickme/shared/api-types';

async function unwrap<T>(p: Promise<{ data: ApiResponse<T> }>): Promise<T> {
  const res = await p;
  if (!res.data.success) {
    const err = new Error(res.data.message ?? 'İşlem başarısız.') as Error & {
      errors?: Record<string, string[]>;
      code?: string;
    };
    err.errors = res.data.errors;
    err.code = res.data.code;
    throw err;
  }
  return res.data.data as T;
}

export const adminApi = {
  // Drivers
  drivers: {
    list: (params?: { search?: string; page?: number; pageSize?: number }) =>
      unwrap(apiClient.get<ApiResponse<PagedResult<DriverListItem>>>('/admin/drivers', { params })),
    get: (id: string) => unwrap(apiClient.get<ApiResponse<DriverDetail>>(`/admin/drivers/${id}`)),
    create: (input: CreateDriverRequest) => unwrap(apiClient.post<ApiResponse<string>>('/admin/drivers', input)),
    update: (id: string, input: UpdateDriverRequest) =>
      unwrap(apiClient.patch<ApiResponse<void>>(`/admin/drivers/${id}`, input)),
    setActive: (id: string, active: boolean) =>
      unwrap(apiClient.post<ApiResponse<void>>(`/admin/drivers/${id}/set-active`, { active })),
    resetPassword: (id: string) =>
      unwrap(apiClient.post<ApiResponse<void>>(`/admin/drivers/${id}/reset-password`, {})),
    remove: (id: string) => unwrap(apiClient.delete<ApiResponse<void>>(`/admin/drivers/${id}`)),
  },
  // Recipients
  recipients: {
    list: () => unwrap(apiClient.get<ApiResponse<RecipientDto[]>>('/admin/recipients')),
    add: (email: string) => unwrap(apiClient.post<ApiResponse<string>>('/admin/recipients', { email })),
    setActive: (id: string, active: boolean) =>
      unwrap(apiClient.post<ApiResponse<void>>(`/admin/recipients/${id}/set-active`, { active })),
    remove: (id: string) => unwrap(apiClient.delete<ApiResponse<void>>(`/admin/recipients/${id}`)),
  },
  // FAQs
  faqs: {
    list: () => unwrap(apiClient.get<ApiResponse<FaqAdminDto[]>>('/admin/faqs')),
    create: (input: CreateFaqRequest) => unwrap(apiClient.post<ApiResponse<string>>('/admin/faqs', input)),
    update: (id: string, input: UpdateFaqRequest) =>
      unwrap(apiClient.put<ApiResponse<void>>(`/admin/faqs/${id}`, input)),
    remove: (id: string) => unwrap(apiClient.delete<ApiResponse<void>>(`/admin/faqs/${id}`)),
  },
  // Contact messages
  contact: {
    list: (params?: { unreadOnly?: boolean; page?: number; pageSize?: number }) =>
      unwrap(apiClient.get<ApiResponse<PagedResult<ContactMessageDto>>>('/admin/contact-messages', { params })),
    markRead: (id: string) =>
      unwrap(apiClient.post<ApiResponse<void>>(`/admin/contact-messages/${id}/mark-read`, {})),
  },
  // Customers
  customers: {
    list: (params?: { search?: string; page?: number; pageSize?: number }) =>
      unwrap(apiClient.get<ApiResponse<PagedResult<CustomerListItem>>>('/admin/customers', { params })),
    get: (id: string) => unwrap(apiClient.get<ApiResponse<CustomerDetail>>(`/admin/customers/${id}`)),
    setActive: (id: string, active: boolean) =>
      unwrap(apiClient.post<ApiResponse<void>>(`/admin/customers/${id}/set-active`, { active })),
  },
  // Ratings (admin view)
  ratings: {
    list: (params?: { driverId?: string; minScore?: number; maxScore?: number; page?: number; pageSize?: number }) =>
      unwrap(apiClient.get<ApiResponse<PagedResult<AdminRatingListItem>>>('/admin/ratings', { params })),
    flag: (id: string, reason: string) =>
      unwrap(apiClient.post<ApiResponse<void>>(`/admin/ratings/${id}/flag`, { reason })),
    unflag: (id: string) => unwrap(apiClient.post<ApiResponse<void>>(`/admin/ratings/${id}/unflag`, {})),
  },
  // Admin users
  admins: {
    list: () => unwrap(apiClient.get<ApiResponse<AdminUserDto[]>>('/admin/admins')),
    create: (input: CreateAdminRequest) => unwrap(apiClient.post<ApiResponse<string>>('/admin/admins', input)),
    update: (id: string, fullName: string) =>
      unwrap(apiClient.patch<ApiResponse<void>>(`/admin/admins/${id}`, { fullName })),
    remove: (id: string) => unwrap(apiClient.delete<ApiResponse<void>>(`/admin/admins/${id}`)),
  },
  // Settings
  settings: {
    list: () => unwrap(apiClient.get<ApiResponse<SystemSettingDto[]>>('/admin/settings')),
    update: (values: Record<string, string>) =>
      unwrap(apiClient.put<ApiResponse<void>>('/admin/settings', { values })),
  },
};
