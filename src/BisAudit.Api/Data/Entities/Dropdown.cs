namespace BisAudit.Api.Data.Entities;

public class DropdownList
{
    public Guid Id { get; set; }
    public string ListKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int SortOrder { get; set; }
    public List<DropdownOption> Options { get; set; } = [];
}

public class DropdownOption
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public string Value { get; set; } = "";
    public int SortOrder { get; set; }
    public DropdownList? List { get; set; }
}

public static class DropdownKeys
{
    public const string AuditStatus = "Audit.Status";
    public const string AuditIndustry = "Audit.Industry";
    public const string WorkstationStatus = "Workstation.Status";
    public const string WorkstationWorkMode = "Workstation.WorkMode";
    public const string WorkstationDepartment = "Workstation.Department";
    public const string WorkstationDeviceType = "Workstation.DeviceType";
    public const string NetworkCategory = "Network.Category";
    public const string NetworkPriority = "Network.Priority";
    public const string NetworkState = "Network.State";
    public const string NetworkStatus = "Network.Status";
    public const string ServerCategory = "Server.Category";
    public const string ServerDirection = "Server.Direction";
    public const string ServerStatus = "Server.Status";
    public const string SecurityCategory = "Security.Category";
    public const string SecurityStatus = "Security.Status";
    public const string SoftwareStatus = "Software.Status";
    public const string IssueCategory = "Issue.Category";
    public const string IssueSeverity = "Issue.Severity";
    public const string IssueStatus = "Issue.Status";
    public const string PurchaseCategory = "Purchase.Category";
    public const string PurchasePriority = "Purchase.Priority";
    public const string PurchaseStatus = "Purchase.Status";
    public const string PhotoCategory = "Photo.Category";
}
