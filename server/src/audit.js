import { load } from 'cheerio';

const FETCH_TIMEOUT_MS = 15000;
const MAX_BYTES = 5 * 1024 * 1024;

const STATUS = { PASS: 'pass', WARN: 'warn', FAIL: 'fail' };

/**
 * Normalize user input into an absolute http(s) URL.
 * Defaults to https:// when no scheme is provided.
 */
export function normalizeUrl(input) {
  if (typeof input !== 'string' || input.trim() === '') {
    throw new Error('A website URL is required.');
  }
  let candidate = input.trim();
  const schemeMatch = candidate.match(/^([a-z][a-z0-9+.-]*):\/\//i);
  if (schemeMatch) {
    const scheme = schemeMatch[1].toLowerCase();
    if (scheme !== 'http' && scheme !== 'https') {
      throw new Error('Only http and https URLs can be audited.');
    }
  } else {
    candidate = `https://${candidate}`;
  }
  let url;
  try {
    url = new URL(candidate);
  } catch {
    throw new Error(`"${input}" is not a valid URL.`);
  }
  if (url.protocol !== 'http:' && url.protocol !== 'https:') {
    throw new Error('Only http and https URLs can be audited.');
  }
  return url;
}

async function fetchPage(url) {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), FETCH_TIMEOUT_MS);
  const startedAt = Date.now();
  try {
    const response = await fetch(url.href, {
      redirect: 'follow',
      signal: controller.signal,
      headers: {
        'User-Agent': 'BIS-Client-Site-Audit/1.0 (+https://github.com/kaybrandon/bis-client-site-audit)',
        Accept: 'text/html,application/xhtml+xml',
      },
    });
    const reader = response.body?.getReader();
    let received = 0;
    const chunks = [];
    if (reader) {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        received += value.length;
        chunks.push(value);
        if (received > MAX_BYTES) {
          controller.abort();
          break;
        }
      }
    }
    const bytes = Buffer.concat(chunks.map((c) => Buffer.from(c)));
    const responseTimeMs = Date.now() - startedAt;
    return {
      finalUrl: response.url || url.href,
      status: response.status,
      headers: response.headers,
      html: bytes.toString('utf8'),
      sizeBytes: received,
      responseTimeMs,
    };
  } finally {
    clearTimeout(timer);
  }
}

function check(id, label, status, detail) {
  return { id, label, status, detail };
}

function auditSeo($) {
  const checks = [];

  const title = $('head > title').first().text().trim();
  if (!title) {
    checks.push(check('title', 'Page title', STATUS.FAIL, 'No <title> element found.'));
  } else if (title.length < 10 || title.length > 65) {
    checks.push(
      check('title', 'Page title', STATUS.WARN, `Title is ${title.length} characters (aim for 10-65): "${title}".`)
    );
  } else {
    checks.push(check('title', 'Page title', STATUS.PASS, `"${title}"`));
  }

  const description = $('meta[name="description"]').attr('content')?.trim() || '';
  if (!description) {
    checks.push(check('meta-description', 'Meta description', STATUS.FAIL, 'No meta description found.'));
  } else if (description.length < 50 || description.length > 160) {
    checks.push(
      check(
        'meta-description',
        'Meta description',
        STATUS.WARN,
        `Description is ${description.length} characters (aim for 50-160).`
      )
    );
  } else {
    checks.push(check('meta-description', 'Meta description', STATUS.PASS, `${description.length} characters.`));
  }

  const h1Count = $('h1').length;
  if (h1Count === 0) {
    checks.push(check('h1', 'H1 heading', STATUS.FAIL, 'No <h1> heading found.'));
  } else if (h1Count > 1) {
    checks.push(check('h1', 'H1 heading', STATUS.WARN, `Found ${h1Count} <h1> headings; a single H1 is recommended.`));
  } else {
    checks.push(check('h1', 'H1 heading', STATUS.PASS, 'Exactly one <h1> heading.'));
  }

  const canonical = $('link[rel="canonical"]').attr('href');
  checks.push(
    canonical
      ? check('canonical', 'Canonical URL', STATUS.PASS, canonical)
      : check('canonical', 'Canonical URL', STATUS.WARN, 'No canonical link element found.')
  );

  const viewport = $('meta[name="viewport"]').attr('content');
  checks.push(
    viewport
      ? check('viewport', 'Mobile viewport', STATUS.PASS, viewport)
      : check('viewport', 'Mobile viewport', STATUS.FAIL, 'No responsive viewport meta tag found.')
  );

  const ogTags = $('meta[property^="og:"]').length;
  checks.push(
    ogTags > 0
      ? check('open-graph', 'Open Graph tags', STATUS.PASS, `${ogTags} Open Graph tag(s) found.`)
      : check('open-graph', 'Open Graph tags', STATUS.WARN, 'No Open Graph tags found (affects social sharing).')
  );

  return checks;
}

