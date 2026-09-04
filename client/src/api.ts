const API = import.meta.env.VITE_API_URL ?? ''

function token() {
  return localStorage.getItem('bis.token')
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (!headers.has('Authorization') && token())
    headers.set('Authorization', `Bearer ${token()}`)
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type'))
    headers.set('Content-Type', 'application/json')

  const res = await fetch(`${API}${path}`, { ...init, headers })
  if (res.status === 401) {
    localStorage.removeItem('bis.token')
    if (!path.startsWith('/api/auth/login'))
      window.location.href = '/login'
    throw new Error('Unauthorized')
  }
  if (!res.ok) {
    let message = `${res.status} ${res.statusText}`
    try {
      const body = await res.json()
      message = body.message || body.title || message
    } catch { /* ignore */ }
    throw new Error(message)
  }
  if (res.status === 204) return undefined as T
  const ct = res.headers.get('content-type') || ''
  if (ct.includes('application/json')) return res.json() as Promise<T>
  return res as T
}

export const api = {
  login: (email: string, password: string) =>
    request<{ token: string; email: string }>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  me: () => request<{ email: string }>('/api/auth/me'),
  audits: () => request<import('./types').AuditSummary[]>('/api/audits'),
  audit: (id: string) => request<import('./types').AuditDetail>(`/api/audits/${id}`),
  createAudit: (companyName: string) =>
    request<import('./types').AuditDetail>('/api/audits', {
      method: 'POST',
      body: JSON.stringify({ companyName }),
    }),
  saveOverview: (id: string, body: unknown) =>
    request<import('./types').AuditDetail>(`/api/audits/${id}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }),
  duplicate: (id: string) =>
    request<import('./types').AuditDetail>(`/api/audits/${id}/duplicate`, { method: 'POST' }),
  contactsCsvUrl: (id: string) => `${API}/api/audits/${id}/contacts.csv`,
  items: (id: string, section: string) => request<Record<string, unknown>[]>(`/api/audits/${id}/${section}`),
  saveItem: (id: string, section: string, item: unknown) =>
    request<Record<string, unknown>>(`/api/audits/${id}/${section}`, {
      method: 'POST',
      body: JSON.stringify(item),
    }),
  deleteItem: (id: string, section: string, itemId: string) =>
    request<void>(`/api/audits/${id}/${section}/${itemId}`, { method: 'DELETE' }),
  photos: (id: string) => request<import('./types').SitePhoto[]>(`/api/audits/${id}/photos`),
  uploadPhoto: async (id: string, file: File, extra: Record<string, string | undefined>) => {
    const fd = new FormData()
    fd.append('file', file)
    Object.entries(extra).forEach(([k, v]) => { if (v) fd.append(k, v) })
    return request<import('./types').SitePhoto>(`/api/audits/${id}/photos`, { method: 'POST', body: fd })
  },
  deletePhoto: (photoId: string) => request<void>(`/api/photos/${photoId}`, { method: 'DELETE' }),
  dropdowns: () => request<import('./types').DropdownList[]>('/api/settings/dropdowns'),
  dropdown: (key: string) => request<string[]>(`/api/settings/dropdowns/${encodeURIComponent(key)}`),
  saveDropdown: (id: string, options: string[]) =>
    request<void>(`/api/settings/dropdowns/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ options }),
    }),
  downloadContacts: async (id: string, companyName: string) => {
    const res = await fetch(api.contactsCsvUrl(id), {
      headers: { Authorization: `Bearer ${token()}` },
    })
    if (!res.ok) throw new Error('Export failed')
    const blob = await res.blob()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${companyName || 'contacts'}-contacts.csv`
    a.click()
    URL.revokeObjectURL(url)
  },
  photoSrc: (path?: string) => {
    if (!path) return ''
    if (path.startsWith('http')) return path
    return `${API}/${path.replace(/^\/+/, '')}`
  },
}

export { API }
