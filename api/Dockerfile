# syntax=docker/dockerfile:1.7

# Build with the full .NET SDK so restore, compile, and publish stay reproducible in CI.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to maximize Docker layer reuse when source files change.
COPY ["src/EngiFlow.Domain/EngiFlow.Domain.csproj", "src/EngiFlow.Domain/"]
COPY ["src/EngiFlow.Application/EngiFlow.Application.csproj", "src/EngiFlow.Application/"]
COPY ["src/EngiFlow.Infrastructure/EngiFlow.Infrastructure.csproj", "src/EngiFlow.Infrastructure/"]
COPY ["src/EngiFlow.Api/EngiFlow.Api.csproj", "src/EngiFlow.Api/"]

RUN dotnet restore "src/EngiFlow.Api/EngiFlow.Api.csproj"

COPY . .

RUN dotnet publish "src/EngiFlow.Api/EngiFlow.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

# Runtime image contains only the ASP.NET runtime and published application output.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# App Runner and Compose both route HTTP traffic through the container port.
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .

# Use the non-root user provided by the official .NET images.
USER $APP_UID

ENTRYPOINT ["dotnet", "EngiFlow.Api.dll"]
