using BisAudit.Api.Data.Entities;
using BisAudit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BisAudit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/audits/{auditId:guid}")]
public class ItemsController(AuditService audits) : ControllerBase
{
    [HttpGet("workstations")]
    public async Task<IActionResult> Workstations(Guid auditId) =>
        Ok((await audits.GetAsync(auditId))?.Workstations ?? []);

    [HttpPost("workstations")]
    public async Task<IActionResult> SaveWorkstation(Guid auditId, [FromBody] WorkstationItem item)
    {
        item.AuditId = auditId;
        await audits.SaveItemAsync(item);
        return Ok(item);
    }

    [HttpDelete("workstations/{itemId:guid}")]
    public async Task<IActionResult> DeleteWorkstation(Guid itemId)
    {
        await audits.DeleteItemAsync<WorkstationItem>(itemId);
        return NoContent();
    }

    [HttpGet("network")]
    public async Task<IActionResult> Network(Guid auditId) =>
        Ok((await audits.GetAsync(auditId))?.NetworkItems ?? []);

    [HttpPost("network")]
    public async Task<IActionResult> SaveNetwork(Guid auditId, [FromBody] NetworkItem item)
    {
        item.AuditId = auditId;
        await audits.SaveItemAsync(item);
        return Ok(item);
    }

    [HttpDelete("network/{itemId:guid}")]
    public async Task<IActionResult> DeleteNetwork(Guid itemId)
    {
        await audits.DeleteItemAsync<NetworkItem>(itemId);
        return NoContent();
    }

    [HttpGet("servers")]
    public async Task<IActionResult> Servers(Guid auditId) =>
        Ok((await audits.GetAsync(auditId))?.ServerStorageItems ?? []);

    [HttpPost("servers")]
    public async Task<IActionResult> SaveServer(Guid auditId, [FromBody] ServerStorageItem item)
    {
        item.AuditId = auditId;
        await audits.SaveItemAsync(item);
        return Ok(item);
    }

    [HttpDelete("servers/{itemId:guid}")]
    public async Task<IActionResult> DeleteServer(Guid itemId)
    {
        await audits.DeleteItemAsync<ServerStorageItem>(itemId);
        return NoContent();
    }

    [HttpGet("security")]
    public async Task<IActionResult> Security(Guid auditId) =>
        Ok((await audits.GetAsync(auditId))?.SecurityAvItems ?? []);

    [HttpPost("security")]
    public async Task<IActionResult> SaveSecurity(Guid auditId, [FromBody] SecurityAvItem item)
    {
        item.AuditId = auditId;
        await audits.SaveItemAsync(item);
        return Ok(item);
    }

    [HttpDelete("security/{itemId:guid}")]
    public async Task<IActionResult> DeleteSecurity(Guid itemId)
    {
        await audits.DeleteItemAsync<SecurityAvItem>(itemId);
        return NoContent();
    }

    [HttpGet("software")]
    public async Task<IActionResult> Software(Guid auditId) =>
        Ok((await audits.GetAsync(auditId))?.SoftwareItems ?? []);

    [HttpPost("software")]
    public async Task<IActionResult> SaveSoftware(Guid auditId, [FromBody] SoftwareItem item)
    {
        item.AuditId = auditId;
        await audits.SaveItemAsync(item);
        return Ok(item);
    }

    [HttpDelete("software/{itemId:guid}")]
    public async Task<IActionResult> DeleteSoftware(Guid itemId)
    {
        await audits.DeleteItemAsync<SoftwareItem>(itemId);
        return NoContent();
    }

    [HttpGet("issues")]
    public async Task<IActionResult> Issues(Guid auditId) =>
        Ok((await audits.GetAsync(auditId))?.Issues ?? []);

    [HttpPost("issues")]
    public async Task<IActionResult> SaveIssue(Guid auditId, [FromBody] IssueItem item)
    {
        item.AuditId = auditId;
        item.PriorityRank = Math.Clamp(item.PriorityRank, 1, 5);
        await audits.SaveItemAsync(item);
        return Ok(item);
    }

    [HttpDelete("issues/{itemId:guid}")]
    public async Task<IActionResult> DeleteIssue(Guid itemId)
    {
        await audits.DeleteItemAsync<IssueItem>(itemId);
        return NoContent();
    }

    [HttpGet("purchases")]
    public async Task<IActionResult> Purchases(Guid auditId) =>
        Ok((await audits.GetAsync(auditId))?.Purchases ?? []);

    [HttpPost("purchases")]
    public async Task<IActionResult> SavePurchase(Guid auditId, [FromBody] PurchaseItem item)
    {
        item.AuditId = auditId;
        if (item.Quantity < 0) item.Quantity = 0;
        await audits.SaveItemAsync(item);
        return Ok(item);
    }

    [HttpDelete("purchases/{itemId:guid}")]
    public async Task<IActionResult> DeletePurchase(Guid itemId)
    {
        await audits.DeleteItemAsync<PurchaseItem>(itemId);
        return NoContent();
    }
}