function auditAccessibility($) {
  const checks = [];

  const lang = $('html').attr('lang');
  checks.push(
    lang
      ? check('html-lang', 'Document language', STATUS.PASS, `lang="${lang}"`)
      : check('html-lang', 'Document language', STATUS.FAIL, 'The <html> element has no lang attribute.')
  );

  const images = $('img');
  const missingAlt = images.filter((_, el) => $(el).attr('alt') === undefined).length;
  if (images.length === 0) {
    checks.push(check('img-alt', 'Image alt text', STATUS.PASS, 'No <img> elements on the page.'));
  } else if (missingAlt === 0) {
    checks.push(check('img-alt', 'Image alt text', STATUS.PASS, `All ${images.length} image(s) have alt attributes.`));
  } else {
    checks.push(
      check(
        'img-alt',
        'Image alt text',
        STATUS.FAIL,
        `${missingAlt} of ${images.length} image(s) are missing alt attributes.`
      )
    );
  }

  const inputs = $('input:not([type="hidden"]), select, textarea');
  const unlabeled = inputs.filter((_, el) => {
    const $el = $(el);
    const id = $el.attr('id');
    const hasLabel = id && $(`label[for="${id}"]`).length > 0;
    const hasAria = $el.attr('aria-label') || $el.attr('aria-labelledby');
    const wrapped = $el.parents('label').length > 0;
    return !(hasLabel || hasAria || wrapped);
  }).length;
  if (inputs.length === 0) {
    checks.push(check('form-labels', 'Form field labels', STATUS.PASS, 'No form fields on the page.'));
  } else if (unlabeled === 0) {
    checks.push(check('form-labels', 'Form field labels', STATUS.PASS, `All ${inputs.length} field(s) are labeled.`));
  } else {
    checks.push(
      check(
        'form-labels',
        'Form field labels',
        STATUS.WARN,
        `${unlabeled} of ${inputs.length} form field(s) have no associated label.`
      )
    );
  }

  return checks;
}

function auditSecurity(page, finalUrl) {
  const checks = [];
  const h = page.headers;

  const isHttps = finalUrl.protocol === 'https:';
  checks.push(
    isHttps
      ? check('https', 'HTTPS', STATUS.PASS, 'Page is served over HTTPS.')
      : check('https', 'HTTPS', STATUS.FAIL, 'Page is not served over HTTPS.')
  );

  const securityHeaders = [
    ['hsts', 'Strict-Transport-Security', 'strict-transport-security'],
    ['csp', 'Content-Security-Policy', 'content-security-policy'],
    ['xcto', 'X-Content-Type-Options', 'x-content-type-options'],
    ['xfo', 'X-Frame-Options', 'x-frame-options'],
    ['refpol', 'Referrer-Policy', 'referrer-policy'],
  ];
  for (const [id, label, header] of securityHeaders) {
    const value = h.get(header);
    checks.push(
      value
        ? check(id, label, STATUS.PASS, value)
        : check(id, label, STATUS.WARN, `Missing ${label} response header.`)
    );
  }

  return checks;
}

function auditPerformance(page, $) {
  const checks = [];

  const t = page.responseTimeMs;
  if (t < 800) {
    checks.push(check('ttfb', 'Response time', STATUS.PASS, `Document loaded in ${t} ms.`));
  } else if (t < 2500) {
    checks.push(check('ttfb', 'Response time', STATUS.WARN, `Document loaded in ${t} ms (aim for < 800 ms).`));
  } else {
    checks.push(check('ttfb', 'Response time', STATUS.FAIL, `Document loaded in ${t} ms (slow).`));
  }

  const kb = Math.round(page.sizeBytes / 1024);
  if (kb < 500) {
    checks.push(check('page-size', 'HTML document size', STATUS.PASS, `${kb} KB.`));
  } else if (kb < 1500) {
    checks.push(check('page-size', 'HTML document size', STATUS.WARN, `${kb} KB (consider reducing).`));
  } else {
    checks.push(check('page-size', 'HTML document size', STATUS.FAIL, `${kb} KB (very large HTML document).`));
  }

  const compression = page.headers.get('content-encoding');
  checks.push(
    compression
      ? check('compression', 'Compression', STATUS.PASS, `content-encoding: ${compression}`)
      : check('compression', 'Compression', STATUS.WARN, 'No content-encoding header (gzip/br) detected.')
  );

  const scripts = $('script[src]').length;
  const styles = $('link[rel="stylesheet"]').length;
  const totalRequests = scripts + styles;
  if (totalRequests <= 20) {
    checks.push(
      check('requests', 'External JS/CSS requests', STATUS.PASS, `${scripts} script(s), ${styles} stylesheet(s).`)
    );
  } else {
    checks.push(
      check(
        'requests',
        'External JS/CSS requests',
        STATUS.WARN,
        `${totalRequests} external JS/CSS resources (${scripts} scripts, ${styles} stylesheets).`
      )
    );
  }

  return checks;
}

function scoreCategory(checks) {
  if (checks.length === 0) return 100;
  const points = checks.reduce((sum, c) => {
    if (c.status === STATUS.PASS) return sum + 1;
    if (c.status === STATUS.WARN) return sum + 0.5;
    return sum;
  }, 0);
  return Math.round((points / checks.length) * 100);
}

/**
 * Run a full audit against a URL and return a structured report.
 */
export async function auditSite(rawUrl) {
  const url = normalizeUrl(rawUrl);
  let page;
  try {
    page = await fetchPage(url);
  } catch (err) {
    if (err.name === 'AbortError') {
      throw new Error(`Request to ${url.href} timed out after ${FETCH_TIMEOUT_MS / 1000}s.`);
    }
    throw new Error(`Could not reach ${url.href}: ${err.message}`);
  }

  const finalUrl = new URL(page.finalUrl);
  const $ = load(page.html);

  const categories = [
    { id: 'seo', label: 'SEO', checks: auditSeo($) },
    { id: 'accessibility', label: 'Accessibility', checks: auditAccessibility($) },
    { id: 'security', label: 'Security', checks: auditSecurity(page, finalUrl) },
    { id: 'performance', label: 'Performance', checks: auditPerformance(page, $) },
  ].map((cat) => ({ ...cat, score: scoreCategory(cat.checks) }));

  const overallScore = Math.round(categories.reduce((sum, c) => sum + c.score, 0) / categories.length);

  return {
    url: url.href,
    finalUrl: page.finalUrl,
    httpStatus: page.status,
    responseTimeMs: page.responseTimeMs,
    sizeBytes: page.sizeBytes,
    auditedAt: new Date().toISOString(),
    overallScore,
    categories,
  };
}
