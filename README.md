# Home Hub

Dashboard web para estadísticas del hogar. Versión inicial: paneles solares via Growatt/ShinePhone.

## Stack

- **Backend**: ASP.NET Core 10 Web API + EF Core + PostgreSQL (Neon)
- **Frontend**: React 19 + Vite + TypeScript + Tailwind CSS + Recharts
- **Deploy**: Render (Docker monolith)

---

## Desarrollo local

### Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Setup

1. **Clonar y configurar variables de entorno**

   ```bash
   git clone <repo-url>
   cd Home-hub
   cp .env.example .env
   # Editar .env con tus valores reales
   ```

2. **Levantar la base de datos local**

   ```bash
   docker compose up db -d
   ```

3. **Aplicar migraciones**

   ```bash
   cd src/HomeHub.Api
   dotnet ef database update
   ```

4. **Iniciar el backend**

   ```bash
   cd src/HomeHub.Api
   dotnet run
   # API disponible en http://localhost:5000
   ```

5. **Iniciar el frontend** (en otra terminal)

   ```bash
   cd src/HomeHub.Web
   npm install
   npm run dev
   # UI disponible en http://localhost:5173 (proxied a :5000)
   ```

### Comandos útiles

```bash
# Correr tests
dotnet test

# Lint frontend
cd src/HomeHub.Web && npm run lint

# Formatear frontend
cd src/HomeHub.Web && npm run format

# Verificar formato .NET
dotnet format --verify-no-changes
```

---

## Deploy en Render

Ver [docs/deploy-render.md](docs/deploy-render.md) para la guía completa paso a paso.

---

## Variables de entorno

| Variable | Descripción |
|----------|-------------|
| `DATABASE_URL` | Connection string de Neon PostgreSQL |
| `JWT_SECRET` | Clave para firmar tokens JWT (string aleatorio largo) |
| `GROWATT_USER` | Email de cuenta ShinePhone/Growatt (es también el usuario del login web) |
| `GROWATT_PASSWORD` | Contraseña de cuenta ShinePhone/Growatt (es también la contraseña del login web) |
