# IIS + SQL Server deploy (Dell / Windows Server)

Two supported layouts. Both use the same API and the same React `npm run build` output.

## Shared prerequisites

1. Install the **.NET 8 Hosting Bundle** (ASP.NET Core Module V2).  
2. Install SQL Server. Create database `BisAudit` or let the API migrate it on first start.  
3. Install IIS with the Default Web Site (or a new site).  
4. Give the app-pool identity `Modify` on the photo folder (`wwwroot\uploads` or a path you configure).

Publish the API:

```powershell
dotnet publish src/BisAudit.Api -c Release -o C:\inetpub\bis-audit-api
```

Build the SPA:

```powershell
cd client
npm ci
npm run build
```

`client/dist` is the static site.

Set production config (IIS Configuration Editor, `appsettings.Production.json`, or environment variables):

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key` (long random secret)
- `Seed__LoadSampleAudit` = `false` on a live server unless you want demo data
- `Cors__Origins__0` = the browser origin of the SPA (only needed for **separate sites**)
- `Uploads__RootPath` = `wwwroot/uploads` or e.g. `D:\BisAudit\uploads`

`web.config` in the API project uses in-process hosting and writes stdout to `.\logs\stdout`.

---

## Option A — same IIS site (API serves the SPA)

Copy `client/dist/*` into `C:\inetpub\bis-audit-api\wwwroot\` (alongside `uploads`).

The API serves those static files and falls back to `index.html` for client-side routes.

In IIS:

1. App pool: **No Managed Code**, Integrated.  
2. Website physical path: `C:\inetpub\bis-audit-api`.  
3. Bind HTTPS / hostname as you normally would.

Users open `https://audit.yourfirm.local/` (React) and the browser calls `/api/...` on the same origin. No CORS needed.

---

## Option B — separate IIS sites

**API site** (example `https://audit-api.yourfirm.local/`):

- Path: `C:\inetpub\bis-audit-api`
- Same app pool rules as above

**SPA site** (example `https://audit.yourfirm.local/`):

- Path: `C:\inetpub\bis-audit-web` (contents of `client/dist`)
- Add a `web.config` so React Router deep links work:

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

Build the client with the API origin:

```powershell
$env:VITE_API_URL="https://audit-api.yourfirm.local"
npm run build
```

Add that origin to `Cors:Origins` on the API.

URL-rewrite module must be installed on IIS for the SPA `web.config` above.

---

## Photos

Default: files under `wwwroot/uploads/{auditId}/`. The API serves them at `/uploads/...`.

If you set `Uploads:RootPath` to an absolute folder, the API maps `/uploads` to that folder. Grant the app-pool identity modify rights there.

---

## Auth later (not on in Phase 1)

`Program.cs` comments show where to add Windows Authentication / IIS and Entra ID. Phase 1 is JWT from `/api/auth/login`.
