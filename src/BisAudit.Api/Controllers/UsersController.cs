using BisAudit.Api.Identity;
using BisAudit.Api.Models;
using BisAudit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BisAudit.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/users")]
public class UsersController(UserService users, ILogger<UsersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> List() =>
        Ok(await users.ListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var result = await users.CreateAsync(request);
        if (!result.Succeeded)
            return Error(result);
        logger.LogInformation("Admin created user {Email}", result.User?.Email);
        return Created($"/api/users/{result.User!.Id}", result.User);
    }

    [HttpPost("{id}/disable")]
    public async Task<IActionResult> Disable(string id)
    {
        var result = await users.DisableAsync(id);
        if (!result.Succeeded)
            return Error(result);
        logger.LogInformation("Admin disabled user {Email}", result.User?.Email);
        return Ok(result.User);
    }

    [HttpPost("{id}/enable")]
    public async Task<IActionResult> Enable(string id)
    {
        var result = await users.EnableAsync(id);
        if (!result.Succeeded)
            return Error(result);
        logger.LogInformation("Admin re-enabled user {Email}", result.User?.Email);
        return Ok(result.User);
    }

    private ObjectResult Error(UserActionResult result) =>
        StatusCode(result.Status, new { message = result.Message, errors = result.Errors });
}
