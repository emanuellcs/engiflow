# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

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

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "EngiFlow.Api.dll"]
