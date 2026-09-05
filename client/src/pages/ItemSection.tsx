import { useEffect, useState } from 'react'
import { useOutletContext, useParams } from 'react-router-dom'
import { api } from '../api'
import type { AuditOutlet } from '../components/AuditShell'
import { ConfirmSheet } from '../components/ConfirmSheet'
import { Modal, Field } from '../components/Modal'
import { PhotoAttach } from '../components/PhotoAttach'
import { StatusBadge } from '../components/StatusBadge'

export type FieldDef = {
  key: string
  label: string
  kind?: 'text' | 'textarea' | 'select' | 'number' | 'check'
  optionsKey?: string
  full?: boolean
  more?: boolean
}

export function ItemSection({
  title, section, ownerType, photoCategory, fields, titleOf, detailOf, badgesOf, defaults,
  extraFilters, addFirstLabel,
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
  addFirstLabel: string
}) {
  const { id = '' } = useParams()
  const { reload: reloadAudit } = useOutletContext<AuditOutlet>()
  const usesNa = fields.some((f) => f.key === 'needsAttention')
  const [items, setItems] = useState<Record<string, unknown>[]>([])
  const [draft, setDraft] = useState<Record<string, unknown> | null>(null)
  const [options, setOptions] = useState<Record<string, string[]>>({})
  const [severityFilter, setSeverityFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<string | null>(null)

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
    setSaving(true)
    setError(null)
    try {
      await api.saveItem(id, section, draft)
      setDraft(null)
      await load()
      await reloadAudit()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Save failed. Check Wi-Fi and try again.')
    } finally {
      setSaving(false)
    }
  }

  const startAdd = () => { setError(null); setDraft({ ...defaults, auditId: id }) }

  const remove = async (itemId: string) => {
    await api.deleteItem(id, section, itemId)
    setPendingDelete(null)
    await load()
    await reloadAudit()
  }

  const primary = fields.filter((f) => !f.more)
  const extra = fields.filter((f) => f.more)

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
          <button type="button" className="btn btn-primary" onClick={startAdd}>+ Add item</button>
        </div>
      </div>
      {error && !draft && <p className="error">{error}</p>}
      {items.length === 0 && (
        <div className="empty">
          <p>Nothing documented in this section yet.</p>
          <button type="button" className="btn btn-primary" onClick={startAdd}>{addFirstLabel}</button>
        </div>
      )}
      {items.length > 0 && filtered.length === 0 && <div className="empty">No items match the current filter.</div>}
      {filtered.map((item) => (
        <article className={`item-card ${usesNa && item.needsAttention ? 'needs-attention' : ''}`} key={String(item.id)}>
          <div>
            <div className="item-title">
              <h3>{titleOf(item)}</h3>
              {(badgesOf?.(item) || []).map((b) => b && <StatusBadge key={b} value={b} />)}
              {usesNa && item.needsAttention ? <span className="warn" title="Needs attention">⚠</span> : null}
              <div className="item-title-actions">
                <button type="button" className="btn btn-ghost-dark" onClick={() => { setError(null); setDraft({ ...item }) }}>Edit</button>
                <button type="button" className="btn btn-danger" onClick={() => setPendingDelete(String(item.id))}>Delete</button>
              </div>
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
        <Modal title={title} busy={saving} error={error} onSave={() => void save()} onClose={() => { setDraft(null); setError(null) }}>
          {renderFieldGrid(primary, draft, setDraft, options)}
          {extra.length > 0 && (
            <details className="more-fields" open={extraHasValues(extra, draft)}>
              <summary>More fields</summary>
              {renderFieldGrid(extra, draft, setDraft, options)}
            </details>
          )}
        </Modal>
      )}
      {pendingDelete && (
        <ConfirmSheet
          title="Delete item"
          message="Delete this item? This cannot be undone."
          confirmLabel="Delete"
          danger
          onConfirm={() => void remove(pendingDelete)}
          onCancel={() => setPendingDelete(null)}
        />
      )}
    </>
  )
}

function extraHasValues(fields: FieldDef[], draft: Record<string, unknown>) {
  return fields.some((f) => {
    const v = draft[f.key]
    if (v === undefined || v === null || v === '' || v === false) return false
    return true
  })
}

function renderFieldGrid(
  fields: FieldDef[],
  draft: Record<string, unknown>,
  setDraft: (next: Record<string, unknown>) => void,
  options: Record<string, string[]>,
) {
  return (
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
  )
}
