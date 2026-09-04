import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api'
import type { DropdownList } from '../types'

type Draft = DropdownList & { newValue: string; message?: string }

export function Settings() {
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
    await api.saveDropdown(list.id, list.options)
    setLists((all) => all.map((x) => x.id === list.id ? { ...x, message: 'Saved.' } : x))
  }

  return (
    <div className="page-shell">
      <header className="app-header">
        <div className="app-brand">
          <Link className="btn btn-ghost btn-icon" to="/">←</Link>
          <div>
            <h1>Dropdown Settings</h1>
            <p>Edit the options available in dropdown menus across all audit modules.</p>
          </div>
        </div>
      </header>
      <div className="content">
        <p className="muted" style={{ marginBottom: 18 }}>
          Changes apply immediately to new and existing records. One panel per list — add, remove, then save.
        </p>
        {lists.map((list) => (
          <section className="panel" style={{ padding: '18px 20px', marginBottom: 16 }} key={list.id}>
            <div className="toolbar">
              <h3 style={{ margin: 0 }}>{list.displayName}</h3>
              <button type="button" className="btn btn-primary btn-sm" onClick={() => void save(list)}>💾 Save</button>
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
            {list.message && <p className="muted" style={{ marginTop: 10 }}>{list.message}</p>}
          </section>
        ))}
      </div>
    </div>
  )
}
