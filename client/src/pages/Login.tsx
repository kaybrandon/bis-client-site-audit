import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth'
import { persistRememberedEmail, readRememberedEmail } from '../rememberEmail'

export function Login() {
  const { email, login } = useAuth()
  const nav = useNavigate()
  const remembered = readRememberedEmail()
  const [form, setForm] = useState({
    email: remembered ?? 'admin@bis.local',
    password: '',
  })
  const [rememberEmail, setRememberEmail] = useState(!!remembered)
  const [error, setError] = useState<string | null>(null)

  if (email) return <Navigate to="/" replace />

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    try {
      await login(form.email, form.password)
      persistRememberedEmail(rememberEmail, form.email)
      nav('/')
    } catch {
      setError('Invalid email or password.')
    }
  }

  return (
    <div className="login-wrap">
      <form className="login-card" onSubmit={(e) => void submit(e)}>
        <p className="muted" style={{ margin: 0 }}>🛡️ BIS Client IT Audit</p>
        <h1>Infrastructure assurance workspace</h1>
        <p className="muted">Sign in with a local account. Windows Authentication and Entra ID can be enabled later.</p>
        {error && <p className="error">{error}</p>}
        <label className="fld" htmlFor="login-email">Email
          <input
            id="login-email"
            name="username"
            type="email"
            inputMode="email"
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
            autoComplete="username"
          />
        </label>
        <label className="fld" htmlFor="login-password" style={{ marginTop: 12 }}>Password
          <input
            id="login-password"
            name="password"
            type="password"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            autoComplete="current-password"
          />
        </label>
        <label className="check remember-email">
          <input
            type="checkbox"
            checked={rememberEmail}
            onChange={(e) => {
              const on = e.target.checked
              setRememberEmail(on)
              if (!on) persistRememberedEmail(false, '')
            }}
          />
          Remember email
        </label>
        <button type="submit" className="btn btn-primary" style={{ width: '100%', justifyContent: 'center', marginTop: 18 }}>Log in</button>
      </form>
    </div>
  )
}
