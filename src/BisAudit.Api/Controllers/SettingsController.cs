using BisAudit.Api.Identity;
using BisAudit.Api.Models;
using BisAudit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BisAudit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/settings")]
public class SettingsController(DropdownService dropdowns, ISwaggerEnablement swagger) : ControllerBase
{
    [HttpGet("dropdowns")]
    public async Task<IActionResult> List()
    {
        var lists = await dropdowns.GetAllAsync();
        return Ok(lists.Select(l => new
        {
            l.Id,
            l.ListKey,
            l.DisplayName,
            l.SortOrder,
            options = l.Options.OrderBy(o => o.SortOrder).Select(o => o.Value).ToList()
        }));
    }

    [HttpGet("dropdowns/{key}")]
    public async Task<IActionResult> Options(string key) =>
        Ok(await dropdowns.OptionsAsync(key));

    public record SaveDropdownRequest(List<string> Options);

    [HttpPut("dropdowns/{id:guid}")]
    public async Task<IActionResult> Save(Guid id, [FromBody] SaveDropdownRequest body)
    {
        await dropdowns.SaveListAsync(id, body.Options);
        return NoContent();
    }

    [HttpGet("swagger")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<SwaggerSettingDto>> GetSwagger(CancellationToken cancellationToken) =>
        Ok(new SwaggerSettingDto(await swagger.IsEnabledAsync(cancellationToken)));

    [HttpPut("swagger")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<SwaggerSettingDto>> SaveSwagger(
        [FromBody] SaveSwaggerSettingRequest body,
        CancellationToken cancellationToken)
    {
        await swagger.SetEnabledAsync(body.Enabled, cancellationToken);
        return Ok(new SwaggerSettingDto(body.Enabled));
    }
}
