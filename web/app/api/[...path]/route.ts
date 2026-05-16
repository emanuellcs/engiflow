type ProxyRouteContext = {
  params: Promise<{
    path: string[];
  }>;
};

const hopByHopHeaders = new Set([
  "connection",
  "content-encoding",
  "content-length",
  "keep-alive",
  "proxy-authenticate",
  "proxy-authorization",
  "te",
  "trailer",
  "transfer-encoding",
  "upgrade",
]);

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

export async function GET(request: Request, context: ProxyRouteContext) {
  return proxyApiRequest(request, context);
}

export async function POST(request: Request, context: ProxyRouteContext) {
  return proxyApiRequest(request, context);
}

export async function PUT(request: Request, context: ProxyRouteContext) {
  return proxyApiRequest(request, context);
}

export async function PATCH(request: Request, context: ProxyRouteContext) {
  return proxyApiRequest(request, context);
}

export async function DELETE(request: Request, context: ProxyRouteContext) {
  return proxyApiRequest(request, context);
}

export async function OPTIONS(request: Request, context: ProxyRouteContext) {
  return proxyApiRequest(request, context);
}

async function proxyApiRequest(request: Request, context: ProxyRouteContext) {
  const targetUrl = await buildTargetUrl(request, context);
  const body = hasRequestBody(request) ? await request.arrayBuffer() : undefined;

  try {
    const response = await fetch(targetUrl, {
      method: request.method,
      headers: copyProxyHeaders(request.headers),
      body,
      cache: "no-store",
    });

    return new Response(response.body, {
      status: response.status,
      statusText: response.statusText,
      headers: copyProxyHeaders(response.headers),
    });
  } catch {
    return Response.json(
      {
        title: "API gateway unavailable.",
        detail: "The EngiFlow API could not be reached from the web service.",
      },
      { status: 502 },
    );
  }
}

async function buildTargetUrl(
  request: Request,
  context: ProxyRouteContext,
): Promise<string> {
  const requestUrl = new URL(request.url);
  const { path } = await context.params;
  const targetPath = path.map((segment) => encodeURIComponent(segment)).join("/");
  const targetUrl = new URL(`/api/${targetPath}`, getApiInternalBaseUrl());

  targetUrl.search = requestUrl.search;

  return targetUrl.toString();
}

function getApiInternalBaseUrl(): string {
  const baseUrl =
    process.env.API_INTERNAL_BASE_URL ||
    process.env.API_BASE_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    process.env.NEXT_PUBLIC_API_BASE_URL ||
    "http://localhost:8080";

  return baseUrl.replace(/\/+$/, "");
}

function copyProxyHeaders(headers: Headers): Headers {
  const nextHeaders = new Headers(headers);

  for (const headerName of hopByHopHeaders) {
    nextHeaders.delete(headerName);
  }

  return nextHeaders;
}

function hasRequestBody(request: Request): boolean {
  return request.method !== "GET" && request.method !== "HEAD";
}
