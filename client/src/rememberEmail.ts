const KEY = 'bis.rememberedEmail'

export function rememberEmailKey() {
  return KEY
}

export function readRememberedEmail(): string | null {
  try {
    const value = localStorage.getItem(KEY)
    const email = value?.trim() ?? ''
    return email || null
  } catch {
    return null
  }
}

export function persistRememberedEmail(remember: boolean, email: string) {
  try {
    const trimmed = email.trim()
    if (remember && trimmed) localStorage.setItem(KEY, trimmed)
    else localStorage.removeItem(KEY)
  } catch { /* ignore quota / private mode */ }
}
