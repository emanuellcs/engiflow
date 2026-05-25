import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

const COOKIE_NAME = "engi-locale";
const DEFAULT_LOCALE = "en";
const SUPPORTED_LOCALES = ["en", "pt-BR"];

/**
 * Next.js 16 Edge Proxy (Middleware replacement).
 * Handles enterprise-grade locale detection and cookie synchronization.
 *
 * @param request - Incoming Next.js request object.
 * @returns Synchronized response with locale preference.
 */
export function proxy(request: NextRequest) {
  const { cookies, headers } = request;
  
  // 1. Check for explicit user preference in cookies
  let locale = cookies.get(COOKIE_NAME)?.value;

  // 2. If cookie is absent, negotiate via Accept-Language header
  if (!locale) {
    const acceptLanguage = headers.get("accept-language") || "";
    
    // Detect Brazilian Portuguese preference
    if (acceptLanguage.includes("pt-BR") || acceptLanguage.includes("pt")) {
      locale = "pt-BR";
    } else {
      locale = DEFAULT_LOCALE;
    }
  }

  // 3. Validate against supported locales
  if (!SUPPORTED_LOCALES.includes(locale)) {
    locale = DEFAULT_LOCALE;
  }

  // 4. Prepare response and synchronize cookie
  const response = NextResponse.next();
  
  // Set long-duration cookie (1 year)
  response.cookies.set(COOKIE_NAME, locale, {
    path: "/",
    maxAge: 60 * 60 * 24 * 365,
    sameSite: "lax",
  });

  return response;
}

/**
 * Edge runtime configuration for the proxy layer.
 */
export const config = {
  matcher: [
    /*
     * Match all request paths except for the ones starting with:
     * - api (API routes)
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     * - public files (images, svgs, etc)
     */
    "/((?!api|_next/static|_next/image|favicon.ico|.*\\..*).*)",
  ],
};
