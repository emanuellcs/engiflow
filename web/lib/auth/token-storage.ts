/** Custom event dispatched to synchronize authentication state across components and tabs. */
export const AUTH_STATE_CHANGED_EVENT = "engiflow:auth-state-changed";
/** Custom event dispatched when an API call returns a 401 Unauthorized status. */
export const AUTH_UNAUTHORIZED_EVENT = "engiflow:auth-unauthorized";

/**
 * Encapsulates the persisted authentication session.
 */
export type StoredAuthSession = {
  /** The active JWT bearer token. */
  accessToken: string;
  /** The token type descriptor. */
  tokenType?: string;
  /** The UTC expiration timestamp. */
  expiresAtUtc?: string;
  /** The display name of the user. */
  userName?: string;
  /** The name of the active tenant company. */
  companyName?: string;
  /** The list of permissions/roles for the active session. */
  roles?: string[];
};

const authSessionStorageKey = "engiflow.auth.session";
const legacyAuthTokenStorageKey = "engiflow.auth.token";
const authTokenCookieName = "engiflow_auth_token";

/**
 * Retrieves a serialized snapshot of the current authentication session from storage.
 * Orchestrates lookups across SessionStorage, LocalStorage, and legacy keys.
 *
 * @returns The serialized session JSON or null if no session exists.
 */
export function getStoredAuthSessionSnapshot(): string | null {
  if (!isBrowser()) {
    return null;
  }

  return (
    readWebStorageItem(window.sessionStorage, authSessionStorageKey) ??
    readWebStorageItem(window.localStorage, authSessionStorageKey) ??
    readLegacyAuthSessionSnapshot()
  );
}

/**
 * Extracts the active JWT from the stored session or fallback cookie.
 *
 * @returns The raw JWT string or null if unauthenticated.
 */
export function getStoredAuthToken(): string | null {
  if (!isBrowser()) {
    return null;
  }

  const session = parseStoredAuthSession(getStoredAuthSessionSnapshot());

  if (session?.accessToken) {
    return session.accessToken;
  }

  return readCookie(authTokenCookieName);
}

/**
 * Checks if the current session was persisted via rememberMe (stored in localStorage).
 */
export function getRememberMe(): boolean {
  if (!isBrowser()) {
    return false;
  }

  return Boolean(readWebStorageItem(window.localStorage, authSessionStorageKey));
}

/**
 * Persists an authentication session to appropriate storage tiers based on preference.
 * Synchronizes SessionStorage/LocalStorage with HttpOnly-compatible cookies.
 *
 * @param session - The authentication payload to persist.
 * @param rememberMe - Whether to use long-lived LocalStorage and persistent cookies.
 */
export function storeAuthSession(
  session: StoredAuthSession,
  rememberMe: boolean,
): void {
  if (!isBrowser()) {
    return;
  }

  const serializedSession = JSON.stringify(session);
  const primaryStorage = rememberMe ? window.localStorage : window.sessionStorage;
  const secondaryStorage = rememberMe ? window.sessionStorage : window.localStorage;

  try {
    primaryStorage.setItem(authSessionStorageKey, serializedSession);
  } catch {
    // Cookie mirroring still allows authenticated fetches if web storage is blocked.
  }

  clearWebStorageItem(secondaryStorage, authSessionStorageKey);
  clearWebStorageItem(window.localStorage, legacyAuthTokenStorageKey);
  clearWebStorageItem(window.sessionStorage, legacyAuthTokenStorageKey);

  writeCookie(authTokenCookieName, session.accessToken, {
    expiresAtUtc: session.expiresAtUtc,
    persistent: rememberMe,
  });
  dispatchAuthStateChanged();
}

/**
 * Purges all authentication identifiers from all storage tiers (Storage and Cookies).
 */
export function clearStoredAuthToken(): void {
  if (!isBrowser()) {
    return;
  }

  clearWebStorageItem(window.localStorage, authSessionStorageKey);
  clearWebStorageItem(window.sessionStorage, authSessionStorageKey);
  clearWebStorageItem(window.localStorage, legacyAuthTokenStorageKey);
  clearWebStorageItem(window.sessionStorage, legacyAuthTokenStorageKey);

  expireCookie(authTokenCookieName);
  dispatchAuthStateChanged();
}

