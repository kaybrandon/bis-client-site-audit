import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { api } from '../api'
import { Modal, Field } from '../components/Modal'
import { PhotoAttach } from '../components/PhotoAttach'
import { StatusBadge } from '../components/StatusBadge'

export type FieldDef = {
  key: string
  label: string
  kind?: 'text' | 'textarea' | 'select' | 'number' | 'check'
  optionsKey?: string
  full?: boolean
}

export function ItemSection({
  title, section, ownerType, photoCategory, fields, titleOf, detailOf, badgesOf, defaults,
  extraFilters,
}: {
  title: string
  section: string
  ownerType: string
  photoCategory: string
  fields: FieldDef[]
  titleOf: (item: Record<string, unknown>) => string
  detailOf?: (item: Record<string, unknown>) => string | undefined
  badgesOf?: (item: Record<string, unknown>) => (string | undefined)[]
  defaults: Record<string, unknown>
  extraFilters?: { severity?: boolean; status?: boolean }
}) {
  const { id = '' } = useParams()
  const [items, setItems] = useState<Record<string, unknown>[]>([])
  const [draft, setDraft] = useState<Record<string, unknown> | null>(null)
  const [options, setOptions] = useState<Record<string, string[]>>({})
  const [severityFilter, setSeverityFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')

  const load = () => api.items(id, section).then(setItems)

  useEffect(() => {
    void load()
    const keys = fields.map((f) => f.optionsKey).filter(Boolean) as string[]
    void Promise.all(keys.map(async (k) => [k, await api.dropdown(k)] as const))
      .then((pairs) => setOptions(Object.fromEntries(pairs)))
  }, [id, section])

  const filtered = items.filter((i) => {
    if (severityFilter && String(i.severity) !== severityFilter) return false
    if (statusFilter && String(i.status) !== statusFilter) return false
    return true
  })

  const save = async () => {
    if (!draft) return
    await api.saveItem(id, section, draft)
    setDraft(null)
    await load()
  }

  const remove = async (itemId: string) => {
    if (!confirm('Delete this item?')) return
    await api.deleteItem(id, section, itemId)
    await load()
  }

  return (
    <>
      <div className="toolbar">
        <div>
          <h2>{title}</h2>
          <p className="count">{items.length} documented items</p>
        </div>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          {extraFilters?.severity && (
            <select className="form-select" value={severityFilter} onChange={(e) => setSeverityFilter(e.target.value)}>
              <option value="">All severities</option>
              {(options['Issue.Severity'] || []).map((o) => <option key={o}>{o}</option>)}
            </select>
          )}
          {extraFilters?.status && (
            <select className="form-select" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">All statuses</option>
              {(options['Issue.Status'] || []).map((o) => <option key={o}>{o}</option>)}
            </select>
          )}
          <button type="button" className="btn btn-primary" onClick={() => setDraft({ ...defaults, auditId: id })}>+ Add item</button>
        </div>
      </div>
      {filtered.length === 0 && <div className="empty">No items match the current filter.</div>}
      {filtered.map((item) => (
        <article className={`item-card ${item.needsAttention ? 'needs-attention' : ''}`} key={String(item.id)}>
          <div>
            <div className="item-title">
              <h3>{titleOf(item)}</h3>
              {(badgesOf?.(item) || []).map((b) => b && <StatusBadge key={b} value={b} />)}
              {item.needsAttention ? <span className="warn" title="Needs attention">⚠</span> : null}
              <button type="button" className="btn btn-ghost-dark btn-icon btn-sm" onClick={() => setDraft({ ...item })}>✏️</button>
              <button type="button" className="btn btn-danger btn-icon btn-sm" onClick={() => void remove(String(item.id))}>🗑</button>
            </div>
            {(() => {
              const detail = detailOf?.(item)
              return detail ? <p style={{ margin: '8px 0 0' }}>{detail}</p> : null
            })()}
            {item.lastAudited ? <p className="muted">Last audited: {new Date(String(item.lastAudited)).toLocaleString()}</p> : null}
          </div>
          <PhotoAttach auditId={id} ownerId={String(item.id)} ownerType={ownerType} category={photoCategory} />
        </article>
      ))}
      {draft && (
        <Modal title={title} onSave={() => void save()} onClose={() => setDraft(null)}>
          <div className="form-grid">
            {fields.map((f) => (
              <Field key={f.key} label={f.kind === 'check' ? '' : f.label} full={f.full} check={f.kind === 'check'}>
                {f.kind === 'textarea' ? (
                  <textarea value={String(draft[f.key] ?? '')} onChange={(e) => setDraft({ ...draft, [f.key]: e.target.value })} />
                ) : f.kind === 'select' ? (
                  <select value={String(draft[f.key] ?? '')} onChange={(e) => setDraft({ ...draft, [f.key]: e.target.value })}>
                    <option value="">Select…</option>
                    {(options[f.optionsKey!] || []).map((o) => <option key={o}>{o}</option>)}
                  </select>
                ) : f.kind === 'number' ? (
                  <input type="number" value={draft[f.key] === undefined || draft[f.key] === null ? '' : String(draft[f.key])}
                    onChange={(e) => setDraft({ ...draft, [f.key]: e.target.value === '' ? null : Number(e.target.value) })} />
                ) : f.kind === 'check' ? (
                  <>
                    <input type="checkbox" checked={Boolean(draft[f.key])} onChange={(e) => setDraft({ ...draft, [f.key]: e.target.checked })} />
                    {' '}{f.label}
                  </>
                ) : (
                  <input value={String(draft[f.key] ?? '')} onChange={(e) => setDraft({ ...draft, [f.key]: e.target.value })} />
                )}
              </Field>
            ))}
          </div>
        </Modal>
      )}
    </>
  )
}
