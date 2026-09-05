import type { ReactNode } from 'react'
import { Navigate, Outlet, Route, createBrowserRouter, createRoutesFromElements } from 'react-router-dom'
import { useAuth } from './auth'
import { AuditShell } from './components/AuditShell'
import { SessionResume } from './components/SessionResume'
import { Login } from './pages/Login'
import { Dashboard } from './pages/Dashboard'
import { Settings } from './pages/Settings'
import { Reports, ReportPrint } from './pages/Reports'
import { Overview } from './pages/Overview'
import { Workstations, Network, Servers, Security, Software, Issues, Purchases, Photos } from './pages/Sections'

function Guard({ children }: { children: ReactNode }) {
  const { email } = useAuth()
  if (!email) return <Navigate to="/login" replace />
  return children
}

function Root() {
  return (
    <>
      <SessionResume />
      <Outlet />
    </>
  )
}

export const router = createBrowserRouter(
  createRoutesFromElements(
    <Route element={<Root />}>
      <Route path="/login" element={<Login />} />
      <Route path="/" element={<Guard><Dashboard /></Guard>} />
      <Route path="/settings" element={<Guard><Settings /></Guard>} />
      <Route path="/reports" element={<Guard><Reports /></Guard>} />
      <Route path="/reports/:id" element={<Guard><ReportPrint /></Guard>} />
      <Route path="/audits/:id" element={<Guard><AuditShell /></Guard>}>
        <Route path="overview" element={<Overview />} />
        <Route path="workstations" element={<Workstations />} />
        <Route path="network" element={<Network />} />
        <Route path="servers" element={<Servers />} />
        <Route path="security" element={<Security />} />
        <Route path="software" element={<Software />} />
        <Route path="issues" element={<Issues />} />
        <Route path="purchases" element={<Purchases />} />
        <Route path="photos" element={<Photos />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Route>,
  ),
)
