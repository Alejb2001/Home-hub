# Home Hub Plans.md

Creado: 2026-11-08

---

## Phase 1: Bootstrap & Tooling

Purpose: Crear la estructura base del monorepo, herramientas de calidad y entorno local antes de cualquier código de negocio.

| Task | Contenido | DoD | Depends | Status |
|------|-----------|-----|---------|--------|
| 1.1 | `[Setup]` `[lane:gate]` `[tdd:skip:project-scaffold]` Inicializar solución .NET + proyectos. | `dotnet build` succeeds; `npm run build` succeeds; .gitignore y .env.example completos | - | cc:完了 [24d4161] |
| 1.2 | `[Setup]` `[lane:gate]` `[tdd:skip:tooling-only]` Baseline linting/formateo (.editorconfig, oxlint, Prettier). | `npm run lint` exits 0; `npm run format:check` exits 0 | 1.1 | cc:完了 [24d4161] |
| 1.3 | `[Setup]` `[lane:gate]` `[tdd:skip:infra-config]` docker-compose.yml + README setup local. | docker-compose.yml con PostgreSQL 16 + healthcheck; README completo | 1.1 | cc:完了 [24d4161] |

---

## Phase 2: Backend

Purpose: Implementar el backend completo — datos, autenticación, integración Growatt y endpoints REST.

| Task | Contenido | DoD | Depends | Status |
|------|-----------|-----|---------|--------|
| 2.1 | `[spike]` `[lane:gate]` `[tdd:skip:research-spike]` Validar API de Growatt. | `docs/spikes/growatt-api.md` con auth flow, campos mapeados, rate limits. Veredicto: viable. | Phase 1 | cc:完了 [a80b8b5] |
| 2.2 | `[Feature]` `[lane:gate]` `[tdd:skip:ef-migrations]` Modelos EF Core + migración inicial. | `dotnet ef migrations add` exitoso; tablas `solar_readings` y `sync_log` generadas | Phase 1 | cc:完了 [a80b8b5] |
| 2.3 | `[Feature]` `[lane:gate]` `[tdd:required]` `[feature:security]` Autenticación JWT. | 4 integration tests pasan (200+token, 401 wrong pass, 401 wrong user, 401 no token) | 2.2 | cc:完了 [a80b8b5] |
| 2.4 | `[Feature]` `[lane:gate]` `[tdd:required]` `[needs-spike]` Growatt API client. | GrowattClient implementado con MD5 auth, session cookies, mapeo a SolarReading | 2.1, 2.2 | cc:完了 [a80b8b5] |
| 2.5 | `[Feature]` `[lane:gate]` `[tdd:required]` GrowattSyncService (IHostedService). | 2 unit tests pasan (sync success + sync error → log sin rethrow) | 2.4 | cc:完了 [a80b8b5] |
| 2.6 | `[Feature]` `[lane:gate]` `[tdd:required]` Endpoints REST solar. | SolarController: summary, history, stats — todos requieren JWT | 2.3, 2.5 | cc:完了 [a80b8b5] |

---

## Phase 3: Frontend

Purpose: Construir la SPA React con login, dashboard y página de historial.

| Task | Contenido | DoD | Depends | Status |
|------|-----------|-----|---------|--------|
| 3.1 | `[Setup]` `[lane:gate]` `[tdd:skip:frontend-scaffold]` Base React (Router, TanStack Query, Tailwind v4, Recharts, Vite proxy + wwwroot output). | `npm run build` genera output en wwwroot; `npm run lint` exits 0 | Phase 1 | cc:完了 [1aa901d] |
| 3.2 | `[Feature]` `[lane:gate]` `[tdd:skip:ui-login]` Página de Login. | Formulario con JWT flow; redirect a / en éxito; mensaje error en fallo; PrivateRoute guard | 3.1, 2.3 | cc:完了 [1aa901d] |
| 3.3 | `[Feature]` `[lane:gate]` `[tdd:skip:ui-dashboard]` Página Dashboard. | 3 tarjetas + bar chart 7 días + line chart hoy; polling 5 min | 3.2, 2.6 | cc:完了 [1aa901d] |
| 3.4 | `[Feature]` `[lane:gate]` `[tdd:skip:ui-history]` Página de Historial. | Date range picker + line chart + tabla + exportar CSV | 3.1, 2.6 | cc:完了 [1aa901d] |

---

## Phase 4: Deployment

Purpose: Empaquetar la app como imagen Docker y prepararla para deploy en Render.

| Task | Contenido | DoD | Depends | Status |
|------|-----------|-----|---------|--------|
| 4.1 | `[Feature]` `[lane:gate]` `[tdd:skip:dockerfile]` Dockerfile multi-stage (node:20-alpine → sdk:10.0 → aspnet:10.0, puerto 8080). | Dockerfile + .dockerignore creados; build verificado localmente (stages correctos) | Phase 3 | cc:完了 [de14331] |
| 4.2 | `[Docs]` `[lane:release]` `[tdd:skip:docs-only]` docs/deploy-render.md + README actualizado. | docs/deploy-render.md con pasos completos, env vars, troubleshooting | 4.1 | cc:完了 [de14331] |

---

## Notas del plan

- **Spike obligatorio (2.1):** La API de Growatt es no oficial. Si el resultado del spike es "inviable", las tareas 2.4 y 2.5 se rediseñan antes de implementar.
- **formatter_baseline:** missing al iniciar. La tarea 1.2 establece el baseline antes de cualquier código de negocio.
- **TDD:** Aplicado a todas las clases de lógica de negocio backend (auth, Growatt client, sync service, endpoints). Omitido en scaffold, infra, migraciones y UI.
