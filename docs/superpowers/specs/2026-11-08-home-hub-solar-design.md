# Home Hub — Solar Dashboard Design

**Date:** 2026-11-08
**Status:** Approved

---

## Overview

A monolithic web application that fetches historical solar panel data from the Growatt/ShinePhone platform and displays it as a dashboard for personal/family use. Built to be extensible toward other home metrics in the future (water, electricity, etc.).

---

## Architecture

Single Docker container deployed to Render as a Web Service. ASP.NET Core serves both the REST API (`/api/*`) and the React SPA (compiled into `wwwroot` at build time). No CORS configuration needed. A background job polls Growatt's servers every hour and persists data to Neon PostgreSQL.

```
Home-hub/
├── src/
│   ├── HomeHub.Api/
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── BackgroundJobs/
│   │   ├── Data/
│   │   ├── Models/
│   │   └── wwwroot/           <- React build output (generated)
│   └── HomeHub.Web/
│       ├── src/
│       │   ├── pages/
│       │   ├── components/
│       │   └── api/
│       └── package.json
├── Dockerfile
├── docker-compose.yml
└── Home-hub.slnx
```

---

## Backend

### Growatt Integration

Growatt does not publish an official API. The community has documented the endpoints on `https://server.growatt.com`. Authentication uses the ShinePhone account credentials (email + password). The client fetches:

- Instantaneous power (W)
- Energy generated today (kWh)
- Energy generated total (kWh)
- Inverter status (online/offline)

Credentials are stored exclusively in environment variables and never committed to git.

### Background Job

An `IHostedService` using `PeriodicTimer` runs every hour. On each tick it:

1. Calls the Growatt API
2. Normalizes the response
3. Persists a new row to `solar_readings`
4. Writes a row to `sync_log` (success or failure)

On failure, it logs the error and waits for the next scheduled tick. No immediate retries to avoid IP bans from Growatt.

### REST Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/auth/login` | Returns a signed JWT |
| `GET` | `/api/solar/summary` | Current day: power, energy, status |
| `GET` | `/api/solar/history?from=&to=` | Time series for a date range |
| `GET` | `/api/solar/stats?period=month` | Aggregates: day / week / month / year |

### Authentication

JWT, single user. `ADMIN_USER` and `ADMIN_PASSWORD` are set as environment variables on Render. No user table in the database. No public registration.

---

## Frontend

**Stack:** React (Vite) + TypeScript + Tailwind CSS + Recharts + TanStack Query + React Router

### Pages

| Route | Content |
|-------|---------|
| `/login` | Simple form, stores JWT in localStorage |
| `/` | Dashboard — current day summary |
| `/history` | Interactive chart with date range selector |

### Dashboard

- Card: current power (W), auto-refreshes every 5 minutes
- Card: energy generated today (kWh)
- Card: inverter status (online / offline)
- Bar chart: energy per day for the last 7 days
- Line chart: power curve throughout the current day

### History Page

- Date range picker
- Line or bar chart depending on selected period
- Raw data table with CSV export

---

## Data Model

### `solar_readings`

```sql
id                BIGSERIAL PRIMARY KEY
plant_id          VARCHAR(50)
inverter_sn       VARCHAR(50)
timestamp_utc     TIMESTAMPTZ
power_w           DECIMAL(10,2)
energy_today_kwh  DECIMAL(10,3)
energy_total_kwh  DECIMAL(12,3)
status            SMALLINT          -- 1=online, 0=offline
created_at        TIMESTAMPTZ DEFAULT NOW()
```

Indexes:
- `(plant_id, timestamp_utc DESC)` — history queries
- `(timestamp_utc DESC)` — daily summary queries

### `sync_log`

```sql
id              BIGSERIAL PRIMARY KEY
synced_at       TIMESTAMPTZ
success         BOOLEAN
records_saved   INT
error_message   TEXT
```

Schema changes managed by EF Core Migrations. No user table. Future home metric domains (water, electricity) each get their own table following the same pattern.

---

## Deployment

### Dockerfile (multi-stage)

| Stage | Base image | Action |
|-------|-----------|--------|
| 1 | `node:alpine` | Install React deps, build to `/dist` |
| 2 | `mcr.microsoft.com/dotnet/sdk` | Copy `/dist` to `wwwroot`, publish .NET app |
| 3 | `mcr.microsoft.com/dotnet/aspnet` | Final runtime image (~150 MB) |

### Render

- Service type: Web Service
- Runtime: Docker
- Port: `8080`

### Environment Variables

| Variable | Description |
|----------|-------------|
| `DATABASE_URL` | Neon connection string |
| `ADMIN_USER` | Login username |
| `ADMIN_PASSWORD` | Login password |
| `JWT_SECRET` | Token signing key |
| `GROWATT_USER` | ShinePhone account email |
| `GROWATT_PASSWORD` | ShinePhone account password |

### Neon

- 1 database, 1 branch (`main`)
- SSL required (`SslMode=Require` in EF Core connection string)
- Free tier: 0.5 GB storage (sufficient for years of hourly readings)

### Local Development

`docker-compose.yml` starts the full stack with a local PostgreSQL instance. Environment variables loaded from `.env` (git-ignored).

---

## Extensibility

The domain-per-table data model and modular service structure allow adding new home metrics without touching existing code:

- New metric = new table + new `IHostedService` + new controller + new React page
- The auth and deployment infrastructure is shared from day one
