import { useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { api } from '../api'
import { Header } from '../components/Header'
import { Modal, Field } from '../components/Modal'
import { StatusBadge } from '../components/StatusBadge'
import type { AuditSummary } from '../types'

export function Dashboard() {
  const [items, setItems] = useState<AuditSummary[]>([])
  const [showNew, setShowNew] = useState(false)
  const [name, setName] = useState('')
  const [params] = useSearchParams()
  const nav = useNavigate()

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
    if (!confirm('Duplicate this audit?')) return
    const copy = await api.duplicate(id)
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
                  <button type="button" className="btn btn-ghost-dark" onClick={() => void duplicate(audit.id)}>Duplicate</button>
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
    </div>
  )
}
