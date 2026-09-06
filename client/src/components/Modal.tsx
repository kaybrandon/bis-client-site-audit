import type { ReactNode } from 'react'
import { TakePhotoButton } from './TakePhotoButton'

export function Modal({
  title, children, onSave, onClose, saveLabel = 'Save', busy = false, error, auditId,
}: {
  title: string
  children: ReactNode
  onSave: () => void
  onClose: () => void
  saveLabel?: string
  busy?: boolean
  error?: string | null
  /** When set, mobile sheets show Take photo in the header (Title | Take photo | Close). */
  auditId?: string
}) {
  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()} role="dialog" aria-modal="true" aria-labelledby="modal-title">
        <header className={auditId ? 'has-take-photo' : undefined}>
          <h3 id="modal-title" style={{ margin: 0 }}>{title}</h3>
          {auditId ? (
            <span className="modal-take-photo">
              <TakePhotoButton auditId={auditId} />
            </span>
          ) : null}
          <button type="button" className="btn btn-ghost-dark modal-close" onClick={onClose}>Close</button>
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
  label, children, full, check, error,
}: { label: string; children: ReactNode; full?: boolean; check?: boolean; error?: string | string[] }) {
  const message = Array.isArray(error) ? error.filter(Boolean).join(' ') : error
  return (
    <label className={`fld ${full ? 'full' : ''} ${check ? 'check' : ''}`}>
      {label}{children}
      {message ? <span className="field-error" role="alert">{message}</span> : null}
    </label>
  )
}
