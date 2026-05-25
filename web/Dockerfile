# syntax=docker/dockerfile:1.7

# --- STAGE: Base Footprint ---
# We anchor our build on the Node 24 Alpine image. This selection ensures a minimal operating system 
# footprint, significantly enhancing security by reducing the available binaries and stripping 
# unnecessary telemetry layers (NEXT_TELEMETRY_DISABLED=1).
FROM node:24-alpine AS base
WORKDIR /app
ENV NEXT_TELEMETRY_DISABLED=1

# --- STAGE: Dependency Lock (Cache Layer) ---
# We isolate the dependency installation phase to maximize cache hits. By copying ONLY the 
# package manifests first and executing `npm ci`, we ensure that the heavy node_modules 
# layer is only rebuilt when the lockfile itself changes.
FROM base AS deps
COPY package.json package-lock.json ./
RUN npm ci

# --- STAGE: Builder (Next.js Compilation) ---
# The builder phase utilizes the full source tree to generate a production-optimized Next.js bundle.
# Next.js 16 uses the 'standalone' output mode to trace the minimum required file graph for runtime.
FROM base AS builder
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

# --- STAGE: Runner (Slim Production Stage) ---
# The final runner stage is a fresh Node Alpine environment, completely isolated from build-time devDependencies.
# This keeps the production container light, performant, and secure.
FROM node:24-alpine AS runner
WORKDIR /app

ENV HOSTNAME=0.0.0.0 \
    NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000

# We explicitly create a non-root system user and group (nextjs:nodejs) to execute the server process.
RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 nextjs

# We extract ONLY the standalone server, public assets, and static artifacts from the builder stage.
COPY --from=builder --chown=nextjs:nodejs /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

# Stripping root privileges: The active production process runs under the restricted 'nextjs' user.
USER nextjs

EXPOSE 3000

# The container entrypoint executes the lightweight standalone server.
CMD ["node", "server.js"]
