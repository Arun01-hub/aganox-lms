# Aganox LMS

Enterprise Leave Management System — ASP.NET Core + Angular.

## Status

Phases 1–8 implemented:

- Foundation, organization, leave config, requests & balances
- Approval workflow & notifications
- Dashboards & reports
- Admin, audit, backup
- **Hardening, publish scripts, Inno Setup installer, documentation, CI**

## Prerequisites

- .NET 10 SDK
- SQL Server LocalDB (default) or SQL Server Express
- Node.js 20 LTS for the Angular app
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) (optional, for building the installer)

If LocalDB fails to start, run `.\scripts\repair-localdb.ps1`.

## Default admin (seeded)

| | |
|---|---|
| Email | `admin@aganox.local` |
| Password | `ChangeMe!123` |
| Note | Must change password on first login |

## Quick start (development)

```powershell
.\scripts\check-prerequisites.ps1
.\scripts\build.ps1
.\scripts\run-api.ps1      # terminal 1
.\scripts\run-web.ps1      # terminal 2
```

- API Swagger: HTTPS URL from launch settings (e.g. `https://localhost:7277/swagger`)
- UI: `http://localhost:4200` (proxies `/api` to the API)

## Production publish

```powershell
.\scripts\publish-win-x64.ps1 -Configuration Release
# Output: artifacts/publish/win-x64/
# Run: artifacts/publish/win-x64/Start-AganoxLms.cmd
```

Build installer:

```powershell
.\scripts\build-installer.ps1
# Output: artifacts/installer/AganoxLms-Setup-1.0.0-x64.exe
```

**Production requires** `Jwt__Key` (32+ character secret). See [Deployment Guide](docs/deployment/README.md).

## Scripts

| Script | Purpose |
|--------|---------|
| `check-prerequisites.ps1` | Verify .NET, Node, LocalDB |
| `build.ps1` | Build backend + frontend |
| `test.ps1` | Run all .NET tests |
| `publish-win-x64.ps1` | Self-contained x64 publish (API + SPA) |
| `publish-win-x86.ps1` | Self-contained x86 publish |
| `build-installer.ps1` | Publish + Inno Setup installer |
| `backup-db.ps1` | SQL Server `.bak` via sqlcmd |
| `repair-localdb.ps1` | Recreate broken LocalDB |

## Documentation

| Doc | Audience |
|-----|----------|
| [Deployment Guide](docs/deployment/README.md) | IT / install & upgrade |
| [Administrator Guide](docs/admin-guide/README.md) | HR / system admins |
| [User Manual](docs/user-manual/README.md) | Employees & managers |
| [Future Enhancements](docs/future-enhancements.md) | AD/SSO, cloud, roadmap |
| [Design docs](docs/design/) | Architecture, API, roadmap |

## Tests & CI

```powershell
.\scripts\test.ps1 -Configuration Release
```

GitHub Actions workflow: `.github/workflows/ci.yml` (build, test, publish smoke on Windows).

## Docker (optional dev)

```powershell
cd docker
docker compose up --build
```

## Solution layout

```
src/backend/          # .NET Clean Architecture API
src/frontend/         # Angular SPA
src/installer/        # Inno Setup script
scripts/              # Build, publish, backup
docs/                 # Deployment, admin, user guides
tests/                # Domain, application, API integration tests
```

## Related Aganox repositories

This repo contains **Aganox LMS only**. Other products live in separate repositories:

| Repository | Product |
|------------|---------|
| [Aganox-Identity](../Aganox-Identity) | OpenIddict authentication server |
| [Aganox-Rental](../Aganox-Rental) | Property rental API + SPA (uses Identity) |
