# Home Hub — Estado del proyecto

## Stack
- **Backend**: .NET 10 ASP.NET Core Web API + EF Core 10 + Npgsql + PostgreSQL (Neon serverless)
- **Frontend**: React 19 + Vite + TypeScript + TailwindCSS v4 + Recharts + TanStack Query v5
- **Deploy**: Docker multi-stage build en Render (auto-deploy desde `main`)
- **Repo**: `C:\Users\alejb\source\repos\Home-hub`

---

## Qué está implementado y funcional

### Autenticación
- Registro de usuarios: `POST /api/auth/register` (email + contraseña hasheada con bcrypt)
- Login: `POST /api/auth/login` validado contra tabla `users` en BD
- JWT con `ClaimTypes.NameIdentifier` = userId (expira en 30 días)
- Página `/register` con validación de contraseña
- Página `/login` actualizada (campo "Correo electrónico", link a /register)

### Base de datos (tablas activas)
- `users`: email único + password_hash
- `homes`: 1:1 con usuario, se crea automáticamente al registrarse con nombre "Mi Hogar"
- `service_configs`: credenciales JSON por servicio (type, display_name, credentials_json, plant_id, enabled)
- `solar_readings`: lecturas del inversor Growatt (plant_id, power_w, energy_today_kwh, etc.)
- `sync_log`: log de sincronizaciones

### Growatt / Paneles Solares
- `GrowattClient`: login con MD5 hash, `POST /index/getPlantListTitle` para plant list, `POST /panel/getDevicesByPlantList` para datos del dispositivo
- `GrowattClientFactory`: singleton que mantiene un cliente por `serviceConfigId` (cada usuario tiene su propia sesión/cookie)
- `GrowattSyncService`: background service que corre cada hora, lee `service_configs` de la BD, sincroniza cada servicio activo
- `EquipmentStore`: diccionario en memoria (keyed por serviceConfigId) con modelo, potencia nominal, nombre de planta
- Endpoints solar scoped al userId del JWT: `/api/solar/summary`, `/api/solar/history`, `/api/solar/stats`

### Panel de administración del Hogar
- Página `/home` ("Mi Hogar") en el navbar
- Renombrar el hogar
- Agregar servicio Growatt con credenciales (usuario + contraseña)
- Editar servicio (contraseña en blanco = mantener la existente)
- Activar/desactivar servicio
- Eliminar servicio
- API: `GET/PUT /api/home`, `POST/PUT/DELETE /api/home/services/{id}`, `PUT /api/home/services/{id}/toggle`

### Dashboard
- 4 stat cards: Potencia actual, Energía hoy, Este mes, Total acumulado
- Gráfica de potencia (línea) y energía 7 días (barras)
- Card "Instalación": modelo del inversor, potencia nominal, nombre de planta
- Card "Impacto ambiental": CO2 evitado, árboles equivalentes

### Tests
- `AuthControllerTests`: register/login/unauthorized con in-memory DB + usuario pre-seeded
- `GrowattSyncServiceTests`: SyncAll con service config en BD, factory con mock client, caso sin servicios

---

## Flujo de primer uso (tras deploy)

1. Ir a `/register` → crear cuenta con email + contraseña
2. Ir a **Mi Hogar** (navbar) → `+ Agregar` → Growatt → ingresar credenciales de Growatt
3. Esperar primer sync (máx 30 segundos tras startup, luego cada hora)
4. Dashboard muestra datos (los históricos de `solar_readings` existentes también aparecen)

> Las variables de entorno `GROWATT_USER` y `GROWATT_PASSWORD` ya no son necesarias y pueden eliminarse de Render.

---

## Lo que falta por implementar

### Prioridad alta

#### 1. Verificación de correo electrónico
- Al registrarse, enviar email con link/token de verificación
- Agregar columna `email_verified BOOL DEFAULT FALSE` a tabla `users`
- Agregar columna `verification_token VARCHAR(100)` a tabla `users`
- Endpoint `GET /api/auth/verify?token=xxx` que marca el usuario como verificado
- Bloquear login si `email_verified = false`
- Servicio de envío de email (SendGrid o similar — agregar `SENDGRID_API_KEY` en env vars)
- Frontend: página de "Revisa tu correo" tras el registro + página de confirmación exitosa

#### 2. Recuperación de contraseña
- Endpoint `POST /api/auth/forgot-password` (recibe email, genera token temporal)
- Agregar columna `reset_token VARCHAR(100)` + `reset_token_expires TIMESTAMPTZ` a tabla `users`
- Enviar email con link de reset
- Endpoint `POST /api/auth/reset-password` (token + nueva contraseña)
- Frontend: página `/forgot-password` y `/reset-password?token=xxx`

### Prioridad media

#### 3. Múltiples usuarios reales
- La arquitectura ya lo soporta, pero actualmente cualquiera puede registrarse
- Considerar: invitaciones por email, o mantener registro abierto

#### 4. Más tipos de servicios
- La arquitectura está lista (`service_configs.type` es un string)
- Solo requiere agregar el tipo al select en `HomeAdmin.tsx` y la lógica de sync correspondiente
- Posibles servicios futuros: electricidad (CFE), agua, gas, etc.

#### 5. Notificaciones / alertas
- Alertar cuando el inversor está offline (status ≠ 1)
- Alertar cuando la producción es muy baja en un día soleado

#### 6. Trigger de sync manual
- Botón "Sincronizar ahora" en el dashboard o en Mi Hogar
- Endpoint `POST /api/solar/sync` que dispara un sync inmediato para el usuario

### Prioridad baja

#### 7. Perfil de usuario
- Página `/profile`: cambiar email, cambiar contraseña
- Endpoint `PUT /api/auth/change-password`

#### 8. Multi-hogar
- Actualmente es 1 usuario : 1 hogar (relación `WithOne`)
- Para soportar múltiples hogares por usuario, cambiar a `WithMany`

---

## Notas técnicas importantes

### Migraciones y Neon DB
- `CREATE TABLE` migrations funcionan correctamente en Neon
- **`ALTER TABLE ADD COLUMN` falla en Neon** en este setup — siempre usar nuevas tablas o columnas `[NotMapped]` para campos en memoria

### Credenciales Growatt
- Guardadas como JSON en `service_configs.credentials_json`: `{"username":"...","password":"..."}`
- Actualmente en texto plano — para producción real considerar cifrado (AES con clave de env var)

### Tokens JWT
- Tokens anteriores al refactor (sin `NameIdentifier` claim) → userId = 0 → app muestra estado vacío, no crash
- El usuario debe hacer logout/login con la nueva cuenta

### Variables de entorno requeridas en Render
```
DATABASE_URL=postgresql://...   # Neon connection string
JWT_SECRET=<string de 32+ chars>
```
