using BisAudit.Api.Data;
using BisAudit.Api.Identity;
using BisAudit.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BisAudit.Api.Services;

public sealed class UserActionResult
{
    public bool Succeeded { get; init; }
    public int Status { get; init; } = StatusCodes.Status200OK;
    public string? Message { get; init; }
    public Dictionary<string, List<string>> Errors { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public UserListItemDto? User { get; init; }

    public static UserActionResult Ok(UserListItemDto? user = null, string? message = null) =>
        new() { Succeeded = true, User = user, Message = message };

    public static UserActionResult Fail(int status, string message, Dictionary<string, List<string>>? errors = null) =>
        new()
        {
            Succeeded = false,
            Status = status,
            Message = message,
            Errors = errors ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        };
}

public class UserService(UserManager<ApplicationUser> users)
{
    public async Task<IReadOnlyList<UserListItemDto>> ListAsync()
    {
        var all = await users.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync();
        var list = new List<UserListItemDto>(all.Count);
        foreach (var user in all)
            list.Add(await ToDtoAsync(user));
        return list;
    }

    public async Task<UserActionResult> CreateAsync(CreateUserRequest request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var email = (request.Email ?? "").Trim();
        var password = request.Password ?? "";
        var confirm = request.ConfirmPassword ?? "";

        if (string.IsNullOrWhiteSpace(email))
            Add(errors, "email", "Email is required.");
        else if (email.IndexOf('@') < 1)
            Add(errors, "email", "Enter a valid email address.");

        if (string.IsNullOrEmpty(password))
            Add(errors, "password", "Password is required.");

        if (password != confirm)
            Add(errors, "confirmPassword", "Password and confirmation do not match.");

        if (errors.Count > 0)
            return UserActionResult.Fail(StatusCodes.Status400BadRequest, "Could not create user.", errors);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            LockoutEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                Add(errors, FieldFor(error), error.Description);
            return UserActionResult.Fail(StatusCodes.Status400BadRequest, "Could not create user.", errors);
        }

        return UserActionResult.Ok(await ToDtoAsync(user));
    }

    public async Task<UserActionResult> DisableAsync(string id)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null)
            return UserActionResult.Fail(StatusCodes.Status404NotFound, "User not found.");

        if (await users.IsLockedOutAsync(user))
            return UserActionResult.Ok(await ToDtoAsync(user));

        if (await users.IsInRoleAsync(user, AppRoles.Admin) && await CountEnabledAdminsAsync() <= 1)
        {
            return UserActionResult.Fail(
                StatusCodes.Status400BadRequest,
                "Cannot disable the last remaining admin.");
        }

        await users.SetLockoutEnabledAsync(user, true);
        await users.SetLockoutEndDateAsync(user, UserDisableLockout.Until);
        return UserActionResult.Ok(await ToDtoAsync(user));
    }

    public async Task<UserActionResult> EnableAsync(string id)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null)
            return UserActionResult.Fail(StatusCodes.Status404NotFound, "User not found.");

        await users.SetLockoutEndDateAsync(user, null);
        await users.SetLockoutEnabledAsync(user, true);
        return UserActionResult.Ok(await ToDtoAsync(user));
    }

    public async Task<int> CountEnabledAdminsAsync()
    {
        var admins = await users.GetUsersInRoleAsync(AppRoles.Admin);
        var count = 0;
        foreach (var admin in admins)
        {
            if (!await users.IsLockedOutAsync(admin))
                count++;
        }

        return count;
    }

    private async Task<UserListItemDto> ToDtoAsync(ApplicationUser user)
    {
        var isAdmin = await users.IsInRoleAsync(user, AppRoles.Admin);
        var active = !await users.IsLockedOutAsync(user);
        return new UserListItemDto(
            user.Id,
            user.Email ?? user.UserName ?? "",
            active,
            isAdmin,
            user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt);
    }

    private static void Add(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var list))
        {
            list = [];
            errors[field] = list;
        }

        list.Add(message);
    }

    private static string FieldFor(IdentityError error) => error.Code switch
    {
        "DuplicateEmail" or "InvalidEmail" => "email",
        "DuplicateUserName" or "InvalidUserName" => "email",
        "PasswordTooShort" or "PasswordRequiresNonAlphanumeric" or "PasswordRequiresDigit"
            or "PasswordRequiresLower" or "PasswordRequiresUpper" or "PasswordRequiresUniqueChars"
            or "PasswordMismatch" => "password",
        _ => "password"
    };
}
