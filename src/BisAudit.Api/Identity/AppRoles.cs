namespace BisAudit.Api.Identity;

public static class AppRoles
{
    public const string Admin = "Admin";
}

/// <summary>
/// Identity lockout used as a soft-disable. Login and JWT validation treat this as disabled.
/// </summary>
public static class UserDisableLockout
{
    public static readonly DateTimeOffset Until = new(9999, 12, 31, 0, 0, 0, TimeSpan.Zero);

    public static bool IsDisabled(DateTimeOffset? lockoutEnd, bool lockoutEnabled) =>
        lockoutEnabled && lockoutEnd is { } end && end > DateTimeOffset.UtcNow;
}
