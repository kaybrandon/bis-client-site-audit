using BisAudit.Api.Data;
using BisAudit.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BisAudit.Api.Services;

public class DropdownService(IDbContextFactory<ApplicationDbContext> factory)
{
    public async Task<List<DropdownList>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.DropdownLists
            .AsNoTracking()
            .Include(l => l.Options)
            .OrderBy(l => l.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<List<string>> OptionsAsync(string listKey, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.DropdownOptions
            .AsNoTracking()
            .Where(o => o.List!.ListKey == listKey)
            .OrderBy(o => o.SortOrder)
            .Select(o => o.Value)
            .ToListAsync(ct);
    }

    public async Task SaveListAsync(Guid listId, IReadOnlyList<string> values, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var list = await db.DropdownLists.Include(l => l.Options).FirstOrDefaultAsync(l => l.Id == listId, ct)
                   ?? throw new InvalidOperationException("Dropdown list not found.");

        db.DropdownOptions.RemoveRange(list.Options);
        var order = 0;
        foreach (var value in values.Select(v => v.Trim()).Where(v => v.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            db.DropdownOptions.Add(new DropdownOption
            {
                Id = Guid.NewGuid(),
                ListId = list.Id,
                Value = value,
                SortOrder = order++
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
