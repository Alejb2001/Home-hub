# Estado del Proyecto — Home Hub

**Última actualización:** 2026-11-08

---

## Lo que está hecho

### Código completo (14/14 tareas)

| Fase | Qué se construyó | Commit |
|------|-----------------|--------|
| **Phase 1** Bootstrap | Solución .NET 10 + React 19/Vite, .gitignore, .env.example, docker-compose local, linting/formateo | `24d4161` |
| **Phase 2** Backend | Modelos EF Core (solar_readings, sync_log), migración PostgreSQL, JWT auth, GrowattClient (API no oficial), GrowattSyncService (cada hora), endpoints REST (summary/history/stats) | `a80b8b5` |
| **Phase 3** Frontend | Login, Dashboard (3 tarjetas + 2 gráficas), Historial (date picker + gráfica + exportar CSV) con Tailwind + Recharts | `1aa901d` |
| **Phase 4** Deploy | Dockerfile multi-stage, .dockerignore, guía completa de Render (`docs/deploy-render.md`) | `de14331` |

### Tests
- 6/6 tests pasan (`dotnet test`)
- Auth integration: login correcto, contraseña incorrecta, usuario incorrecto, sin token
- Sync service unit: sync exitoso, sync con error (no relanza)

### Archivos clave

```
Home-hub/
├── spec.md                              # Contrato de producto SSOT
├── Plans.md                             # Plan de implementación (todo cc:完了)
├── Dockerfile                           # Build multi-stage para Render
├── docker-compose.yml                   # PostgreSQL local para desarrollo
├── .env.example                         # Plantilla de variables de entorno
├── docs/
│   ├── deploy-render.md                 # Guía paso a paso para Render + Neon
│   ├── spikes/growatt-api.md            # Documentación API Growatt
│   └── superpowers/specs/...-design.md  # Diseño original del proyecto
├── src/
│   ├── HomeHub.Api/                     # Backend ASP.NET Core 10
│   │   ├── Controllers/                 # AuthController, SolarController
│   │   ├── Services/                    # GrowattClient, SolarService
│   │   ├── BackgroundJobs/              # GrowattSyncService (cada 1h)
│   │   ├── Data/                        # AppDbContext + Migrations
│   │   └── Models/                      # SolarReading, SyncLog
│   ├── HomeHub.Api.Tests/               # xUnit tests
│   └── HomeHub.Web/                     # React frontend
│       └── src/
│           ├── api/client.ts            # API client + JWT
│           ├── pages/                   # Login, Dashboard, History
│           └── components/Layout.tsx    # Navbar compartido
```

---

## Próximos pasos para hacer el deploy

### Paso 1 — Crear la base de datos en Neon (5 min)

1. Ir a [neon.tech](https://neon.tech) → crear cuenta gratuita
2. Crear un nuevo proyecto (nombre: `home-hub`)
3. Copiar el **Connection String** del dashboard:
   ```
   postgresql://user:password@ep-xxx.eu-central-1.aws.neon.tech/neondb?sslmode=require
   ```
4. Guardar este string — se necesita en el Paso 3

### Paso 2 — Subir el código a GitHub (2 min)

El repositorio ya está conectado a GitHub. Solo hacer push:

```bash
cd C:\Users\alejb\source\repos\Home-hub
git push origin main
```

### Paso 3 — Crear el Web Service en Render (5 min)

1. Ir a [render.com](https://render.com) → crear cuenta gratuita
2. **New → Web Service**
3. Conectar el repositorio `Home-hub` de GitHub
4. Configurar:
   - **Runtime:** Docker
   - **Port:** `8080`
   - **Region:** Frankfurt (más cercano a España)
   - **Branch:** `main`

### Paso 4 — Configurar variables de entorno en Render

En la sección **Environment Variables** del servicio, agregar estas 6 variables:

| Variable | Valor |
|----------|-------|
| `DATABASE_URL` | El Connection String de Neon del Paso 1 |
| `ADMIN_USER` | El nombre de usuario para el login (ej: `papa`) |
| `ADMIN_PASSWORD` | Una contraseña segura (ej: `Solar2024!`) |
| `JWT_SECRET` | String aleatorio largo — generar con `openssl rand -base64 32` o usar cualquier string largo |
| `GROWATT_USER` | El email de la cuenta ShinePhone |
| `GROWATT_PASSWORD` | La contraseña de ShinePhone |

### Paso 5 — Deploy automático

1. Hacer clic en **Create Web Service**
2. Render construirá la imagen Docker automáticamente (~3-5 min la primera vez)
3. Una vez listo, la URL será: `https://home-hub-xxxx.onrender.com`

### Paso 6 — Verificar que funciona

1. Abrir la URL → aparece la pantalla de login
2. Entrar con `ADMIN_USER` / `ADMIN_PASSWORD`
3. El dashboard mostrará "Sin datos" los primeros ~30 segundos
4. Pasado ese tiempo, el job de Growatt corre y aparecen los primeros datos

---

## Notas importantes

### Cuenta de Growatt
- Usar exactamente el mismo email y contraseña con los que entrás a ShinePhone
- Si aparece error de "account locked" en los logs de Render, es porque Growatt bloqueó la cuenta por intentos fallidos previos → esperar 24h

### Free tier de Render
- El servicio se "duerme" después de 15 min sin visitas
- El primer acceso puede tardar 30-60 segundos en despertar
- Para uso familiar es completamente aceptable

### Redeploys automáticos
- Cada `git push` a `main` dispara un redeploy automático en Render

### Ver logs en Render
- Dashboard del servicio → pestaña **Logs**
- Ahí se ven los mensajes del sync de Growatt cada hora

---

## Para el futuro (posibles expansiones)

- **Agua / Electricidad:** Agregar nueva tabla + `IHostedService` + endpoints + página React, sin tocar lo existente
- **Notificaciones:** Alertar si el inversor se pone offline por más de X horas
- **Multi-usuario:** Agregar tabla de usuarios y registro si más familiares quieren acceso
