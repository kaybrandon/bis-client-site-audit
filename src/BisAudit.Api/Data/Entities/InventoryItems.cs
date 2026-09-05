using System.Text.Json.Serialization;

namespace BisAudit.Api.Data.Entities;

public class WorkstationItem : IAuditOwned
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public string? DeviceName { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; }
    public string? WorkMode { get; set; }
    public string? DeviceType { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Monitors { get; set; }
    public string? HardwareNotes { get; set; }
    public string? Software { get; set; }
    public string? PrinterAccess { get; set; }
    public string? PeripheralsDock { get; set; }
    public string? IssuesReported { get; set; }
    public string? Recommendations { get; set; }
    public string Status { get; set; } = "Current";
    public DateTime? LastAudited { get; set; }
    public bool NeedsAttention { get; set; }
    [JsonIgnore]
    public ClientAudit? Audit { get; set; }

    public string DisplayTitle =>
        string.Join(" / ", new[] { UserName, DeviceName }.Where(s => !string.IsNullOrWhiteSpace(s)).DefaultIfEmpty("Workstation"));

    public string? DisplayDetail => IssuesReported ?? Recommendations;
}

public class NetworkItem : IAuditOwned
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public string? DeviceName { get; set; }
    public string? Category { get; set; }
    public string? State { get; set; }
    public string? Details { get; set; }
    public string? Priority { get; set; }
    public string Status { get; set; } = "Current";
    public DateTime? LastAudited { get; set; }
    public bool NeedsAttention { get; set; }
    [JsonIgnore]
    public ClientAudit? Audit { get; set; }

    public string DisplayTitle => string.IsNullOrWhiteSpace(DeviceName) ? "Network item" : DeviceName;
    public string? DisplayDetail => Details;
}

public class ServerStorageItem : IAuditOwned
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public string? DeviceName { get; set; }
    public string? Category { get; set; }
    public string? CurrentState { get; set; }
    public string? RiskRecommendation { get; set; }
    public string? Direction { get; set; }
    public string Status { get; set; } = "Current";
    public DateTime? LastAudited { get; set; }
    public bool NeedsAttention { get; set; }
    [JsonIgnore]
    public ClientAudit? Audit { get; set; }

    public string DisplayTitle => string.IsNullOrWhiteSpace(DeviceName) ? "Server / storage item" : DeviceName;
    public string? DisplayDetail => RiskRecommendation ?? CurrentState;
}

public class SecurityAvItem : IAuditOwned
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public string? DeviceName { get; set; }
    public string? Category { get; set; }
    public string? CurrentState { get; set; }
    public string? Recommendation { get; set; }
    public string Status { get; set; } = "Current";
    public DateTime? LastAudited { get; set; }
    public bool NeedsAttention { get; set; }
    [JsonIgnore]
    public ClientAudit? Audit { get; set; }

    public string DisplayTitle => string.IsNullOrWhiteSpace(DeviceName) ? "Security / AV item" : DeviceName;
    public string? DisplayDetail => Recommendation ?? CurrentState;
}

public class SoftwareItem : IAuditOwned
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public string? SoftwareAccount { get; set; }
    public string? AccountOwner { get; set; }
    public string? CurrentState { get; set; }
    public string? RecommendedDirection { get; set; }
    public string? ActionItem { get; set; }
    public string Status { get; set; } = "Current";
    public DateTime? LastAudited { get; set; }
    public bool NeedsAttention { get; set; }
    [JsonIgnore]
    public ClientAudit? Audit { get; set; }

    public string DisplayTitle => string.IsNullOrWhiteSpace(SoftwareAccount) ? "Software / account" : SoftwareAccount;
    public string? DisplayDetail => ActionItem ?? RecommendedDirection;
}
