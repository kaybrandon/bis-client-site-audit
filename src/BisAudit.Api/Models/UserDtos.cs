namespace BisAudit.Api.Models;

public record UserListItemDto(string Id, string Email, bool Active, bool IsAdmin, DateTime CreatedAt);

public record CreateUserRequest(string Email, string Password, string ConfirmPassword);
