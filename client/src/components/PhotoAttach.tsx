import { useEffect, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { api } from '../api'
import type { SitePhoto } from '../types'
import type { AuditOutlet } from './AuditShell'
import { PhotoPicker } from './PhotoPicker'

export function PhotoAttach({
  auditId, ownerId, ownerType, category,
}: {
  auditId: string
  ownerId?: string
  ownerType: string
  category: string
}) {
  const { reload } = useOutletContext<AuditOutlet>()
  const [items, setItems] = useState<SitePhoto[]>([])
  const [open, setOpen] = useState(false)

  const load = async () => {
    const all = await api.photos(auditId)
    setItems(all.filter((p) => p.ownerType === ownerType && (p.ownerId || '') === (ownerId || '')))
  }

  useEffect(() => { void load() }, [auditId, ownerId, ownerType])

  return (
    <div className="photo-attach">
      <PhotoPicker
        allowMultiple
        compact
        cameraLabel="Take photo"
        libraryLabel="From library"
        upload={async (file) => {
          await api.uploadPhoto(auditId, file, { category, ownerType, ownerId })
          setOpen(true)
          await load()
          await reload()
        }}
      />
      <button type="button" className="btn btn-ghost-dark" onClick={() => setOpen((v) => !v)}>
        {open ? 'Hide photos' : `Photos (${items.length})`}
      </button>
      {open && (
        <div className="photo-grid" style={{ marginTop: 10 }}>
          {items.length === 0 && <p className="muted">No photos yet.</p>}
          {items.map((p) => (
            <figure className="photo-tile" key={p.id}>
              <img src={api.photoSrc(p.publicUrl || p.relativePath)} alt={p.caption || p.fileName} />
              <figcaption>
                {p.fileName}
                <button type="button" className="btn btn-danger" onClick={() => void api.deletePhoto(p.id).then(() => { void load(); void reload() })}>
                  Delete
                </button>
              </figcaption>
            </figure>
          ))}
        </div>
      )}
    </div>
  )
}
