# Logging

The API uses **Serilog** on top of ASP.NET Core `ILogger`.

- **Console** — `dotnet run` window, and IIS stdout when enabled
- **Rolling file** — `logs/bisaudit-YYYYMMDD.log` (new file each day, keep 30 by default)
- **IIS stdout** — `logs/stdout_*.log` if the site fails before Serilog starts (500.30)

Folder is under the API content root (`src/BisAudit.Api/logs` in dev, `C:\inetpub\bis-audit-api\logs` after publish). Created on startup.

## Where to look

| Place | What |
|---|---|
| `logs\bisaudit-YYYYMMDD.log` | App + request log (login, errors, HTTP). **Start here.** |
| `logs\stdout_*.log` | ANCM / startup crash (IIS `web.config` `stdoutLogEnabled`) |
| Event Viewer → Application | Hosting Bundle / app-pool identity failures |
| Browser console | React-only messages (failed fetch). No SPA log file. |

HTTP requests are logged by Serilog request logging (`HTTP GET /api/audits responded 200 in 12ms`). Failed logins are `Warning`. Unhandled exceptions are `Error` with method + path.

## IIS ACL for `logs\`

The app-pool identity must be able to create and write files:

```powershell
icacls C:\inetpub\bis-audit-api\logs /grant "IIS AppPool\BisAudit:(OI)(CI)M"
```

If you skip this, the site may start then fail, or you only get stdout. Same Modify right is needed on `wwwroot\uploads` for photos.

Turn IIS stdout **off** (`stdoutLogEnabled="false"` in `web.config`) once startup is healthy if you do not want a second growing file. Keep the Serilog files.

## Detailed errors (staging only)

`Logging:DetailedErrors` = `true` turns on the developer exception page. Swagger UI is a separate admin Settings toggle (database). The first-run default is on in Development/Staging and off in Production.

```json
"Logging": {
  "DetailedErrors": true
}
```

Or environment variable `Logging__DetailedErrors=true`.

- **Development:** already on (`appsettings.Development.json`).
- **Staging:** set `true` while you chase a bug, then set `false`.
- **Production:** keep `false`. Clients get `{ "message": "An error occurred." }`. The real exception is in `logs\bisaudit-*.log`.

Do not leave detailed errors on a server users can reach.

## Log levels

| | Development | Production |
|---|---|---|
| Default | Debug / Information | Information |
| `Microsoft.AspNetCore` | Information | Warning |
| `Microsoft.EntityFrameworkCore.Database.Command` | Information (shows SQL) | Warning |
| `Logging:DetailedErrors` | true | false |
| File retain | 30 days | 30 days (`Logging:File:RetainedFileCountLimit`) |

Change `Serilog:MinimumLevel` in `appsettings.json` / `appsettings.Production.json`. Do not log `Jwt:Key`, SQL passwords, or photo bytes.

## What to attach when filing a bug

1. Git commit or published version (`BisAudit.Api.dll` date)
2. `ASPNETCORE_ENVIRONMENT` (Development / Staging / Production)
3. Request: method + path (example `POST /api/audits/{id}/issues`)
4. Time (local + UTC) of the failure
5. Relevant lines from `logs/bisaudit-YYYYMMDD.log` (exception stack)
6. If it never started: `logs/stdout_*.log` and Event Viewer
7. Connection **shape** only (`Server=…;Database=BisAudit;Trusted_Connection=…`) — **not** the password
8. Browser + a screenshot if the UI is wrong

## Quick checks

| Symptom | File |
|---|---|
| Pool won’t start | `stdout_*.log`, Event Viewer |
| Login 401 | `bisaudit-*.log` — “Failed login” vs JWT key change |
| 500 after it was working | `bisaudit-*.log` — search the request path |
| Photos 404 | uploads ACL + `Uploads:RootPath` |
| CORS | layout B origin vs `Cors:Origins` |
