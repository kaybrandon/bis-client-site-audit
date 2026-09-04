using BisAudit.Web.Data.Entities;

namespace BisAudit.Web.Services;

public record ReportMetrics(
    int CriticalHighCount,
    int OpenIssueCount,
    decimal AnnualCostOfInaction,
    decimal Investment,
    decimal AnnualSavings,
    decimal? PaybackMonths)
{
    public static ReportMetrics From(ClientAudit audit)
    {
        var open = audit.Issues.Where(i => !ProgressCalculator.ClosedIssueStatuses.Contains(i.Status)).ToList();
        var critHigh = open.Count(i => ProgressCalculator.CriticalHigh.Contains(i.Severity));
        var annualInaction = open.Sum(i => i.MonthlyCostImpact ?? 0) * 12;
        var activePurchases = audit.Purchases
            .Where(p => !ProgressCalculator.CancelledPurchaseStatuses.Contains(p.Status))
            .ToList();
        var investment = activePurchases.Sum(p => p.Quantity * (p.EstimatedUnitCost ?? 0));
        var annualSavings = activePurchases.Sum(p => p.MonthlySavings ?? 0) * 12;
        decimal? payback = investment > 0 && annualSavings > 0
            ? Math.Round(investment / (annualSavings / 12), 1)
            : investment > 0 && annualSavings <= 0
                ? null
                : 0;
        if (investment <= 0) payback = 0;
        return new ReportMetrics(critHigh, open.Count, annualInaction, investment, annualSavings, payback);
    }
}
