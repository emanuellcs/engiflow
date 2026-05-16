"use client";

import {
  createContext,
  type PropsWithChildren,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useSyncExternalStore,
} from "react";
import { useRouter } from "next/navigation";
import {
  type AuthenticatedUser,
  decodeAuthenticatedUser,
} from "@/lib/auth/jwt";
import {
  AUTH_STATE_CHANGED_EVENT,
  clearStoredAuthToken,
  getStoredAuthToken,
  storeAuthToken,
} from "@/lib/auth/token-storage";

export type AuthContextValue = {
  user: AuthenticatedUser | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (token: string) => void;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: PropsWithChildren) {
  const router = useRouter();
  const token = useSyncExternalStore(
    subscribeToAuthStore,
    getStoredAuthToken,
    getServerAuthSnapshot,
  );
  const user = useMemo(() => decodeStoredUser(token), [token]);

  useEffect(() => {
    if (token && !user) {
      clearStoredAuthToken();
    }
  }, [token, user]);

  const login = useCallback((nextToken: string) => {
    const authenticatedUser = decodeAuthenticatedUser(nextToken);

    storeAuthToken(nextToken, authenticatedUser.expiresAtUtc);
  }, []);

  const logout = useCallback(() => {
    clearStoredAuthToken();
    router.push("/login");
  }, [router]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      isAuthenticated: Boolean(user && token),
      isLoading: false,
      login,
      logout,
    }),
    [login, logout, token, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider.");
  }

  return context;
}

function subscribeToAuthStore(onStoreChange: () => void): () => void {
  if (typeof window === "undefined") {
    return () => undefined;
  }

  window.addEventListener(AUTH_STATE_CHANGED_EVENT, onStoreChange);
  window.addEventListener("storage", onStoreChange);

  return () => {
    window.removeEventListener(AUTH_STATE_CHANGED_EVENT, onStoreChange);
    window.removeEventListener("storage", onStoreChange);
  };
}

function getServerAuthSnapshot(): string | null {
  return null;
}

function decodeStoredUser(token: string | null): AuthenticatedUser | null {
  if (!token) {
    return null;
  }

  try {
    return decodeAuthenticatedUser(token);
  } catch {
    return null;
  }
}
