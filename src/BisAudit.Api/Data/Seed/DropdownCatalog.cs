using BisAudit.Api.Data.Entities;

namespace BisAudit.Api.Data.Seed;

public static class DropdownCatalog
{
    public static IReadOnlyList<(string Key, string Name, string[] Options)> All { get; } =
    [
        (DropdownKeys.AuditStatus, "Overview - Audit Status",
            ["Planning", "In Progress", "Review", "Complete"]),
        (DropdownKeys.AuditIndustry, "Overview - Industry",
        [
            "Marketing", "Legal", "Healthcare", "Finance / Insurance", "Manufacturing", "Retail",
            "Construction", "Education", "Non-profit", "Hospitality", "Real Estate", "Technology",
            "Consulting", "Agriculture", "Government", "Transportation", "Media / Entertainment", "Other"
        ]),
        (DropdownKeys.WorkstationStatus, "Team & Workstations - Status",
            ["Current", "Needs Replacement", "Replacement Scheduled", "In Repair", "Retired", "N/A"]),
        (DropdownKeys.WorkstationWorkMode, "Team & Workstations - Work mode",
            ["Onsite", "Remote", "Hybrid"]),
        (DropdownKeys.WorkstationDepartment, "Team & Workstations - Department",
        [
            "Administration", "Creative/Design", "Customer Service", "Finance/Accounting", "HR",
            "IT/Technology", "Management/Executive", "Marketing", "Operations", "Production",
            "Sales", "Warehouse/Shipping"
        ]),
        (DropdownKeys.WorkstationDeviceType, "Team & Workstations - Device type",
            ["Desktop", "Laptop", "Tablet", "2-in-1", "Mini PC", "All-in-One", "Server", "Other"]),
        (DropdownKeys.NetworkCategory, "Network & Infrastructure - Category",
            ["Router", "Switch", "Firewall", "Access Point", "Wireless", "ISP/WAN", "VPN", "Load Balancer", "Cabling", "Other"]),
        (DropdownKeys.NetworkPriority, "Network & Infrastructure - Priority",
            ["Critical", "High", "Medium", "Low"]),
        (DropdownKeys.NetworkState, "Network & Infrastructure - State",
            ["Current", "Proposed"]),
        (DropdownKeys.NetworkStatus, "Network & Infrastructure - Status",
            ["Current", "Needs Review", "Proposed", "Scheduled", "In Progress", "Blocked", "N/A", "Discovery", "Missing", "Recommended"]),
        (DropdownKeys.ServerCategory, "Servers, Storage & Cloud - Category",
            ["Server", "NAS", "Cloud"]),
        (DropdownKeys.ServerDirection, "Servers, Storage & Cloud - Direction",
            ["On-prem", "Cloud", "Hybrid", "Pending"]),
        (DropdownKeys.ServerStatus, "Servers, Storage & Cloud - Status",
            ["Current", "Needs Review", "Proposed", "Scheduled", "In Progress", "Blocked", "N/A"]),
        (DropdownKeys.SecurityCategory, "Security, Cameras & AV - Category",
            ["Security", "Camera", "AV"]),
        (DropdownKeys.SecurityStatus, "Security, Cameras & AV - Status",
            ["Current", "Needs Review", "Proposed", "Scheduled", "In Progress", "Blocked", "N/A"]),
        (DropdownKeys.SoftwareStatus, "Software & Licensing - Status",
            ["Current", "Needs Review", "Proposed", "Scheduled", "In Progress", "Blocked", "N/A"]),
        (DropdownKeys.IssueCategory, "Issues & Recommendations - Category",
        [
            "Network Security", "Hardware", "Software", "Cloud", "Data Backup", "Access Control",
            "Email", "Internet", "Compliance", "User Training", "Productivity", "Vendors", "Other"
        ]),
        (DropdownKeys.IssueSeverity, "Issues & Recommendations - Severity",
            ["Critical", "High", "Medium", "Low"]),
        (DropdownKeys.IssueStatus, "Issues & Recommendations - Status",
            ["Open", "In Progress", "Blocked", "Pending Decision", "Planned", "Ready", "Closed", "Optional"]),
        (DropdownKeys.PurchaseCategory, "Purchase Tracker - Category",
        [
            "Workstation", "Network", "Server/Storage", "Security", "Software/Licensing",
            "Cloud Services", "Backup", "AV/Camera", "Peripherals", "Cabling", "Other"
        ]),
        (DropdownKeys.PurchasePriority, "Purchase Tracker - Priority",
            ["Critical", "High", "Medium", "Low"]),
        (DropdownKeys.PurchaseStatus, "Purchase Tracker - Status",
            ["To Quote", "Quoted", "Approved", "Ordered", "Received", "Installed", "Blocked", "Pending client choice", "Optional", "Cancelled"]),
        (DropdownKeys.PhotoCategory, "Site Photos - Category",
            ["Workstation", "Network", "Server/Storage", "Security/Camera", "Site Photo", "General", "Other"])
    ];
}
