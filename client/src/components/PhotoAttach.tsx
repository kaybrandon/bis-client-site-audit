import { useEffect, useState } from 'react'
import { api } from '../api'
import type { SitePhoto } from '../types'

export function PhotoAttach({
  auditId, ownerId, ownerType, category,
}: {
  auditId: string
  ownerId?: string
  ownerType: string
  category: string
}) {
  const [items, setItems] = useState<SitePhoto[]>([])
  const [open, setOpen] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = async () => {
    const all = await api.photos(auditId)
    setItems(all.filter((p) => p.ownerType === ownerType && (p.ownerId || '') === (ownerId || '')))
  }

  useEffect(() => { void load() }, [auditId, ownerId, ownerType])

  const upload = async (file?: File) => {
    if (!file) return
    setError(null)
    try {
      await api.uploadPhoto(auditId, file, { category, ownerType, ownerId })
      setOpen(true)
      await load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Upload failed')
    }
  }

  return (
    <div>
      <div className="item-actions">
        <label className="btn btn-ghost-dark btn-sm">
          📷 Take photo
          <input type="file" accept="image/*" hidden onChange={(e) => void upload(e.target.files?.[0])} />
        </label>
        <button type="button" className="btn btn-ghost-dark btn-icon" title="Gallery" onClick={() => setOpen((v) => !v)}>🖼</button>
      </div>
      {error && <p className="error">{error}</p>}
      {open && (
        <div className="photo-grid" style={{ marginTop: 10, minWidth: 240 }}>
          {items.length === 0 && <p className="muted">No photos yet.</p>}
          {items.map((p) => (
            <figure className="photo-tile" key={p.id}>
              <img src={api.photoSrc(p.publicUrl || p.relativePath)} alt={p.caption || p.fileName} />
              <figcaption>
                {p.fileName}
                <button type="button" className="btn btn-danger btn-sm" onClick={() => void api.deletePhoto(p.id).then(load)}>Delete</button>
              </figcaption>
            </figure>
          ))}
        </div>
      )}
    </div>
  )
}
