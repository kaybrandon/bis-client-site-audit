import { KEYS } from '../types'
import { ItemSection, type FieldDef } from './ItemSection'
import { Photos } from './Photos'

const attention: FieldDef = { key: 'needsAttention', label: 'Needs Attention', kind: 'check' }

export function Workstations() {
  return (
    <ItemSection
      title="Team & Workstations"
      section="workstations"
      ownerType="Workstation"
      photoCategory="Workstation"
      defaults={{ status: 'Current', workMode: 'Onsite' }}
      titleOf={(i) => [i.userName, i.deviceName].filter(Boolean).join(' / ') || 'Workstation'}
      detailOf={(i) => String(i.issuesReported || i.recommendations || '') || undefined}
      badgesOf={(i) => [String(i.status)]}
      fields={[
        { key: 'deviceName', label: 'Device Name' },
        { key: 'userName', label: 'User Name' },
        { key: 'email', label: 'Email' },
        { key: 'phone', label: 'Phone' },
        { key: 'department', label: 'Department', kind: 'select', optionsKey: KEYS.workstationDepartment },
        { key: 'workMode', label: 'Work Mode', kind: 'select', optionsKey: KEYS.workstationWorkMode },
        { key: 'deviceType', label: 'Device Type', kind: 'select', optionsKey: KEYS.workstationDeviceType },
        { key: 'operatingSystem', label: 'OS' },
        { key: 'monitors', label: 'Monitors' },
        { key: 'status', label: 'Status', kind: 'select', optionsKey: KEYS.workstationStatus },
        { key: 'hardwareNotes', label: 'Hardware Notes', kind: 'textarea', full: true },
        { key: 'software', label: 'Software', kind: 'textarea', full: true },
        { key: 'printerAccess', label: 'Printer Access' },
        { key: 'peripheralsDock', label: 'Peripherals/Dock' },
        { key: 'issuesReported', label: 'Issues Reported', kind: 'textarea', full: true },
        { key: 'recommendations', label: 'Recommendations', kind: 'textarea', full: true },
        attention,
      ]}
    />
  )
}

export function Network() {
  return (
    <ItemSection
      title="Network & Infrastructure"
      section="network"
      ownerType="Network"
      photoCategory="Network"
      defaults={{ status: 'Current', state: 'Current' }}
      titleOf={(i) => String(i.deviceName || 'Network item')}
      detailOf={(i) => String(i.details || '') || undefined}
      badgesOf={(i) => [String(i.priority || ''), String(i.status)]}
      fields={[
        { key: 'deviceName', label: 'Device Name' },
        { key: 'category', label: 'Category', kind: 'select', optionsKey: KEYS.networkCategory },
        { key: 'state', label: 'State', kind: 'select', optionsKey: KEYS.networkState },
        { key: 'priority', label: 'Priority', kind: 'select', optionsKey: KEYS.networkPriority },
        { key: 'status', label: 'Status', kind: 'select', optionsKey: KEYS.networkStatus },
        { key: 'details', label: 'Details', kind: 'textarea', full: true },
        attention,
      ]}
    />
  )
}

export function Servers() {
  return (
    <ItemSection
      title="Servers, Storage & Cloud"
      section="servers"
      ownerType="ServerStorage"
      photoCategory="Server/Storage"
      defaults={{ status: 'Current' }}
      titleOf={(i) => String(i.deviceName || 'Server / storage item')}
      detailOf={(i) => String(i.riskRecommendation || i.currentState || '') || undefined}
      badgesOf={(i) => [String(i.category || ''), String(i.status)]}
      fields={[
        { key: 'deviceName', label: 'Device Name' },
        { key: 'category', label: 'Category', kind: 'select', optionsKey: KEYS.serverCategory },
        { key: 'direction', label: 'Direction', kind: 'select', optionsKey: KEYS.serverDirection },
        { key: 'status', label: 'Status', kind: 'select', optionsKey: KEYS.serverStatus },
        { key: 'currentState', label: 'Current State', kind: 'textarea', full: true },
        { key: 'riskRecommendation', label: 'Risk / Recommendation', kind: 'textarea', full: true },
        attention,
      ]}
    />
  )
}

