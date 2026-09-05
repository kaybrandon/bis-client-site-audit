import { useState, type FormEvent } from 'react'
import { useAuth } from '../auth'

export function SessionResume() {
  const { email, expired, login } = useAuth()
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  if (!expired) return null

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await login(email || '', password)
      setPassword('')
    } catch {
      setError('Invalid email or password.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <div className="toast toast-warn" role="status">Session expired. Sign in to keep working — your draft is still here.</div>
      <div className="modal-backdrop session-resume" onClick={(e) => e.stopPropagation()}>
        <form className="modal-card session-card" onSubmit={(e) => void submit(e)}>
          <header>
            <h3 id="session-title" style={{ margin: 0 }}>Sign in again</h3>
          </header>
          <div className="body">
            <p className="muted">Your session timed out. Sign in to continue. Unsaved fields stay on this page.</p>
            {error && <p className="error">{error}</p>}
            <label className="fld">Email
              <input type="email" value={email || ''} readOnly autoComplete="username" />
            </label>
            <label className="fld" style={{ marginTop: 12 }}>Password
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" autoFocus />
            </label>
          </div>
          <footer>
            <button type="submit" className="btn btn-primary" disabled={busy || !password}>
              {busy ? 'Signing in…' : 'Resume'}
            </button>
          </footer>
        </form>
      </div>
    </>
  )
}
