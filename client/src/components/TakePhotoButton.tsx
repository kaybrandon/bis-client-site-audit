import { useRef, useState } from 'react'
import { api } from '../api'
import { Toast } from './Toast'

type Props = {
  /** When set, capture uploads into this audit’s Site Photos. */
  auditId?: string
  onSaved?: () => void | Promise<void>
}

export function TakePhotoButton({ auditId, onSaved }: Props) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [busy, setBusy] = useState(false)
  const [toast, setToast] = useState<string | null>(null)
  const [warn, setWarn] = useState(false)

  const show = (message: string, isWarn = false) => {
    setWarn(isWarn)
    setToast(message)
  }

  if (!auditId) {
    return (
      <>
        <button
          type="button"
          className="btn btn-primary take-photo-btn"
          aria-disabled="true"
          title="Open an audit first."
          onClick={() => show('Open an audit first.', true)}
        >
          Take photo
        </button>
        {toast && <Toast warn={warn} onDone={() => setToast(null)}>{toast}</Toast>}
      </>
    )
  }

  const capture = async (file: File) => {
    setBusy(true)
    try {
      await api.uploadPhoto(auditId, file, { category: 'Site Photo', ownerType: 'Audit' })
      show('Saved to Site Photos')
      await onSaved?.()
    } catch (e) {
      show(e instanceof Error ? e.message : 'Couldn’t save photo. Check Wi-Fi and retry.', true)
    } finally {
      setBusy(false)
      if (inputRef.current) inputRef.current.value = ''
    }
  }

  return (
    <>
      <label className={`btn btn-primary take-photo-btn${busy ? ' is-disabled' : ''}`}>
        {busy ? 'Saving…' : 'Take photo'}
        <input
          ref={inputRef}
          type="file"
          accept="image/*"
          capture="environment"
          className="sr-only"
          disabled={busy}
          aria-label="Take photo"
          onChange={(e) => {
            const file = e.target.files?.[0]
            if (file) void capture(file)
          }}
        />
      </label>
      {toast && <Toast warn={warn} onDone={() => setToast(null)}>{toast}</Toast>}
    </>
  )
}
