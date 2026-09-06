import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, setAuthExpiredHandler } from './api'

type AuthState = {
  email: string | null
  isAdmin: boolean
  expired: boolean
  ready: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
}

const Ctx = createContext<AuthState | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [email, setEmail] = useState<string | null>(() => {
    const t = localStorage.getItem('bis.token')
    return t ? localStorage.getItem('bis.email') : null
  })
  const [isAdmin, setIsAdmin] = useState(() => localStorage.getItem('bis.isAdmin') === '1')
  const [expired, setExpired] = useState(false)

  useEffect(() => {
    setAuthExpiredHandler(() => setExpired(true))
    return () => setAuthExpiredHandler(null)
  }, [])

  useEffect(() => {
    if (!localStorage.getItem('bis.token')) return
    void api.me().then((me) => {
      setEmail(me.email)
      setIsAdmin(!!me.isAdmin)
      localStorage.setItem('bis.email', me.email)
      localStorage.setItem('bis.isAdmin', me.isAdmin ? '1' : '0')
    }).catch(() => { /* expired handler covers 401 */ })
  }, [])

  const value = useMemo<AuthState>(() => ({
    email,
    isAdmin,
    expired,
    ready: true,
    login: async (e, p) => {
      const result = await api.login(e, p)
      localStorage.setItem('bis.token', result.token)
      localStorage.setItem('bis.email', result.email)
      localStorage.setItem('bis.isAdmin', result.isAdmin ? '1' : '0')
      setEmail(result.email)
      setIsAdmin(!!result.isAdmin)
      setExpired(false)
    },
    logout: () => {
      localStorage.removeItem('bis.token')
      localStorage.removeItem('bis.email')
      localStorage.removeItem('bis.isAdmin')
      setEmail(null)
      setIsAdmin(false)
      setExpired(false)
    },
  }), [email, isAdmin, expired])

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>
}

export function useAuth() {
  const ctx = useContext(Ctx)
  if (!ctx) throw new Error('AuthProvider missing')
  return ctx
}
