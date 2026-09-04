namespace BisAudit.Api.Data.Entities;

public interface IAuditOwned
{
    Guid Id { get; set; }
    Guid AuditId { get; set; }
    bool NeedsAttention { get; set; }
    DateTime? LastAudited { get; set; }
    string DisplayTitle { get; }
    string? DisplayDetail { get; }
}
