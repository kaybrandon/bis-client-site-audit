import { useEffect, useState } from 'react'
import { useOutletContext, useParams } from 'react-router-dom'
import { api } from '../api'
import { Field } from '../components/Modal'
import { PhotoPicker } from '../components/PhotoPicker'
import type { AuditOutlet } from '../components/AuditShell'
import { KEYS, type AuditContact, type AuditDetail, type ContactRole, type SubLocation } from '../types'

const emptyContact = (role: ContactRole, auditId: string): AuditContact => ({
  id: crypto.randomUUID(), auditId, role,
})

export function Overview() {
  const { id = '' } = useParams()
  const { audit, reload } = useOutletContext<AuditOutlet>()
  const [form, setForm] = useState<AuditDetail | null>(null)
  const [industries, setIndustries] = useState<string[]>([])
  const [statuses, setStatuses] = useState<string[]>([])
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    void api.dropdown(KEYS.auditIndustry).then(setIndustries)
    void api.dropdown(KEYS.auditStatus).then(setStatuses)
  }, [])

  useEffect(() => {
    if (!audit) return
    if (error || saving) return
    const contacts = (['Primary', 'Technical', 'Billing'] as ContactRole[]).map(
      (role) => audit.contacts.find((c) => c.role === role) || emptyContact(role, id),
    )
    setForm({
      ...audit,
      contacts,
      subLocations: audit.subLocations.length ? audit.subLocations : [{ id: crypto.randomUUID(), auditId: id, name: '' }],
    })
  }, [audit, id, error, saving])

  if (!form) return <p>Loading…</p>

  const set = (patch: Partial<AuditDetail>) => setForm({ ...form, ...patch })
  const contact = (role: ContactRole) => form.contacts.find((c) => c.role === role)!
  const setContact = (role: ContactRole, patch: Partial<AuditContact>) =>
    set({ contacts: form.contacts.map((c) => c.role === role ? { ...c, ...patch } : c) })

  const save = async () => {
    setSaving(true)
    setError(null)
    try {
      await api.saveOverview(id, form)
      setMessage('Saved.')
      reload()
    } catch (e) {
      setMessage(null)
      setError(e instanceof Error ? e.message : 'Save failed. Check Wi-Fi and try again.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <div className="toolbar">
        <div>
          <h2>Overview</h2>
          <p className="count">Client profile, contacts, and engagement scope.</p>
        </div>
        <div className="toolbar-actions">
          <button type="button" className="btn btn-primary" disabled={saving} onClick={() => void save()}>
            {saving ? 'Saving…' : 'Save'}
          </button>
          {error && (
            <button type="button" className="btn btn-primary" disabled={saving} onClick={() => void save()}>
              Retry
            </button>
          )}
        </div>
      </div>
      {message && <p className="muted">{message}</p>}
      {error && <p className="error">{error}</p>}
      <div className="panel" style={{ padding: 20 }}>
        <div className="form-grid">
          <div className="full client-photo-row">
            <div className="client-thumb client-thumb-lg">
              {form.clientPhotoPath ? <img src={api.photoSrc(form.clientPhotoPath)} alt="Client" /> : <span>🏢</span>}
            </div>
            <PhotoPicker
              allowMultiple={false}
              compact
              cameraLabel="Take client photo"
              libraryLabel="Choose photo"
              upload={async (file) => {
                const photo = await api.uploadPhoto(id, file, { category: 'Site Photo', ownerType: 'Audit', caption: 'Client photo' })
                const next = { ...form, clientPhotoPath: photo.relativePath }
                setForm(next)
                await api.saveOverview(id, next)
                reload()
              }}
            />
          </div>
          <Field label="Company Name"><input value={form.companyName} onChange={(e) => set({ companyName: e.target.value })} /></Field>
          <Field label="Industry">
            <select value={form.industry} onChange={(e) => set({ industry: e.target.value })}>
              {industries.map((o) => <option key={o}>{o}</option>)}
            </select>
          </Field>
          <Field label="Employee Count">
            <input type="number" value={form.employeeCount ?? ''} onChange={(e) => set({ employeeCount: e.target.value === '' ? undefined : Number(e.target.value) })} />
          </Field>
          <Field label="Audit Status">
            <select value={form.status} onChange={(e) => set({ status: e.target.value })}>
              {statuses.map((o) => <option key={o}>{o}</option>)}
            </select>
          </Field>
          <Field label="Address" full><input value={form.address ?? ''} onChange={(e) => set({ address: e.target.value })} /></Field>
          <Field label="Prepared By"><input value={form.preparedBy ?? ''} onChange={(e) => set({ preparedBy: e.target.value })} /></Field>
          <Field label="Audit Date">
            <input type="date" value={form.auditDate?.slice(0, 10) ?? ''} onChange={(e) => set({ auditDate: e.target.value })} />
          </Field>
          <Field label="Previous IT Support" full><textarea value={form.previousItSupport ?? ''} onChange={(e) => set({ previousItSupport: e.target.value })} /></Field>
          <Field label="Audit Scope" full><textarea value={form.auditScope ?? ''} onChange={(e) => set({ auditScope: e.target.value })} /></Field>
          <Field label="Executive Summary" full><textarea value={form.executiveSummary ?? ''} onChange={(e) => set({ executiveSummary: e.target.value })} /></Field>
        </div>
        <h3 style={{ margin: '22px 0 10px' }}>Contacts</h3>
        {(['Primary', 'Technical', 'Billing'] as ContactRole[]).map((role) => {
          const c = contact(role)
          return (
            <div key={role}>
              <h4 style={{ margin: '14px 0 8px' }}>{role} contact</h4>
              <div className="form-grid">
                <Field label="Name"><input value={c.name ?? ''} onChange={(e) => setContact(role, { name: e.target.value })} /></Field>
                <Field label="Title"><input value={c.title ?? ''} onChange={(e) => setContact(role, { title: e.target.value })} /></Field>
                <Field label="Email"><input type="email" inputMode="email" autoComplete="email" value={c.email ?? ''} onChange={(e) => setContact(role, { email: e.target.value })} /></Field>
                <Field label="Phone"><input type="tel" inputMode="tel" autoComplete="tel" value={c.phone ?? ''} onChange={(e) => setContact(role, { phone: e.target.value })} /></Field>
              </div>
            </div>
          )
        })}
        <h3 style={{ margin: '22px 0 10px' }}>Sub-locations</h3>
        {form.subLocations.map((loc, i) => (
          <div className="form-grid" style={{ marginBottom: 10 }} key={loc.id || i}>
            <Field label="Name"><input value={loc.name} onChange={(e) => {
              const next = form.subLocations.map((l, idx) => idx === i ? { ...l, name: e.target.value } : l)
              set({ subLocations: next })
            }} /></Field>
            <Field label="Address"><input value={loc.address ?? ''} onChange={(e) => {
              const next = form.subLocations.map((l, idx) => idx === i ? { ...l, address: e.target.value } : l)
              set({ subLocations: next })
            }} /></Field>
            <Field label="Notes" full><input value={loc.notes ?? ''} onChange={(e) => {
              const next = form.subLocations.map((l, idx) => idx === i ? { ...l, notes: e.target.value } : l)
              set({ subLocations: next })
            }} /></Field>
          </div>
        ))}
        <button type="button" className="btn btn-ghost-dark" onClick={() => set({ subLocations: [...form.subLocations, { id: crypto.randomUUID(), auditId: id, name: '' } as SubLocation] })}>
          + Add sub-location
        </button>
      </div>
    </>
  )
}
