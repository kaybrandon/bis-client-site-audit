import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { api } from '../api'
import { KEYS, type SitePhoto } from '../types'

export function Photos() {
  const { id = '' } = useParams()
  const [items, setItems] = useState<SitePhoto[]>([])
  const [categories, setCategories] = useState<string[]>([])
  const [filter, setFilter] = useState('')
  const [uploadCategory, setUploadCategory] = useState('Site Photo')

  const load = () => api.photos(id).then(setItems)

  useEffect(() => {
    void load()
    void api.dropdown(KEYS.photoCategory).then((c) => {
      setCategories(c)
      if (c.includes('Site Photo')) setUploadCategory('Site Photo')
    })
  }, [id])

  const upload = async (file?: File) => {
    if (!file) return
    await api.uploadPhoto(id, file, { category: uploadCategory, ownerType: 'Audit' })
    await load()
  }

  const filtered = items.filter((p) => !filter || p.category === filter)

  return (
    <>
      <div className="toolbar">
        <div>
          <h2>Site Photos</h2>
          <p className="count">{items.length} photos in the global library (including item attachments)</p>
        </div>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center' }}>
          <select className="form-select" value={filter} onChange={(e) => setFilter(e.target.value)}>
            <option value="">All categories</option>
            {categories.map((c) => <option key={c}>{c}</option>)}
          </select>
          <select className="form-select" value={uploadCategory} onChange={(e) => setUploadCategory(e.target.value)}>
            {categories.map((c) => <option key={c}>{c}</option>)}
          </select>
          <label className="btn btn-primary">
            + Add photo
            <input type="file" accept="image/*" hidden onChange={(e) => void upload(e.target.files?.[0])} />
          </label>
        </div>
      </div>
      <p className="muted">Upload site-level photos here. Item cards across the audit also attach photos; they appear in this library.</p>
      {filtered.length === 0 ? <div className="empty">No photos yet.</div> : (
        <div className="photo-grid">
          {filtered.map((photo) => (
            <figure className="photo-tile" key={photo.id}>
              <img src={api.photoSrc(photo.publicUrl || photo.relativePath)} alt={photo.caption} />
              <figcaption>
                <strong>{photo.category}</strong><br />
                {photo.ownerType}{photo.ownerId ? ' attachment' : ''}<br />
                {photo.fileName}
                <div style={{ marginTop: 6 }}>
                  <button type="button" className="btn btn-danger btn-sm" onClick={() => {
                    if (confirm('Delete this photo?')) void api.deletePhoto(photo.id).then(load)
                  }}>Delete</button>
                </div>
              </figcaption>
            </figure>
          ))}
        </div>
      )}
    </>
  )
}
