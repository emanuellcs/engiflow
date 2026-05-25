# syntax=docker/dockerfile:1.7

# --- STAGE: Build Environment (SDK) ---
# We use the official .NET 10 SDK image (mcr.microsoft.com/dotnet/sdk:10.0) to construct the compilation environment.
# This heavy image contains the complete MSBuild toolchain, compilers, and NuGet restoration utilities
# required to produce a deterministic and reproducible build in CI/CD pipelines.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Establishing the WORKDIR anchors our relative paths. 
# This structured directory isolation ensures that our container filesystem does not collide with default Linux binaries.
WORKDIR /src

# --- LAYER OPTIMIZATION: Dependency Caching ---
# By extracting and copying ONLY the `.csproj` files first, we maximize Docker's layer caching mechanism.
# The `dotnet restore` execution will lock into an intermediary layer. If application source code (.cs files) changes 
# but dependencies remain the same, Docker skips the expensive restore phase entirely, drastically accelerating build times.
COPY ["src/EngiFlow.Domain/EngiFlow.Domain.csproj", "src/EngiFlow.Domain/"]
COPY ["src/EngiFlow.Application/EngiFlow.Application.csproj", "src/EngiFlow.Application/"]
COPY ["src/EngiFlow.Infrastructure/EngiFlow.Infrastructure.csproj", "src/EngiFlow.Infrastructure/"]
COPY ["src/EngiFlow.Api/EngiFlow.Api.csproj", "src/EngiFlow.Api/"]

RUN dotnet restore "src/EngiFlow.Api/EngiFlow.Api.csproj"

# Once dependencies are restored and cached, we bring in the remaining application source.
COPY . .

# --- STAGE: Publish Compilation ---
# The publish phase executes with `--configuration Release` to strip debug symbols, optimize IL code, 
# and compress the compilation output. We set `/p:UseAppHost=false` to prevent the generation of 
# platform-specific executables, relying instead on the cross-platform `dotnet` runtime host.
RUN dotnet publish "src/EngiFlow.Api/EngiFlow.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

# --- STAGE: Production Runtime (Ephemeral Footprint) ---
# We transition to the lightweight ASP.NET 10 runtime image (mcr.microsoft.com/dotnet/aspnet:10.0).
# Separating the SDK compilation environment from the runtime yields a secure, minimal production container footprint.
# It eliminates compilers and shell tools, significantly reducing the attack surface and image bloat.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Npgsql requires GSSAPI support when probing PostgreSQL connections. 
# We explicitly inject this minimal native library dependency into the runtime layer.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# We explicitly bind Kestrel to listen on port 8080.
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

# We extract ONLY the compiled binaries from the heavy 'build' stage into our slim runtime stage.
COPY --from=build /app/publish .

# Elevating security by stripping root privileges. The container process runs under the restricted, 
# built-in non-root user ($APP_UID) provided by the official Microsoft runtime image.
USER $APP_UID

# --- ENTRYPOINT INITIALIZATION ---
# The execution pattern uses the lightweight `dotnet` host to spin up our managed DLL.
ENTRYPOINT ["dotnet", "EngiFlow.Api.dll"]
