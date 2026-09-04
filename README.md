# BIS Client Site Audit

A small full-stack tool for quickly auditing a client's website. Enter a URL and get a
scored report across four categories:

- **SEO** – title, meta description, headings, canonical URL, viewport, Open Graph tags
- **Accessibility** – document language, image alt text, form field labels
- **Security** – HTTPS and security response headers (HSTS, CSP, X-Content-Type-Options, …)
- **Performance** – response time, HTML size, compression, external JS/CSS requests

## Stack

- **server/** – Express API that fetches a page and runs the audit ([`cheerio`](https://cheerio.js.org/) for HTML parsing)
- **web/** – Vite + React single-page frontend

The project uses npm workspaces, so a single `npm install` at the root installs everything.

## Getting started

```bash
npm install        # install all workspace dependencies
npm run dev        # start API (:3001) and web (:5173) together
```

Then open http://localhost:5173 and audit a site. The Vite dev server proxies
`/api/*` to the API on port 3001.

### Individual services

```bash
npm run dev:server   # API only  -> http://localhost:3001
npm run dev:web      # web only  -> http://localhost:5173
```

### Tests

```bash
npm test             # runs the server audit-engine test suite (node --test)
```

## API

`POST /api/audit` with a JSON body `{ "url": "example.com" }` returns the full
audit report. `GET /api/health` returns a liveness check.

## Cloud Agent environment

`.cursor/environment.json` installs dependencies with `npm install` and launches the
API and web dev servers as persistent terminals, so a fresh Cloud Agent boots straight
into a runnable dev environment.
