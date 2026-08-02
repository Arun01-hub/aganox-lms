# Aganox LMS

Enterprise Leave Management System (ASP.NET Core 8 + Angular 18).

## Phase 1 status

Foundation is implemented:

- Clean Architecture solution under `src/backend`
- JWT auth (login, refresh, logout, me, change/forgot/reset password)
- EF Core SQL Server schema + `InitialCreate` migration + seed data
- Angular auth shell + dashboard
- Docker Compose (optional SQL + API)

## Prerequisites

- .NET 8 SDK
- SQL Server LocalDB (default) or SQL Server Express
- Node.js 18+ (20 LTS recommended) for the Angular app

If LocalDB fails to start with missing `model.mdf` / `MSDBData.mdf` paths, recreate the instance:

```powershell
sqllocaldb stop MSSQLLocalDB
sqllocaldb delete MSSQLLocalDB
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

## Default admin (seeded)

- Email: `admin@aganox.local`
- Password: `ChangeMe!123`
- `MustChangePassword` is set to `true`

## Scripts

From the repo root (`C:\Aganox`):

| Script | Purpose |
|--------|---------|
| `.\scripts\check-prerequisites.ps1` | Verify .NET 8, Node 18+, LocalDB, HTTPS cert |
| `.\scripts\build.ps1` | Build backend + frontend |
| `.\scripts\build-backend.ps1` | Build .NET solution only |
| `.\scripts\build-frontend.ps1` | Build Angular app only |
| `.\scripts\run-api.ps1` | Check prereqs, build, start API (HTTPS) |
| `.\scripts\run-web.ps1` | Check prereqs, start Angular dev server |
| `.\scripts\test.ps1` | Run unit tests |
| `.\scripts\repair-localdb.ps1` | Recreate broken LocalDB instance |

```powershell
# Check everything
.\scripts\check-prerequisites.ps1

# Build all
.\scripts\build.ps1

# Run (two terminals)
.\scripts\run-api.ps1
.\scripts\run-web.ps1
```

## Run API (manual)

```powershell
cd src/backend/Aganox.Lms.Api
dotnet run
```

- Swagger: use the HTTPS URL from launchSettings (typically `https://localhost:7xxx/swagger`)
- Health: `GET /health`

On startup the API applies migrations and seeds roles, leave types, calendar, and the admin user.

## Run Angular UI (manual)

```powershell
cd src/frontend/aganox-lms-web
npm install
npm start
```

UI proxies `/api` to the API (see `proxy.conf.json`). Update the target port if your API HTTPS port differs.

## Smoke test (login)

```powershell
curl -k -X POST https://localhost:<api-port>/api/v1/auth/login `
  -H "Content-Type: application/json" `
  -d "{\"email\":\"admin@aganox.local\",\"password\":\"ChangeMe!123\"}"
```

## Docker (optional)

```powershell
cd docker
docker compose up --build
```

API: `http://localhost:8080` (SQL SA password is in `docker-compose.yml` for local/dev only).

## Solution layout

See `docs/design/` for architecture, ER, API, and roadmap.
