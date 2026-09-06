import assert from 'node:assert/strict'
import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join, relative } from 'node:path'
import test from 'node:test'
import { createContext, runInContext } from 'node:vm'
import { fileURLToPath } from 'node:url'

const clientRoot = fileURLToPath(new URL('..', import.meta.url))
const srcRoot = join(clientRoot, 'src')
const ALLOWED_STORAGE_KEYS = new Set([
  'bis.token',
  'bis.email',
  'bis.isAdmin',
  'bis.lastAuditId',
  'bis.rememberedEmail',
])

function walk(dir) {
  const out = []
  for (const name of readdirSync(dir)) {
    const path = join(dir, name)
    if (statSync(path).isDirectory()) out.push(...walk(path))
    else if (/\.(ts|tsx|js|jsx)$/.test(name)) out.push(path)
  }
  return out
}

function read(rel) {
  return readFileSync(join(srcRoot, rel), 'utf8')
}

const files = walk(srcRoot)

test('login email and password use password-manager autocomplete', () => {
  const login = read('pages/Login.tsx')
  assert.match(login, /autoComplete="username"/)
  assert.match(login, /autoComplete="current-password"/)
  assert.match(login, /name="username"/)
  assert.match(login, /name="password"/)
  assert.match(login, /Remember email/)
  assert.match(login, /persistRememberedEmail/)
})

test('remember-email helper never mentions password and only writes bis.rememberedEmail', () => {
  const src = read('rememberEmail.ts')
  assert.match(src, /bis\.rememberedEmail/)
  assert.doesNotMatch(src, /password/i)
  assert.match(src, /localStorage\.setItem\(KEY/)
  assert.doesNotMatch(src, /indexedDB/i)
})

test('persistRememberedEmail stores only the email when remember is on', () => {
  const store = new Map()
  const raw = read('rememberEmail.ts')
    .replaceAll('export ', '')
    .replace(/:\s*string \| null/g, '')
    .replace(/:\s*boolean/g, '')
    .replace(/:\s*string/g, '')
  const sandbox = {
    localStorage: {
      getItem: (key) => (store.has(key) ? store.get(key) : null),
      setItem: (key, value) => { store.set(key, String(value)) },
      removeItem: (key) => { store.delete(key) },
    },
  }
  runInContext(
    `${raw}\nthis.readRememberedEmail = readRememberedEmail\nthis.persistRememberedEmail = persistRememberedEmail\nthis.rememberEmailKey = rememberEmailKey\n`,
    createContext(sandbox),
  )

  sandbox.persistRememberedEmail(true, '  tech@bis.local  ')
  assert.equal(store.get('bis.rememberedEmail'), 'tech@bis.local')
  assert.equal(sandbox.readRememberedEmail(), 'tech@bis.local')
  assert.equal([...store.keys()].join(','), 'bis.rememberedEmail')

  sandbox.persistRememberedEmail(false, 'tech@bis.local')
  assert.equal(sandbox.readRememberedEmail(), null)
  assert.equal(store.size, 0)
})

test('client source never writes a password to localStorage, sessionStorage, or IndexedDB', () => {
  const setItem = /(?:local|session)Storage\.setItem\(\s*(['"`])([^'"`]+)\1\s*,\s*([^)\n]+)\)/g
  const forbiddenValue = /\bpassword\b|\bform\.password\b|(?<![\w.])p(?![\w.])/

  for (const file of files) {
    const text = readFileSync(file, 'utf8')
    const rel = relative(srcRoot, file)
    assert.doesNotMatch(text, /\bindexedDB\b|\bIDBFactory\b|\bIDBDatabase\b|\bopenDB\b/, rel)
    assert.doesNotMatch(text, /sessionStorage/, rel)

    let match
    const re = new RegExp(setItem.source, 'g')
    while ((match = re.exec(text))) {
      const key = match[2]
      const value = match[3].trim()
      assert.ok(ALLOWED_STORAGE_KEYS.has(key), `${rel} unexpected storage key ${key}`)
      assert.doesNotMatch(key, /password/i, `${rel} password-like key`)
      assert.doesNotMatch(value, forbiddenValue, `${rel} setItem(${key}, ${value}) looks like a password`)
    }
  }
})

test('logout clears session keys but not the remembered-email helper', () => {
  const auth = read('auth.tsx')
  assert.match(auth, /removeItem\('bis\.token'\)/)
  assert.match(auth, /removeItem\('bis\.email'\)/)
  assert.match(auth, /removeItem\('bis\.isAdmin'\)/)
  assert.doesNotMatch(auth, /rememberedEmail/)
  assert.doesNotMatch(auth, /localStorage\.setItem\([^)]*password/)
})

test('sticky Take photo uses the Site Photos pipeline and does not navigate', () => {
  const btn = read('components/TakePhotoButton.tsx')
  const shell = read('components/AuditShell.tsx')
  const modal = read('components/Modal.tsx')
  const items = read('pages/ItemSection.tsx')
  const photos = read('pages/Photos.tsx')
  const picker = read('components/PhotoPicker.tsx')

  assert.match(btn, /Take photo/)
  assert.match(btn, /capture="environment"/)
  assert.match(btn, /uploadPhoto\(/)
  assert.match(btn, /category: 'Site Photo'/)
  assert.match(btn, /ownerType: 'Audit'/)
  assert.match(btn, /Saved to Site Photos/)
  assert.match(btn, /Open an audit first\./)
  assert.doesNotMatch(btn, /navigate|useNavigate|\/photos/)

  assert.match(shell, /app-header-sticky/)
  assert.match(shell, /TakePhotoButton auditId=\{id\}/)

  assert.match(modal, /modal-take-photo/)
  assert.match(modal, /TakePhotoButton auditId=\{auditId\}/)
  assert.match(modal, /Close/)
  assert.match(items, /<Modal title=\{title\} auditId=\{id\}/)

  assert.match(photos, /<PhotoPicker/)
  assert.match(picker, /cameraLabel = 'Take photo'/)
  assert.match(picker, /libraryLabel = 'From library'/)
  assert.match(picker, /capture="environment"/)
})
