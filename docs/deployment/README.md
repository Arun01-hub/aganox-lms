# Aganox LMS — Deployment Guide

**Version:** 1.0  
**Audience:** IT administrators deploying on Windows 10/11

---

## 1. Overview

Aganox LMS ships as a **self-contained** Windows package (no separate .NET runtime install required). The API hosts both the REST API and the Angular UI from `wwwroot`.

| Component | Requirement |
|-----------|-------------|
| OS | Windows 10/11 (x64 or x86) |
| Runtime | Visual C++ 2015–2022 Redistributable (x64) — installed automatically by setup |
| Database | SQL Server LocalDB, Express, or full SQL Server |
| RAM | 2 GB minimum (4 GB recommended) |
| Disk | ~500 MB install + database/logs |

---

## 2. Quick install (installer)

1. Build or obtain `AganoxLms-Setup-1.0.0-x64.exe` from `artifacts/installer/` (see [Build from source](#4-build-from-source)).
2. Run the installer (administrator rights required).
3. Launch **Aganox LMS** from the Start menu or desktop shortcut.
4. Browser opens **`http://localhost:8080`** (HTTP — no TLS certificate setup required for local SME installs).
5. Log in with the seeded admin account and change the password when prompted.

> **Note:** The installer automatically installs **Visual C++ Redistributable (x64)** and **SQL Server LocalDB** when missing. VC++ is installed before LocalDB.

> **Note:** The launcher auto-creates `appsettings.Local.json` with a secure JWT key on first run. Keep the **Start Aganox LMS** console window open while using the app.

### Silent install

```cmd
AganoxLms-Setup-1.0.0-x64.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
```

Install logs:

- `%TEMP%\Setup Log*.txt` (Inno Setup session log)
- `{app}\logs\installer\` — persisted copies including `Setup.log`, `VcRedist-Install.log`, `SqlLocalDB-Install.log`, and `install-summary.txt` (written even when prerequisite install fails)

---

## 3. Manual / folder deploy

```powershell
# From repo root
.\scripts\publish-win-x64.ps1 -Configuration Release
```

Output: `artifacts/publish/win-x64/`

1. Copy the folder to the target machine (e.g. `C:\Program Files\Aganox\LMS`).
2. Configure `appsettings.Production.json` (see [Configuration](#5-configuration)).
3. Set environment variables (recommended for secrets):

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:Jwt__Key = "<64-char-random-secret>"
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\mssqllocaldb;Database=AganoxLms;Trusted_Connection=True;TrustServerCertificate=True"
```

4. Run `Start-AganoxLms.cmd` or `Aganox.Lms.Api.exe`.

On first start the application **applies EF migrations** and **seeds** roles, leave types, calendar, and the default admin user.

---

## 4. Build from source

| Step | Command |
|------|---------|
| Prerequisites | `.\scripts\check-prerequisites.ps1` |
| Full build | `.\scripts\build.ps1 -Configuration Release` |
| Tests | `.\scripts\test.ps1 -Configuration Release` |
| Publish x64 | `.\scripts\publish-win-x64.ps1` |
| Publish x86 | `.\scripts\publish-win-x86.ps1` |
| Installer | `.\scripts\build-installer.ps1` (requires [Inno Setup 6](https://jrsoftware.org/isinfo.php)) |

---

## 5. Configuration

### Connection string

Edit `appsettings.Production.json` or use environment variable `ConnectionStrings__DefaultConnection`.

**LocalDB (default SME):**

```
Server=(localdb)\mssqllocaldb;Database=AganoxLms;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

**SQL Server Express (named instance):**

```
Server=.\SQLEXPRESS;Database=AganoxLms;Trusted_Connection=True;TrustServerCertificate=True
```

### JWT secret (required in Production)

Production **will not start** without a secure `Jwt:Key` (32+ characters, not a dev placeholder).

Set via environment variable:

```
Jwt__Key=<your-long-random-secret>
```

Generate one:

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

### URLs

Default in `Start-AganoxLms.cmd` / `Start-AganoxLms.ps1`:

```
http://localhost:8080
```

The launcher waits for `/health` to respond before opening the browser.

For HTTPS (optional, e.g. behind IIS or with a proper certificate):

```
ASPNETCORE_URLS=https://localhost:8443;http://localhost:8080
Jwt__Key=<your-secret>   # or use appsettings.Local.json
dotnet dev-certs https --trust
```

---

## 6. Database backup (script)

```powershell
.\scripts\backup-db.ps1 -OutputFolder "D:\Backups\AganoxLms"
```

In-app backup/restore is also available under **Admin → Backup** (Administrator role).

---

## 7. Upgrades

1. Stop the running application.
2. Back up the database (`backup-db.ps1` or Admin UI).
3. Run the new installer (same `AppId` — upgrades in place) or replace the publish folder.
4. Start the application — migrations run automatically on startup.

---

## 8. Uninstall

- **Installer:** Settings → Apps → Aganox LMS → Uninstall, or Start menu → Uninstall Aganox LMS.
- **Manual:** Stop the process, delete the install folder. The database is **not** removed automatically — drop it in SQL Server if no longer needed.

---

## 9. Troubleshooting

| Issue | Action |
|-------|--------|
| LocalDB missing/corrupt | `.\scripts\repair-localdb.ps1` |
| Production startup: JWT error | Set `Jwt__Key` (32+ chars) |
| 500 on first request | Check `logs/aganox-lms-*.log`; verify SQL Server is running |
| Blank UI | Confirm `wwwroot/index.html` exists next to the exe |
| Health check | `GET /health` should return Healthy |

---

## 10. Security checklist

- [ ] Change default admin password immediately
- [ ] Set a unique `Jwt__Key` per environment
- [ ] Use HTTPS in production (IIS or reverse proxy)
- [ ] Restrict SQL Server access to the service account
- [ ] Schedule backups (Admin settings or `backup-db.ps1` + Task Scheduler)
- [ ] Review role permissions after install (Admin → Permissions)

See also [Future enhancements](../future-enhancements.md) for AD/SSO and cloud deployment options.
