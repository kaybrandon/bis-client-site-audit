using BisAudit.Web.Data.Entities;
using BisAudit.Web.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BisAudit.Web.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
        var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();
        await SeedAdminAsync(users, seedOptions, logger);
        await SeedDropdownsAsync(db);
        if (seedOptions.LoadSampleAudit)
            await SeedSampleAuditsAsync(db, logger);
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> users, SeedOptions options, ILogger logger)
    {
        var existing = await users.FindByEmailAsync(options.AdminEmail);
        if (existing is not null) return;

        var admin = new ApplicationUser
        {
            UserName = options.AdminEmail,
            Email = options.AdminEmail,
            EmailConfirmed = true
        };
        var result = await users.CreateAsync(admin, options.AdminPassword);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to seed admin: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException("Could not seed admin user.");
        }

        logger.LogInformation("Seeded admin user {Email}", options.AdminEmail);
    }

    private static async Task SeedDropdownsAsync(ApplicationDbContext db)
    {
        var existing = await db.DropdownLists.Select(l => l.ListKey).ToListAsync();
        var order = existing.Count;
        foreach (var (key, name, options) in DropdownCatalog.All)
        {
            if (existing.Contains(key)) continue;
            var list = new DropdownList
            {
                Id = Guid.NewGuid(),
                ListKey = key,
                DisplayName = name,
                SortOrder = order++
            };
            var i = 0;
            foreach (var value in options)
            {
                list.Options.Add(new DropdownOption
                {
                    Id = Guid.NewGuid(),
                    Value = value,
                    SortOrder = i++
                });
            }

            db.DropdownLists.Add(list);
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedSampleAuditsAsync(ApplicationDbContext db, ILogger logger)
    {
        if (await db.Audits.AnyAsync()) return;

        var murray = BuildMurrayMedia();
        var harbor = BuildHarborLegal();
        db.Audits.AddRange(murray, harbor);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded SAMPLE demo audits: {First}, {Second}", murray.CompanyName, harbor.CompanyName);
    }

    private static ClientAudit BuildMurrayMedia()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var last = new DateTime(2026, 8, 17, 15, 34, 0);
        var audit = new ClientAudit
        {
            Id = id,
            CompanyName = "Murray Media",
            Industry = "Marketing",
            EmployeeCount = 12,
            Address = "418 Harbor Street, Suite 200",
            Status = "In Progress",
            AuditDate = new DateTime(2026, 8, 17),
            PreparedBy = "BIS Audit Team",
            AuditScope = "On-site infrastructure, workstation fleet, network edge, cameras, and software licensing.",
            ExecutiveSummary = "Murray Media is a 12-person creative shop with aging workstations, an unmanaged perimeter, and a shared NVR on the production LAN. Immediate work is a managed firewall, isolating cameras, and replacing Ronnie's failing Mac.",
            PreviousItSupport = "Break/fix vendor (no retainer). Internal owner: Tejo.",
            CreatedAt = DateTime.UtcNow.AddDays(-18),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        audit.Contacts.AddRange(
        [
            new AuditContact { Id = Guid.NewGuid(), Role = ContactRole.Primary, Name = "Tejo Murray", Title = "Owner", Email = "tejo@murraymedia.example", Phone = "555-0148" },
            new AuditContact { Id = Guid.NewGuid(), Role = ContactRole.Technical, Name = "Vincent Cole", Title = "Producer", Email = "vincent@murraymedia.example", Phone = "555-0172" },
            new AuditContact { Id = Guid.NewGuid(), Role = ContactRole.Billing, Name = "Ava Murray", Title = "Office Manager", Email = "ava@murraymedia.example", Phone = "555-0110" }
        ]);

        audit.SubLocations.Add(new SubLocation
        {
            Id = Guid.NewGuid(),
            Name = "Studio loft",
            Address = "418 Harbor Street, Suite 200",
            Notes = "Open floor, boardroom, edit bays."
        });

        audit.Workstations.AddRange(
        [
            Ws(id, "Mac 24", "Tejo", "tejo@murraymedia.example", "555-0148", "Management/Executive", "Onsite", "All-in-One", "macOS", "1x 24\"", "Current", true, last, null),
            Ws(id, "Mac 23", "Vincent", "vincent@murraymedia.example", "555-0172", "Production", "Hybrid", "Desktop", "macOS", "2x 27\"", "Current", true, last, null),
            Ws(id, "Spare workstation", "N/A", null, null, "Operations", "Onsite", "Desktop", "Windows 11", "1x 24\"", "Current", true, last, "Unassigned spare; inventory tag missing."),
            Ws(id, "Mac", "Ronnie", "ronnie@murraymedia.example", "555-0190", "Creative/Design", "Onsite", "Desktop", "macOS", "2x 27\"", "Needs Replacement", false, last, "Photoshop disk error interrupts production work.")
        ]);

        audit.NetworkItems.AddRange(
        [
            new NetworkItem
            {
                Id = Guid.NewGuid(), AuditId = id, DeviceName = "ISP handoff / no firewall",
                Category = "Firewall", State = "Current", Priority = "Critical", Status = "Needs Review",
                Details = "Circuit lands on an unmanaged consumer gateway. No managed perimeter firewall identified.",
                LastAudited = last, NeedsAttention = true
            },
            new NetworkItem
            {
                Id = Guid.NewGuid(), AuditId = id, DeviceName = "Office switch stack",
                Category = "Switch", State = "Current", Priority = "Medium", Status = "Current",
                Details = "Unmanaged 24-port GbE. Cameras and workstations share the same VLAN.",
                LastAudited = last, NeedsAttention = true
            }
        ]);

        audit.SecurityAvItems.AddRange(
        [
            new SecurityAvItem
            {
                Id = Guid.NewGuid(), AuditId = id, DeviceName = "Shared NVR", Category = "Camera",
                CurrentState = "NVR and cameras sit on the production LAN.",
                Recommendation = "Move cameras and NVR to a dedicated VLAN.",
                Status = "Needs Review", LastAudited = last, NeedsAttention = true
            },
            new SecurityAvItem
            {
                Id = Guid.NewGuid(), AuditId = id, DeviceName = "Boardroom conferencing", Category = "AV",
                CurrentState = "No dedicated conferencing appliance.",
                Recommendation = "Choose between Meeting Owl and Logitech options.",
                Status = "Proposed", LastAudited = last, NeedsAttention = true
            }
        ]);

        audit.Issues.AddRange(
        [
            Issue(id, "Network Security", "No managed perimeter firewall identified", "Critical", 1, "Open",
                "Confirm circuit details and size a SonicWall appliance.", 420, "Estimated incident / downtime exposure", last, true),
            Issue(id, "Hardware", "Ronnie’s Photoshop workstation reports disk errors", "High", 2, "Open",
                "Run SMART and file-system diagnostics; protect current creative files.", 180, "Lost production hours", last, true),
            Issue(id, "Network Security", "Shared NVR is not isolated from business systems", "High", 3, "Planned",
                "Move cameras and NVR to a dedicated VLAN.", 90, "Lateral-movement risk", last, true),
            Issue(id, "Productivity", "Boardroom has no dedicated conferencing solution", "Medium", 4, "Pending Decision",
                "Choose between Meeting Owl and Logitech options.", 60, "Wasted meeting time", last, true),
            Issue(id, "Cloud", "NAS usage numbers are unavailable", "High", 5, "Blocked",
                "Capacity and growth data are required before final sizing.", 0, "Blocked on client data", last, true)
        ]);

        audit.Purchases.AddRange(
        [
            new PurchaseItem
            {
                Id = Guid.NewGuid(), AuditId = id, Item = "SonicWall TZ series firewall",
                Category = "Network", Rationale = "Managed perimeter with VPN and content filtering.",
                Priority = "Critical", Quantity = 1, EstimatedUnitCost = 1450, MonthlySavings = 350,
                SavingsBasis = "Reduced incident risk vs. unmanaged gateway", Status = "To Quote",
                LastAudited = last, NeedsAttention = true
            },
            new PurchaseItem
            {
                Id = Guid.NewGuid(), AuditId = id, Item = "Replacement Mac for Ronnie",
                Category = "Workstation", Rationale = "Disk errors on production Photoshop workstation.",
                Priority = "High", Quantity = 1, EstimatedUnitCost = 2800, MonthlySavings = 180,
                SavingsBasis = "Recovered creative hours", Status = "Quoted",
                LastAudited = last, NeedsAttention = true
            }
        ]);

        return audit;
    }

    private static ClientAudit BuildHarborLegal()
    {
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        return new ClientAudit
        {
            Id = id,
            CompanyName = "Harbor Legal",
            Industry = "Legal",
            EmployeeCount = 8,
            Address = "12 Court Plaza",
            Status = "Planning",
            AuditDate = DateTime.Today,
            PreparedBy = "BIS Audit Team",
            AuditScope = "Discovery visit — workstations, document management, and backup.",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow.AddDays(-3),
            Contacts =
            [
                new AuditContact { Id = Guid.NewGuid(), Role = ContactRole.Primary, Name = "Elena Harbor", Title = "Managing Partner", Email = "elena@harborlegal.example", Phone = "555-0201" },
                new AuditContact { Id = Guid.NewGuid(), Role = ContactRole.Technical },
                new AuditContact { Id = Guid.NewGuid(), Role = ContactRole.Billing }
            ]
        };
    }

    private static WorkstationItem Ws(
        Guid auditId, string device, string user, string? email, string? phone, string dept,
        string mode, string type, string os, string monitors, string status, bool attention,
        DateTime last, string? issues) => new()
    {
        Id = Guid.NewGuid(),
        AuditId = auditId,
        DeviceName = device,
        UserName = user,
        Email = email,
        Phone = phone,
        Department = dept,
        WorkMode = mode,
        DeviceType = type,
        OperatingSystem = os,
        Monitors = monitors,
        Status = status,
        NeedsAttention = attention,
        LastAudited = last,
        IssuesReported = issues
    };

    private static IssueItem Issue(
        Guid auditId, string category, string title, string severity, int rank, string status,
        string notes, decimal monthly, string basis, DateTime last, bool attention) => new()
    {
        Id = Guid.NewGuid(),
        AuditId = auditId,
        Category = category,
        IssueRecommendation = title,
        Severity = severity,
        PriorityRank = rank,
        Status = status,
        Notes = notes,
        MonthlyCostImpact = monthly,
        ImpactBasis = basis,
        LastAudited = last,
        NeedsAttention = attention,
        Owner = "BIS"
    };
}
