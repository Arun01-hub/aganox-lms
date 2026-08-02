# Folder Structure — Aganox LMS

**Version:** 1.0

---

## 1. Repository Root

```text
Aganox/
├── docs/
│   ├── design/                 # Architecture, DB, API, roadmap (this package)
│   ├── deployment/             # (later) Deployment guide
│   ├── admin-guide/            # (later)
│   └── user-manual/            # (later)
├── plans/
│   └── plan-1.md
├── src/
│   ├── backend/
│   │   └── Aganox.Lms.sln
│   ├── frontend/
│   │   └── aganox-lms-web/     # Angular 18 workspace
│   └── installer/
│       └── Aganox.Lms.Installer/
├── tests/
│   ├── Aganox.Lms.Domain.Tests/
│   ├── Aganox.Lms.Application.Tests/
│   ├── Aganox.Lms.Api.IntegrationTests/
│   └── aganox-lms-web-e2e/     # optional Playwright/Cypress
├── docker/
│   ├── Dockerfile.api
│   ├── docker-compose.yml
│   └── .dockerignore
├── scripts/
│   ├── publish-win-x64.ps1
│   ├── publish-win-x86.ps1
│   └── backup-db.ps1
└── README.md
```

---

## 2. Backend Solution (`src/backend`)

```text
Aganox.Lms.sln
│
├── Aganox.Lms.Domain/
│   ├── Entities/
│   │   ├── Common/             # AuditableEntity, SoftDeletable
│   │   ├── Employees/
│   │   ├── Leave/
│   │   └── Organization/
│   ├── Enums/
│   ├── ValueObjects/
│   ├── DomainEvents/
│   ├── Services/               # LeaveDayCalculator, OverlapChecker, ...
│   └── Exceptions/
│
├── Aganox.Lms.Application/
│   ├── Common/
│   │   ├── Interfaces/         # IAppDbContext, ICurrentUser, IEmailSender, ...
│   │   ├── Models/             # PagedResult, Result<T>
│   │   ├── Behaviors/          # ValidationBehavior, LoggingBehavior
│   │   └── Mappings/
│   ├── Features/
│   │   ├── Auth/
│   │   ├── Employees/
│   │   ├── LeaveTypes/
│   │   ├── LeaveRequests/
│   │   ├── LeaveBalances/
│   │   ├── Approvals/
│   │   ├── Dashboards/
│   │   ├── Reports/
│   │   ├── Holidays/
│   │   └── Admin/
│   │       └── each: Commands/ Queries/ Dtos/ Validators/
│   └── DependencyInjection.cs
│
├── Aganox.Lms.Persistence/
│   ├── Context/                # AppDbContext
│   ├── Configurations/         # IEntityTypeConfiguration<>
│   ├── Migrations/
│   ├── Seed/
│   ├── Interceptors/           # Audit, soft delete
│   └── DependencyInjection.cs
│
├── Aganox.Lms.Infrastructure/
│   ├── Identity/
│   ├── Auth/                   # JWT, refresh
│   ├── Email/
│   ├── Export/                 # Excel, PDF, CSV
│   ├── Backup/
│   ├── Notifications/
│   └── DependencyInjection.cs
│
├── Aganox.Lms.Api/
│   ├── Controllers/v1/
│   ├── Middleware/
│   ├── Filters/
│   ├── Extensions/
│   ├── wwwroot/                # Angular publish output (prod)
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── appsettings.Production.json
│   └── Program.cs
│
└── Aganox.Lms.Installer/       # may live under src/installer
```

### Project references

```text
Api → Application, Infrastructure, Persistence
Infrastructure → Application, Persistence, Domain
Persistence → Domain, Application (for IAppDbContext only)
Application → Domain
Domain → (none)
```

---

## 3. Frontend (`src/frontend/aganox-lms-web`)

```text
aganox-lms-web/
├── angular.json
├── package.json
├── src/
│   ├── app/
│   │   ├── core/               # Auth guard, interceptors, API base, layout
│   │   ├── shared/             # Components, pipes, Bootstrap helpers
│   │   ├── features/
│   │   │   ├── auth/
│   │   │   ├── dashboard/
│   │   │   ├── employees/
│   │   │   ├── leave/
│   │   │   ├── approvals/
│   │   │   ├── reports/
│   │   │   ├── leave-types/
│   │   │   ├── holidays/
│   │   │   └── admin/
│   │   ├── app.routes.ts
│   │   └── app.config.ts
│   ├── assets/
│   ├── environments/
│   ├── styles/
│   │   ├── _variables.scss
│   │   └── styles.scss
│   └── index.html
└── README.md
```

**Module loading:** lazy-loaded feature routes; `core` is singleton.

---

## 4. Naming Conventions

| Area | Convention |
|------|------------|
| C# projects | `Aganox.Lms.*` |
| Features | Vertical slices under `Application/Features/{Name}` |
| Entities | Singular (`LeaveRequest`) |
| Tables | Plural (`LeaveRequests`) |
| Angular | Feature folders; `*.service.ts`, `*-list.component.ts` |
| API routes | `/api/v1/{resource}` kebab or plural nouns |

---

## 5. Configuration files of note

| File | Role |
|------|------|
| `appsettings.json` | Connection strings, JWT, SMTP, leave defaults |
| `angular environments` | `apiBaseUrl` |
| `docker-compose.yml` | Future cloud/dev parity |
| WiX `.wxs` | Install paths, service, shortcuts |

---

## Approval checkpoint

Approve structure (especially Persistence vs Infrastructure split) before scaffolding the solution.
