using BisAudit.Web.Data;
using BisAudit.Web.Data.Entities;
using BisAudit.Web.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BisAudit.Web.Services;

public class PhotoService(ApplicationDbContext db, IWebHostEnvironment env, IOptions<UploadOptions> options)
{
    public string ResolveRoot()
    {
        var configured = options.Value.RootPath;
        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(env.ContentRootPath, configured);
    }

    public async Task<SitePhoto> SaveAsync(
        Guid auditId,
        Stream content,
        string originalFileName,
        string contentType,
        string category,
        string ownerType,
        Guid? ownerId,
        string? caption = null,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 8)
            ext = GuessExtension(contentType);

        var id = Guid.NewGuid();
        var fileName = id + ext.ToLowerInvariant();
        var relativeDir = Path.Combine("uploads", auditId.ToString("D"));
        var root = ResolveRoot();
        var destDir = Path.IsPathRooted(options.Value.RootPath)
            ? Path.Combine(root, auditId.ToString("D"))
            : Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), relativeDir);

        Directory.CreateDirectory(destDir);
        var destPath = Path.Combine(destDir, fileName);
        await using (var fs = File.Create(destPath))
            await content.CopyToAsync(fs, ct);

        var publicRelative = Path.Combine("uploads", auditId.ToString("D"), fileName).Replace('\\', '/');
        var photo = new SitePhoto
        {
            Id = id,
            AuditId = auditId,
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category,
            FileName = originalFileName,
            RelativePath = publicRelative,
            Caption = caption,
            OwnerType = ownerType,
            OwnerId = ownerId,
            UploadedAt = DateTime.UtcNow
        };
        db.Photos.Add(photo);
        await db.SaveChangesAsync(ct);
        return photo;
    }

    public async Task DeleteAsync(Guid photoId, CancellationToken ct = default)
    {
        var photo = await db.Photos.FirstOrDefaultAsync(p => p.Id == photoId, ct);
        if (photo is null) return;
        TryDeleteFile(photo);
        db.Photos.Remove(photo);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<SitePhoto>> ForOwnerAsync(Guid auditId, string ownerType, Guid? ownerId, CancellationToken ct = default) =>
        await db.Photos
            .Where(p => p.AuditId == auditId && p.OwnerType == ownerType && p.OwnerId == ownerId)
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync(ct);

    public async Task<List<SitePhoto>> ForAuditAsync(Guid auditId, CancellationToken ct = default) =>
        await db.Photos.Where(p => p.AuditId == auditId).OrderByDescending(p => p.UploadedAt).ToListAsync(ct);

    public void TryDeleteFile(SitePhoto photo)
    {
        try
        {
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var path = Path.Combine(webRoot, photo.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    private static string GuessExtension(string? contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        _ => ".jpg"
    };
}
