# Project Roadmap — Aganox LMS

**Version:** 1.0  
**Approach:** Design approval → module-by-module implementation (per plan-1)

---

## Phase 0 — Design (THIS DELIVERY)

| # | Deliverable | Doc |
|---|-------------|-----|
| 1 | Solution Architecture | [01-solution-architecture.md](./01-solution-architecture.md) |
| 2 | Database Design | [02-database-design.md](./02-database-design.md) |
| 3 | Folder Structure | [03-folder-structure.md](./03-folder-structure.md) |
| 4 | UI Wireframes | [04-ui-wireframes.md](./04-ui-wireframes.md) |
| 5 | ER Diagram | [05-er-diagram.md](./05-er-diagram.md) |
| 6 | API Design | [06-api-design.md](./06-api-design.md) |
| 7 | Project Roadmap | this file |

**Gate:** Stakeholder approval before scaffolding code.

---

## Phase 1 — Foundation (Sprint 1)

**Goal:** Runnable API + empty Angular shell + DB migrate/seed.

- [ ] Create `Aganox.Lms.sln` and Clean Architecture projects
- [ ] Auditable/soft-delete base entities
- [ ] `AppDbContext`, Identity integration, initial migration
- [ ] Seed roles, permissions, leave types, admin user
- [ ] Serilog, exception middleware, Swagger, health, API versioning
- [ ] JWT login / refresh / me
- [ ] Angular workspace, auth module, app shell layout
- [ ] Docker Compose optional for dev SQL

**Exit:** Login works; Swagger lists auth endpoints; DB seeded.

---

## Phase 2 — Organization & Employees (Sprint 2)

- [ ] Departments CRUD
- [ ] Employees CRUD + search/filter/page/sort
- [ ] Manager hierarchy
- [ ] Employee profile UI
- [ ] Link employee ↔ user account
- [ ] Unit tests for validators; integration tests for employee API

**Exit:** HR can manage employee master data end-to-end.

---

## Phase 3 — Leave configuration (Sprint 3)

- [ ] Leave types CRUD (UI configurable)
- [ ] Leave policies (weekends/holidays/CF/reset)
- [ ] Calendars & holidays
- [ ] Opening balances on employee
- [ ] `LeaveDayCalculator` domain service + tests

**Exit:** Admin configures types/holidays; day preview API accurate.

---

## Phase 4 — Leave requests & balances (Sprint 4)

- [ ] Apply / edit / cancel leave
- [ ] Overlap detection, validation, balance guard
- [ ] Balance ledger + adjustments (HR)
- [ ] Employee leave list & detail UI
- [ ] Domain + application tests for edge cases (half-day, holidays)

**Exit:** Employee can submit valid leave; invalid cases blocked with clear errors.

---

## Phase 5 — Approval workflow & notifications (Sprint 5)

- [ ] Manager → HR steps engine
- [ ] Approvals inbox UI
- [ ] Comments + approval history
- [ ] Re-approval after edit
- [ ] Email notifications (submit/approve/reject/cancel) + outbox
- [ ] Pending reminder job (hosted service)

**Exit:** Full happy-path approval with emails (SMTP test harness).

---

## Phase 6 — Dashboards & reports (Sprint 6)

- [ ] Employee / Manager / Admin dashboards
- [ ] Team calendar (manager)
- [ ] Reports listed in plan + Excel/PDF/CSV export
- [ ] Basic charts (department stats, trends)

**Exit:** Role dashboards usable; exports download correctly.

---

## Phase 7 — Admin, audit, backup (Sprint 7)

- [ ] Permissions matrix UI
- [ ] Audit/activity viewers
- [ ] Database backup/restore + scheduled backup option
- [ ] System settings
- [ ] Forgot/reset password polished

**Exit:** Ops-ready admin tooling for SME deploy.

---

## Phase 8 — Hardening, installer, docs (Sprint 8)

- [ ] Security review (authz, injection, secrets)
- [ ] Performance pass on list endpoints
- [ ] Self-contained publish scripts (x64/x86)
- [ ] WiX/Inno installer: prereqs, migrate/seed, shortcuts, uninstaller, silent mode
- [ ] Deployment guide, admin guide, user manual
- [ ] Future enhancements doc (AD/SSO, multi-tenant, K8s)
- [ ] Integration + E2E smoke tests in CI

**Exit:** One-click install on clean Windows 10/11 VM; manuals complete.

---

## Suggested timeline (indicative)

| Phase | Duration (1–2 devs) |
|-------|---------------------|
| 0 Design | 2–3 days |
| 1 Foundation | 1–1.5 weeks |
| 2 Employees | 1 week |
| 3 Leave config | 1 week |
| 4 Leave + balances | 1.5–2 weeks |
| 5 Workflow + email | 1.5 weeks |
| 6 Dashboards/reports | 1.5 weeks |
| 7 Admin/audit/backup | 1 week |
| 8 Installer/docs/harden | 1.5–2 weeks |
| **Total** | **~10–12 weeks** |

---

## Implementation rules (from plan-1)

1. Do **not** generate the entire codebase in one shot.  
2. After Phase 0 approval, implement **one phase at a time** at production quality.  
3. Each phase ends with: compiling solution, migrations applied, smoke test notes.  
4. Prefer complete vertical slices (API + UI + tests) over backend-only dumps.

---

## Next action after approval

Reply with **“Approve design”** (and any deltas). Implementation will start at **Phase 1 — Foundation**: solution scaffolding, Identity/JWT, migrations/seed, and Angular auth shell.
