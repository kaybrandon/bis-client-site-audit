namespace BisAudit.Api.Data.Entities;

public class ClientAudit
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = "";
    public string Industry { get; set; } = "Marketing";
    public int? EmployeeCount { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = "Planning";
    public DateTime AuditDate { get; set; } = DateTime.Today;
    public string? PreparedBy { get; set; }
    public string? AuditScope { get; set; }
    public string? ExecutiveSummary { get; set; }
    public string? PreviousItSupport { get; set; }
    public string? ClientPhotoPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<AuditContact> Contacts { get; set; } = [];
    public List<SubLocation> SubLocations { get; set; } = [];
    public List<WorkstationItem> Workstations { get; set; } = [];
    public List<NetworkItem> NetworkItems { get; set; } = [];
    public List<ServerStorageItem> ServerStorageItems { get; set; } = [];
    public List<SecurityAvItem> SecurityAvItems { get; set; } = [];
    public List<SoftwareItem> SoftwareItems { get; set; } = [];
    public List<IssueItem> Issues { get; set; } = [];
    public List<PurchaseItem> Purchases { get; set; } = [];
    public List<SitePhoto> Photos { get; set; } = [];

    public AuditContact? PrimaryContact => Contacts.FirstOrDefault(c => c.Role == ContactRole.Primary);
    public AuditContact? TechnicalContact => Contacts.FirstOrDefault(c => c.Role == ContactRole.Technical);
    public AuditContact? BillingContact => Contacts.FirstOrDefault(c => c.Role == ContactRole.Billing);
}

public enum ContactRole
{
    Primary = 0,
    Technical = 1,
    Billing = 2
}

public class AuditContact
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public ContactRole Role { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public ClientAudit? Audit { get; set; }
}

public class SubLocation
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public ClientAudit? Audit { get; set; }
}
