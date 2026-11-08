# Home Hub Plans.md

Creado: 2026-11-08

---

## Phase 1: Bootstrap & Tooling

Purpose: Crear la estructura base del monorepo, herramientas de calidad y entorno local antes de cualquier código de negocio.

| Task | Contenido | DoD | Depends | Status |
|------|-----------|-----|---------|--------|
| 1.1 | `[Setup]` `[lane:gate]` `[tdd:skip:project-scaffold]` Inicializar solución .NET + proyectos. Crear `Home-hub.slnx` con `HomeHub.Api` (ASP.NET Core Web API, .NET 8) y `HomeHub.Web` (Vite + React + TypeScript). Configurar `.gitignore` y `.env.example` con todas las variables requeridas (DATABASE_URL, ADMIN_USER, ADMIN_PASSWORD, JWT_SECRET, GROWATT_USER, GROWATT_PASSWORD). | `dotnet build` succeeds; `npm install && npm run build` succeeds en HomeHub.Web; .gitignore cubre bin/, obj/, node_modules/, .env; .env.example tiene todos los campos | - | cc:TODO |
| 1.2 | `[Setup]` `[lane:gate]` `[tdd:skip:tooling-only]` Configurar baseline de linting y formateo. .NET: `.editorconfig` + `dotnet format`. React/TS: ESLint + Prettier con config base. Agregar scripts `lint` y `format` en package.json. | `dotnet format --verify-no-changes` exits 0; `npm run lint` exits 0; `npm run format -- --check` exits 0 sobre archivos scaffold | 1.1 | cc:TODO |
| 1.3 | `[Setup]` `[lane:gate]` `[tdd:skip:infra-config]` Crear `docker-compose.yml` para desarrollo local con servicio PostgreSQL (imagen `postgres:16`), variables de entorno desde `.env`, y health check. Documentar comandos de inicio en README. | `docker compose up -d` levanta PostgreSQL; app puede conectar a `localhost:5432`; README tiene instrucciones de setup local | 1.1 | cc:TODO |

---

## Phase 2: Backend

Purpose: Implementar el backend completo — datos, autenticación, integración Growatt y endpoints REST.

| Task | Contenido | DoD | Depends | Status |
|------|-----------|-----|---------|--------|
| 2.1 | `[spike]` `[lane:gate]` `[tdd:skip:research-spike]` Validar API de Growatt. Autenticarse contra `server.growatt.com` con credenciales de prueba, obtener lista de plantas e inversores, y documentar shape exacto de respuestas (campos, tipos, auth flow, rate limits observados). Registrar resultado en `docs/spikes/growatt-api.md`. | `docs/spikes/growatt-api.md` existe con: auth flow documentado, ejemplo de respuesta JSON de planta e inversor, campos mapeados a `solar_readings`, nota sobre rate limits. Veredicto: viable / inviable / requiere ajuste de diseño. | Phase 1 | cc:TODO |
| 2.2 | `[Feature]` `[lane:gate]` `[tdd:skip:ef-migrations]` Modelos EF Core + conexión Neon + migración inicial. Crear `SolarReading` y `SyncLog` como entidades, `AppDbContext`, connection string con SSL (`SslMode=Require`), y ejecutar `dotnet ef migrations add InitialCreate`. | `dotnet ef database update` aplica migración sin errores; tablas `solar_readings` y `sync_log` existen en DB con esquema correcto; EF Core puede insertar y leer un registro de prueba | Phase 1 | cc:TODO |
| 2.3 | `[Feature]` `[lane:gate]` `[tdd:required]` `[feature:security]` Autenticación JWT. Implementar `POST /api/auth/login` que valida `ADMIN_USER`/`ADMIN_PASSWORD` de env vars y devuelve JWT firmado con `JWT_SECRET`. Middleware que rechaza requests a `/api/*` sin token válido (excepto `/api/auth/login`). | Tests unitarios pasan: login correcto → 200 + token válido; credenciales incorrectas → 401; request sin token a endpoint protegido → 401; `dotnet format --verify-no-changes` exits 0 | 2.2 | cc:TODO |
| 2.4 | `[Feature]` `[lane:gate]` `[tdd:required]` `[needs-spike]` Growatt API client. Implementar `GrowattClient` como servicio inyectable (HttpClient) con: login, obtener datos de planta e inversor, mapear respuesta a `SolarReading`. Usar shape documentado en spike 2.1. | Tests unitarios pasan con HttpClient mockeado: login exitoso, datos de planta parseados correctamente a `SolarReading`, fallo de red → excepción tipada; `dotnet format --verify-no-changes` exits 0 | 2.1, 2.2 | cc:TODO |
| 2.5 | `[Feature]` `[lane:gate]` `[tdd:required]` GrowattSyncService (IHostedService). Implementar `GrowattSyncService` que usa `PeriodicTimer` cada 1 hora, llama a `GrowattClient`, persiste en `solar_readings` y escribe en `sync_log`. En fallo: loguea error, escribe fila de error en `sync_log`, espera al próximo tick (sin reintentos inmediatos). | Tests unitarios pasan: un tick llama al client 1 vez y persiste 1 reading; fallo del client → escribe `sync_log` con `success=false` y no relanza excepción; `dotnet format --verify-no-changes` exits 0 | 2.4 | cc:TODO |
| 2.6 | `[Feature]` `[lane:gate]` `[tdd:required]` Endpoints REST solar. Implementar `SolarController` con: `GET /api/solar/summary` (última lectura + energía del día), `GET /api/solar/history?from=&to=` (serie de tiempo), `GET /api/solar/stats?period=day|week|month|year` (agregados). Todos requieren JWT válido. | Tests de integración (WebApplicationFactory) pasan: summary devuelve datos del día, history filtra por rango, stats agrega correctamente; sin token → 401; `dotnet format --verify-no-changes` exits 0 | 2.3, 2.5 | cc:TODO |

