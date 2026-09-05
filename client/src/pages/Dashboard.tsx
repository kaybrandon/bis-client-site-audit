import { useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { api } from '../api'
import { ConfirmSheet } from '../components/ConfirmSheet'
import { Header } from '../components/Header'
import { Modal, Field } from '../components/Modal'
import { StatusBadge } from '../components/StatusBadge'
import { readLastAuditId } from '../lastAudit'
import { nextIncompleteSection, type AuditSummary } from '../types'

export function Dashboard() {
  const [items, setItems] = useState<AuditSummary[]>([])
  const [showNew, setShowNew] = useState(false)
  const [name, setName] = useState('')
  const [pendingDup, setPendingDup] = useState<string | null>(null)
  const [params] = useSearchParams()
  const nav = useNavigate()
  const lastId = readLastAuditId()
  const last = items.find((a) => a.id === lastId)
  const continueSlug = last ? (nextIncompleteSection(last.sections, '')?.slug ?? 'overview') : null

  const load = () => api.audits().then(setItems)

  useEffect(() => {
    void load()
    if (params.get('new')) setShowNew(true)
  }, [params])

  const active = items.filter((a) => a.status.toLowerCase() !== 'complete').length
  const completed = items.length - active
  const crit = items.reduce((s, a) => s + a.openCriticalHigh, 0)

  const create = async () => {
    if (!name.trim()) return
    const audit = await api.createAudit(name.trim())
    nav(`/audits/${audit.id}/overview`)
  }

  const duplicate = async (id: string) => {
    const copy = await api.duplicate(id)
    setPendingDup(null)
    nav(`/audits/${copy.id}/overview`)
  }

  return (
    <div className="page-shell">
      <Header onNewAudit={() => setShowNew(true)} />
      <div className="content">
        <div className="stats-row">
          <div className="stat-card"><label>Active audits</label><strong>{active}</strong></div>
          <div className="stat-card"><label>Open critical / high</label><strong>{crit}</strong></div>
          <div className="stat-card"><label>Completed audits</label><strong>{completed}</strong></div>
        </div>
        {last && continueSlug && (
          <div className="continue-card">
            <div>
              <p className="muted" style={{ margin: 0 }}>Pick up where you left off</p>
              <h2 style={{ margin: '4px 0 0' }}>{last.companyName}</h2>
              <p className="muted">
                {last.progress}% · next: {nextIncompleteSection(last.sections, '')?.title ?? 'Overview'}
              </p>
            </div>
            <Link className="btn btn-primary" to={`/audits/${last.id}/${continueSlug}`}>Continue</Link>
          </div>
        )}
        <div className="section-head">
          <div>
            <h2>Client audits</h2>
            <p className="muted">A live pulse across every engagement.</p>
          </div>
        </div>
        {items.length === 0 && <div className="empty">No audits yet. Create one to start an engagement.</div>}
        <div className="audit-grid">
          {items.map((audit) => (
            <article className="audit-card" key={audit.id}>
              <div className="audit-card-top">
                <div style={{ display: 'flex', gap: 12 }}>
                  <div className="client-thumb">
                    {audit.clientPhotoPath
                      ? <img src={api.photoSrc(audit.clientPhotoPath)} alt="" />
                      : <span>🏢</span>}
                  </div>
                  <div>
                    <h3>{audit.companyName}</h3>
                    <p className="muted">{audit.industry} · {audit.employeeCount ?? 0} employees</p>
                  </div>
                </div>
                <StatusBadge value={audit.status} />
              </div>
              <div>
                <div className="progress-row"><span>Audit progress</span><span>{audit.progress}%</span></div>
                <div className="progress"><span style={{ width: `${audit.progress}%` }} /></div>
              </div>
              <div className="card-foot">
                <span className={`open-items ${audit.openItems === 0 ? 'zero' : ''}`}>{audit.openItems} open items</span>
                <div className="card-actions">
                  <Link className="btn btn-primary" to={`/audits/${audit.id}/overview`}>Open</Link>
                  <Link className="btn btn-ghost-dark" to={`/reports/${audit.id}`}>Report</Link>
                  <button type="button" className="btn btn-ghost-dark" onClick={() => setPendingDup(audit.id)}>Duplicate</button>
                </div>
              </div>
            </article>
          ))}
        </div>
      </div>
      {showNew && (
        <Modal title="New audit" saveLabel="Create audit" onSave={() => void create()} onClose={() => setShowNew(false)}>
          <Field label="Company name" full>
            <input value={name} onChange={(e) => setName(e.target.value)} />
          </Field>
          <p className="muted">Status will be Planning, audit date today, industry Marketing. You can change these on Overview.</p>
        </Modal>
      )}
      {pendingDup && (
        <ConfirmSheet
          title="Duplicate audit"
          message={`Create a copy of ${items.find((a) => a.id === pendingDup)?.companyName ?? 'this audit'}?`}
          confirmLabel="Duplicate"
          onConfirm={() => void duplicate(pendingDup)}
          onCancel={() => setPendingDup(null)}
        />
      )}
    </div>
  )
}
