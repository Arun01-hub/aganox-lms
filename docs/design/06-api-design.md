# API Design — Aganox LMS

**Version:** 1.0  
**Base URL:** `/api/v1`  
**Style:** REST + JSON  
**Auth:** Bearer JWT  
**Errors:** `application/problem+json`  
**Docs:** Swagger / OpenAPI 3

---

## 1. Conventions

| Topic | Rule |
|-------|------|
| Versioning | URL segment `/api/v1` |
| Paging | `page` (1-based), `pageSize` (default 10, max 100) |
| Sorting | `sortBy`, `sortDir=asc\|desc` |
| Filtering | Query params per resource |
| IDs | UUID (`Guid`) |
| Dates | ISO-8601 date `YYYY-MM-DD`; date-times UTC `Z` |
| Idempotency | Optional `Idempotency-Key` on create leave submit |

### Envelope (lists)

```json
{
  "items": [],
  "page": 1,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5
}
```

### ProblemDetails (errors)

```json
{
  "type": "https://aganox.local/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": { "fromDate": ["Must be on or before toDate."] }
}
```

---

## 2. Auth

| Method | Path | Roles | Description |
|--------|------|-------|-------------|
| POST | `/auth/login` | Anon | Email/password → access + refresh |
| POST | `/auth/refresh` | Anon | Rotate refresh token |
| POST | `/auth/logout` | Auth | Revoke refresh |
| POST | `/auth/forgot-password` | Anon | Send reset email |
| POST | `/auth/reset-password` | Anon | Token + new password |
| POST | `/auth/change-password` | Auth | Current + new |
| GET | `/auth/me` | Auth | Profile, roles, permissions |

### Login response (shape)

```json
{
  "accessToken": "...",
  "expiresIn": 3600,
  "refreshToken": "...",
  "user": {
    "id": "...",
    "email": "...",
    "employeeId": "...",
    "roles": ["Employee"],
    "permissions": ["Leave.Apply", "Leave.ViewOwn"]
  }
}
```

---

## 3. Employees

| Method | Path | Permissions |
|--------|------|-------------|
| GET | `/employees` | `Employees.View` |
| GET | `/employees/{id}` | `Employees.View` |
| POST | `/employees` | `Employees.Create` |
| PUT | `/employees/{id}` | `Employees.Update` |
| DELETE | `/employees/{id}` | `Employees.Delete` (soft) |
| GET | `/employees/{id}/profile` | `Employees.View` / own |

**Query:** `search`, `departmentId`, `status`, `managerId`, `sortBy`, `sortDir`, `page`, `pageSize`

### EmployeeDto (summary)

```json
{
  "id": "uuid",
  "employeeCode": "E001",
  "fullName": "A. Kumar",
  "departmentId": "uuid",
  "departmentName": "Engineering",
  "designation": "Engineer",
  "dateOfJoining": "2024-01-15",
  "status": "Active",
  "email": "a.kumar@company.com",
  "phone": "+91...",
  "managerId": "uuid",
  "managerName": "R. Shah"
}
```

---

## 4. Departments / Holidays / Leave types

| Method | Path | Notes |
|--------|------|-------|
| CRUD | `/departments` | Admin/HR |
| CRUD | `/leave-types` | Configurable types |
| GET/PUT | `/leave-policies` | Filter `leaveTypeId`, `year` |
| CRUD | `/leave-calendars` | |
| CRUD | `/holidays` | Filter `calendarId`, `year` |

---

## 5. Leave requests

| Method | Path | Permissions |
|--------|------|-------------|
| GET | `/leave-requests` | View own / team / all by role |
| GET | `/leave-requests/{id}` | |
| POST | `/leave-requests` | `Leave.Apply` — create draft or submit |
| PUT | `/leave-requests/{id}` | Edit draft/pending (policy) |
| POST | `/leave-requests/{id}/submit` | Draft → Pending |
| POST | `/leave-requests/{id}/cancel` | |
| POST | `/leave-requests/preview-days` | Calc days without saving |

### Create / update body

```json
{
  "leaveTypeId": "uuid",
  "fromDate": "2026-08-10",
  "toDate": "2026-08-12",
  "isHalfDay": false,
  "halfDayPeriod": null,
  "remarks": "Family function",
  "submit": true
}
```

### Preview-days response

```json
{
  "numberOfDays": 3.0,
  "skippedWeekends": 0,
  "skippedHolidays": 0,
  "breakdown": [
    { "date": "2026-08-10", "counted": true, "reason": null }
  ]
}
```

