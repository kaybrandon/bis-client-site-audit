import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth'
import { TakePhotoButton } from './TakePhotoButton'

export function Header({ onNewAudit }: { onNewAudit?: () => void }) {
  const { logout } = useAuth()
  const nav = useNavigate()
  return (
    <header className="app-header">
      <div className="header-primary">
        <Link className="app-brand" to="/">
          <span className="brand-mark">🛡️</span>
          <div>
            <h1>BIS Client IT Audit</h1>
            <p>Infrastructure assurance workspace</p>
          </div>
        </Link>
        <TakePhotoButton />
      </div>
      <div className="header-actions">
        <Link className="btn btn-ghost" to="/reports">Reports</Link>
        <Link className="btn btn-ghost" to="/settings">Settings</Link>
        {onNewAudit
          ? <button type="button" className="btn btn-light" onClick={onNewAudit}>+ New audit</button>
          : <Link className="btn btn-light" to="/?new=1">+ New audit</Link>}
        <button type="button" className="btn btn-ghost" onClick={() => { logout(); nav('/login') }}>Sign out</button>
      </div>
    </header>
  )
}
