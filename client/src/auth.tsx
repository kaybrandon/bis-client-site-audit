import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, setAuthExpiredHandler } from './api'

type AuthState = {
  email: string | null
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
  const [expired, setExpired] = useState(false)

  useEffect(() => {
    setAuthExpiredHandler(() => setExpired(true))
    return () => setAuthExpiredHandler(null)
  }, [])

  const value = useMemo<AuthState>(() => ({
    email,
    expired,
    ready: true,
    login: async (e, p) => {
      const result = await api.login(e, p)
      localStorage.setItem('bis.token', result.token)
      localStorage.setItem('bis.email', result.email)
      setEmail(result.email)
      setExpired(false)
    },
    logout: () => {
      localStorage.removeItem('bis.token')
      localStorage.removeItem('bis.email')
      setEmail(null)
      setExpired(false)
    },
  }), [email, expired])

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>
}

export function useAuth() {
  const ctx = useContext(Ctx)
  if (!ctx) throw new Error('AuthProvider missing')
  return ctx
}
