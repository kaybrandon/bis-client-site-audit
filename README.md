# BIS Client IT Audit

Infrastructure assurance workspace — Phase 1 rebuild of the Base44 **bis-audit-flow** app. React + TypeScript + Vite SPA, ASP.NET Core 8 Web API, EF Core + SQL Server. No Blazor. No Base44.

Target host: Windows Server + IIS on Dell hardware, SQL Server on the same box or nearby.

## For developers / IIS deploy

| Doc | Use when |
|---|---|
| **[docs/SETUP.md](docs/SETUP.md)** | Clone, connection string, migrate, run, seed admin, config keys |
| **[docs/IIS-SQL-DEPLOY.md](docs/IIS-SQL-DEPLOY.md)** | IIS site + app pool, `dotnet publish`, SQL login, migrations, HTTPS, 500.30 |
| **[docs/LOGGING.md](docs/LOGGING.md)** | Console / `logs/` files / Event Viewer, staging detailed errors, bug-report checklist |

### Environment (one screen)

| Item | Development | Production (Dell / Windows Server) |
|---|---|---|
| OS | Windows 10/11 64-bit | Windows Server 2019/2022 |
| App | `dotnet run` + Vite (`:5088` / `:5173`) | IIS site, app pool **No Managed Code** |
| .NET | **SDK 8.0** | **Hosting Bundle 8.0** (runtime + ANCM) |
| SQL Server | Express / Developer / LocalDB / Docker | Express or Standard (local or nearby) |
| Node.js | **20+** (SPA build) | Not needed after you copy `client/dist` |
| Logs | `src/BisAudit.Api/logs/bisaudit-YYYYMMDD.log` | `C:\inetpub\bis-audit-api\logs\` (grant Modify to app-pool identity) |
| Seed admin | `admin@bis.local` / `Admin!23456` | Change `Seed` before go-live; `LoadSampleAudit: false` |
| Field clients | Tablet / laptop browsers (iPad-width + phone) | Same SPA; **Take photo** / **From library**; no SignalR |

```powershell
# Local (see docs/SETUP.md)
git clone https://github.com/kaybrandon/bis-client-site-audit.git
cd bis-client-site-audit
dotnet restore BisAudit.sln
dotnet ef database update --project src/BisAudit.Api
dotnet run --project src/BisAudit.Api --launch-profile http
# other terminal:
cd client && npm install && npm run dev
```

```powershell
# Publish (see docs/IIS-SQL-DEPLOY.md)
dotnet publish src/BisAudit.Api -c Release -o C:\inetpub\bis-audit-api
cd client && npm ci && npm run build
Copy-Item -Recurse -Force client\dist\* C:\inetpub\bis-audit-api\wwwroot\
```

| Piece | Location |
|---|---|
| Web API | `src/BisAudit.Api` |
| React client | `client` |
| Docs | `docs/SETUP.md`, `docs/IIS-SQL-DEPLOY.md`, `docs/LOGGING.md` |

## Default login (seeded)

| | |
|---|---|
| Email | `admin@bis.local` |
| Password | `Admin!23456` |

Override in `src/BisAudit.Api/appsettings.json` under `Seed`. Set `LoadSampleAudit` to `true` (default) to load **Murray Media** and **Harbor Legal** when the database is empty.

## Run locally (Windows)

Full walk-through: **[docs/SETUP.md](docs/SETUP.md)**.

```powershell
dotnet restore BisAudit.sln
dotnet run --project src/BisAudit.Api --launch-profile http
```

```powershell
cd client
npm install
npm run dev
```

- API: http://localhost:5088 (Swagger at `/swagger` — Authorize with the JWT from `POST /api/auth/login`)
- UI: http://localhost:5173 (Vite proxies `/api` and `/uploads` to the API)
- CORS is also enabled for `http://localhost:5173` if you set `VITE_API_URL=http://localhost:5088`

## Deploy to IIS + SQL Server

Full walk-through: **[docs/IIS-SQL-DEPLOY.md](docs/IIS-SQL-DEPLOY.md)**.

- **Same site (layout A):** publish the API, copy `client/dist` into `wwwroot`.
- **Separate sites (layout B):** API on one hostname, static SPA on another; set `VITE_API_URL` and `Cors:Origins`.

## Progress calculation

Dashboard and report **audit progress** is a weighted sum (weights add to 100):

| Section | Weight | Complete when |
|---|---:|---|
| Overview | 20 | Fraction of 10 key fields filled (company, industry, employees, address, a named contact, scope, prepared by, audit date, executive summary, previous IT support) |
| Team & Workstations | 12 | At least one item |
| Network & Infrastructure | 12 | At least one item |
| Servers, Storage & Cloud | 12 | At least one item |
| Security, Cameras & AV | 10 | At least one item |
| Software & Licensing | 8 | At least one item |
| Issues & Recommendations | 12 | At least one item |
| Purchase Tracker | 8 | At least one item |
| Site Photos | 6 | At least one photo |

**Open items** = issues whose status is not `Closed` or `Optional`.  
**Open critical / high** = those issues with severity Critical or High.  
**Active / completed** = audit status is not / is `Complete`.

### Report formulas

- **Annual cost of inaction** = `Σ(MonthlyCostImpact)` of open issues × 12
- **Investment** = `Σ(Qty × EstUnitCost)` of purchases whose status is not `Cancelled`
- **Annual savings** = `Σ(MonthlySavings)` of those purchases × 12
- **Payback (months)** = `Investment / (AnnualSavings / 12)` when `Investment > 0` and savings &gt; 0

## Auth

Phase 1: JWT from `POST /api/auth/login`. The SPA stores the token in `localStorage` and sends `Authorization: Bearer`.

Swagger UI (`/swagger`) uses the same Bearer JWT (Authorize). It is **on** in Development/Staging and **off** in Production unless you set `Swagger__Enabled=true`. When it is off, `/swagger` is 404 (not the SPA shell).

`Program.cs` comments show where to add **Windows Authentication / IIS** and **Entra ID** later.

## Photos

Uploaded through `POST /api/audits/{id}/photos` (multipart). Files land under `wwwroot/uploads/{auditId}/` (or `Uploads:RootPath`). Site Photos is the global library; each item card can attach photos too.

## Screens

| Route | Purpose |
|---|---|
| `/` | Dashboard — stats, cards, new / duplicate / report / open |
| `/audits/:id/overview` … `/photos` | Nine-section audit workspace |
| `/reports` and `/reports/:id` | Printable client report (browser Print / PDF) |
| `/settings` | One editable panel per dropdown list |
| `/login` | Local account sign-in |

## Logging

Serilog writes **console** and a **daily rolling file** under `logs/bisaudit-YYYYMMDD.log`. IIS stdout (startup / 500.30) is `logs/stdout_*.log`. Grant the app-pool identity Modify on `logs\` — see **[docs/LOGGING.md](docs/LOGGING.md)**.
