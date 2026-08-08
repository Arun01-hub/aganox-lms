# lms-pay (Aganox LMS)

Enterprise Leave Management + Payroll — ASP.NET Core + Angular.

## Status

Phases 1–8 implemented:

- Foundation, organization, leave config, requests & balances
- Approval workflow & notifications
- Dashboards & reports
- Admin, audit, backup
- **Hardening, publish scripts, Inno Setup installer, documentation, CI**
- **Payroll module:** configurable salary components/rules, attendance, monthly payroll runs, LOP, arrears, payslips, payroll reports (EN/TA UI)

## Prerequisites

- .NET 10 SDK
- SQL Server LocalDB (default) or SQL Server Express — **dev only** for LocalDB; production installer targets SQL Express/full
- Node.js **22.22+**, **24.15+**, or **26+** (required by Angular CLI 22) for the Angular app
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

The web UI defaults to **Tamil**. Use the **தமிழ் / English** switcher in the navbar (or on the login screen) to change language; your choice is saved in the browser.

## Production / server install

**Prerequisite:** SQL Server Express or full SQL Server on the target host.

```powershell
.\scripts\build-installer.ps1
# Output: artifacts/installer/AganoxLms-Setup-1.0.0-x64.exe
```

The installer registers the **AganoxLmsApi** Windows Service, writes config to `ProgramData\Aganox\LMS`, and opens `http://localhost:8080`. See [Deployment Guide](docs/deployment/README.md).

## Development folder publish (console)

```powershell
.\scripts\publish-win-x64.ps1 -Configuration Release
# Output: artifacts/publish/win-x64/
# Run: artifacts/publish/win-x64/Start-AganoxLms.cmd
```

Uses LocalDB + console launcher for local testing only.

## Scripts

| Script | Purpose |
|--------|---------|
| `check-prerequisites.ps1` | Verify .NET, Node, LocalDB |
| `build.ps1` | Build backend + frontend |
| `test.ps1` | Run all .NET tests |
| `publish-server.ps1` | Self-contained server package (installer input) |
| `publish-win-x64.ps1` | Dev console publish (LocalDB + Start-AganoxLms.cmd) |
| `publish-win-x86.ps1` | Self-contained x86 dev publish |
| `build-installer.ps1` | Server publish + Inno Setup installer |
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

GitHub Actions workflow: `.github/workflows/ci.yml` (build, test, server publish smoke, installer build).

## Docker (optional dev)

```powershell
cd docker
docker compose up --build
```

## Solution layout

```
src/backend/          # .NET Clean Architecture API
src/frontend/         # Angular SPA
src/installer/        # Inno Setup script, tools, templates
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
