import { test } from 'node:test';
import assert from 'node:assert/strict';
import http from 'node:http';
import { normalizeUrl, auditSite } from './audit.js';

test('normalizeUrl adds https scheme when missing', () => {
  assert.equal(normalizeUrl('example.com').href, 'https://example.com/');
});

test('normalizeUrl keeps existing scheme', () => {
  assert.equal(normalizeUrl('http://example.com/path').href, 'http://example.com/path');
});

test('normalizeUrl rejects empty input', () => {
  assert.throws(() => normalizeUrl('   '), /required/);
});

test('normalizeUrl rejects non-http protocols', () => {
  assert.throws(() => normalizeUrl('ftp://example.com'), /http and https/);
});

test('auditSite produces categorized report for a served page', async () => {
  const html = `<!doctype html>
    <html lang="en">
      <head>
        <title>Acme Widgets - Quality Tools for Builders</title>
        <meta name="description" content="Acme Widgets sells durable, affordable tools trusted by professional builders across the country for over twenty years." />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <link rel="canonical" href="http://localhost/" />
        <meta property="og:title" content="Acme Widgets" />
      </head>
      <body>
        <h1>Welcome to Acme</h1>
        <img src="/logo.png" alt="Acme logo" />
        <form><label for="q">Search</label><input id="q" type="text" /></form>
      </body>
    </html>`;

  const server = http.createServer((_req, res) => {
    res.setHeader('Content-Type', 'text/html');
    res.end(html);
  });
  await new Promise((resolve) => server.listen(0, resolve));
  const { port } = server.address();

  try {
    const report = await auditSite(`http://localhost:${port}`);
    assert.equal(report.httpStatus, 200);
    assert.ok(report.categories.length === 4);
    const seo = report.categories.find((c) => c.id === 'seo');
    assert.ok(seo.score >= 80, `expected strong SEO score, got ${seo.score}`);
    const a11y = report.categories.find((c) => c.id === 'accessibility');
    const langCheck = a11y.checks.find((c) => c.id === 'html-lang');
    assert.equal(langCheck.status, 'pass');
    assert.ok(typeof report.overallScore === 'number');
  } finally {
    server.close();
  }
});
