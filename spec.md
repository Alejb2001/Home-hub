# Home Hub — Product Spec

**Version:** 0.1.0
**Date:** 2026-11-08
**Full design:** docs/superpowers/specs/2026-11-08-home-hub-solar-design.md

---

## Purpose

A monolithic web application that fetches solar panel data from Growatt/ShinePhone automatically and displays historical statistics as a family dashboard. Built to extend to other home metrics over time.

## Users and Workflows

- **Single user** (the owner's father). Credentials stored in env vars. No registration.
- Views today's solar summary on the dashboard.
- Browses historical production on the history page.
- Background job keeps data fresh without manual intervention.

## Core Rules

1. The Growatt API integration MUST be validated (spike) before building the full sync service. The API is unofficial and may change.
2. Growatt credentials MUST live exclusively in environment variables. Never committed to git.
3. The app MUST be deployable as a single Docker container to Render (one service, one URL, no CORS).
4. Auth is JWT, single user. The JWT secret MUST be set via environment variable.
5. All timestamps MUST be stored as UTC in the database.
6. The data model MUST support future home metric domains (new tables) without modifying existing ones.

## Data and Contracts

- **solar_readings**: plant_id, inverter_sn, timestamp_utc, power_w, energy_today_kwh, energy_total_kwh, status
- **sync_log**: synced_at, success, records_saved, error_message
- **REST API**: POST /api/auth/login | GET /api/solar/summary | GET /api/solar/history | GET /api/solar/stats

## Non-Goals (v1)

- Multi-user registration
- Real-time WebSocket updates
- Mobile app or PWA
- Other home metric domains (water, electricity) — extensibility is designed in, implementation is out of scope

## Open Decisions

- Growatt API endpoint shape and auth flow — to be confirmed via spike (Task 2.1)
- Render free-tier cold start latency — acceptable for personal use, no mitigation in v1

## Links

- Design spec: docs/superpowers/specs/2026-11-08-home-hub-solar-design.md
- Plans: Plans.md