/**
 * Updates the cached roles within the active persistent session.
 * Used for dynamic permission synchronization during tenant switching.
 *
 * @param roles - The updated list of role identifiers.
 */
export function updateStoredAuthSessionRoles(roles: string[]): void {
  if (!isBrowser()) {
    return;
  }

  const normalizedRoles = roles
    .map((role) => role.trim())
    .filter((role, index, allRoles) => role.length > 0 && allRoles.indexOf(role) === index);
  const currentSnapshot = getStoredAuthSessionSnapshot();
  const session = parseStoredAuthSession(currentSnapshot);

  if (!session || normalizedRoles.length === 0) {
    return;
  }

  const nextSession: StoredAuthSession = {
    ...session,
    roles: normalizedRoles,
  };
  const serializedSession = JSON.stringify(nextSession);

  if (readWebStorageItem(window.localStorage, authSessionStorageKey)) {
    writeWebStorageItem(window.localStorage, authSessionStorageKey, serializedSession);
  } else if (readWebStorageItem(window.sessionStorage, authSessionStorageKey)) {
    writeWebStorageItem(window.sessionStorage, authSessionStorageKey, serializedSession);
  } else {
    writeWebStorageItem(window.sessionStorage, authSessionStorageKey, serializedSession);
  }

  dispatchAuthStateChanged();
}

function readLegacyAuthSessionSnapshot(): string | null {
  const legacyToken =
    readWebStorageItem(window.sessionStorage, legacyAuthTokenStorageKey) ??
    readWebStorageItem(window.localStorage, legacyAuthTokenStorageKey);

  if (!legacyToken) {
    return null;
  }

  return JSON.stringify({ accessToken: legacyToken });
}

function parseStoredAuthSession(value: string | null): StoredAuthSession | null {
  if (!value) {
    return null;
  }

  try {
    const parsed = JSON.parse(value) as Partial<StoredAuthSession>;

    if (
      parsed &&
      typeof parsed === "object" &&
      typeof parsed.accessToken === "string" &&
      parsed.accessToken.trim().length > 0
    ) {
      return {
        ...parsed,
        accessToken: parsed.accessToken.trim(),
      };
    }
  } catch {
    return null;
  }

  return null;
}

function readWebStorageItem(storage: Storage, key: string): string | null {
  try {
    return storage.getItem(key);
  } catch {
    return null;
  }
}

function clearWebStorageItem(storage: Storage, key: string): void {
  try {
    storage.removeItem(key);
  } catch {
    // Clearing the cookie below is enough for the request interceptor fallback.
  }
}

function writeWebStorageItem(storage: Storage, key: string, value: string): void {
  try {
    storage.setItem(key, value);
  } catch {
    // The existing token cookie still keeps the proxy authenticated.
  }
}

function readCookie(name: string): string | null {
  const prefix = `${name}=`;
  const match = document.cookie
    .split("; ")
    .find((cookie) => cookie.startsWith(prefix));

  if (!match) {
    return null;
  }

  return decodeURIComponent(match.slice(prefix.length));
}

function writeCookie(
  name: string,
  value: string,
  options: { expiresAtUtc?: string; persistent: boolean },
): void {
  const maxAge = options.persistent
    ? getCookieMaxAge(options.expiresAtUtc)
    : "";
  const secure = window.location.protocol === "https:" ? "; Secure" : "";

  document.cookie = [
    `${name}=${encodeURIComponent(value)}`,
    "Path=/",
    "SameSite=Lax",
    maxAge,
    secure,
  ]
    .filter(Boolean)
    .join("; ");
}

function expireCookie(name: string): void {
  document.cookie = `${name}=; Path=/; SameSite=Lax; Max-Age=0`;
}

function getCookieMaxAge(expiresAtUtc?: string): string {
  if (!expiresAtUtc) {
    return "";
  }

  const seconds = Math.max(
    0,
    Math.floor((Date.parse(expiresAtUtc) - Date.now()) / 1000),
  );

  return `Max-Age=${seconds}`;
}

function dispatchAuthStateChanged(): void {
  window.dispatchEvent(new Event(AUTH_STATE_CHANGED_EVENT));
}

function isBrowser(): boolean {
  return typeof window !== "undefined" && typeof document !== "undefined";
}
