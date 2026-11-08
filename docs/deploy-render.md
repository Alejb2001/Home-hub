# Deploy en Render

Guía paso a paso para publicar Home Hub en [Render](https://render.com) usando Docker.

---

## Requisitos previos

- Cuenta en [render.com](https://render.com) (free tier es suficiente)
- Cuenta en [neon.tech](https://neon.tech) con una base de datos PostgreSQL creada
- Repositorio en GitHub con el código del proyecto

---

## 1. Crear la base de datos en Neon

1. Ve a [neon.tech](https://neon.tech) y crea un proyecto nuevo.
2. En el dashboard del proyecto, copia el **Connection String** (formato `postgresql://...`).
   - Debe incluir `?sslmode=require` al final.
3. Guarda este string — lo necesitas en el paso 3.

---

## 2. Crear el Web Service en Render

1. En Render, haz clic en **New → Web Service**.
2. Conecta tu repositorio de GitHub.
3. Configura el servicio:

| Campo | Valor |
|-------|-------|
| **Name** | `home-hub` (o el nombre que prefieras) |
| **Region** | La más cercana a España (Frankfurt) |
| **Branch** | `main` |
| **Runtime** | **Docker** |
| **Dockerfile Path** | `./Dockerfile` |
| **Port** | `8080` |
| **Instance Type** | Free (suficiente para uso familiar) |

---

## 3. Configurar variables de entorno

En la sección **Environment Variables** del servicio en Render, agrega:

| Variable | Valor | Cómo obtenerlo |
|----------|-------|----------------|
| `DATABASE_URL` | `postgresql://user:pass@host.neon.tech/dbname?sslmode=require` | Neon dashboard → Connection string |
| `ADMIN_USER` | `tu_usuario` | Elige un nombre de usuario para el login |
| `ADMIN_PASSWORD` | `tu_contraseña_segura` | Elige una contraseña segura |
| `JWT_SECRET` | `string_aleatorio_largo` | Genera con: `openssl rand -base64 32` |
| `GROWATT_USER` | `tu@email.com` | El email de tu cuenta ShinePhone |
| `GROWATT_PASSWORD` | `tu_contraseña_growatt` | La contraseña de tu cuenta ShinePhone |

> **Importante:** Render guarda estas variables de forma segura. Nunca las escribas en archivos del repositorio.

---

## 4. Hacer el deploy

1. Haz clic en **Create Web Service**.
2. Render construirá la imagen Docker automáticamente (puede tardar 3-5 minutos la primera vez).
3. Una vez desplegado, la app estará en `https://home-hub-xxxx.onrender.com`.

---

## 5. Verificar

1. Visita la URL del servicio → deberías ver la pantalla de login.
2. Entra con `ADMIN_USER` / `ADMIN_PASSWORD`.
3. El dashboard mostrará "Sin datos" hasta que el job de Growatt corra por primera vez (~30 segundos después del inicio).

---

## Troubleshooting

### "DATABASE_URL is not configured"
- Verifica que la variable está en las Environment Variables del servicio en Render.
- Reinicia el servicio después de agregar variables.

### Error de SSL con Neon
- Asegúrate de que `DATABASE_URL` termina en `?sslmode=require`.
- Neon requiere SSL en todas las conexiones.

### "account locked" de Growatt
- Growatt puede bloquear la cuenta por demasiados intentos de login fallidos.
- Verifica que `GROWATT_USER` y `GROWATT_PASSWORD` son correctos probándolos primero en la app ShinePhone.
- Si la cuenta está bloqueada, espera 24 horas o contacta al soporte de Growatt.

### El servicio se "duerme" (Free tier)
- El plan gratuito de Render pone los servicios a dormir después de 15 minutos de inactividad.
- El primer acceso puede tardar 30-60 segundos en despertar.
- Para uso familiar esto es aceptable. Para evitarlo, considera el plan Starter ($7/mes).

---

## Redeploy automático

Cada vez que hagas `git push` a `main`, Render detectará el cambio y reconstruirá automáticamente la imagen Docker.
