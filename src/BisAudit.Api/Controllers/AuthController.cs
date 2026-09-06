using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BisAudit.Api.Data;
using BisAudit.Api.Identity;
using BisAudit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BisAudit.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<ApplicationUser> users, TokenService tokens, ILogger<AuthController> logger) : ControllerBase
{
    public record LoginRequest(string Email, string Password);
    public record LoginResponse(string Token, string Email, DateTime ExpiresUtc, bool IsAdmin);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !await users.CheckPasswordAsync(user, request.Password) || await users.IsLockedOutAsync(user))
        {
            logger.LogWarning("Failed login for {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var token = await tokens.CreateAsync(user);
        var minutes = int.TryParse(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:ExpiresMinutes"], out var m) ? m : 720;
        var isAdmin = await users.IsInRoleAsync(user, AppRoles.Admin);
        logger.LogInformation("User {Email} signed in", user.Email);
        return new LoginResponse(token, user.Email ?? request.Email, DateTime.UtcNow.AddMinutes(minutes), isAdmin);
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> Me()
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue(ClaimTypes.Name)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Email);
        return Ok(new { email, isAdmin = User.IsInRole(AppRoles.Admin) });
    }
}
