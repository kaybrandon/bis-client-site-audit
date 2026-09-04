using BisAudit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BisAudit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/settings")]
public class SettingsController(DropdownService dropdowns) : ControllerBase
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
}
