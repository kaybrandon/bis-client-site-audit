using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BisAudit.Api.Data;
using Microsoft.IdentityModel.Tokens;

namespace BisAudit.Api.Services;

public class TokenService(IConfiguration config)
{
    public string Create(ApplicationUser user)
    {
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var minutes = int.TryParse(config["Jwt:ExpiresMinutes"], out var m) ? m : 720;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Email ?? user.UserName ?? "")
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
