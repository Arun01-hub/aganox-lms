# Aganox LMS — Administrator Guide

**Version:** 1.0  
**Audience:** HR administrators and IT staff managing the system

---

## 1. First login

| Field | Default (seeded) |
|-------|------------------|
| Email | `admin@aganox.local` |
| Password | `ChangeMe!123` |

You will be prompted to **change the password** on first login. Choose a strong password (8+ chars, upper, lower, digit, symbol).

---

## 2. Roles and permissions

| Role | Typical use |
|------|-------------|
| **Administrator** | Full access; permissions cannot be edited |
| **HR** | Employees, leave config, approvals, reports, audit |
| **Manager** | Team leave, approvals, team reports |
| **Employee** | Apply/view own leave, view balances |

Manage permissions: **Admin → Permissions** (requires `Users.Manage`).

Permissions are enforced on every API call via JWT claims — changes apply on the user’s next login.

---

## 3. Organization setup

### Departments

**Organization → Departments** — create departments used when adding employees.

### Employees

**Organization → Employees** — add staff with employee code, department, manager, and optional user account.

- Linking a **user account** allows the employee to log in.
- **Manager hierarchy** drives approval routing (manager step before HR when configured).

### Leave configuration

| Area | Purpose |
|------|---------|
| **Leave types** | EL, SL, comp-off, etc. — paid/unpaid, half-day, approval required |
| **Policies** | Weekend/holiday rules, carry-forward, yearly reset, opening balance |
| **Holidays** | Calendar holidays excluded from leave day calculation |

Set **opening balances** per employee/year under leave balances or when creating policies.

---

## 4. Approval workflow

Default flow for leave types with `Requires approval`:

1. Employee submits leave request
2. **Manager** approves/rejects (if assigned)
3. **HR** final approval — balance deducted on final approval

**Approvals → Inbox** — pending items for your role.  
**Approvals → History** — completed actions.

Email notifications are queued to the outbox and dispatched by a background service (logged to console/file in dev if SMTP is not configured).

---

## 5. Reports and dashboards

| Dashboard | Role |
|-----------|------|
| Employee | Own balances and requests |
| Manager | Team on leave, pending team approvals |
| Admin | Headcount, department stats, trends |

**Reports** — export CSV, Excel, or PDF for leave summaries, balances, attendance-style views, and trends.

---

## 6. Admin console

Open **Admin** (Administrator / users with admin permissions).

### Settings

System keys (examples):

| Key | Purpose |
|-----|---------|
| `Organization.Name` | Display name |
| `Backup.FolderPath` | Folder for `.bak` files |
| `Backup.ScheduleEnabled` | `true`/`false` for nightly backup job |
| `Backup.ScheduleCron` | Cron expression (default `0 2 * * *`) |
| `Notifications.PendingReminderDays` | Days before pending approval reminder |

### Permissions matrix

Select a role and toggle permission checkboxes. **Administrator** is locked.

### Audit & activity

- **Audit logs** — automatic record of entity create/update/delete
- **Activity logs** — explicit admin actions (settings changes, backups, etc.)

### Backup & restore

- **Create backup** — SQL Server `.bak` file in configured folder
- **Restore** — **destructive**; replaces the entire database from a `.bak` path on the server

> Restore and scheduled backup require SQL Server (not in-memory DB).

---

## 7. User account management

### Forgot password

Users use **Forgot password** on the login page. In development, reset links/tokens appear in API logs (`LoggingEmailSender`). Configure SMTP in production (future: `Email` section in appsettings).

### Password policy

Enforced by ASP.NET Identity: 8+ characters, upper, lower, digit, non-alphanumeric.

---

## 8. Operational tasks

| Task | How |
|------|-----|
| Daily health | Browse to `/health` or monitor logs in `logs/` |
| Manual DB backup | Admin → Backup or `scripts/backup-db.ps1` |
| Scheduled backup | Enable in Admin → Settings |
| Add new leave year policies | Leave config → Policies (per type/year) |
| Year-end carry-forward | Configure in leave policy `CarryForwardMaxDays` |

---

## 9. Support contacts

Document your internal HR/IT escalation path here after deployment.

For technical deployment issues, see [Deployment Guide](../deployment/README.md).
