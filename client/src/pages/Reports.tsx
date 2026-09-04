import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api'
import { Header } from '../components/Header'
import { StatusBadge } from '../components/StatusBadge'
import type { AuditSummary } from '../types'

export function Reports() {
  const [items, setItems] = useState<AuditSummary[]>([])
  useEffect(() => { void api.audits().then(setItems) }, [])

  return (
    <div className="page-shell">
      <Header />
      <div className="content">
        <div className="section-head">
          <div>
            <h2>Client Reports</h2>
            <p className="muted">Printable summaries with cost-of-inaction, investment, and payback.</p>
          </div>
        </div>
        <table className="data">
          <thead>
            <tr><th>Company</th><th>Status</th><th>Open issues</th><th>Critical / high</th><th /></tr>
          </thead>
          <tbody>
            {items.map((a) => (
              <tr key={a.id}>
                <td>{a.companyName}</td>
                <td><StatusBadge value={a.status} /></td>
                <td>{a.openItems}</td>
                <td>{a.openCriticalHigh}</td>
                <td><Link className="btn btn-primary btn-sm" to={`/reports/${a.id}`}>Open report</Link></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

const money = (n: number) => n.toLocaleString(undefined, { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })

export function ReportPrint() {
  const id = window.location.pathname.split('/').pop() || ''
  const [audit, setAudit] = useState<Awaited<ReturnType<typeof api.audit>> | null>(null)
  useEffect(() => { void api.audit(id).then(setAudit) }, [id])
  if (!audit) return <div className="content">Loading…</div>
  const m = audit.metrics
  const payback = m.investment > 0 ? (m.paybackMonths == null ? 'n/a' : `${m.paybackMonths} months`) : '—'

  return (
    <div className="page-shell">
      <header className="app-header no-print">
        <div className="app-brand">
          <Link className="btn btn-ghost btn-icon" to="/reports">←</Link>
          <div>
            <h1>Client Report</h1>
            <p>{audit.companyName}</p>
          </div>
        </div>
        <div className="header-actions">
          <button type="button" className="btn btn-light" onClick={() => window.print()}>Print / PDF</button>
        </div>
      </header>
      <div className="content">
        <article className="report">
          <p className="muted">BIS Client IT Audit · Infrastructure assurance workspace</p>
          <h1>{audit.companyName}</h1>
          <p>{audit.industry} · {audit.employeeCount ?? 0} employees · {audit.status} · Audit date {new Date(audit.auditDate).toLocaleDateString()}</p>
          {audit.address && <p>{audit.address}</p>}
          <p><strong>Prepared by:</strong> {audit.preparedBy || '—'} &nbsp; <strong>Progress:</strong> {audit.progress}%</p>
          <div className="kpi-row">
            <div className="kpi"><span>Open critical / high</span><strong>{m.criticalHighCount}</strong></div>
            <div className="kpi"><span>Open issues</span><strong>{m.openIssueCount}</strong></div>
            <div className="kpi"><span>Annual cost of inaction</span><strong>{money(m.annualCostOfInaction)}</strong></div>
            <div className="kpi"><span>Recommended investment</span><strong>{money(m.investment)}</strong></div>
            <div className="kpi"><span>Annual savings</span><strong>{money(m.annualSavings)}</strong></div>
            <div className="kpi"><span>Payback</span><strong>{payback}</strong></div>
          </div>
          <h2>Executive summary</h2>
          <p>{audit.executiveSummary || 'No executive summary recorded.'}</p>
          <p><strong>Scope:</strong> {audit.auditScope || '—'}</p>
          <p><strong>Previous IT support:</strong> {audit.previousItSupport || '—'}</p>
          <h2>Contacts</h2>
          <table className="data">
            <thead><tr><th>Role</th><th>Name</th><th>Title</th><th>Email</th><th>Phone</th></tr></thead>
            <tbody>
              {audit.contacts.map((c) => (
                <tr key={c.id}><td>{c.role}</td><td>{c.name}</td><td>{c.title}</td><td>{c.email}</td><td>{c.phone}</td></tr>
              ))}
            </tbody>
          </table>
          <h2>Open issues</h2>
          <table className="data">
            <thead><tr><th>Issue / Recommendation</th><th>Severity</th><th>Status</th><th>Monthly impact</th><th>Notes</th></tr></thead>
            <tbody>
              {audit.issues.filter((i) => !['closed', 'optional'].includes(String(i.status).toLowerCase())).map((i) => (
                <tr key={String(i.id)}>
                  <td>{String(i.issueRecommendation)}</td>
                  <td>{String(i.severity)}</td>
                  <td>{String(i.status)}</td>
                  <td>{money(Number(i.monthlyCostImpact || 0))}</td>
                  <td>{String(i.notes || '')}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <h2>Purchase tracker</h2>
          <table className="data">
            <thead><tr><th>Item</th><th>Qty</th><th>Unit</th><th>Monthly savings</th><th>Status</th></tr></thead>
            <tbody>
              {audit.purchases.map((p) => (
                <tr key={String(p.id)}>
                  <td>{String(p.item)}</td>
                  <td>{String(p.quantity)}</td>
                  <td>{money(Number(p.estimatedUnitCost || 0))}</td>
                  <td>{money(Number(p.monthlySavings || 0))}</td>
                  <td>{String(p.status)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {([
            ['Team & workstations', audit.workstations, (w: Record<string, unknown>) => `${w.userName || ''} / ${w.deviceName || ''}`.replace(/^ \/ | \/ $/g, '') || 'Workstation'],
            ['Network & infrastructure', audit.networkItems, (w: Record<string, unknown>) => String(w.deviceName || 'Network item')],
            ['Servers, storage & cloud', audit.serverStorageItems, (w: Record<string, unknown>) => String(w.deviceName || 'Server / storage')],
            ['Security, cameras & AV', audit.securityAvItems, (w: Record<string, unknown>) => String(w.deviceName || 'Security / AV')],
            ['Software & licensing', audit.softwareItems, (w: Record<string, unknown>) => String(w.softwareAccount || 'Software')],
          ] as const).map(([title, rows, label]) => (
            <div key={title}>
              <h2>{title}</h2>
              <table className="data">
                <thead><tr><th>Item</th><th>Status</th></tr></thead>
                <tbody>
                  {rows.map((r) => (
                    <tr key={String(r.id)}><td>{label(r)}</td><td>{String(r.status)}</td></tr>
                  ))}
                </tbody>
              </table>
            </div>
          ))}
          {audit.photos.length > 0 && (
            <>
              <h2>Site photos</h2>
              <div className="photo-grid">
                {audit.photos.map((p) => (
                  <figure className="photo-tile" key={p.id}>
                    <img src={api.photoSrc(p.publicUrl || p.relativePath)} alt={p.fileName} />
                    <figcaption>{p.category} · {p.fileName}</figcaption>
                  </figure>
                ))}
              </div>
            </>
          )}
          <p className="muted">Annual cost of inaction = Σ monthly cost impact × 12. Investment = Σ (qty × unit cost) excluding cancelled. Annual savings = Σ monthly savings × 12. Payback months = investment ÷ (annual savings / 12) when investment &gt; 0.</p>
        </article>
      </div>
    </div>
  )
}
