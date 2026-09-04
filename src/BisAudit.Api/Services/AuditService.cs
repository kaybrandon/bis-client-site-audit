using BisAudit.Api.Data;
using BisAudit.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BisAudit.Api.Services;

public class AuditService(IDbContextFactory<ApplicationDbContext> factory, PhotoService photos)
{
    public async Task<List<ClientAudit>> ListSummariesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Audits
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Issues)
            .Include(a => a.Photos)
            .Include(a => a.Workstations)
            .Include(a => a.NetworkItems)
            .Include(a => a.ServerStorageItems)
            .Include(a => a.SecurityAvItems)
            .Include(a => a.SoftwareItems)
            .Include(a => a.Purchases)
            .Include(a => a.Contacts)
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task<ClientAudit?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Audits
            .AsSplitQuery()
            .Include(a => a.Contacts)
            .Include(a => a.SubLocations)
            .Include(a => a.Workstations)
            .Include(a => a.NetworkItems)
            .Include(a => a.ServerStorageItems)
            .Include(a => a.SecurityAvItems)
            .Include(a => a.SoftwareItems)
            .Include(a => a.Issues)
            .Include(a => a.Purchases)
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<ClientAudit?> GetHeaderAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Audits.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<ClientAudit> CreateAsync(string companyName, CancellationToken ct = default)
    {
        var audit = new ClientAudit
        {
            Id = Guid.NewGuid(),
            CompanyName = companyName.Trim(),
            Status = "Planning",
            AuditDate = DateTime.Today,
            Industry = "Marketing",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Contacts =
            [
                new() { Id = Guid.NewGuid(), Role = ContactRole.Primary },
                new() { Id = Guid.NewGuid(), Role = ContactRole.Technical },
                new() { Id = Guid.NewGuid(), Role = ContactRole.Billing }
            ]
        };
        await using var db = await factory.CreateDbContextAsync(ct);
        db.Audits.Add(audit);
        await db.SaveChangesAsync(ct);
        return audit;
    }

    public async Task SaveOverviewAsync(ClientAudit incoming, IEnumerable<SubLocation> locations, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var audit = await db.Audits
            .Include(a => a.Contacts)
            .Include(a => a.SubLocations)
            .FirstOrDefaultAsync(a => a.Id == incoming.Id, ct)
            ?? throw new InvalidOperationException("Audit not found.");

        audit.CompanyName = incoming.CompanyName.Trim();
        audit.Industry = incoming.Industry;
        audit.EmployeeCount = incoming.EmployeeCount;
        audit.Address = incoming.Address;
        audit.Status = incoming.Status;
        audit.AuditDate = incoming.AuditDate;
        audit.PreparedBy = incoming.PreparedBy;
        audit.AuditScope = incoming.AuditScope;
        audit.ExecutiveSummary = incoming.ExecutiveSummary;
        audit.PreviousItSupport = incoming.PreviousItSupport;
        audit.ClientPhotoPath = incoming.ClientPhotoPath;
        audit.UpdatedAt = DateTime.UtcNow;

        foreach (var role in Enum.GetValues<ContactRole>())
        {
            var src = incoming.Contacts.FirstOrDefault(c => c.Role == role);
            var dest = audit.Contacts.FirstOrDefault(c => c.Role == role);
            if (dest is null)
            {
                dest = new AuditContact { Id = Guid.NewGuid(), AuditId = audit.Id, Role = role };
                audit.Contacts.Add(dest);
            }
            dest.Name = src?.Name;
            dest.Title = src?.Title;
            dest.Email = src?.Email;
            dest.Phone = src?.Phone;
        }

        db.SubLocations.RemoveRange(audit.SubLocations);
        foreach (var loc in locations.Where(l => !string.IsNullOrWhiteSpace(l.Name)))
        {
            audit.SubLocations.Add(new SubLocation
            {
                Id = loc.Id == Guid.Empty ? Guid.NewGuid() : loc.Id,
                AuditId = audit.Id,
                Name = loc.Name.Trim(),
                Address = loc.Address,
                Notes = loc.Notes
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task TouchAsync(Guid auditId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var audit = await db.Audits.FirstOrDefaultAsync(a => a.Id == auditId, ct);
        if (audit is null) return;
        audit.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveItemAsync<T>(T item, CancellationToken ct = default) where T : class, IAuditOwned
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        item.LastAudited = DateTime.Now;
        if (item.Id == Guid.Empty)
        {
            item.Id = Guid.NewGuid();
            db.Set<T>().Add(item);
        }
        else
        {
            var existing = await db.Set<T>().FirstOrDefaultAsync(x => x.Id == item.Id, ct);
            if (existing is null)
                db.Set<T>().Add(item);
            else
                db.Entry(existing).CurrentValues.SetValues(item);
        }

        await db.SaveChangesAsync(ct);
        await TouchAsync(item.AuditId, ct);
    }

    public async Task DeleteItemAsync<T>(Guid id, CancellationToken ct = default) where T : class, IAuditOwned
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var item = await db.Set<T>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return;
        var ownerType = OwnerTypeFor<T>();
        var attached = await db.Photos.Where(p => p.OwnerType == ownerType && p.OwnerId == id).ToListAsync(ct);
        foreach (var photo in attached)
            photos.TryDeleteFile(photo);
        db.Photos.RemoveRange(attached);
        db.Set<T>().Remove(item);
        await db.SaveChangesAsync(ct);
        await TouchAsync(item.AuditId, ct);
    }

    public async Task DeleteAuditAsync(Guid id, CancellationToken ct = default)
    {
        var audit = await GetAsync(id, ct);
        if (audit is null) return;
        foreach (var photo in audit.Photos)
            photos.TryDeleteFile(photo);
        await using var db = await factory.CreateDbContextAsync(ct);
        var tracked = await db.Audits.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (tracked is null) return;
        db.Audits.Remove(tracked);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ClientAudit> DuplicateAsync(Guid id, CancellationToken ct = default)
    {
        var source = await GetAsync(id, ct) ?? throw new InvalidOperationException("Audit not found.");
        var copy = new ClientAudit
        {
            Id = Guid.NewGuid(),
            CompanyName = source.CompanyName + " (copy)",
            Industry = source.Industry,
            EmployeeCount = source.EmployeeCount,
            Address = source.Address,
            Status = "Planning",
            AuditDate = DateTime.Today,
            PreparedBy = source.PreparedBy,
            AuditScope = source.AuditScope,
            ExecutiveSummary = source.ExecutiveSummary,
            PreviousItSupport = source.PreviousItSupport,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var c in source.Contacts)
        {
            copy.Contacts.Add(new AuditContact
            {
                Id = Guid.NewGuid(),
                Role = c.Role,
                Name = c.Name,
                Title = c.Title,
                Email = c.Email,
                Phone = c.Phone
            });
        }

        foreach (var loc in source.SubLocations)
        {
            copy.SubLocations.Add(new SubLocation
            {
                Id = Guid.NewGuid(),
                Name = loc.Name,
                Address = loc.Address,
                Notes = loc.Notes
            });
        }

        await using var db = await factory.CreateDbContextAsync(ct);
        db.Audits.Add(copy);
        await db.SaveChangesAsync(ct);

        await CloneItems(db, source.Workstations, copy.Id, (s, n) => CloneWorkstation(s, n), PhotoOwnerTypes.Workstation, ct);
        await CloneItems(db, source.NetworkItems, copy.Id, (s, n) => CloneNetwork(s, n), PhotoOwnerTypes.Network, ct);
        await CloneItems(db, source.ServerStorageItems, copy.Id, (s, n) => CloneServer(s, n), PhotoOwnerTypes.ServerStorage, ct);
        await CloneItems(db, source.SecurityAvItems, copy.Id, (s, n) => CloneSecurity(s, n), PhotoOwnerTypes.SecurityAv, ct);
        await CloneItems(db, source.SoftwareItems, copy.Id, (s, n) => CloneSoftware(s, n), PhotoOwnerTypes.Software, ct);
        await CloneItems(db, source.Issues, copy.Id, (s, n) => CloneIssue(s, n), PhotoOwnerTypes.Issue, ct);
        await CloneItems(db, source.Purchases, copy.Id, (s, n) => ClonePurchase(s, n), PhotoOwnerTypes.Purchase, ct);

        foreach (var photo in source.Photos.Where(p => p.OwnerType == PhotoOwnerTypes.Audit))
            await ClonePhotoFile(photo, copy.Id, PhotoOwnerTypes.Audit, null, ct);

        if (!string.IsNullOrWhiteSpace(source.ClientPhotoPath))
        {
            var cloned = CloneStandaloneFile(source.ClientPhotoPath, copy.Id);
            if (cloned is not null)
            {
                copy.ClientPhotoPath = cloned;
                await db.SaveChangesAsync(ct);
            }
        }

        return copy;
    }

    private async Task CloneItems<T>(ApplicationDbContext db, IEnumerable<T> source, Guid newAuditId, Func<T, Guid, T> clone, string ownerType, CancellationToken ct)
        where T : class, IAuditOwned
    {
        foreach (var item in source)
        {
            var next = clone(item, newAuditId);
            db.Set<T>().Add(next);
            await db.SaveChangesAsync(ct);
            foreach (var photo in await db.Photos.AsNoTracking()
                         .Where(p => p.OwnerType == ownerType && p.OwnerId == item.Id).ToListAsync(ct))
            {
                await ClonePhotoFile(photo, newAuditId, ownerType, next.Id, ct);
            }
        }
    }

    private async Task ClonePhotoFile(SitePhoto source, Guid newAuditId, string ownerType, Guid? ownerId, CancellationToken ct)
    {
        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var srcPath = Path.Combine(webRoot, source.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(srcPath)) return;
        await using var stream = File.OpenRead(srcPath);
        await photos.SaveAsync(newAuditId, stream, source.FileName, "image/jpeg", source.Category, ownerType, ownerId, source.Caption, ct);
    }

    private static string? CloneStandaloneFile(string relativePath, Guid newAuditId)
    {
        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var srcPath = Path.Combine(webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(srcPath)) return null;
        var ext = Path.GetExtension(srcPath);
        var destRel = Path.Combine("uploads", newAuditId.ToString("D"), Guid.NewGuid() + ext).Replace('\\', '/');
        var destPath = Path.Combine(webRoot, destRel.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        File.Copy(srcPath, destPath);
        return destRel;
    }

    private static WorkstationItem CloneWorkstation(WorkstationItem s, Guid auditId) => new()
    {
        Id = Guid.NewGuid(), AuditId = auditId, DeviceName = s.DeviceName, UserName = s.UserName, Email = s.Email,
        Phone = s.Phone, Department = s.Department, WorkMode = s.WorkMode, DeviceType = s.DeviceType,
        OperatingSystem = s.OperatingSystem, Monitors = s.Monitors, HardwareNotes = s.HardwareNotes,
        Software = s.Software, PrinterAccess = s.PrinterAccess, PeripheralsDock = s.PeripheralsDock,
        IssuesReported = s.IssuesReported, Recommendations = s.Recommendations, Status = s.Status,
        LastAudited = s.LastAudited, NeedsAttention = s.NeedsAttention
    };

    private static NetworkItem CloneNetwork(NetworkItem s, Guid auditId) => new()
    {
        Id = Guid.NewGuid(), AuditId = auditId, DeviceName = s.DeviceName, Category = s.Category, State = s.State,
        Details = s.Details, Priority = s.Priority, Status = s.Status, LastAudited = s.LastAudited,
        NeedsAttention = s.NeedsAttention
    };

    private static ServerStorageItem CloneServer(ServerStorageItem s, Guid auditId) => new()
    {
        Id = Guid.NewGuid(), AuditId = auditId, DeviceName = s.DeviceName, Category = s.Category,
        CurrentState = s.CurrentState, RiskRecommendation = s.RiskRecommendation, Direction = s.Direction,
        Status = s.Status, LastAudited = s.LastAudited, NeedsAttention = s.NeedsAttention
    };

    private static SecurityAvItem CloneSecurity(SecurityAvItem s, Guid auditId) => new()
    {
        Id = Guid.NewGuid(), AuditId = auditId, DeviceName = s.DeviceName, Category = s.Category,
        CurrentState = s.CurrentState, Recommendation = s.Recommendation, Status = s.Status,
        LastAudited = s.LastAudited, NeedsAttention = s.NeedsAttention
    };

    private static SoftwareItem CloneSoftware(SoftwareItem s, Guid auditId) => new()
    {
        Id = Guid.NewGuid(), AuditId = auditId, SoftwareAccount = s.SoftwareAccount, AccountOwner = s.AccountOwner,
        CurrentState = s.CurrentState, RecommendedDirection = s.RecommendedDirection, ActionItem = s.ActionItem,
        Status = s.Status, LastAudited = s.LastAudited, NeedsAttention = s.NeedsAttention
    };

    private static IssueItem CloneIssue(IssueItem s, Guid auditId) => new()
    {
        Id = Guid.NewGuid(), AuditId = auditId, Category = s.Category, IssueRecommendation = s.IssueRecommendation,
        Severity = s.Severity, PriorityRank = s.PriorityRank, Owner = s.Owner, Status = s.Status, Notes = s.Notes,
        MonthlyCostImpact = s.MonthlyCostImpact, ImpactBasis = s.ImpactBasis, LastAudited = s.LastAudited,
        NeedsAttention = s.NeedsAttention
    };

    private static PurchaseItem ClonePurchase(PurchaseItem s, Guid auditId) => new()
    {
        Id = Guid.NewGuid(), AuditId = auditId, Item = s.Item, Category = s.Category, Rationale = s.Rationale,
        Priority = s.Priority, Quantity = s.Quantity, EstimatedUnitCost = s.EstimatedUnitCost,
        MonthlySavings = s.MonthlySavings, SavingsBasis = s.SavingsBasis, Status = s.Status,
        LastAudited = s.LastAudited, NeedsAttention = s.NeedsAttention
    };

    public static string OwnerTypeFor<T>() => typeof(T).Name switch
    {
        nameof(WorkstationItem) => PhotoOwnerTypes.Workstation,
        nameof(NetworkItem) => PhotoOwnerTypes.Network,
        nameof(ServerStorageItem) => PhotoOwnerTypes.ServerStorage,
        nameof(SecurityAvItem) => PhotoOwnerTypes.SecurityAv,
        nameof(SoftwareItem) => PhotoOwnerTypes.Software,
        nameof(IssueItem) => PhotoOwnerTypes.Issue,
        nameof(PurchaseItem) => PhotoOwnerTypes.Purchase,
        _ => PhotoOwnerTypes.Audit
    };

    public static string DefaultPhotoCategoryFor(string ownerType) => ownerType switch
    {
        PhotoOwnerTypes.Workstation => "Workstation",
        PhotoOwnerTypes.Network => "Network",
        PhotoOwnerTypes.ServerStorage => "Server/Storage",
        PhotoOwnerTypes.SecurityAv => "Security/Camera",
        _ => "General"
    };
}
