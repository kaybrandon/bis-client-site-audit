using BisAudit.Web.Data.Entities;

namespace BisAudit.Web.Services;

public record SectionProgress(string Key, string Name, int Weight, double Score);

public record AuditProgress(int Percent, IReadOnlyList<SectionProgress> Sections)
{
    public static readonly IReadOnlyList<(string Key, string Name, int Weight)> Weights =
    [
        ("overview", "Overview", 20),
        ("workstations", "Team & Workstations", 12),
        ("network", "Network & Infrastructure", 12),
        ("servers", "Servers, Storage & Cloud", 12),
        ("security", "Security, Cameras & AV", 10),
        ("software", "Software & Licensing", 8),
        ("issues", "Issues & Recommendations", 12),
        ("purchases", "Purchase Tracker", 8),
        ("photos", "Site Photos", 6)
    ];
}

public static class ProgressCalculator
{
    public static readonly HashSet<string> ClosedIssueStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Closed", "Optional" };

    public static readonly HashSet<string> CriticalHigh =
        new(StringComparer.OrdinalIgnoreCase) { "Critical", "High" };

    public static readonly HashSet<string> CancelledPurchaseStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Cancelled" };

    public static AuditProgress Calculate(ClientAudit audit)
    {
        var sections = new List<SectionProgress>
        {
            new("overview", "Overview", 20, OverviewScore(audit)),
            new("workstations", "Team & Workstations", 12, CollectionScore(audit.Workstations)),
            new("network", "Network & Infrastructure", 12, CollectionScore(audit.NetworkItems)),
            new("servers", "Servers, Storage & Cloud", 12, CollectionScore(audit.ServerStorageItems)),
            new("security", "Security, Cameras & AV", 10, CollectionScore(audit.SecurityAvItems)),
            new("software", "Software & Licensing", 8, CollectionScore(audit.SoftwareItems)),
            new("issues", "Issues & Recommendations", 12, CollectionScore(audit.Issues)),
            new("purchases", "Purchase Tracker", 8, CollectionScore(audit.Purchases)),
            new("photos", "Site Photos", 6, audit.Photos.Count > 0 ? 1 : 0)
        };

        var weighted = sections.Sum(s => s.Weight * s.Score);
        var totalWeight = sections.Sum(s => s.Weight);
        var percent = totalWeight == 0 ? 0 : (int)Math.Round(100 * weighted / totalWeight, MidpointRounding.AwayFromZero);
        return new AuditProgress(percent, sections);
    }

    public static int OpenIssueCount(IEnumerable<IssueItem> issues) =>
        issues.Count(i => !ClosedIssueStatuses.Contains(i.Status));

    public static int OpenCriticalHighCount(IEnumerable<IssueItem> issues) =>
        issues.Count(i => CriticalHigh.Contains(i.Severity) && !ClosedIssueStatuses.Contains(i.Status));

    public static bool IsActive(ClientAudit audit) =>
        !string.Equals(audit.Status, "Complete", StringComparison.OrdinalIgnoreCase);

    private static double OverviewScore(ClientAudit audit)
    {
        string?[] fields =
        [
            audit.CompanyName,
            audit.Industry,
            audit.EmployeeCount is > 0 ? "yes" : null,
            audit.Address,
            audit.Contacts.Any(c => !string.IsNullOrWhiteSpace(c.Name)) ? "yes" : null,
            audit.AuditScope,
            audit.PreparedBy,
            audit.AuditDate != default ? "yes" : null,
            audit.ExecutiveSummary,
            audit.PreviousItSupport
        ];
        return fields.Count(f => !string.IsNullOrWhiteSpace(f)) / (double)fields.Length;
    }

    private static double CollectionScore<T>(ICollection<T> items) => items.Count > 0 ? 1 : 0;
}
