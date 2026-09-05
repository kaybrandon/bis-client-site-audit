import { useEffect, useState } from 'react'
import { useOutletContext, useParams } from 'react-router-dom'
import { api } from '../api'
import type { AuditOutlet } from '../components/AuditShell'
import { ConfirmSheet } from '../components/ConfirmSheet'
import { PhotoPicker } from '../components/PhotoPicker'
import { KEYS, type SitePhoto } from '../types'

export function Photos() {
  const { id = '' } = useParams()
  const { reload } = useOutletContext<AuditOutlet>()
  const [items, setItems] = useState<SitePhoto[]>([])
  const [categories, setCategories] = useState<string[]>([])
  const [filter, setFilter] = useState('')
  const [uploadCategory, setUploadCategory] = useState('Site Photo')
  const [pendingDelete, setPendingDelete] = useState<string | null>(null)

  const load = () => api.photos(id).then(setItems)

  useEffect(() => {
    void load()
    void api.dropdown(KEYS.photoCategory).then((c) => {
      setCategories(c)
      if (c.includes('Site Photo')) setUploadCategory('Site Photo')
    })
  }, [id])

  const filtered = items.filter((p) => !filter || p.category === filter)

  return (
    <>
      <div className="toolbar">
        <div>
          <h2>Site Photos</h2>
          <p className="count">{items.length} photos in the global library (including item attachments)</p>
        </div>
        <div className="toolbar-filters">
          <label className="fld">
            Filter
            <select className="form-select" value={filter} onChange={(e) => setFilter(e.target.value)}>
              <option value="">All categories</option>
              {categories.map((c) => <option key={c}>{c}</option>)}
            </select>
          </label>
          <label className="fld">
            New photo category
            <select className="form-select" value={uploadCategory} onChange={(e) => setUploadCategory(e.target.value)}>
              {categories.map((c) => <option key={c}>{c}</option>)}
            </select>
          </label>
        </div>
      </div>
      <div className="panel photo-upload-panel">
        <p className="muted" style={{ marginTop: 0 }}>
          Take a photo on a phone or tablet, or pick from the library. Item-card attachments also show here.
        </p>
        <PhotoPicker
          showCaption
          upload={async (file, caption) => {
            await api.uploadPhoto(id, file, { category: uploadCategory, ownerType: 'Audit', caption })
            await load()
            await reload()
          }}
        />
      </div>
      {filtered.length === 0 ? (
        <div className="empty">
          <p>{items.length === 0 ? 'No photos yet. Take a photo or pick from the library above.' : 'No photos in this category.'}</p>
        </div>
      ) : (
        <div className="photo-grid">
          {filtered.map((photo) => (
            <figure className="photo-tile" key={photo.id}>
              <img src={api.photoSrc(photo.publicUrl || photo.relativePath)} alt={photo.caption || photo.fileName} />
              <figcaption>
                <strong>{photo.category}</strong><br />
                {photo.caption ? <>{photo.caption}<br /></> : null}
                {photo.ownerType}{photo.ownerId ? ' attachment' : ''}<br />
                {photo.fileName}
                <div className="photo-tile-actions">
                  <button type="button" className="btn btn-danger" onClick={() => setPendingDelete(photo.id)}>Delete</button>
                </div>
              </figcaption>
            </figure>
          ))}
        </div>
      )}
      {pendingDelete && (
        <ConfirmSheet
          title="Delete photo"
          message="Delete this photo? This cannot be undone."
          confirmLabel="Delete"
          danger
          onConfirm={() => {
            void api.deletePhoto(pendingDelete).then(() => { void load(); void reload() })
            setPendingDelete(null)
          }}
          onCancel={() => setPendingDelete(null)}
        />
      )}
    </>
  )
}
