import { useEffect } from 'react'

export function Toast({
  children,
  warn = false,
  onDone,
}: {
  children: string
  warn?: boolean
  onDone?: () => void
}) {
  useEffect(() => {
    const id = window.setTimeout(() => onDone?.(), 3200)
    return () => window.clearTimeout(id)
  }, [children, onDone])

  return (
    <div className={`toast${warn ? ' toast-warn' : ''}`} role="status">
      {children}
    </div>
  )
}
