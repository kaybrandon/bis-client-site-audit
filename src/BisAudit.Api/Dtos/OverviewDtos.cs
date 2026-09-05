using BisAudit.Api.Data.Entities;

namespace BisAudit.Api.Dtos;

public class OverviewSaveRequest
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
    public List<OverviewContactDto> Contacts { get; set; } = [];
    public List<OverviewSubLocationDto> SubLocations { get; set; } = [];

    public ClientAudit ToEntity() => new()
    {
        Id = Id,
        CompanyName = CompanyName,
        Industry = Industry,
        EmployeeCount = EmployeeCount,
        Address = Address,
        Status = Status,
        AuditDate = AuditDate,
        PreparedBy = PreparedBy,
        AuditScope = AuditScope,
        ExecutiveSummary = ExecutiveSummary,
        PreviousItSupport = PreviousItSupport,
        ClientPhotoPath = ClientPhotoPath,
        Contacts = (Contacts ?? []).Select(c => c.ToEntity()).ToList(),
        SubLocations = (SubLocations ?? []).Select(l => l.ToEntity()).ToList()
    };
}

public class OverviewContactDto
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public ContactRole Role { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public AuditContact ToEntity() => new()
    {
        Id = Id,
        AuditId = AuditId,
        Role = Role,
        Name = Name,
        Title = Title,
        Email = Email,
        Phone = Phone
    };
}

public class OverviewSubLocationDto
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Notes { get; set; }

    public SubLocation ToEntity() => new()
    {
        Id = Id,
        AuditId = AuditId,
        Name = Name,
        Address = Address,
        Notes = Notes
    };
}
