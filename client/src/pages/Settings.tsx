import { useEffect, useState } from 'react'
import { API, ForbiddenError, api, sessionToken } from '../api'
import { SettingsShell } from '../components/SettingsShell'
import { useAuth } from '../auth'
import type { DropdownList } from '../types'

type Draft = DropdownList & { newValue: string; message?: string }

function swaggerHref() {
  const origin = API.replace(/\/$/, '')
  return `${origin}/swagger`
}

function itemCountLabel(count: number) {
  return count === 1 ? '1 item' : `${count} items`
}

export function Settings() {
  const { isAdmin } = useAuth()
  const [lists, setLists] = useState<Draft[]>([])
  const [openId, setOpenId] = useState<string | null>(null)

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

  const toggle = (id: string) => {
    setOpenId((current) => current === id ? null : id)
  }

  return (
    <SettingsShell>
      {isAdmin && <SwaggerAdminPanel />}
      <p className="muted settings-lists-intro">
        Lists start collapsed. Only one list is open at a time — add or remove options, then save.
      </p>
      <div className="settings-lists">
        {lists.map((list) => {
          const open = openId === list.id
          const panelId = `dropdown-list-${list.id}`
          return (
            <section className={`settings-accordion${open ? ' is-open' : ''}`} key={list.id}>
              <button
                type="button"
                className="settings-accordion-toggle"
                aria-expanded={open}
                aria-controls={panelId}
                onClick={() => toggle(list.id)}
              >
                <h3>{list.displayName}</h3>
                <span className="settings-accordion-count">{itemCountLabel(list.options.length)}</span>
                <span className="settings-accordion-chevron" aria-hidden="true">{open ? '▾' : '▸'}</span>
              </button>
              {open && (
                <div className="settings-accordion-body" id={panelId}>
                  <ul className="settings-option-list">
                    {list.options.map((option) => (
                      <li className="settings-option-row" key={option}>
                        <span className="settings-option-label">{option}</span>
                        <button
                          type="button"
                          className="settings-option-remove"
                          title="Remove"
                          aria-label={`Remove ${option}`}
                          onClick={() => remove(list, option)}
                        >
                          ×
                        </button>
                      </li>
                    ))}
                  </ul>
                  <div className="settings-option-add">
                    <input
                      value={list.newValue}
                      onChange={(e) => setLists((all) => all.map((x) => x.id === list.id ? { ...x, newValue: e.target.value } : x))}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                          e.preventDefault()
                          add(list)
                        }
                      }}
                      placeholder="Add option"
                      aria-label={`Add option to ${list.displayName}`}
                    />
                    <button type="button" className="btn btn-ghost-dark" onClick={() => add(list)}>+ Add</button>
                  </div>
                  <div className="settings-accordion-actions">
                    <button type="button" className="btn btn-primary" onClick={() => void save(list)}>Save</button>
                  </div>
                  {list.message && <p className={list.message === 'Saved.' ? 'muted' : 'error'} style={{ marginTop: 10 }}>{list.message}</p>}
                </div>
              )}
            </section>
          )
        })}
      </div>
    </SettingsShell>
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
    if (!copyMessage || copyMessage !== 'Copied.') return
    const id = window.setTimeout(() => setCopyMessage(null), 2000)
    return () => window.clearTimeout(id)
  }, [copyMessage])

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
      setCopyMessage('Copied.')
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
        setCopyMessage('Copied.')
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
          <a className="btn btn-primary" href={swaggerHref()} target="_blank" rel="noreferrer">
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
