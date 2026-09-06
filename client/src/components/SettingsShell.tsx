import type { ReactNode } from 'react'
import { Link, NavLink } from 'react-router-dom'
import { useAuth } from '../auth'

export function SettingsShell({ children }: { children: ReactNode }) {
  const { isAdmin } = useAuth()
  return (
    <div className="page-shell">
      <header className="app-header">
        <div className="app-brand">
          <Link className="btn btn-ghost btn-icon" to="/" aria-label="Back to dashboard">←</Link>
          <div>
            <h1>Settings</h1>
            <p>{isAdmin ? 'Dropdown lists and workspace accounts' : 'Edit dropdown options used across audit modules.'}</p>
          </div>
        </div>
      </header>
      <div className="content">
        {isAdmin && (
          <nav className="settings-nav" aria-label="Settings sections">
            <NavLink className={({ isActive }) => `settings-nav-link${isActive ? ' is-active' : ''}`} to="/settings" end>
              Dropdown lists
            </NavLink>
            <NavLink className={({ isActive }) => `settings-nav-link${isActive ? ' is-active' : ''}`} to="/settings/users">
              Users
            </NavLink>
          </nav>
        )}
        {children}
      </div>
    </div>
  )
}
