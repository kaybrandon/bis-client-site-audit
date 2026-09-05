using BisAudit.Api.Data.Entities;
using BisAudit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BisAudit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class PhotosController(PhotoService photos, AuditService audits) : ControllerBase
{
    [HttpGet("audits/{auditId:guid}/photos")]
    public async Task<IActionResult> List(Guid auditId) =>
        Ok(await photos.ForAuditAsync(auditId));

    [HttpPost("audits/{auditId:guid}/photos")]
    [RequestSizeLimit(12_000_000)]
    public async Task<IActionResult> Upload(
        Guid auditId,
        IFormFile file,
        [FromForm] string? category,
        [FromForm] string? ownerType,
        [FromForm] Guid? ownerId,
        [FromForm] string? caption)
    {
        if (file.Length == 0) return BadRequest(new { message = "File is required." });
        await using var stream = file.OpenReadStream();
        var saved = await photos.SaveAsync(
            auditId,
            stream,
            file.FileName,
            file.ContentType,
            category ?? AuditService.DefaultPhotoCategoryFor(ownerType ?? PhotoOwnerTypes.Audit),
            ownerType ?? PhotoOwnerTypes.Audit,
            ownerId,
            caption);
        await audits.TouchAsync(auditId);
        return Ok(saved);
    }

    [HttpDelete("photos/{photoId:guid}")]
    public async Task<IActionResult> Delete(Guid photoId)
    {
        await photos.DeleteAsync(photoId);
        return NoContent();
    }
}
