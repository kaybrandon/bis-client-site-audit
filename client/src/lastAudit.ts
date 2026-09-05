const KEY = 'bis.lastAuditId'

export function rememberLastAudit(id: string) {
  try { localStorage.setItem(KEY, id) } catch { /* ignore */ }
}

export function readLastAuditId(): string | null {
  try { return localStorage.getItem(KEY) } catch { return null }
}
