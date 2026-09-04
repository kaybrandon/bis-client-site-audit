# BIS Client IT Audit

Infrastructure assurance workspace — a Phase 1 rebuild of the Base44 **bis-audit-flow** app as an ASP.NET Core 8 Blazor Server application with EF Core and SQL Server. There is no Base44 runtime, API, or package dependency.

Target host: Windows Server + IIS on Dell hardware, with SQL Server on the same box or a nearby instance.

## Stack

- ASP.NET Core 8 Blazor Server
- EF Core 8 + SQL Server
- ASP.NET Core Identity (local cookie auth)
- Photos stored under `wwwroot/uploads` (path configurable)
- Optional Docker Compose for local SQL Server

## Default login (seeded)

| | |
|---|---|
| Email | `admin@bis.local` |
| Password | `Admin!23456` |

Override in `appsettings.json` under `Seed`. Public registration is not linked from the login page. Change the password after first sign-in on any shared server.

Set `Seed:LoadSampleAudit` to `true` (default) to load the **Murray Media** and **Harbor Legal** demo engagements when the database is empty.

## Progress calculation

Dashboard and report **audit progress** is a weighted sum of nine sections (weights add to 100):

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

`Percent = round(100 × Σ(weight × score) / 100)` where collection scores are 0 or 1.

**Open items** on a dashboard card = issues whose status is not `Closed` or `Optional`.  
**Open critical / high** = those issues with severity Critical or High.  
**Active / completed** = audit status is not / is `Complete`.

### Report formulas

- **Annual cost of inaction** = `Σ(MonthlyCostImpact)` of open issues × 12
- **Investment** = `Σ(Qty × EstUnitCost)` of purchases whose status is not `Cancelled`
- **Annual savings** = `Σ(MonthlySavings)` of those purchases × 12
- **Payback (months)** = `Investment / (AnnualSavings / 12)` when `Investment > 0` and annual savings &gt; 0

## Windows — restore, migrate, run

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server 2019+ or SQL Server Express (local or remote)
- Optional: [EF Core tools](https://learn.microsoft.com/ef/core/cli/dotnet) — `dotnet tool install --global dotnet-ef --version 8.0.30`

### 1. Restore

```powershell
dotnet restore BisAudit.sln
```

### 2. Connection string

Edit `src/BisAudit.Web/appsettings.json` (or User Secrets / environment variables):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=BisAudit;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

Windows Auth (typical on a domain-joined Dell / Windows Server):

```
Server=localhost;Database=BisAudit;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

SQL login (matches Docker Compose):

```
Server=localhost,1433;Database=BisAudit;User Id=sa;Password=Your_password123;TrustServerCertificate=True;MultipleActiveResultSets=true
```

### 3. Migrate (also runs automatically on startup)

```powershell
dotnet ef database update --project src/BisAudit.Web
```

Or start the app; `DatabaseSeeder` calls `Database.Migrate()` then seeds dropdowns and the admin user.

### 4. Run

```powershell
dotnet run --project src/BisAudit.Web
```

Browse to the HTTPS URL printed in the console (for example `https://localhost:7xxx`). Sign in with the seeded admin.

### Docker Compose (optional local SQL Server)

```powershell
docker compose up --build
```

Web: `http://localhost:8080`  
SQL: `localhost,1433` (sa / `Your_password123`)

## IIS + SQL Server deploy (Windows Server)

1. Install the [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) (includes ASP.NET Core Module V2).
2. Install / attach SQL Server. Create an empty database or let migrate create `BisAudit`.
3. Publish:

   ```powershell
   dotnet publish src/BisAudit.Web -c Release -o C:\inetpub\bis-audit
   ```

4. In IIS Manager:
   - Add an Application Pool, **No Managed Code**, Integrated pipeline.
   - Add a website pointing at the publish folder. Bind hostname / HTTPS as required.
   - Grant the app-pool identity `Modify` on `wwwroot\uploads` (and on an alternate folder if you relocate uploads).
5. Set production connection string via `appsettings.Production.json`, environment variable `ConnectionStrings__DefaultConnection`, or IIS Configuration Editor.
6. Optional: store photos outside the site, for example `D:\BisAudit\uploads`, and set `Uploads:RootPath` to that absolute path. The app maps `/uploads` to it.
7. Recycle the pool. First request runs migrations and seed (disable `LoadSampleAudit` in production if you do not want demo data).
8. `web.config` is included and uses in-process hosting. Enable stdout logs under `.\logs` if you need to diagnose startup failures.

## Authentication hooks (Phase 1 is local Identity)

`Program.cs` documents the later switches:

- **Windows Authentication / IIS** — `AddAuthentication(IISDefaults.AuthenticationScheme)` plus IIS Anonymous disabled / Windows enabled.
- **Entra ID** — `AddMicrosoftIdentityWebApp` against an `AzureAd` configuration section.

Cookie Identity remains the default so the site runs on a workgroup server without AD.

## Solution layout

```
BisAudit.sln
src/BisAudit.Web/          Blazor Server host, EF model, UI
  Data/Entities/           Audits, inventory, issues, purchases, photos, dropdowns
  Data/Seed/               Dropdown catalog + SAMPLE audits
  Services/                CRUD, progress, photos, CSV, report metrics
  Components/Pages/        Dashboard, settings, reports, 9 audit sections
docker-compose.yml
```

## Screens

| Route | Purpose |
|---|---|
| `/` | Dashboard — stats, cards, new / duplicate / report / open |
| `/audits/{id}/overview` … `/photos` | Nine-section audit workspace |
| `/reports` and `/reports/{id}` | Printable client report (Print / PDF via browser) |
| `/settings` | One editable panel per dropdown list |
| `/Account/Login` | Local Identity sign-in |

## Photos

- Saved as files under the configured upload root (`wwwroot/uploads/{auditId}/…` by default).
- `SitePhoto` rows track category, owner type (audit or inventory item), and caption.
- Site Photos is the global library; each item card can attach photos as well.

## Development notes

- Create a new migration after model changes: `dotnet ef migrations add Name --project src/BisAudit.Web`
- Needs Attention items render with a yellow left border and warning icon.
- Export Contacts downloads a CSV of primary / technical / billing contacts plus workstation users.
