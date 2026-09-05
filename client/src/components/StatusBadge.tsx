const MAP: Record<string, string> = {
  planning: 'badge-planning',
  'in progress': 'badge-in-progress',
  review: 'badge-review',
  complete: 'badge-complete',
  critical: 'badge-critical',
  high: 'badge-high',
  medium: 'badge-medium',
  low: 'badge-low',
  open: 'badge-open',
  blocked: 'badge-blocked',
  planned: 'badge-planned',
  'pending decision': 'badge-pending',
}

export function StatusBadge({ value }: { value?: string }) {
  if (!value) return null
  const css = MAP[value.trim().toLowerCase()] || ''
  return <span className={`badge ${css}`}>{value}</span>
}
