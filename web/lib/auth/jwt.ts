export type AuthenticatedUser = {
  id: string;
  tenantId: string;
  role: string;
  roles: string[];
  userName: string;
  companyName: string;
  expiresAtUtc?: string;
};

type JwtPayload = {
  sub?: unknown;
  tenant?: unknown;
  role?: unknown;
  roles?: unknown;
  user_name?: unknown;
  company_name?: unknown;
  exp?: unknown;
};

const jwtSegmentCount = 3;

export function decodeAuthenticatedUser(token: string): AuthenticatedUser {
  const payload = decodeJwtPayload(token);
  const id = readRequiredClaim(payload.sub, "sub");
  const tenantId = readRequiredClaim(payload.tenant, "tenant");
  const roles = readRoles(payload.role, payload.roles);
  const role = roles[0];
  const userName = readOptionalClaim(payload.user_name) ?? "User";
  const companyName = readOptionalClaim(payload.company_name) ?? "Workspace";
  const expiresAtUtc = readExpiry(payload.exp);

  if (expiresAtUtc && Date.parse(expiresAtUtc) <= Date.now()) {
    throw new Error("The authentication token has expired.");
  }

  return {
    id,
    tenantId,
    role,
    roles,
    userName,
    companyName,
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

function readOptionalClaim(value: unknown): string | undefined {
  if (typeof value !== "string" || value.trim().length === 0) {
    return undefined;
  }

  return value.trim();
}

function readRoles(roleClaim: unknown, rolesClaim: unknown): string[] {
  const roles = [
    ...readStringArrayClaim(roleClaim),
    ...readStringArrayClaim(rolesClaim),
  ].filter((role, index, allRoles) => allRoles.indexOf(role) === index);

  if (roles.length === 0) {
    throw new Error("The authentication token is missing the role claim.");
  }

  return roles;
}

function readStringArrayClaim(value: unknown): string[] {
  if (typeof value === "string" && value.trim().length > 0) {
    return [value.trim()];
  }

  if (Array.isArray(value)) {
    return value.filter(
      (item): item is string => typeof item === "string" && item.trim().length > 0,
    );
  }

  return [];
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
