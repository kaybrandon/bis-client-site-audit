# Logging

## What you get out of the box

The API uses the built-in ASP.NET Core logger:

- **Console** (visible in `dotnet run` and in IIS stdout when enabled)
- **Debug** (Visual Studio / `dotnet` debugger)

Levels live in `src/BisAudit.Api/appsettings.json`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
  }
}
```

Raise `Microsoft.EntityFrameworkCore.Database.Command` to `Information` if you need to see SQL while debugging.

Startup seed writes Information lines when the admin user or SAMPLE audits are created.

## IIS

`web.config` sets `stdoutLogEnabled="true"` and `stdoutLogFile=".\logs\stdout"`.

1. Create `C:\inetpub\bis-audit-api\logs` and grant the app-pool identity modify.  
2. Recycle the pool after a failed start and open the newest `stdout_*.log`.  
3. Turn stdout off (`false`) once the site is healthy if you do not want growing text files.

IIS failed-request tracing and the HTTPERR log are still useful for 502 / module issues.

## React client

The SPA logs to the browser console only. Failed API calls surface as on-page errors (`Invalid email or password`, upload failures, and so on). There is no client-side log file.

## What to look for

| Symptom | Where |
|---|---|
| Site will not start | `logs\stdout_*.log`, Event Viewer → Application |
| Login 401 | API console — wrong password vs JWT key mismatch after a recycle |
| Photos 404 | `Uploads:RootPath` vs the URL `/uploads/...` |
| CORS errors in the browser | `Cors:Origins` must include the SPA origin (separate-site deploy only) |
| Migration errors | Connection string, SQL permissions, stdout log |

## Optional later

You can plug in Serilog or Windows Event Log without changing controllers. Keep secrets (JWT key, SQL password) out of log output.
