using BisAudit.Api.Data.Entities;

namespace BisAudit.Api.Models;

/// <summary>
/// Flat contact row for Overview GET/PUT. No <c>audit</c> navigation — nested graphs
/// must not round-trip through the Overview endpoint.
/// </summary>
public class AuditContactDto
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public ContactRole Role { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public static AuditContactDto From(AuditContact c) => new()
    {
        Id = c.Id,
        AuditId = c.AuditId,
        Role = c.Role,
        Name = c.Name,
        Title = c.Title,
        Email = c.Email,
        Phone = c.Phone
    };
}

/// <summary>
/// Flat sub-location row for Overview GET/PUT. No <c>audit</c> navigation.
/// </summary>
public class SubLocationDto
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Notes { get; set; }

    public static SubLocationDto From(SubLocation l) => new()
    {
        Id = l.Id,
        AuditId = l.AuditId,
        Name = l.Name,
        Address = l.Address,
        Notes = l.Notes
    };
}

/// <summary>
/// Overview PUT body. Binds only header + contacts + sub-locations so a client
/// cannot post a nested <see cref="ClientAudit"/> graph.
/// </summary>
public class OverviewSaveRequest
{
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
    public List<AuditContactDto> Contacts { get; set; } = [];
    public List<SubLocationDto> SubLocations { get; set; } = [];
}
