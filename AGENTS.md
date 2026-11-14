# Aganox LMS — agent guide

**Catalog**: Leave management (+ payroll direction) · **Workflow**: `lms-workflow.md`

## Purpose

HR leave management: ASP.NET Core API, Angular SPA, Windows installer/publish scripts, optional OIDC to Aganox Identity.

## Stack

- .NET 10, EF Core + SQL Server/LocalDB, Angular (`src/frontend/aganox-lms-web`), Inno Setup installer.

## Layout

```
src/backend/Aganox.Lms.*     # Clean Architecture
src/frontend/aganox-lms-web/
tests/Aganox.Lms.*.Tests/
docs/design/                 # Architecture reference
scripts/                     # build, run-api, run-web, test, publish-*
```

## Commands

| Task | Command |
|------|---------|
| Prereqs | `.\scripts\check-prerequisites.ps1` |
| Build | `.\scripts\build.ps1` |
| API | `.\scripts\run-api.ps1` |
| Web | `.\scripts\run-web.ps1` |
| Test | `.\scripts\test.ps1` |
| Installer | `.\scripts\build-installer.ps1` |

## Configuration

| File | Notes |
|------|-------|
| `src/backend/Aganox.Lms.Api/appsettings.*.json` | Shared defaults |
| `appsettings.Development.local.json` | Gitignored — see `*.example` |
| `Oidc` section | Enable when integrating Identity (`Authority` `https://localhost:7036`) |

## Dependencies

- **Aganox-Identity** when `Oidc:Enabled` is true.
- Default admin seed: `admin@aganox.local` / `ChangeMe!123`.

## Agent notes

- UI i18n: Tamil + English JSON under `public/assets/i18n/`.
- Do not commit `artifacts/`, `SQLEXPR_*`, or `wwwroot/` SPA publish output.