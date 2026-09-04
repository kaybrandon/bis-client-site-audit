# Setup (Windows / Microsoft shop)

BIS Client IT Audit is an on-prem **infrastructure assurance** app: a React website plus an ASP.NET Core API that stores client IT audits in SQL Server (workstations, network, issues, purchases, photos, printable reports). No Base44. No cloud required.

## Environment

| Item | Dev (Windows 10/11) | Server (Windows Server) |
|---|---|---|
| OS | Windows 10/11 64-bit | Windows Server 2019/2022 on Dell |
| .NET | **SDK 8.0** | **Hosting Bundle 8.0** (runtime + IIS module) |
| SQL Server | Express / Developer / Standard | Express or Standard (same box or nearby) |
| Node.js | **20+** (for the React build) | Not needed after you copy `client/dist` |
| IIS | Optional (use `dotnet run` + Vite) | Required — see [IIS-SQL-DEPLOY.md](IIS-SQL-DEPLOY.md) |
| Docker | Optional (`docker compose` for SQL) | Optional |

SQL notes: **Express** is fine for a shop with a handful of engagements. Use **Standard** if you want Agent jobs, more memory, or a dedicated SQL box. LocalDB works on a laptop; on a server use a real SQL instance.

IIS roles (server only): Web Server (IIS) → Common HTTP, ASP.NET, Management Tools. The **.NET 8 Hosting Bundle** adds ASP.NET Core Module V2. URL Rewrite is needed only for a **separate** static SPA site.

## Prerequisites checklist

- [ ] .NET 8 SDK — https://dotnet.microsoft.com/download/dotnet/8.0
- [ ] Node.js 20+ — https://nodejs.org
- [ ] SQL Server (or Docker Desktop for `docker compose`)
- [ ] Git
- [ ] Optional: `dotnet tool install --global dotnet-ef --version 8.0.30`

## Local run (copy-paste)

```powershell
git clone https://github.com/kaybrandon/bis-client-site-audit.git
cd bis-client-site-audit
```

### Connection string

Edit `src/BisAudit.Api/appsettings.json` → `ConnectionStrings:DefaultConnection`.

Windows login (typical on a domain PC):

```
Server=localhost;Database=BisAudit;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

SQL password (matches Docker Compose):

```
Server=localhost,1433;Database=BisAudit;User Id=sa;Password=Your_password123;TrustServerCertificate=True;MultipleActiveResultSets=true
```

### Restore, migrate, run API

```powershell
dotnet restore BisAudit.sln
dotnet ef database update --project src/BisAudit.Api
dotnet run --project src/BisAudit.Api --launch-profile http
```

First start also runs `Database.Migrate()` and seed if the DB is empty.

- API: http://localhost:5088
- Swagger: http://localhost:5088/swagger
- App log file: `src/BisAudit.Api/logs/bisaudit-YYYYMMDD.log`

### Run the React app

```powershell
cd client
npm install
npm run dev
```

UI: http://localhost:5173 (proxies `/api` and `/uploads` to the API).

### Seed admin (defaults)

| | |
|---|---|
| Email | `admin@bis.local` |
| Password | `Admin!23456` |

Change these in `Seed` before you share a machine. `LoadSampleAudit: true` loads **Murray Media** and **Harbor Legal** when there are no audits.

### Optional: SQL in Docker

```powershell
docker compose up -d sqlserver
```

Then use the SQL-password connection string above.

### New migration (after you change C# entities)

```powershell
dotnet ef migrations add SomeName --project src/BisAudit.Api --output-dir Data/Migrations
```

## Config keys (plain English)

All live under `src/BisAudit.Api/appsettings.json` (override with `appsettings.Development.json`, User Secrets, or environment variables like `ConnectionStrings__DefaultConnection`).

| Key | Meaning |
|---|---|
| `ConnectionStrings:DefaultConnection` | How the API finds SQL Server. |
| `Jwt:Key` | Secret that signs login tokens. **≥ 32 characters. Change before production.** |
| `Jwt:Issuer` / `Jwt:Audience` | Labels baked into the token. Leave as `BisAudit` / `BisAudit.Client` unless you know you need to change them. |
| `Jwt:ExpiresMinutes` | How long a login stays valid (default 720 = 12 hours). |
| `Cors:Origins` | Browser origins allowed to call the API. Needed only if the website is on a **different** host than the API. Vite defaults are already listed. |
| `Uploads:RootPath` | Folder for photos. Default `wwwroot/uploads`. Use an absolute path (e.g. `D:\BisAudit\uploads`) if you want files off the site disk. |
| `Seed:AdminEmail` / `Seed:AdminPassword` | First admin created when the user table is empty. |
| `Seed:LoadSampleAudit` | `true` = demo audits on empty DB. Set `false` on a live server. |
| `Logging:DetailedErrors` | `true` = yellow developer error page + Swagger. **Staging only. Never Production.** |
| `Logging:File:Path` | Folder for rolling app logs (default `logs` under the API folder). |
| `Logging:File:RetainedFileCountLimit` | How many daily log files to keep (default 30). |
| `Serilog:MinimumLevel` | How chatty the log is. See [LOGGING.md](LOGGING.md). |

## Field use (tablets / laptops on site)

- UI is built for **iPad-ish widths** and phones: 44px tap targets, sticky horizontal section nav, 16px form text (avoids iOS zoom).
- Photos: **Take photo** opens the rear camera (`<input capture="environment">`); **From library** picks existing images (multi-select). Uploads are ordinary HTTP multipart — **no SignalR / WebSockets**.
- Large camera shots are resized in the browser before upload (API max **12 MB**).
- Spotty Wi-Fi: GET retries once; failed uploads show **Retry**; failed saves stay on the form with an error.
- Refresh the page if something looks stale after a dropped connection.

Next: [IIS-SQL-DEPLOY.md](IIS-SQL-DEPLOY.md) · [LOGGING.md](LOGGING.md)
