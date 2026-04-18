import axios, { type AxiosError } from 'axios';
import { useAuthStore } from './auth-store';
import type { ApiResponse, AuthTokens } from '@pickme/shared/api-types';

const baseURL = import.meta.env.VITE_API_URL ?? '/api';

export const apiClient = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000,
});

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let refreshing: Promise<AuthTokens | null> | null = null;

apiClient.interceptors.response.use(
  (res) => res,
  async (error: AxiosError<ApiResponse<unknown>>) => {
    const original = error.config;
    if (error.response?.status === 401 && original && !(original as any)._retry) {
      const store = useAuthStore.getState();
      if (!store.refreshToken) return Promise.reject(error);

      (original as any)._retry = true;
      refreshing ??= (async () => {
        try {
          const res = await axios.post<ApiResponse<AuthTokens>>(
            `${baseURL}/auth/refresh`,
            { refreshToken: store.refreshToken },
            { headers: { 'Content-Type': 'application/json' } },
          );
          if (res.data.success && res.data.data) {
            store.setTokens(res.data.data);
            return res.data.data;
          }
          store.clear();
          return null;
        } catch {
          store.clear();
          return null;
        } finally {
          refreshing = null;
        }
      })();

      const tokens = await refreshing;
      if (!tokens) return Promise.reject(error);
      original.headers = original.headers ?? {};
      (original.headers as Record<string, string>).Authorization = `Bearer ${tokens.accessToken}`;
      return apiClient(original);
    }
    return Promise.reject(error);
  },
);
