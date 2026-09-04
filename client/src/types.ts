export type ContactRole = 'Primary' | 'Technical' | 'Billing'

export interface AuditContact {
  id: string
  auditId: string
  role: ContactRole
  name?: string
  title?: string
  email?: string
  phone?: string
}

export interface SubLocation {
  id: string
  auditId: string
  name: string
  address?: string
  notes?: string
}

export interface SitePhoto {
  id: string
  auditId: string
  category: string
  fileName: string
  relativePath: string
  publicUrl: string
  caption?: string
  ownerType: string
  ownerId?: string
  uploadedAt: string
}

export interface ReportMetrics {
  criticalHighCount: number
  openIssueCount: number
  annualCostOfInaction: number
  investment: number
  annualSavings: number
  paybackMonths: number | null
}

export interface SectionProgress {
  key: string
  name: string
  weight: number
  score: number
}

export interface AuditSummary {
  id: string
  companyName: string
  industry: string
  employeeCount?: number
  status: string
  auditDate: string
  clientPhotoPath?: string
  updatedAt: string
  progress: number
  sections: SectionProgress[]
  openItems: number
  openCriticalHigh: number
  metrics: ReportMetrics
}

export interface AuditDetail extends AuditSummary {
  address?: string
  preparedBy?: string
  auditScope?: string
  executiveSummary?: string
  previousItSupport?: string
  createdAt: string
  contacts: AuditContact[]
  subLocations: SubLocation[]
  workstations: Record<string, unknown>[]
  networkItems: Record<string, unknown>[]
  serverStorageItems: Record<string, unknown>[]
  securityAvItems: Record<string, unknown>[]
  softwareItems: Record<string, unknown>[]
  issues: Record<string, unknown>[]
  purchases: Record<string, unknown>[]
  photos: SitePhoto[]
}

export interface DropdownList {
  id: string
  listKey: string
  displayName: string
  sortOrder: number
  options: string[]
}

export const KEYS = {
  auditStatus: 'Audit.Status',
  auditIndustry: 'Audit.Industry',
  workstationStatus: 'Workstation.Status',
  workstationWorkMode: 'Workstation.WorkMode',
  workstationDepartment: 'Workstation.Department',
  workstationDeviceType: 'Workstation.DeviceType',
  networkCategory: 'Network.Category',
  networkPriority: 'Network.Priority',
  networkState: 'Network.State',
  networkStatus: 'Network.Status',
  serverCategory: 'Server.Category',
  serverDirection: 'Server.Direction',
  serverStatus: 'Server.Status',
  securityCategory: 'Security.Category',
  securityStatus: 'Security.Status',
  softwareStatus: 'Software.Status',
  issueCategory: 'Issue.Category',
  issueSeverity: 'Issue.Severity',
  issueStatus: 'Issue.Status',
  purchaseCategory: 'Purchase.Category',
  purchasePriority: 'Purchase.Priority',
  purchaseStatus: 'Purchase.Status',
  photoCategory: 'Photo.Category',
}

export const SECTIONS = [
  { num: '01', slug: 'overview', title: 'Overview' },
  { num: '02', slug: 'workstations', title: 'Team & Workstations' },
  { num: '03', slug: 'network', title: 'Network & Infrastructure' },
  { num: '04', slug: 'servers', title: 'Servers, Storage & Cloud' },
  { num: '05', slug: 'security', title: 'Security, Cameras & AV' },
  { num: '06', slug: 'software', title: 'Software & Licensing' },
  { num: '07', slug: 'issues', title: 'Issues & Recommendations' },
  { num: '08', slug: 'purchases', title: 'Purchase Tracker' },
  { num: '09', slug: 'photos', title: 'Site Photos' },
] as const
