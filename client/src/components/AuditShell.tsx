import { Link, NavLink, Outlet, useLocation, useParams } from 'react-router-dom'
import { useCallback, useEffect, useRef, useState } from 'react'
import { api } from '../api'
import { rememberLastAudit } from '../lastAudit'
import { SECTIONS, nextIncompleteSection, sectionCue, type AuditDetail } from '../types'

export type AuditOutlet = { audit: AuditDetail | null; reload: () => Promise<void> }

export function AuditShell() {
  const { id } = useParams()
  const loc = useLocation()
  const current = loc.pathname.split('/').pop() || 'overview'
  const [audit, setAudit] = useState<AuditDetail | null>(null)
  const navRef = useRef<HTMLElement>(null)

  const reload = useCallback(async () => {
    if (!id) return
    setAudit(await api.audit(id))
  }, [id])

  useEffect(() => {
    void reload()
  }, [reload])

  useEffect(() => {
    if (id) rememberLastAudit(id)
  }, [id])

  useEffect(() => {
    const active = navRef.current?.querySelector<HTMLElement>('.nav-item.active')
    active?.scrollIntoView({ inline: 'nearest', block: 'nearest', behavior: 'smooth' })
  }, [current])

  const next = nextIncompleteSection(audit?.sections, current)

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
          <div className="audit-nav-tray">
            <nav ref={navRef} className="audit-nav" aria-label="Audit sections">
              {SECTIONS.map((s) => {
                const progress = audit?.sections.find((p) => p.key === s.slug)
                const cue = sectionCue(progress)
                return (
                  <NavLink
                    key={s.slug}
                    to={`/audits/${id}/${s.slug}`}
                    title={s.title}
                    className={({ isActive }) => `nav-item ${isActive ? 'active' : ''} cue-${cue.kind}`}
                  >
                    <span className="num">{s.num}</span>
                    <span className="nav-short">{s.short}</span>
                    {cue.kind === 'na' ? <span className="nav-dot">·</span> : null}
                    <span className={`nav-cue cue-${cue.kind}`}>{cue.text}</span>
                  </NavLink>
                )
              })}
            </nav>
          </div>
          <div className="audit-strip">
            <span className="audit-strip-progress">
              {audit ? `${audit.progress}% complete` : 'Loading…'}
            </span>
            {id && next ? (
              <Link className="btn btn-primary btn-sm" to={`/audits/${id}/${next.slug}`}>
                Next incomplete: {next.short}
              </Link>
            ) : (
              <span className="muted">All sections started</span>
            )}
          </div>
        </aside>
        <main className="audit-main">
          <Outlet context={{ audit, reload } satisfies AuditOutlet} />
        </main>
      </div>
    </div>
  )
}
