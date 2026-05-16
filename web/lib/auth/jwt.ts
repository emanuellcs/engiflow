export type AuthenticatedUser = {
  id: string;
  tenantId: string;
  role: string;
  expiresAtUtc?: string;
};

type JwtPayload = {
  sub?: unknown;
  tenant?: unknown;
  role?: unknown;
  exp?: unknown;
};

const jwtSegmentCount = 3;

export function decodeAuthenticatedUser(token: string): AuthenticatedUser {
  const payload = decodeJwtPayload(token);
  const id = readRequiredClaim(payload.sub, "sub");
  const tenantId = readRequiredClaim(payload.tenant, "tenant");
  const role = readRequiredClaim(payload.role, "role");
  const expiresAtUtc = readExpiry(payload.exp);

  if (expiresAtUtc && Date.parse(expiresAtUtc) <= Date.now()) {
    throw new Error("The authentication token has expired.");
  }

  return {
    id,
    tenantId,
    role,
    expiresAtUtc,
  };
}

function decodeJwtPayload(token: string): JwtPayload {
  const segments = token.split(".");

  if (segments.length !== jwtSegmentCount || !segments[1]) {
    throw new Error("The authentication token is not a valid JWT.");
  }

  const payloadJson = decodeBase64Url(segments[1]);
  const payload = JSON.parse(payloadJson) as JwtPayload;

  if (!payload || typeof payload !== "object") {
    throw new Error("The authentication token payload is invalid.");
  }

  return payload;
}

function readRequiredClaim(value: unknown, claimName: string): string {
  if (typeof value !== "string" || value.trim().length === 0) {
    throw new Error(`The authentication token is missing the ${claimName} claim.`);
  }

  return value;
}

function readExpiry(value: unknown): string | undefined {
  if (value === undefined) {
    return undefined;
  }

  if (typeof value !== "number" || !Number.isFinite(value)) {
    throw new Error("The authentication token has an invalid exp claim.");
  }

  return new Date(value * 1000).toISOString();
}

function decodeBase64Url(value: string): string {
  const normalized = value.replaceAll("-", "+").replaceAll("_", "/");
  const padded = normalized.padEnd(
    normalized.length + ((4 - (normalized.length % 4)) % 4),
    "=",
  );
  const binary = globalThis.atob(padded);
  const bytes = Uint8Array.from(binary, (character) => character.charCodeAt(0));

  return new TextDecoder().decode(bytes);
}
