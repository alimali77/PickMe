import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { AuthTokens, CurrentUserDto } from '@pickme/shared/api-types';

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  accessTokenExpiresAt: string | null;
  refreshTokenExpiresAt: string | null;
  user: CurrentUserDto | null;

  setTokens: (tokens: AuthTokens) => void;
  setUser: (user: CurrentUserDto | null) => void;
  clear: () => void;
  isAuthenticated: () => boolean;
  hasRole: (role: CurrentUserDto['role']) => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      accessTokenExpiresAt: null,
      refreshTokenExpiresAt: null,
      user: null,

      setTokens: (t) =>
        set({
          accessToken: t.accessToken,
          refreshToken: t.refreshToken,
          accessTokenExpiresAt: t.accessTokenExpiresAt,
          refreshTokenExpiresAt: t.refreshTokenExpiresAt,
        }),
      setUser: (user) => set({ user }),
      clear: () =>
        set({
          accessToken: null,
          refreshToken: null,
          accessTokenExpiresAt: null,
          refreshTokenExpiresAt: null,
          user: null,
        }),
      isAuthenticated: () => !!get().accessToken,
      hasRole: (role) => get().user?.role === role,
    }),
    { name: 'pickme-auth' },
  ),
);
