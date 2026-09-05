import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth'

export function Login() {
  const { email, login } = useAuth()
  const nav = useNavigate()
  const [form, setForm] = useState({ email: 'admin@bis.local', password: '' })
  const [error, setError] = useState<string | null>(null)

  if (email) return <Navigate to="/" replace />

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    try {
      await login(form.email, form.password)
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
        <label className="fld">Email
          <input type="email" inputMode="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} autoComplete="username" />
        </label>
        <label className="fld" style={{ marginTop: 12 }}>Password
          <input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} autoComplete="current-password" />
        </label>
        <button type="submit" className="btn btn-primary" style={{ width: '100%', justifyContent: 'center', marginTop: 18 }}>Log in</button>
      </form>
    </div>
  )
}
