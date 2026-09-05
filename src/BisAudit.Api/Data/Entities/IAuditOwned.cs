namespace BisAudit.Api.Data.Entities;

public interface IAuditOwned
{
    Guid Id { get; set; }
    Guid AuditId { get; set; }
    DateTime? LastAudited { get; set; }
    string DisplayTitle { get; }
    string? DisplayDetail { get; }
}
