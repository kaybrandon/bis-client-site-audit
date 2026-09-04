import { useState } from 'react';
import './App.css';

const STATUS_META = {
  pass: { icon: '✓', label: 'Pass', className: 'pass' },
  warn: { icon: '!', label: 'Warn', className: 'warn' },
  fail: { icon: '✕', label: 'Fail', className: 'fail' },
};

function scoreClass(score) {
  if (score >= 90) return 'pass';
  if (score >= 60) return 'warn';
  return 'fail';
}

function ScoreRing({ score, size = 132, label }) {
  const stroke = 12;
  const radius = (size - stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const offset = circumference - (score / 100) * circumference;
  const cls = scoreClass(score);
  return (
    <div className="ring-wrap">
      <svg width={size} height={size} className={`ring ring-${cls}`}>
        <circle cx={size / 2} cy={size / 2} r={radius} className="ring-track" strokeWidth={stroke} fill="none" />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          className="ring-value"
          strokeWidth={stroke}
          fill="none"
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          strokeLinecap="round"
          transform={`rotate(-90 ${size / 2} ${size / 2})`}
        />
      </svg>
      <div className="ring-center">
        <span className="ring-score">{score}</span>
        {label && <span className="ring-label">{label}</span>}
      </div>
    </div>
  );
}

export default function App() {
  const [url, setUrl] = useState('https://example.com');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [report, setReport] = useState(null);

  async function runAudit(e) {
    e.preventDefault();
    setLoading(true);
    setError('');
    setReport(null);
    try {
      const res = await fetch('/api/audit', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url }),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'Audit failed.');
      setReport(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          <div className="brand-mark">BIS</div>
          <div className="brand-text">
            <strong>Client Site Audit</strong>
            <span>SEO · Accessibility · Security · Performance</span>
          </div>
        </div>
      </header>

      <main className="container">
        <section className="hero">
          <h1>Audit any client website in seconds</h1>
          <p>Enter a URL to scan its SEO, accessibility, security headers and performance signals.</p>
          <form className="search" onSubmit={runAudit}>
            <input
              type="text"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              placeholder="example.com"
              aria-label="Website URL"
              spellCheck="false"
            />
            <button type="submit" disabled={loading}>
              {loading ? 'Auditing…' : 'Run audit'}
            </button>
          </form>
          {error && <div className="error" role="alert">{error}</div>}
        </section>

        {loading && (
          <div className="loading">
            <div className="spinner" />
            <span>Fetching and analyzing {url}…</span>
          </div>
        )}

        {report && (
          <section className="results">
            <div className="summary card">
              <ScoreRing score={report.overallScore} label="Overall" />
              <div className="summary-meta">
                <a className="summary-url" href={report.finalUrl} target="_blank" rel="noreferrer noopener">
                  {report.finalUrl}
                </a>
                <div className="summary-stats">
                  <span>HTTP {report.httpStatus}</span>
                  <span>{report.responseTimeMs} ms</span>
                  <span>{Math.round(report.sizeBytes / 1024)} KB</span>
                </div>
                <div className="mini-scores">
                  {report.categories.map((c) => (
                    <div key={c.id} className={`mini-score ${scoreClass(c.score)}`}>
                      <strong>{c.score}</strong>
                      <span>{c.label}</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            <div className="categories">
              {report.categories.map((cat) => (
                <div key={cat.id} className="card category">
                  <div className="category-head">
                    <h2>{cat.label}</h2>
                    <span className={`badge ${scoreClass(cat.score)}`}>{cat.score}</span>
                  </div>
                  <ul className="checks">
                    {cat.checks.map((chk) => {
                      const meta = STATUS_META[chk.status];
                      return (
                        <li key={chk.id} className="check">
                          <span className={`dot ${meta.className}`} title={meta.label}>
                            {meta.icon}
                          </span>
                          <div className="check-body">
                            <span className="check-label">{chk.label}</span>
                            <span className="check-detail">{chk.detail}</span>
                          </div>
                        </li>
                      );
                    })}
                  </ul>
                </div>
              ))}
            </div>
          </section>
        )}
      </main>

      <footer className="footer">
        <span>BIS Client Site Audit · built for the Cloud Agent dev environment demo</span>
      </footer>
    </div>
  );
}
