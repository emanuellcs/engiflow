import {
  AUTH_UNAUTHORIZED_EVENT,
  clearStoredAuthToken,
  getStoredAuthToken,
} from "@/lib/auth/token-storage";

/**
 * Represents a standard JSON-compatible value.
 */
export type JsonValue =
  | string
  | number
  | boolean
  | null
  | JsonValue[]
  | { [key: string]: JsonValue };

/**
 * Extended fetch options for the EngiFlow API client.
 */
export type ApiFetchOptions = Omit<RequestInit, "body"> & {
  /** The request payload, either a native BodyInit or a serializable JSON object. */
  body?: BodyInit | JsonValue;
  /** Whether to bypass the automatic inclusion of the Bearer authentication token. */
  skipAuth?: boolean;
};

/**
 * Enterprise-grade API error encapsulating HTTP status and server-provided error details.
 */
export class ApiError extends Error {
  /** The HTTP status code returned by the server. */
  readonly status: number;
  /** The raw error response body (usually ProblemDetails). */
  readonly details: unknown;

  constructor(status: number, message: string, details: unknown) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.details = details;
  }
}

const defaultApiBaseUrl = "";
const LOCALE_COOKIE_NAME = "engi-locale";

/**
 * Enterprise API client wrapper.
 * Handles authentication, serialization, and locale synchronization.
 */
export async function apiFetch<TResponse = unknown>(
  path: string,
  options: ApiFetchOptions = {},
): Promise<TResponse> {
  const { skipAuth = false, body, headers, ...requestOptions } = options;
  const requestHeaders = new Headers(headers);
  
  // 1. Authenticate Request
  const token = getStoredAuthToken();
  if (!skipAuth && token && !requestHeaders.has("Authorization")) {
    requestHeaders.set("Authorization", `Bearer ${token}`);
  }

  // 2. Synchronize Locale
  const locale = await resolveLocale();
  if (locale && !requestHeaders.has("Accept-Language")) {
    requestHeaders.set("Accept-Language", locale);
  }

  const response = await fetch(resolveApiUrl(path), {
    ...requestOptions,
    headers: requestHeaders,
    body: normalizeBody(body, requestHeaders),
  });

  if (response.status === 401) {
    handleUnauthorized();
  }

  const responseBody = await parseResponseBody(response);

  if (!response.ok) {
    throw new ApiError(
      response.status,
      getErrorMessage(response, responseBody),
      responseBody,
    );
  }

  return responseBody as TResponse;
}

/**
 * Resolves the active locale from cookies isomorphically.
 */
async function resolveLocale(): Promise<string | null> {
  // Client-side detection
  if (typeof window !== "undefined") {
    return (
      document.cookie
        .split("; ")
        .find((row) => row.startsWith(`${LOCALE_COOKIE_NAME}=`))
        ?.split("=")[1] || "en"
    );
  }

  // Server-side detection (Next.js 16)
  try {
    const { cookies } = await import("next/headers");
    const cookieStore = await cookies();
    return cookieStore.get(LOCALE_COOKIE_NAME)?.value || "en";
  } catch {
    return "en";
  }
}

function resolveApiUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  const baseUrl = (
    process.env.NEXT_PUBLIC_API_URL ||
    process.env.NEXT_PUBLIC_API_BASE_URL ||
    defaultApiBaseUrl
  ).replace(/\/+$/, "");

  if (!baseUrl) {
    return normalizedPath;
  }

  return `${baseUrl}${normalizedPath}`;
}

function normalizeBody(
  body: ApiFetchOptions["body"],
  headers: Headers,
): BodyInit | undefined {
  if (body === undefined) {
    return undefined;
  }

  if (isBodyInit(body)) {
    return body;
  }

  if (!headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  return JSON.stringify(body);
}

async function parseResponseBody(response: Response): Promise<unknown> {
  if (response.status === 204) {
    return undefined;
  }

  const text = await response.text();

  if (!text) {
    return undefined;
  }

  const contentType = response.headers.get("Content-Type") ?? "";

  if (contentType.includes("application/json") || contentType.includes("+json")) {
    try {
      return JSON.parse(text);
    } catch {
      return text;
    }
  }

  return text;
}

function getErrorMessage(response: Response, responseBody: unknown): string {
  if (
    responseBody &&
    typeof responseBody === "object" &&
    "title" in responseBody &&
    typeof responseBody.title === "string"
  ) {
    return responseBody.title;
  }

  return response.statusText || "API request failed.";
}

function handleUnauthorized(): void {
  if (typeof window === "undefined") {
    return;
  }

  clearStoredAuthToken();
  window.dispatchEvent(new Event(AUTH_UNAUTHORIZED_EVENT));

  if (window.location.pathname !== "/auth") {
    window.location.assign("/auth?mode=login");
  }
}

function isBodyInit(body: ApiFetchOptions["body"]): body is BodyInit {
  return (
    typeof body === "string" ||
    body instanceof ArrayBuffer ||
    ArrayBuffer.isView(body) ||
    isBlob(body) ||
    isFormData(body) ||
    isUrlSearchParams(body) ||
    isReadableStream(body)
  );
}

function isBlob(value: unknown): value is Blob {
  return typeof Blob !== "undefined" && value instanceof Blob;
}

function isFormData(value: unknown): value is FormData {
  return typeof FormData !== "undefined" && value instanceof FormData;
}

function isUrlSearchParams(value: unknown): value is URLSearchParams {
  return (
    typeof URLSearchParams !== "undefined" && value instanceof URLSearchParams
  );
}

function isReadableStream(value: unknown): value is ReadableStream {
  return (
    typeof ReadableStream !== "undefined" && value instanceof ReadableStream
  );
}
