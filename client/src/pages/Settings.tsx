import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ForbiddenError, api, sessionToken } from '../api'
import { useAuth } from '../auth'
import type { DropdownList } from '../types'

type Draft = DropdownList & { newValue: string; message?: string }

export function Settings() {
  const { isAdmin } = useAuth()
  const [lists, setLists] = useState<Draft[]>([])

  useEffect(() => {
    void api.dropdowns().then((rows) => setLists(rows.map((l) => ({ ...l, newValue: '' }))))
  }, [])

  const add = (list: Draft) => {
    const v = list.newValue.trim()
    if (!v || list.options.some((o) => o.toLowerCase() === v.toLowerCase())) return
    setLists((all) => all.map((x) => x.id === list.id ? { ...x, options: [...x.options, v], newValue: '' } : x))
  }

  const remove = (list: Draft, option: string) => {
    setLists((all) => all.map((x) => x.id === list.id ? { ...x, options: x.options.filter((o) => o !== option) } : x))
  }

  const save = async (list: Draft) => {
    try {
      await api.saveDropdown(list.id, list.options)
      setLists((all) => all.map((x) => x.id === list.id ? { ...x, message: 'Saved.' } : x))
    } catch (e) {
      const message = e instanceof Error ? e.message : 'Save failed. Check Wi-Fi and try again.'
      setLists((all) => all.map((x) => x.id === list.id ? { ...x, message } : x))
    }
  }

  return (
    <div className="page-shell">
      <header className="app-header">
        <div className="app-brand">
          <Link className="btn btn-ghost btn-icon" to="/">←</Link>
          <div>
            <h1>Settings</h1>
            <p>Dropdown lists{isAdmin ? ' and API documentation' : ''}.</p>
          </div>
        </div>
      </header>
      <div className="content">
        {isAdmin && <SwaggerAdminPanel />}
        <div className="section-head">
          <div>
            <h2>Dropdown lists</h2>
            <p className="muted">
              Changes apply immediately to new and existing records. One panel per list — add, remove, then save.
            </p>
          </div>
        </div>
        {lists.map((list) => (
          <section className="panel" style={{ padding: '18px 20px', marginBottom: 16 }} key={list.id}>
            <div className="toolbar">
              <h3 style={{ margin: 0 }}>{list.displayName}</h3>
              <button type="button" className="btn btn-primary" onClick={() => void save(list)}>Save</button>
            </div>
            <div className="chip-row">
              {list.options.map((option) => (
                <span className="chip" key={option}>
                  {option}
                  <button type="button" title="Remove" onClick={() => remove(list, option)}>×</button>
                </span>
              ))}
              <span className="chip chip-add">
                <input
                  value={list.newValue}
                  onChange={(e) => setLists((all) => all.map((x) => x.id === list.id ? { ...x, newValue: e.target.value } : x))}
                  placeholder="Add option"
                  style={{ border: 0, background: 'transparent', width: 140 }}
                />
                <button type="button" onClick={() => add(list)}>+ Add</button>
              </span>
            </div>
            {list.message && <p className={list.message === 'Saved.' ? 'muted' : 'error'} style={{ marginTop: 10 }}>{list.message}</p>}
          </section>
        ))}
      </div>
    </div>
  )
}

function SwaggerAdminPanel() {
  const [enabled, setEnabled] = useState(false)
  const [ready, setReady] = useState(false)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [copyMessage, setCopyMessage] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    void api.swaggerSetting().then((row) => {
      if (cancelled) return
      setEnabled(row.enabled)
      setLoadError(null)
      setReady(true)
    }).catch((e) => {
      if (cancelled) return
      if (e instanceof ForbiddenError) return
      setLoadError(e instanceof Error ? e.message : 'Could not load Swagger setting.')
      setReady(true)
    })
    return () => { cancelled = true }
  }, [])

  const toggle = async () => {
    setBusy(true)
    setSaveError(null)
    const next = !enabled
    try {
      const row = await api.saveSwaggerSetting(next)
      setEnabled(row.enabled)
    } catch (e) {
      if (e instanceof ForbiddenError) {
        setSaveError('Admins only.')
        return
      }
      setSaveError(e instanceof Error ? e.message : 'Could not update Swagger.')
    } finally {
      setBusy(false)
    }
  }

  const copyToken = async () => {
    const value = sessionToken()
    if (!value) {
      setCopyMessage('Sign in again, then copy the token.')
      return
    }
    try {
      if (navigator.clipboard?.writeText)
        await navigator.clipboard.writeText(value)
      else
        throw new Error('clipboard unavailable')
      setCopyMessage('Copied. Paste it into Swagger Authorize (token only).')
    } catch {
      try {
        const field = document.createElement('textarea')
        field.value = value
        field.setAttribute('readonly', '')
        field.style.position = 'fixed'
        field.style.left = '-9999px'
        document.body.appendChild(field)
        field.select()
        const ok = document.execCommand('copy')
        document.body.removeChild(field)
        if (!ok) throw new Error('copy command failed')
        setCopyMessage('Copied. Paste it into Swagger Authorize (token only).')
      } catch {
        setCopyMessage('Could not copy. Check browser clipboard permission.')
      }
    }
  }

  return (
    <section className="panel swagger-panel" style={{ padding: '18px 20px', marginBottom: 20 }}>
      <div className="toolbar">
        <div>
          <h3 style={{ margin: 0 }}>API documentation (Swagger)</h3>
          <p className="muted">Admins only. Stored in the database — no App Setting change needed.</p>
        </div>
      </div>
      {loadError && <p className="error">{loadError}</p>}
      <div className="swagger-controls">
        <button
          type="button"
          className={`setting-toggle${enabled ? ' is-on' : ''}`}
          role="switch"
          aria-checked={enabled}
          aria-label="Enable Swagger UI"
          disabled={busy || !ready}
          onClick={() => void toggle()}
        >
          <span className="setting-toggle-track" aria-hidden="true">
            <span className="setting-toggle-thumb" />
          </span>
          <span>Enable Swagger UI</span>
          <span className="badge">{enabled ? 'On' : 'Off'}</span>
        </button>
        <button type="button" className="btn btn-ghost-dark" onClick={() => void copyToken()}>
          Copy Bearer token
        </button>
        {enabled && (
          <a className="btn btn-primary" href="/swagger" target="_blank" rel="noreferrer">
            Open Swagger UI
          </a>
        )}
      </div>
      <p className="muted" style={{ marginTop: 12 }}>
        When on, <code>/swagger</code> serves the UI. Authorize with the copied session JWT — this does not open anonymous API access.
        When off, <code>/swagger</code> returns 404.
      </p>
      {saveError && <p className="error" style={{ marginTop: 10 }}>{saveError}</p>}
      {copyMessage && <p className="muted" style={{ marginTop: 10 }}>{copyMessage}</p>}
    </section>
  )
}
