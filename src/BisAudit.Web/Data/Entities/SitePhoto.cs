namespace BisAudit.Web.Data.Entities;

public static class PhotoOwnerTypes
{
    public const string Audit = "Audit";
    public const string Workstation = "Workstation";
    public const string Network = "Network";
    public const string ServerStorage = "ServerStorage";
    public const string SecurityAv = "SecurityAv";
    public const string Software = "Software";
    public const string Issue = "Issue";
    public const string Purchase = "Purchase";
}

public class SitePhoto
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public string Category { get; set; } = "General";
    public string FileName { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string? Caption { get; set; }
    public string OwnerType { get; set; } = PhotoOwnerTypes.Audit;
    public Guid? OwnerId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public ClientAudit? Audit { get; set; }

    public string PublicUrl => "/" + RelativePath.Replace('\\', '/').TrimStart('/');
}