export function Security() {
  return (
    <ItemSection
      title="Security, Cameras & AV"
      section="security"
      ownerType="SecurityAv"
      photoCategory="Security/Camera"
      defaults={{ status: 'Current' }}
      titleOf={(i) => String(i.deviceName || 'Security / AV item')}
      detailOf={(i) => String(i.recommendation || i.currentState || '') || undefined}
      badgesOf={(i) => [String(i.category || ''), String(i.status)]}
      fields={[
        { key: 'deviceName', label: 'Device Name' },
        { key: 'category', label: 'Category', kind: 'select', optionsKey: KEYS.securityCategory },
        { key: 'status', label: 'Status', kind: 'select', optionsKey: KEYS.securityStatus },
        { key: 'currentState', label: 'Current State', kind: 'textarea', full: true },
        { key: 'recommendation', label: 'Recommendation', kind: 'textarea', full: true },
        attention,
      ]}
    />
  )
}

export function Software() {
  return (
    <ItemSection
      title="Software & Licensing"
      section="software"
      ownerType="Software"
      photoCategory="General"
      defaults={{ status: 'Current' }}
      titleOf={(i) => String(i.softwareAccount || 'Software / account')}
      detailOf={(i) => String(i.actionItem || i.recommendedDirection || '') || undefined}
      badgesOf={(i) => [String(i.status)]}
      fields={[
        { key: 'softwareAccount', label: 'Software / Account' },
        { key: 'accountOwner', label: 'Account / Owner' },
        { key: 'status', label: 'Status', kind: 'select', optionsKey: KEYS.softwareStatus },
        { key: 'currentState', label: 'Current State', kind: 'textarea', full: true },
        { key: 'recommendedDirection', label: 'Recommended Direction', kind: 'textarea', full: true },
        { key: 'actionItem', label: 'Action Item', kind: 'textarea', full: true },
        attention,
      ]}
    />
  )
}

export function Issues() {
  return (
    <ItemSection
      title="Issues & Recommendations"
      section="issues"
      ownerType="Issue"
      photoCategory="General"
      defaults={{ status: 'Open', severity: 'Medium', priorityRank: 3, needsAttention: true }}
      titleOf={(i) => String(i.issueRecommendation || 'Issue')}
      detailOf={(i) => String(i.notes || '') || undefined}
      badgesOf={(i) => [String(i.severity), String(i.status)]}
      extraFilters={{ severity: true, status: true }}
      fields={[
        { key: 'category', label: 'Category', kind: 'select', optionsKey: KEYS.issueCategory },
        { key: 'severity', label: 'Severity', kind: 'select', optionsKey: KEYS.issueSeverity },
        { key: 'issueRecommendation', label: 'Issue / Recommendation', full: true },
        { key: 'priorityRank', label: 'Priority Rank (1–5)', kind: 'number' },
        { key: 'owner', label: 'Owner' },
        { key: 'status', label: 'Status', kind: 'select', optionsKey: KEYS.issueStatus },
        { key: 'monthlyCostImpact', label: 'Monthly Cost Impact $', kind: 'number' },
        { key: 'impactBasis', label: 'Impact Basis' },
        { key: 'notes', label: 'Notes', kind: 'textarea', full: true },
        attention,
      ]}
    />
  )
}

export function Purchases() {
  return (
    <ItemSection
      title="Purchase Tracker"
      section="purchases"
      ownerType="Purchase"
      photoCategory="General"
      defaults={{ status: 'To Quote', quantity: 1 }}
      titleOf={(i) => String(i.item || 'Purchase item')}
      detailOf={(i) => String(i.rationale || '') || undefined}
      badgesOf={(i) => [String(i.priority || ''), String(i.status)]}
      fields={[
        { key: 'item', label: 'Item', full: true },
        { key: 'category', label: 'Category', kind: 'select', optionsKey: KEYS.purchaseCategory },
        { key: 'priority', label: 'Priority', kind: 'select', optionsKey: KEYS.purchasePriority },
        { key: 'quantity', label: 'Qty', kind: 'number' },
        { key: 'estimatedUnitCost', label: 'Est Unit Cost', kind: 'number' },
        { key: 'monthlySavings', label: 'Monthly Savings $', kind: 'number' },
        { key: 'savingsBasis', label: 'Savings Basis' },
        { key: 'status', label: 'Status', kind: 'select', optionsKey: KEYS.purchaseStatus },
        { key: 'rationale', label: 'Rationale', kind: 'textarea', full: true },
        attention,
      ]}
    />
  )
}

export { Photos }
