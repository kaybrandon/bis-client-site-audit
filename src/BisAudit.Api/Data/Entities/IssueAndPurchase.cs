using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BisAudit.Api.Data.Entities;

public class IssueItem : IAuditOwned
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public string? Category { get; set; }
    public string IssueRecommendation { get; set; } = "";
    public string Severity { get; set; } = "Medium";
    public int PriorityRank { get; set; } = 3;
    public string? Owner { get; set; }
    public string Status { get; set; } = "Open";
    public string? Notes { get; set; }
    public decimal? MonthlyCostImpact { get; set; }
    public string? ImpactBasis { get; set; }
    public DateTime? LastAudited { get; set; }
    [JsonIgnore]
    [BindNever]
    [ValidateNever]
    public ClientAudit? Audit { get; set; }

    public string DisplayTitle => string.IsNullOrWhiteSpace(IssueRecommendation) ? "Issue" : IssueRecommendation;
    public string? DisplayDetail => Notes;
}

public class PurchaseItem : IAuditOwned
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public string Item { get; set; } = "";
    public string? Category { get; set; }
    public string? Rationale { get; set; }
    public string? Priority { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? EstimatedUnitCost { get; set; }
    public decimal? MonthlySavings { get; set; }
    public string? SavingsBasis { get; set; }
    public string Status { get; set; } = "To Quote";
    public DateTime? LastAudited { get; set; }
    [JsonIgnore]
    [BindNever]
    [ValidateNever]
    public ClientAudit? Audit { get; set; }

    public string DisplayTitle => string.IsNullOrWhiteSpace(Item) ? "Purchase item" : Item;
    public string? DisplayDetail => Rationale;
    public decimal LineInvestment => Quantity * (EstimatedUnitCost ?? 0);
}
