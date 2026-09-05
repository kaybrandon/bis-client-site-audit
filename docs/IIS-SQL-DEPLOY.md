# IIS + SQL Server deploy (Dell / Windows Server)

Two layouts, same bits: publish the API, build the React app.

Traffic is ordinary **HTTP/HTTPS** (JSON + multipart photos). No SignalR, no sticky WebSocket. Field techs use tablet/laptop browsers; the SPA is static files.

| Layout | When |
|---|---|
| **A — one IIS site** | API serves the React files from `wwwroot`. Easiest. No CORS. |
| **B — two IIS sites** | Website on one hostname, API on another. |

## 1. Server prep

- [ ] Windows Server 2019/2022
- [ ] IIS: Web Server role (Common HTTP Features, Management Tools)
- [ ] [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) (ASP.NET Core Module V2)
- [ ] SQL Server Express or Standard (local or nearby)
- [ ] URL Rewrite module — **only** for layout B

## 2. SQL Server

```sql
CREATE DATABASE BisAudit;
GO
CREATE LOGIN [bis_audit] WITH PASSWORD = 'PickALongPassword!';
GO
USE BisAudit;
CREATE USER [bis_audit] FOR LOGIN [bis_audit];
ALTER ROLE db_owner ADD MEMBER [bis_audit];
GO
```

`db_owner` is simple for Phase 1 (migrate + seed + CRUD). Tighten later if your DBA requires it.

**Connection string** (SQL login):

```
Server=SQLHOST;Database=BisAudit;User Id=bis_audit;Password=PickALongPassword!;TrustServerCertificate=True;MultipleActiveResultSets=true
```

**Windows / gMSA** (app-pool identity already has a SQL login):

```
Server=SQLHOST;Database=BisAudit;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Firewall: allow **TCP 1433** from the IIS box to SQL if they are not the same machine. On SQL Configuration Manager, enable TCP/IP.

## 3. Publish

On a build PC (or the server if the SDK is installed):

```powershell
dotnet publish src/BisAudit.Api -c Release -o C:\inetpub\bis-audit-api
cd client
npm ci
npm run build
```

**Layout A** — copy SPA into the API site:

```powershell
Copy-Item -Recurse -Force client\dist\* C:\inetpub\bis-audit-api\wwwroot\
```

**Layout B** — copy SPA to its own folder:

```powershell
$env:VITE_API_URL="https://audit-api.yourfirm.local"
npm run build
Copy-Item -Recurse -Force client\dist\* C:\inetpub\bis-audit-web\
```

## 4. IIS site + app pool

**API (and layout A website)**

| Setting | Value |
|---|---|
| App pool | New pool, e.g. `BisAudit` |
| .NET CLR version | **No Managed Code** (ASP.NET Core, not Framework) |
| Pipeline | Integrated |
| Identity | `ApplicationPoolIdentity` (or a domain service account) |
| Site physical path | `C:\inetpub\bis-audit-api` |
| Binding | HTTPS 443 + hostname (see below) |

**Permissions** (app-pool identity = `IIS AppPool\BisAudit`):

```powershell
icacls C:\inetpub\bis-audit-api\logs /grant "IIS AppPool\BisAudit:(OI)(CI)M"
icacls C:\inetpub\bis-audit-api\wwwroot\uploads /grant "IIS AppPool\BisAudit:(OI)(CI)M"
```

If `Uploads:RootPath` is `D:\BisAudit\uploads`, grant Modify there instead.

**Layout B SPA site:** path `C:\inetpub\bis-audit-web`, any app pool (static files). Drop this `web.config` in that folder (needs URL Rewrite):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="SPA" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

Add the SPA origin to API `Cors:Origins`.

## 5. Production config

Set via `appsettings.Production.json`, IIS Configuration Editor, or environment variables on the app pool:

| Variable | Example |
|---|---|
| `ConnectionStrings__DefaultConnection` | string from §2 |
| `Jwt__Key` | long random secret (≥ 32 chars) |
| `Seed__LoadSampleAudit` | `false` |
| `Logging__DetailedErrors` | `false` |
| `Swagger__Enabled` | omit or `false` (UI is off in Production). Set `true` only if you intentionally need `/swagger` on that host |
| `Cors__Origins__0` | `https://audit.yourfirm.local` (layout B only) |
| `Uploads__RootPath` | `wwwroot/uploads` or `D:\BisAudit\uploads` |
| `ASPNETCORE_ENVIRONMENT` | `Production` (already in `web.config`) |

`web.config` hosts **in-process** and writes IIS stdout to `.\logs\stdout`. App rolling logs go to `.\logs\bisaudit-YYYYMMDD.log`.

## 6. Apply EF migrations on the server

**Option 1 — first request.** The API calls `Database.Migrate()` at startup if the app-pool identity can create/alter tables.

**Option 2 — CLI** (SDK on a jump box, or copy the published folder):

```powershell
$env:ConnectionStrings__DefaultConnection="Server=SQLHOST;Database=BisAudit;User Id=bis_audit;Password=PickALongPassword!;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet ef database update --project src/BisAudit.Api
```

Recycle the app pool after config or migration changes.

## 7. HTTPS / bindings

1. IIS → site → **Bindings** → Add → https, port 443, hostname `audit.yourfirm.local`, pick a cert (internal CA or Let's Encrypt).
2. Optional: HTTP 80 binding that redirects to HTTPS (IIS URL Rewrite, or keep `UseHttpsRedirection` in Production).
3. Add the hostname to DNS (or hosts file for a first test).

## 8. Common failures

| Symptom | Likely cause | Fix |
|---|---|---|
| **HTTP 500.30** (in-process start failure) | Bad connection string, missing `Jwt:Key`, crash in seed/migrate | Open `C:\inetpub\bis-audit-api\logs\stdout_*.log` and `logs\bisaudit-*.log`. Fix config, recycle pool. |
| **500.19** | Broken `web.config` / missing Hosting Bundle | Reinstall Hosting Bundle, confirm ANCM in Modules. |
| **502.5** | Old hosting model / wrong `processPath` | Keep `hostingModel="inprocess"` and `arguments=".\BisAudit.Api.dll"`. |
| Login always 401 | JWT key changed after tokens were issued, or clock skew | Recycle after changing `Jwt:Key`; users sign in again. |
| SQL timeout / login failed | Wrong server name, TCP off, firewall, bad user | Test with SSMS from the IIS box. Enable TCP/IP. Open 1433. |
| Photos fail / 404 | No write ACL on uploads, or `RootPath` mismatch | `icacls` on the uploads folder; URL is `/uploads/...`. |
| CORS error in browser | Layout B and origin not listed | Add the exact SPA origin (`https://…`, no trailing slash). |
| Blank page on refresh (layout B) | No URL Rewrite / no SPA `web.config` | Install rewrite module; add the file in §4. |
| App starts then dies | Identity cannot write `logs\` | Grant Modify on `logs` to `IIS AppPool\BisAudit`. |

Event Viewer → Windows Logs → Application also shows ANCM / CLR startup errors.

## Auth later (not on in Phase 1)

`Program.cs` comments show Windows Authentication / IIS and Entra ID. Phase 1 is JWT from `POST /api/auth/login`.

See [LOGGING.md](LOGGING.md) for log paths and staging `DetailedErrors`.
