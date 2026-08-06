# Aganox LMS — Deployment Guide

**Version:** 1.0  
**Audience:** IT administrators deploying on Windows Server / Windows 10/11

---

## 1. Overview

Aganox LMS ships as a **self-contained** Windows x64 package. The installer:

- Installs the API + Angular UI under `Program Files\Aganox\LMS`
- Writes runtime config to `ProgramData\Aganox\LMS`
- Registers a **Windows Service** (`AganoxLmsApi`, automatic start)
- Configures SQL Server Express or full SQL Server (not LocalDB)

| Component | Requirement |
|-----------|-------------|
| OS | Windows 10/11 or Windows Server 2019+ (x64) |
| Runtime | Self-contained .NET (no separate runtime install) |
| Database | **SQL Server Express** (optional install via setup) or existing SQL Server |
| RAM | 4 GB recommended |
| Disk | ~500 MB install + database/logs |

---

## 2. Quick install (installer)

### Wizard behavior

Setup **detects existing SQL Server instances** in the registry:

| Machine state | Default wizard behavior |
|---------------|-------------------------|
| **No SQL instance found** | Offers to **install SQL Server 2022 Express** (`SQLEXPRESS`) — selected by default. Requires internet during setup to download engine files. |
| **Existing instance found** | Skips Express install; pre-fills the best detected instance (`.\SQLEXPRESS`, then default instance `.`, then first found). **Create AganoxLms database** is checked by default. |

### Run setup

1. Build or obtain `AganoxLms-Setup-1.0.0-x64.exe` from `artifacts/installer/`.
2. Run the installer **as Administrator**.
3. Follow the wizard:
   - **SQL Server setup** page (only when no instance exists): install Express or skip.
   - **Database and service** page: confirm instance, database name (`AganoxLms`), port (`8080`), service account (`LocalSystem`).
   - **Tasks**: create database (default on), firewall rule (default on).
4. Setup installs SQL Express if chosen, writes config, creates the Windows Service, runs migrations/seed, and verifies `/health`.
5. Open **`http://localhost:8080`** (or `http://<server-name>:8080` from other PCs on the LAN).
6. Log in with the seeded admin account and change the password when prompted.

> **Config location:** `C:\ProgramData\Aganox\LMS\appsettings.Production.json`  
> **Logs:** `C:\ProgramData\Aganox\LMS\logs\` and `{app}\logs\installer\`

### Silent install (SCCM / Intune)

```cmd
REM Clean machine — install Express + create database (defaults when no SQL found)
AganoxLms-Setup-1.0.0-x64.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /InstallSqlExpress=1

REM Existing SQL instance — use detected/custom server, create database
AganoxLms-Setup-1.0.0-x64.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART ^
  /InstallSqlExpress=0 /SqlServer=".\SQLEXPRESS" /SqlDatabase="AganoxLms" /Port=8080
```

Optional Inno tasks: `/TASKS=createdatabase,firewall`

Install logs:

- `%TEMP%\Setup Log*.txt` (Inno Setup session log)
- `{app}\logs\installer\` — `Setup.log`, `Install-SqlExpress.log`, `Configure-Server.log`, and `install-summary.txt`

Setup **aborts with a non-zero exit code** if SQL Express install, SQL connectivity, service install, or health check fails.

---

## 3. Manual / folder deploy

For advanced scenarios, publish the server package and configure manually:

```powershell
.\scripts\publish-server.ps1 -Configuration Release
```

Output: `artifacts/publish/server-x64/`

```powershell
cd artifacts\publish\server-x64
.\tools\Configure-Server.ps1 -InstallDir . -SqlServer ".\SQLEXPRESS" -SqlDatabase AganoxLms -Port 8080 -CreateDatabase
```

---

## 4. Development console publish (optional)

For local testing without the Windows Service:

```powershell
.\scripts\publish-win-x64.ps1 -Configuration Release
.\artifacts\publish\win-x64\Start-AganoxLms.cmd
```

Uses LocalDB-friendly defaults via `appsettings.Local.json` on first run. **Not used by the enterprise installer.**

---

## 5. Build from source

| Step | Command |
|------|---------|
| Prerequisites | `.\scripts\check-prerequisites.ps1` |
| Full build | `.\scripts\build.ps1 -Configuration Release` |
| Tests | `.\scripts\test.ps1 -Configuration Release` |
| Server publish | `.\scripts\publish-server.ps1` |
| Installer | `.\scripts\build-installer.ps1` (requires [Inno Setup 6](https://jrsoftware.org/isinfo.php)) |

---

## 6. Configuration

### Connection string

Production config is written to:

```
C:\ProgramData\Aganox\LMS\appsettings.Production.json
```

**SQL Server Express (default):**

```
Server=.\SQLEXPRESS;Database=AganoxLms;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

**LocalDB (development only):**

```
Server=(localdb)\mssqllocaldb;Database=AganoxLms;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

### JWT secret

Generated automatically during install into ProgramData config. To rotate manually, edit `Jwt:Key` (32+ characters) and restart the `AganoxLmsApi` service.

### URLs and service

The Windows Service listens on `http://+:<port>` (all interfaces). Default port `8080`. Change via reinstall or edit `Urls` in ProgramData config and restart the service.

For HTTPS, place IIS or a reverse proxy in front of the service (see [Security checklist](#10-security-checklist)).

---

## 7. Database backup

```powershell
.\scripts\backup-db.ps1 -OutputFolder "D:\Backups\AganoxLms"
```

In-app backup/restore: **Admin → Backup** (Administrator role).

---

## 8. Upgrades

1. Stop the service: `Stop-Service AganoxLmsApi`
2. Back up the database.
3. Run the new installer (same `AppId` — upgrades in place). ProgramData config is preserved.
4. Service restarts automatically; migrations run on startup.

---

## 9. Uninstall

- **Installer:** Settings → Apps → Aganox LMS → Uninstall (stops and removes the Windows Service).
- **Manual:** `Stop-Service AganoxLmsApi`, run `tools\Uninstall-Service.ps1`, delete the install folder.
- Database and `ProgramData\Aganox\LMS` are **not** removed automatically.

---

## 10. Troubleshooting

| Issue | Action |
|-------|--------|
| Setup fails on SQL | Verify SQL Express is running; test with SSMS using the same instance name |
| Service won't start | Check `C:\ProgramData\Aganox\LMS\logs\aganox-lms-*.log` |
| 500 on first request | Verify connection string and SQL permissions for the service account |
| Blank UI | Confirm `wwwroot\index.html` exists next to the exe |
| Health check | `GET http://localhost:8080/health` should return Healthy |
| LocalDB (dev only) | `.\scripts\repair-localdb.ps1` |

---

## 11. Security checklist

- [ ] Change default admin password immediately
- [ ] Use a dedicated SQL login/service account in production (instead of LocalSystem) when required by policy
- [ ] Use HTTPS via IIS or reverse proxy for remote access
- [ ] Restrict SQL Server network access
- [ ] Schedule backups (Admin settings or Task Scheduler)
- [ ] Review role permissions after install (Admin → Permissions)

See also [Future enhancements](../future-enhancements.md) for AD/SSO and cloud deployment options.
