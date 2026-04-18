import { apiClient } from '@/lib/api-client';
import type {
  ApiResponse,
  CreateReservationRequest,
  DriverSummary,
  PagedResult,
  RateReservationRequest,
  ReservationDetail,
  ReservationStatusLiteral,
  ReservationSummary,
} from '@pickme/shared/api-types';

async function unwrap<T>(promise: Promise<{ data: ApiResponse<T> }>): Promise<T> {
  const res = await promise;
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

export const reservationsApi = {
  // Customer
  create: (input: CreateReservationRequest) =>
    unwrap(apiClient.post<ApiResponse<string>>('/reservations', input)),
  listMine: (status?: ReservationStatusLiteral) =>
    unwrap(
      apiClient.get<ApiResponse<PagedResult<ReservationSummary>>>('/reservations', {
        params: status ? { status } : undefined,
      }),
    ),
  getMine: (id: string) =>
    unwrap(apiClient.get<ApiResponse<ReservationDetail>>(`/reservations/${id}`)),
  cancel: (id: string) =>
    unwrap(apiClient.post<ApiResponse<void>>(`/reservations/${id}/cancel`, {})),
  rate: (id: string, input: RateReservationRequest) =>
    unwrap(apiClient.post<ApiResponse<void>>(`/reservations/${id}/rating`, input)),
  editRating: (id: string, input: RateReservationRequest) =>
    unwrap(apiClient.patch<ApiResponse<void>>(`/reservations/${id}/rating`, input)),

  // Admin
  adminList: (params: {
    status?: ReservationStatusLiteral;
    page?: number;
    pageSize?: number;
    search?: string;
  }) =>
    unwrap(
      apiClient.get<ApiResponse<PagedResult<ReservationDetail>>>('/admin/reservations', { params }),
    ),
  adminGet: (id: string) =>
    unwrap(apiClient.get<ApiResponse<ReservationDetail>>(`/admin/reservations/${id}`)),
  adminAssign: (id: string, driverId: string) =>
    unwrap(apiClient.post<ApiResponse<void>>(`/admin/reservations/${id}/assign`, { driverId })),
  adminCancel: (id: string, reason: string) =>
    unwrap(apiClient.post<ApiResponse<void>>(`/admin/reservations/${id}/cancel`, { reason })),
  adminActiveDrivers: () =>
    unwrap(apiClient.get<ApiResponse<DriverSummary[]>>('/admin/drivers/active')),

  // Driver
  driverTasks: () =>
    unwrap(apiClient.get<ApiResponse<PagedResult<ReservationDetail>>>('/driver/tasks')),
  driverGet: (id: string) =>
    unwrap(apiClient.get<ApiResponse<ReservationDetail>>(`/driver/tasks/${id}`)),
  driverStart: (id: string) =>
    unwrap(apiClient.post<ApiResponse<void>>(`/driver/tasks/${id}/start`, {})),
  driverComplete: (id: string) =>
    unwrap(apiClient.post<ApiResponse<void>>(`/driver/tasks/${id}/complete`, {})),
};
