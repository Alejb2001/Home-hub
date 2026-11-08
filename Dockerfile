# ── Stage 1: Build React frontend ──────────────────────────────────────────────
FROM node:20-alpine AS frontend-build

WORKDIR /app/web
COPY src/HomeHub.Web/package*.json ./
RUN npm ci

COPY src/HomeHub.Web/ ./
# Build outputs to ../HomeHub.Api/wwwroot (relative to vite.config.ts)
# We redirect output to a known location inside this stage instead
RUN npx vite build --outDir /app/wwwroot --emptyOutDir

# ── Stage 2: Build .NET backend ─────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build

WORKDIR /app

COPY Home-hub.slnx ./
COPY src/HomeHub.Api/HomeHub.Api.csproj src/HomeHub.Api/
COPY src/HomeHub.Api.Tests/HomeHub.Api.Tests.csproj src/HomeHub.Api.Tests/

RUN dotnet restore src/HomeHub.Api/HomeHub.Api.csproj

COPY src/HomeHub.Api/ src/HomeHub.Api/
COPY src/HomeHub.Api.Tests/ src/HomeHub.Api.Tests/

# Copy React build output into wwwroot
COPY --from=frontend-build /app/wwwroot src/HomeHub.Api/wwwroot/

RUN dotnet publish src/HomeHub.Api/HomeHub.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 3: Runtime image ──────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app
COPY --from=backend-build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "HomeHub.Api.dll"]