---

## Phase 3: Frontend

Purpose: Construir la SPA React con login, dashboard y página de historial.

| Task | Contenido | DoD | Depends | Status |
|------|-----------|-----|---------|--------|
| 3.1 | `[Setup]` `[lane:gate]` `[tdd:skip:frontend-scaffold]` Base React. Configurar React Router, TanStack Query (QueryClient + provider), Tailwind CSS, Recharts. Crear layout base con navegación (Dashboard / History). Configurar proxy Vite (`/api` → `localhost:5xxx`) para desarrollo local. Configurar `vite.config.ts` para output en `../HomeHub.Api/wwwroot` en build de producción. | `npm run dev` levanta sin errores; `npm run build` genera output en `wwwroot`; navegación entre / y /history funciona; `npm run lint` exits 0 | Phase 1 | cc:TODO |
| 3.2 | `[Feature]` `[lane:gate]` `[tdd:skip:ui-login]` Página de Login. Formulario usuario/contraseña que llama a `POST /api/auth/login`, guarda JWT en localStorage y redirige a dashboard. Redirige a /login si el token está ausente o expirado. | Login con credenciales correctas → redirige a /; credenciales incorrectas → muestra mensaje de error; sin token → redirige a /login; `npm run lint` exits 0 | 3.1, 2.3 | cc:TODO |
| 3.3 | `[Feature]` `[lane:gate]` `[tdd:skip:ui-dashboard]` Página Dashboard. Mostrar: tarjeta potencia actual (W), tarjeta energía hoy (kWh), tarjeta estado inversor (online/offline). Gráfica de barras: energía últimos 7 días. Gráfica de línea: potencia durante el día actual. Polling automático cada 5 minutos via TanStack Query `refetchInterval`. | Dashboard muestra las 3 tarjetas y 2 gráficas con datos reales del backend; al expirar token redirige a /login; `npm run lint` exits 0 | 3.2, 2.6 | cc:TODO |
| 3.4 | `[Feature]` `[lane:gate]` `[tdd:skip:ui-history]` Página de Historial. Selector de rango de fechas, gráfica (línea o barras según período), tabla de datos crudos con botón de exportar CSV (generado en cliente con los datos del query). | Cambiar rango de fechas actualiza gráfica y tabla; botón "Exportar CSV" descarga archivo con datos del período visible; `npm run lint` exits 0 | 3.1, 2.6 | cc:TODO |

---

## Phase 4: Deployment

Purpose: Empaquetar la app como imagen Docker y prepararla para deploy en Render.

| Task | Contenido | DoD | Depends | Status |
|------|-----------|-----|---------|--------|
| 4.1 | `[Feature]` `[lane:gate]` `[tdd:skip:dockerfile]` Dockerfile multi-stage. Stage 1: `node:20-alpine` → instala deps, compila React a `/app/dist`. Stage 2: `mcr.microsoft.com/dotnet/sdk:8.0` → copia `/app/dist` a `wwwroot`, publica .NET. Stage 3: `mcr.microsoft.com/dotnet/aspnet:8.0` → imagen final, expone puerto 8080, ENV `ASPNETCORE_URLS=http://+:8080`. | `docker build -t home-hub .` completa sin errores; `docker run --env-file .env home-hub` levanta y sirve la app en puerto 8080; imagen final < 300 MB | Phase 3 | cc:TODO |
| 4.2 | `[Docs]` `[lane:release]` `[tdd:skip:docs-only]` Configuración Render + documentación. Documentar en README: cómo crear el Web Service en Render (Runtime: Docker, Port: 8080), qué env vars configurar y dónde obtener cada valor (Neon connection string, credenciales Growatt). Incluir sección de troubleshooting para problemas comunes (SSL Neon, cold start). | README tiene sección "Deploy en Render" con pasos completos; todas las env vars de `.env.example` están documentadas con descripción y ejemplo de valor | 4.1 | cc:TODO |

---

## Notas del plan

- **Spike obligatorio (2.1):** La API de Growatt es no oficial. Si el resultado del spike es "inviable", las tareas 2.4 y 2.5 se rediseñan antes de implementar.
- **formatter_baseline:** missing al iniciar. La tarea 1.2 establece el baseline antes de cualquier código de negocio.
- **TDD:** Aplicado a todas las clases de lógica de negocio backend (auth, Growatt client, sync service, endpoints). Omitido en scaffold, infra, migraciones y UI.
