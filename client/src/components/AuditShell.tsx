import { Link, NavLink, Outlet, useParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { api } from '../api'
import { SECTIONS, type AuditDetail } from '../types'

export function AuditShell() {
  const { id } = useParams()
  const [audit, setAudit] = useState<AuditDetail | null>(null)

  useEffect(() => {
    if (!id) return
    void api.audit(id).then(setAudit)
  }, [id])

  return (
    <div className="page-shell">
      <header className="app-header">
        <div className="app-brand">
          <Link className="btn btn-ghost btn-icon" to="/" title="Dashboard">←</Link>
          <span className="brand-mark">🏢</span>
          <div>
            <h1>{audit?.companyName ?? 'Audit'}</h1>
            <p>{audit?.industry} · {audit?.status}</p>
          </div>
        </div>
        <div className="header-actions">
          {id && (
            <>
              <button type="button" className="btn btn-ghost" onClick={() => void api.downloadContacts(id, audit?.companyName || 'audit')}>
                ⬇ Export Contacts
              </button>
              <Link className="btn btn-light" to={`/reports/${id}`}>Client Report</Link>
            </>
          )}
        </div>
      </header>
      <div className="audit-shell">
        <aside className="audit-side no-print">
          <nav className="audit-nav" aria-label="Audit sections">
            {SECTIONS.map((s) => (
              <NavLink key={s.slug} to={`/audits/${id}/${s.slug}`} className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
                <span className="num">{s.num}</span>
                <span>{s.title}</span>
              </NavLink>
            ))}
          </nav>
        </aside>
        <main className="audit-main">
          <Outlet context={{ audit, reload: () => id && api.audit(id).then(setAudit) }} />
        </main>
      </div>
    </div>
  )
}
