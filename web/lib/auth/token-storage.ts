export const AUTH_STATE_CHANGED_EVENT = "engiflow:auth-state-changed";
export const AUTH_UNAUTHORIZED_EVENT = "engiflow:auth-unauthorized";

const authTokenStorageKey = "engiflow.auth.token";
const authTokenCookieName = "engiflow_auth_token";

export function getStoredAuthToken(): string | null {
  if (!isBrowser()) {
    return null;
  }

  const localStorageToken = readLocalStorageToken();

  if (localStorageToken) {
    return localStorageToken;
  }

  return readCookie(authTokenCookieName);
}

export function storeAuthToken(token: string, expiresAtUtc?: string): void {
  if (!isBrowser()) {
    return;
  }

  try {
    window.localStorage.setItem(authTokenStorageKey, token);
  } catch {
    // Cookie mirroring still allows authenticated fetches if local storage is blocked.
  }

  writeCookie(authTokenCookieName, token, expiresAtUtc);
  dispatchAuthStateChanged();
}

export function clearStoredAuthToken(): void {
  if (!isBrowser()) {
    return;
  }

  try {
    window.localStorage.removeItem(authTokenStorageKey);
  } catch {
    // Clearing the cookie below is enough for the request interceptor fallback.
  }

  expireCookie(authTokenCookieName);
  dispatchAuthStateChanged();
}

function readLocalStorageToken(): string | null {
  try {
    return window.localStorage.getItem(authTokenStorageKey);
  } catch {
    return null;
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

function writeCookie(name: string, value: string, expiresAtUtc?: string): void {
  const maxAge = getCookieMaxAge(expiresAtUtc);
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