---

## 6. Approvals

| Method | Path | Permissions |
|--------|------|-------------|
| GET | `/approvals/pending` | `Approvals.Act` |
| GET | `/approvals/history` | |
| POST | `/approvals/{leaveRequestId}/approve` | Body: `{ "comments": "..." }` |
| POST | `/approvals/{leaveRequestId}/reject` | Body: `{ "comments": "..." }` required |

---

## 7. Leave balances

| Method | Path | Permissions |
|--------|------|-------------|
| GET | `/leave-balances` | Own or `Balances.View` |
| GET | `/leave-balances/employee/{employeeId}` | HR/Manager scoped |
| POST | `/leave-balances/adjustments` | `Balances.Adjust` |
| POST | `/leave-balances/yearly-reset` | `Balances.Admin` |
| POST | `/leave-balances/carry-forward` | `Balances.Admin` |

### Adjustment body

```json
{
  "employeeId": "uuid",
  "leaveTypeId": "uuid",
  "year": 2026,
  "deltaDays": 1.5,
  "reason": "HR correction — missed accrual"
}
```

---

## 8. Dashboards

| Method | Path | Roles |
|--------|------|-------|
| GET | `/dashboards/employee` | Employee+ |
| GET | `/dashboards/manager` | Manager+ |
| GET | `/dashboards/admin` | HR/Admin |

Admin payload includes: totals, on-leave-today, pending counts, department stats, trend series.

---

## 9. Reports

| Method | Path | Query |
|--------|------|-------|
| GET | `/reports/employee-leave` | employeeId, from, to |
| GET | `/reports/monthly` | year, month, departmentId? |
| GET | `/reports/yearly` | year, departmentId? |
| GET | `/reports/department` | departmentId, from, to |
| GET | `/reports/balances` | year, departmentId? |
| GET | `/reports/attendance-summary` | from, to |
| GET | `/reports/trends` | from, to |
| GET | `/reports/{name}/export` | `format=xlsx\|pdf\|csv` |

---

## 10. Notifications & admin

| Method | Path | Notes |
|--------|------|-------|
| GET | `/notifications` | In-app list |
| POST | `/notifications/{id}/read` | |
| GET/PUT | `/admin/settings` | |
| GET | `/admin/permissions` | Matrix |
| PUT | `/admin/roles/{roleId}/permissions` | |
| POST | `/admin/backup` | |
| POST | `/admin/restore` | Upload / path (secured) |
| GET | `/admin/audit-logs` | Filter entity, user, date |
| GET | `/health` | Anon/limited |

---

## 11. Permission catalog (initial)

| Permission | Description |
|------------|-------------|
| `Employees.View` | List/view employees |
| `Employees.Create` | Create |
| `Employees.Update` | Update |
| `Employees.Delete` | Soft delete |
| `Leave.Apply` | Create/submit own leave |
| `Leave.ViewOwn` | Own requests |
| `Leave.ViewTeam` | Direct reports |
| `Leave.ViewAll` | All org |
| `Approvals.Act` | Approve/reject |
| `Balances.View` | View balances |
| `Balances.Adjust` | Manual adjust |
| `Balances.Admin` | Yearly reset / CF |
| `LeaveTypes.Manage` | CRUD types/policies |
| `Holidays.Manage` | CRUD holidays |
| `Reports.View` | Run reports |
| `Reports.Export` | Export files |
| `Admin.Settings` | System settings |
| `Admin.Backup` | Backup/restore |
| `Admin.Audit` | Audit viewer |
| `Users.Manage` | Accounts & roles |

**Default role maps**

| Role | Permissions (summary) |
|------|------------------------|
| Employee | Leave.*Own/Apply, Balances.View (own via API scope), Reports limited |
| Manager | Employee + Leave.ViewTeam + Approvals.Act |
| HR | Broad employees, balances adjust, leave types, holidays, reports, approvals |
| Administrator | All |

---

## 12. Cross-cutting API behaviors

- Authorization via policy `Permission:Xxx`
- FluentValidation → 400 ProblemDetails
- Domain conflicts (overlap, balance) → 409 Conflict
- Not found → 404
- Concurrency (`RowVersion`) → 409
- Serilog request logging with correlation id header `X-Correlation-ID`

---

## Approval checkpoint

Approve endpoint surface and permission model before controller implementation.
