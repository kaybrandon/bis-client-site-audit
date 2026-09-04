# Local setup — BIS Client IT Audit

Plain-English steps to run the React client and the ASP.NET Core API on a Windows workstation (or Linux with Docker SQL Server).

## What you need

1. **.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0  
2. **Node.js 20+** (includes npm) — https://nodejs.org  
3. **SQL Server** 2019+ / Express on this machine, **or** Docker Desktop for the compose file  
4. Optional: `dotnet tool install --global dotnet-ef --version 8.0.30`

## 1. Get the code

```powershell
git clone <this-repo>
cd bis-client-site-audit
```

## 2. Point the API at SQL Server

Edit `src/BisAudit.Api/appsettings.json` (or use User Secrets / environment variables).

Windows / SQL with your Windows login:

```
Server=localhost;Database=BisAudit;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

SQL password login (matches Docker Compose):

```
Server=localhost,1433;Database=BisAudit;User Id=sa;Password=Your_password123;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Keep `Jwt:Key` at least 32 characters. Change it before you put this on a real server.

## 3. Restore, migrate, run the API

```powershell
dotnet restore BisAudit.sln
dotnet ef database update --project src/BisAudit.Api
dotnet run --project src/BisAudit.Api --launch-profile http
```

The first start also runs migrations and seed (admin + SAMPLE audits if the database is empty).

API listens on **http://localhost:5088**. Swagger: http://localhost:5088/swagger

## 4. Install and run the React app

```powershell
cd client
npm install
npm run dev
```

Vite listens on **http://localhost:5173** and proxies `/api` and `/uploads` to the API so you do not have to fight CORS during daily work. CORS is still enabled on the API for `http://localhost:5173` if you set `VITE_API_URL=http://localhost:5088`.

## 5. Sign in

| | |
|---|---|
| Email | `admin@bis.local` |
| Password | `Admin!23456` |

Change this after the first login on any shared box (`Seed` section in `appsettings.json`).

## Docker Compose (optional)

From the repo root:

```powershell
docker compose up --build
```

- SQL Server: `localhost,1433` (sa / `Your_password123`)  
- API: http://localhost:5088  

Run `npm run dev` in `client/` against that API.

## New EF migration (after model changes)

```powershell
dotnet ef migrations add SomeName --project src/BisAudit.Api --output-dir Data/Migrations
```
