import type { ReactNode } from 'react'

export function Modal({
  title, children, onSave, onClose, saveLabel = 'Save',
}: {
  title: string
  children: ReactNode
  onSave: () => void
  onClose: () => void
  saveLabel?: string
}) {
  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <header>
          <h3 style={{ margin: 0 }}>{title}</h3>
          <button type="button" className="btn btn-ghost-dark btn-sm" onClick={onClose}>Close</button>
        </header>
        <div className="body">{children}</div>
        <footer>
          <button type="button" className="btn btn-ghost-dark" onClick={onClose}>Cancel</button>
          <button type="button" className="btn btn-primary" onClick={onSave}>{saveLabel}</button>
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
