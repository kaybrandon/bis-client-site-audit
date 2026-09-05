using BisAudit.Api.Data.Entities;
using BisAudit.Api.Dtos;
using BisAudit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BisAudit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/audits")]
public class AuditsController(AuditService audits) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await audits.ListSummariesAsync();
        return Ok(items.Select(ToSummary));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var audit = await audits.GetAsync(id);
        return audit is null ? NotFound() : Ok(ToDetail(audit));
    }

    public record CreateAuditRequest(string CompanyName);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAuditRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.CompanyName))
            return BadRequest(new { message = "Company name is required." });
        var audit = await audits.CreateAsync(body.CompanyName);
        return Created($"/api/audits/{audit.Id}", ToDetail(audit));
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id)
    {
        var copy = await audits.DuplicateAsync(id);
        return Ok(ToDetail(copy));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> SaveOverview(Guid id, [FromBody] OverviewSaveRequest body)
    {
        body.Id = id;
        var incoming = body.ToEntity();
        var locations = (body.SubLocations ?? []).Select(l => l.ToEntity()).ToList();
        incoming.SubLocations.Clear();
        await audits.SaveOverviewAsync(incoming, locations);
        var updated = await audits.GetAsync(id);
        return Ok(ToDetail(updated!));
    }

    [HttpGet("{id:guid}/contacts.csv")]
    public async Task<IActionResult> ContactsCsv(Guid id)
    {
        var audit = await audits.GetAsync(id);
        if (audit is null) return NotFound();
        var csv = ContactExport.ToCsv(audit);
        var safe = string.Concat(audit.CompanyName.Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)));
        if (string.IsNullOrWhiteSpace(safe)) safe = "contacts";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"{safe}-contacts.csv");
    }

    [HttpGet("{id:guid}/report")]
    public async Task<IActionResult> Report(Guid id)
    {
        var audit = await audits.GetAsync(id);
        return audit is null ? NotFound() : Ok(ToDetail(audit));
    }

    private static object ToSummary(ClientAudit a)
    {
        var progress = ProgressCalculator.Calculate(a);
        var metrics = ReportMetrics.From(a);
        return new
        {
            a.Id,
            a.CompanyName,
            a.Industry,
            a.EmployeeCount,
            a.Status,
            a.AuditDate,
            a.ClientPhotoPath,
            a.UpdatedAt,
            progress = progress.Percent,
            sections = progress.Sections,
            openItems = metrics.OpenIssueCount,
            openCriticalHigh = metrics.CriticalHighCount,
            metrics
        };
    }

    private static object ToDetail(ClientAudit a)
    {
        var progress = ProgressCalculator.Calculate(a);
        var metrics = ReportMetrics.From(a);
        return new
        {
            a.Id,
            a.CompanyName,
            a.Industry,
            a.EmployeeCount,
            a.Address,
            a.Status,
            a.AuditDate,
            a.PreparedBy,
            a.AuditScope,
            a.ExecutiveSummary,
            a.PreviousItSupport,
            a.ClientPhotoPath,
            a.CreatedAt,
            a.UpdatedAt,
            contacts = (a.Contacts ?? []).Select(c => new
            {
                c.Id,
                c.AuditId,
                c.Role,
                c.Name,
                c.Title,
                c.Email,
                c.Phone
            }),
            subLocations = a.SubLocations,
            workstations = a.Workstations,
            networkItems = a.NetworkItems,
            serverStorageItems = a.ServerStorageItems,
            securityAvItems = a.SecurityAvItems,
            softwareItems = a.SoftwareItems,
            issues = a.Issues,
            purchases = a.Purchases,
            photos = a.Photos,
            progress = progress.Percent,
            sections = progress.Sections,
            openItems = metrics.OpenIssueCount,
            openCriticalHigh = metrics.CriticalHighCount,
            metrics
        };
    }
}
