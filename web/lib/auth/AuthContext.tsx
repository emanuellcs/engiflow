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
  getStoredAuthSessionSnapshot,
  storeAuthSession,
  type StoredAuthSession,
} from "@/lib/auth/token-storage";

/**
 * Represents the raw authentication payload returned by the identity provider.
 * Encapsulates multi-tenant identity claims and session metadata.
 */
export type AuthSessionResult = {
  /** The opaque JWT bearer token. */
  accessToken?: unknown;
  /** The token type (e.g., "Bearer"). */
  tokenType?: unknown;
  /** The UTC expiration timestamp. */
  expiresAtUtc?: unknown;
  /** The display name of the authenticated user. */
  userName?: unknown;
  /** The name of the currently active tenant company. */
  companyName?: unknown;
  /** The list of permissions/roles assigned to the user in the active tenant. */
  roles?: unknown;
};

/**
 * Orchestrates the global authentication state across the application.
 * Manages identity resolution, session persistence, and multi-tenant switching.
 */
export type AuthContextValue = {
  /** The decoded identity claims for the current user. */
  user: AuthenticatedUser | null;
  /** The active JWT used for authenticated API calls. */
  token: string | null;
  /** Whether a valid session is currently established. */
  isAuthenticated: boolean;
  /** Whether the session is still being hydrated from persistent storage. */
  isLoading: boolean;
  /**
   * Commits a new authentication session to persistent storage.
   * @param result - The raw session payload.
   * @param rememberMe - Whether to persist the session across browser restarts.
   */
  login: (result: AuthSessionResult, rememberMe: boolean) => void;
  /** Terminates the active session and clears all security identifiers. */
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);
const serverAuthPendingSnapshot = "__engiflow_auth_pending__";

/**
 * Root authentication provider.
 * Synchronizes identity state across multiple tabs and handles isomorphic hydration.
 *
 * @param props - React children to be wrapped in the security context.
 * @returns The hydrated authentication context provider.
 */
export function AuthProvider({ children }: PropsWithChildren) {
  const router = useRouter();
  const sessionSnapshot = useSyncExternalStore(
    subscribeToAuthStore,
    getStoredAuthSessionSnapshot,
    getServerAuthSnapshot,
  );
  const isAuthSnapshotPending = sessionSnapshot === serverAuthPendingSnapshot;
  const session = useMemo(
    () => parseStoredAuthSession(sessionSnapshot),
    [sessionSnapshot],
  );
  const user = useMemo(() => decodeStoredUser(session), [session]);
  const token = session?.accessToken ?? null;

  useEffect(() => {
    if (!isAuthSnapshotPending && session && !user) {
      clearStoredAuthToken();
    }
  }, [isAuthSnapshotPending, session, user]);

  const login = useCallback((result: AuthSessionResult, rememberMe: boolean) => {
    const nextSession = normalizeAuthSessionResult(result);
    const authenticatedUser = decodeAuthenticatedUser(nextSession.accessToken);

    storeAuthSession(
      {
        ...nextSession,
        expiresAtUtc: nextSession.expiresAtUtc ?? authenticatedUser.expiresAtUtc,
        userName: nextSession.userName ?? authenticatedUser.userName,
        companyName: nextSession.companyName ?? authenticatedUser.companyName,
        roles: nextSession.roles.length > 0 ? nextSession.roles : authenticatedUser.roles,
      },
      rememberMe,
    );
  }, []);

  const logout = useCallback(() => {
    clearStoredAuthToken();
    router.push("/auth?mode=login");
  }, [router]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      isAuthenticated: Boolean(user && token),
      isLoading: isAuthSnapshotPending,
      login,
      logout,
    }),
    [isAuthSnapshotPending, login, logout, token, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/**
 * Enterprise hook for consuming global authentication state.
 * Use this to access the current user, token, and session lifecycle controls.
 *
 * @returns The active authentication context value.
 * @throws Error if used outside of AuthProvider.
 */
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

function getServerAuthSnapshot(): string {
  return serverAuthPendingSnapshot;
}

function decodeStoredUser(session: StoredAuthSession | null): AuthenticatedUser | null {
  if (!session) {
    return null;
  }

  try {
    const tokenUser = decodeAuthenticatedUser(session.accessToken);

    return {
      ...tokenUser,
      userName: session.userName ?? tokenUser.userName,
      companyName: session.companyName ?? tokenUser.companyName,
      roles: session.roles && session.roles.length > 0 ? session.roles : tokenUser.roles,
      role:
        session.roles && session.roles.length > 0
          ? session.roles[0]
          : tokenUser.role,
    };
  } catch {
    return null;
  }
}

function parseStoredAuthSession(snapshot: string | null): StoredAuthSession | null {
  if (!snapshot || snapshot === serverAuthPendingSnapshot) {
    return null;
  }

  try {
    const parsed = JSON.parse(snapshot) as Partial<StoredAuthSession>;

    if (
      parsed &&
      typeof parsed === "object" &&
      typeof parsed.accessToken === "string" &&
      parsed.accessToken.trim().length > 0
    ) {
      return {
        accessToken: parsed.accessToken.trim(),
        tokenType: typeof parsed.tokenType === "string" ? parsed.tokenType : undefined,
        expiresAtUtc:
          typeof parsed.expiresAtUtc === "string" ? parsed.expiresAtUtc : undefined,
        userName: typeof parsed.userName === "string" ? parsed.userName : undefined,
        companyName:
          typeof parsed.companyName === "string" ? parsed.companyName : undefined,
        roles: Array.isArray(parsed.roles)
          ? parsed.roles.filter(
              (role): role is string =>
                typeof role === "string" && role.trim().length > 0,
            )
          : undefined,
      };
    }
  } catch {
    return null;
  }

  return null;
}

function normalizeAuthSessionResult(result: AuthSessionResult): StoredAuthSession & {
  roles: string[];
} {
  if (
    !result ||
    typeof result !== "object" ||
    typeof result.accessToken !== "string" ||
    result.accessToken.trim().length === 0
  ) {
    throw new Error("The server returned an invalid authentication response.");
  }

  return {
    accessToken: result.accessToken.trim(),
    tokenType: typeof result.tokenType === "string" ? result.tokenType : undefined,
    expiresAtUtc:
      typeof result.expiresAtUtc === "string" ? result.expiresAtUtc : undefined,
    userName:
      typeof result.userName === "string" && result.userName.trim().length > 0
        ? result.userName.trim()
        : undefined,
    companyName:
      typeof result.companyName === "string" && result.companyName.trim().length > 0
        ? result.companyName.trim()
        : undefined,
    roles: Array.isArray(result.roles)
      ? result.roles.filter(
          (role): role is string =>
            typeof role === "string" && role.trim().length > 0,
        )
      : [],
  };
}
