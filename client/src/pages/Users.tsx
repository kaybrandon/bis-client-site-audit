import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError, ForbiddenError, api } from '../api'
import { ConfirmSheet } from '../components/ConfirmSheet'
import { Header } from '../components/Header'
import { Field, Modal } from '../components/Modal'
import type { AppUser } from '../types'

type FieldKey = 'email' | 'password' | 'confirmPassword'
type FormErrors = Partial<Record<FieldKey, string[]>>

const emptyForm = { email: '', password: '', confirmPassword: '' }

function formatCreated(iso: string) {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

export function Users() {
  const nav = useNavigate()
  const [users, setUsers] = useState<AppUser[]>([])
  const [loadError, setLoadError] = useState<string | null>(null)
  const [showAdd, setShowAdd] = useState(false)
  const [form, setForm] = useState(emptyForm)
  const [fieldErrors, setFieldErrors] = useState<FormErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [pendingDisable, setPendingDisable] = useState<AppUser | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const load = async () => {
    try {
      setUsers(await api.users())
      setLoadError(null)
    } catch (e) {
      if (e instanceof ForbiddenError) {
        nav('/', { replace: true })
        return
      }
      setLoadError(e instanceof Error ? e.message : 'Could not load users.')
    }
  }

  useEffect(() => { void load() }, [])

  const enabledAdmins = users.filter((u) => u.isAdmin && u.active).length

  const openAdd = () => {
    setForm(emptyForm)
    setFieldErrors({})
    setFormError(null)
    setShowAdd(true)
  }

  const create = async () => {
    const next: FormErrors = {}
    if (!form.email.trim()) next.email = ['Email is required.']
    if (!form.password) next.password = ['Password is required.']
    if (form.password !== form.confirmPassword) next.confirmPassword = ['Password and confirmation do not match.']
    if (next.email || next.password || next.confirmPassword) {
      setFieldErrors(next)
      setFormError(null)
      return
    }

    setBusy(true)
    setFieldErrors({})
    setFormError(null)
    try {
      await api.createUser(form.email.trim(), form.password, form.confirmPassword)
      setShowAdd(false)
      setForm(emptyForm)
      await load()
    } catch (e) {
      if (e instanceof ForbiddenError) {
        nav('/', { replace: true })
        return
      }
      if (e instanceof ApiError) {
        setFieldErrors({
          email: e.errors.email,
          password: e.errors.password,
          confirmPassword: e.errors.confirmPassword,
        })
        setFormError(e.message)
      } else {
        setFormError(e instanceof Error ? e.message : 'Could not create user.')
      }
    } finally {
      setBusy(false)
    }
  }

  const disable = async (user: AppUser) => {
    setActionError(null)
    try {
      await api.disableUser(user.id)
      setPendingDisable(null)
      await load()
    } catch (e) {
      if (e instanceof ForbiddenError) {
        nav('/', { replace: true })
        return
      }
      setActionError(e instanceof Error ? e.message : 'Could not disable user.')
      setPendingDisable(null)
    }
  }

  const enable = async (user: AppUser) => {
    setActionError(null)
    try {
      await api.enableUser(user.id)
      await load()
    } catch (e) {
      if (e instanceof ForbiddenError) {
        nav('/', { replace: true })
        return
      }
      setActionError(e instanceof Error ? e.message : 'Could not re-enable user.')
    }
  }

  return (
    <div className="page-shell">
      <Header />
      <div className="content">
        <div className="section-head">
          <div>
            <h2>Users</h2>
            <p className="muted">Local accounts for the audit workspace. Admins only — no public sign-up.</p>
          </div>
          <button type="button" className="btn btn-primary" onClick={openAdd}>Add user</button>
        </div>
        {loadError && <p className="error">{loadError}</p>}
        {actionError && <p className="error">{actionError}</p>}
        {users.length === 0 && !loadError && <div className="empty">No users found.</div>}
        {users.length > 0 && (
          <div className="table-scroll panel" style={{ padding: '8px 16px 12px' }}>
            <table className="data users-table">
              <thead>
                <tr>
                  <th>Email</th>
                  <th>Status</th>
                  <th>Created</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {users.map((user) => {
                  const lastAdmin = user.isAdmin && user.active && enabledAdmins <= 1
                  return (
                    <tr key={user.id}>
                      <td>
                        <div className="user-email">
                          <span>{user.email}</span>
                          {user.isAdmin && <span className="badge">Admin</span>}
                        </div>
                      </td>
                      <td>
                        <span className={`badge ${user.active ? 'badge-active' : 'badge-disabled'}`}>
                          {user.active ? 'Active' : 'Disabled'}
                        </span>
                      </td>
                      <td>{formatCreated(user.createdAt)}</td>
                      <td>
                        <div className="card-actions">
                          {user.active
                            ? (
                                <button
                                  type="button"
                                  className="btn btn-danger"
                                  disabled={lastAdmin}
                                  title={lastAdmin ? 'Cannot disable the last remaining admin.' : 'Disable this user'}
                                  onClick={() => setPendingDisable(user)}
                                >
                                  Disable
                                </button>
                              )
                            : (
                                <button type="button" className="btn btn-ghost-dark" onClick={() => void enable(user)}>
                                  Re-enable
                                </button>
                              )}
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
      {showAdd && (
        <Modal
          title="Add user"
          saveLabel="Create user"
          busy={busy}
          error={formError && !fieldErrors.email && !fieldErrors.password && !fieldErrors.confirmPassword ? formError : null}
          onSave={() => void create()}
          onClose={() => { if (!busy) setShowAdd(false) }}
        >
          <div className="form-grid">
            <Field label="Email" full error={fieldErrors.email}>
              <input
                type="email"
                inputMode="email"
                autoComplete="off"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
              />
            </Field>
            <Field label="Password" full error={fieldErrors.password}>
              <input
                type="password"
                autoComplete="new-password"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
              />
            </Field>
            <Field label="Confirm password" full error={fieldErrors.confirmPassword}>
              <input
                type="password"
                autoComplete="new-password"
                value={form.confirmPassword}
                onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })}
              />
            </Field>
          </div>
          <p className="muted" style={{ marginTop: 12 }}>
            At least 8 characters, including upper and lower case, a number, and a symbol.
            The new user can sign in and use the same audit flows as other signed-in staff.
          </p>
        </Modal>
      )}
      {pendingDisable && (
        <ConfirmSheet
          title="Disable user"
          message={`${pendingDisable.email} will not be able to sign in until an admin re-enables the account.`}
          confirmLabel="Disable"
          danger
          onConfirm={() => void disable(pendingDisable)}
          onCancel={() => setPendingDisable(null)}
        />
      )}
    </div>
  )
}
