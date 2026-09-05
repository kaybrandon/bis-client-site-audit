import type { ReactNode } from 'react'

export function Modal({
  title, children, onSave, onClose, saveLabel = 'Save', busy = false, error,
}: {
  title: string
  children: ReactNode
  onSave: () => void
  onClose: () => void
  saveLabel?: string
  busy?: boolean
  error?: string | null
}) {
  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()} role="dialog" aria-modal="true" aria-labelledby="modal-title">
        <header>
          <h3 id="modal-title" style={{ margin: 0 }}>{title}</h3>
          <button type="button" className="btn btn-ghost-dark" onClick={onClose}>Close</button>
        </header>
        <div className="body">{children}</div>
        {error && <p className="error modal-error">{error}</p>}
        <footer>
          <button type="button" className="btn btn-ghost-dark" disabled={busy} onClick={onClose}>Cancel</button>
          {error && (
            <button type="button" className="btn btn-primary" disabled={busy} onClick={onSave}>
              {busy ? 'Saving…' : 'Retry'}
            </button>
          )}
          <button type="button" className="btn btn-primary" disabled={busy} onClick={onSave}>
            {busy ? 'Saving…' : saveLabel}
          </button>
        </footer>
      </div>
    </div>
  )
}

export function Field({
  label, children, full, check,
}: { label: string; children: ReactNode; full?: boolean; check?: boolean }) {
  return <label className={`fld ${full ? 'full' : ''} ${check ? 'check' : ''}`}>{label}{children}</label>
}
