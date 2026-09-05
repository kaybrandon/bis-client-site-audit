import { useRef, useState } from 'react'

type Props = {
  /** Upload one prepared file. Called sequentially for library multi-select. */
  upload: (file: File, caption?: string) => Promise<void>
  allowMultiple?: boolean
  showCaption?: boolean
  cameraLabel?: string
  libraryLabel?: string
  compact?: boolean
}

export function PhotoPicker({
  upload,
  allowMultiple = true,
  showCaption = false,
  cameraLabel = 'Take photo',
  libraryLabel = 'From library',
  compact = false,
}: Props) {
  const cameraRef = useRef<HTMLInputElement>(null)
  const libraryRef = useRef<HTMLInputElement>(null)
  const [caption, setCaption] = useState('')
  const [busy, setBusy] = useState(false)
  const [status, setStatus] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState<File[]>([])
  const [failedName, setFailedName] = useState<string | null>(null)
  const [failedIndex, setFailedIndex] = useState(0)

  const resetInputs = () => {
    if (cameraRef.current) cameraRef.current.value = ''
    if (libraryRef.current) libraryRef.current.value = ''
  }

  const send = async (files: File[], startAt = 0) => {
    if (!files.length) return
    setPending(files)
    setBusy(true)
    setError(null)
    setFailedName(null)
    try {
      for (let i = startAt; i < files.length; i++) {
        setStatus(files.length > 1 ? `Uploading ${i + 1} of ${files.length}…` : 'Uploading…')
        try {
          await upload(files[i], caption.trim() || undefined)
        } catch (e) {
          setFailedIndex(i)
          setFailedName(files[i].name)
          setError(e instanceof Error ? e.message : 'Upload failed. Check Wi-Fi and retry.')
          setStatus(null)
          return
        }
      }
      setPending([])
      setCaption('')
      setStatus(null)
    } finally {
      setBusy(false)
      resetInputs()
    }
  }

  const remainingAfterFail = pending.length - failedIndex - 1

  return (
    <div className="photo-picker">
      {showCaption && (
        <label className="fld">
          Caption (optional)
          <input
            value={caption}
            onChange={(e) => setCaption(e.target.value)}
            placeholder="What is this shot?"
            disabled={busy}
          />
        </label>
      )}
      <div className="photo-picker-actions">
        <label className={`btn btn-primary ${busy ? 'is-disabled' : ''}`}>
          {cameraLabel}
          <input
            ref={cameraRef}
            type="file"
            accept="image/*"
            capture="environment"
            className="sr-only"
            disabled={busy}
            onChange={(e) => { if (e.target.files?.length) void send(Array.from(e.target.files)) }}
          />
        </label>
        <label className={`btn btn-ghost-dark ${busy ? 'is-disabled' : ''}`}>
          {libraryLabel}
          <input
            ref={libraryRef}
            type="file"
            accept="image/*,.heic,.heif"
            multiple={allowMultiple}
            className="sr-only"
            disabled={busy}
            onChange={(e) => { if (e.target.files?.length) void send(Array.from(e.target.files)) }}
          />
        </label>
        {error && pending.length > 0 && (
          <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void send(pending, failedIndex)}>
            Retry {failedName ? `"${failedName}"` : 'upload'}
          </button>
        )}
        {error && remainingAfterFail > 0 && (
          <button type="button" className="btn btn-ghost-dark" disabled={busy} onClick={() => void send(pending, failedIndex + 1)}>
            Skip and continue ({remainingAfterFail} left)
          </button>
        )}
      </div>
      {status && <p className="muted">{status}</p>}
      {failedName && <p className="error">Couldn’t upload “{failedName}”.</p>}
      {error && <p className="error">{error}</p>}
      {!compact && (
        <p className="muted photo-picker-hint">
          Camera uses the rear lens on phones and tablets. Large photos are resized before upload (max 12 MB).
        </p>
      )}
    </div>
  )
}
