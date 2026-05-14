# syntax=docker/dockerfile:1.7

# Node 24 satisfies the Next.js 16 engine requirement and keeps the image small.
FROM node:24-alpine AS base
WORKDIR /app
ENV NEXT_TELEMETRY_DISABLED=1

# Install dependencies from the lockfile for deterministic container builds.
FROM base AS deps
COPY package.json package-lock.json ./
RUN npm ci

# Build a production Next.js standalone bundle that can run without dev tooling.
FROM base AS builder
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

# Runtime stage contains only the standalone server, static files, and public assets.
FROM node:24-alpine AS runner
WORKDIR /app

ENV HOSTNAME=0.0.0.0 \
    NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000

RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 nextjs

# The standalone output traces the minimum files needed by Next.js at runtime.
COPY --from=builder --chown=nextjs:nodejs /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

# Run the web container as a non-root user.
USER nextjs

EXPOSE 3000

CMD ["node", "server.js"]
