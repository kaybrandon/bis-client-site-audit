using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BisAudit.Api.Controllers;
using BisAudit.Api.Models;

namespace BisAudit.Api.Tests;

public class UserManagementTests
{
    private const string TechPassword = "Tech!23456";

    [Fact]
    public async Task Users_api_is_admin_only()
    {
        await using var host = await AuthTestHost.StartAsync();

        var anon = await host.Client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);

        var register = await host.Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "public@example.com",
            password = TechPassword
        });
        Assert.Equal(HttpStatusCode.NotFound, register.StatusCode);

        await host.UseAdminAsync();
        var created = await host.Client.PostAsJsonAsync("/api/users", new
        {
            email = UniqueEmail("tech"),
            password = TechPassword,
            confirmPassword = TechPassword
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdJson = await created.Content.ReadAsStringAsync();
        var tech = JsonSerializer.Deserialize<UserListItemDto>(createdJson, AuthTestHost.Json);
        Assert.False(string.IsNullOrWhiteSpace(tech?.Id));
        Assert.False(tech!.IsAdmin);
        Assert.True(tech.Active);
        Assert.DoesNotContain("password", createdJson, StringComparison.OrdinalIgnoreCase);

        host.ClearAuth();
        var techToken = await host.LoginAsync(tech.Email, TechPassword);
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", techToken);

        var forbidden = await host.Client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var forbiddenCreate = await host.Client.PostAsJsonAsync("/api/users", new
        {
            email = UniqueEmail("other"),
            password = TechPassword,
            confirmPassword = TechPassword
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCreate.StatusCode);

        var audits = await host.Client.GetAsync("/api/audits");
        Assert.Equal(HttpStatusCode.OK, audits.StatusCode);
    }

    [Fact]
    public async Task Admin_can_add_user_and_list_never_includes_password()
    {
        await using var host = await AuthTestHost.StartAsync();
        await host.UseAdminAsync();

        var email = UniqueEmail("added");
        var created = await host.Client.PostAsJsonAsync("/api/users", new
        {
            email,
            password = TechPassword,
            confirmPassword = TechPassword
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var user = await created.Content.ReadFromJsonAsync<UserListItemDto>(AuthTestHost.Json);
        Assert.Equal(email, user?.Email);
        Assert.True(user!.Active);
        Assert.False(user.IsAdmin);
        Assert.True(user.CreatedAt > DateTime.UtcNow.AddMinutes(-5));

        var list = await host.Client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var json = await list.Content.ReadAsStringAsync();
        Assert.Contains(email, json);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(json);
        Assert.All(doc.RootElement.EnumerateArray(), row =>
        {
            Assert.True(row.TryGetProperty("email", out _));
            Assert.True(row.TryGetProperty("active", out _));
            Assert.True(row.TryGetProperty("createdAt", out _));
            Assert.False(row.TryGetProperty("password", out _));
            Assert.False(row.TryGetProperty("passwordHash", out _));
        });
    }

    [Fact]
    public async Task Add_user_returns_identity_errors_under_fields()
    {
        await using var host = await AuthTestHost.StartAsync();
        await host.UseAdminAsync();

        var weak = await host.Client.PostAsJsonAsync("/api/users", new
        {
            email = UniqueEmail("weak"),
            password = "short",
            confirmPassword = "short"
        });
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        using var weakDoc = JsonDocument.Parse(await weak.Content.ReadAsStringAsync());
        Assert.True(weakDoc.RootElement.GetProperty("errors").TryGetProperty("password", out var passwordErrors));
        Assert.True(passwordErrors.GetArrayLength() > 0);

        var mismatch = await host.Client.PostAsJsonAsync("/api/users", new
        {
            email = UniqueEmail("mismatch"),
            password = TechPassword,
            confirmPassword = "Tech!99999"
        });
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
        using var mismatchDoc = JsonDocument.Parse(await mismatch.Content.ReadAsStringAsync());
        Assert.True(mismatchDoc.RootElement.GetProperty("errors").TryGetProperty("confirmPassword", out var confirmErrors));
        Assert.Contains("match", confirmErrors[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Disable_blocks_login_and_reenable_restores_it()
    {
        await using var host = await AuthTestHost.StartAsync();
        await host.UseAdminAsync();

        var email = UniqueEmail("lock");
        var created = await host.Client.PostAsJsonAsync("/api/users", new
        {
            email,
            password = TechPassword,
            confirmPassword = TechPassword
        });
        var user = await created.Content.ReadFromJsonAsync<UserListItemDto>(AuthTestHost.Json);
        Assert.NotNull(user);

        host.ClearAuth();
        var before = await host.Client.PostAsJsonAsync("/api/auth/login", new { email, password = TechPassword });
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        await host.UseAdminAsync();
        var disable = await host.Client.PostAsJsonAsync($"/api/users/{user!.Id}/disable", new { });
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);
        var disabled = await disable.Content.ReadFromJsonAsync<UserListItemDto>(AuthTestHost.Json);
        Assert.False(disabled!.Active);

        host.ClearAuth();
        var blocked = await host.Client.PostAsJsonAsync("/api/auth/login", new { email, password = TechPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, blocked.StatusCode);

        await host.UseAdminAsync();
        var enable = await host.Client.PostAsJsonAsync($"/api/users/{user.Id}/enable", new { });
        Assert.Equal(HttpStatusCode.OK, enable.StatusCode);
        var enabled = await enable.Content.ReadFromJsonAsync<UserListItemDto>(AuthTestHost.Json);
        Assert.True(enabled!.Active);

        host.ClearAuth();
        var after = await host.Client.PostAsJsonAsync("/api/auth/login", new { email, password = TechPassword });
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        var login = await after.Content.ReadFromJsonAsync<AuthController.LoginResponse>(AuthTestHost.Json);
        Assert.False(login!.IsAdmin);
    }

    [Fact]
    public async Task Cannot_disable_the_last_remaining_admin()
    {
        await using var host = await AuthTestHost.StartAsync();
        await host.UseAdminAsync();

        var list = await host.Client.GetFromJsonAsync<List<UserListItemDto>>("/api/users", AuthTestHost.Json);
        var admin = Assert.Single(list!, u => u.IsAdmin);
        Assert.True(admin.Active);

        var disable = await host.Client.PostAsJsonAsync($"/api/users/{admin.Id}/disable", new { });
        Assert.Equal(HttpStatusCode.BadRequest, disable.StatusCode);
        var body = await disable.Content.ReadAsStringAsync();
        Assert.Contains("last remaining admin", body, StringComparison.OrdinalIgnoreCase);

        host.ClearAuth();
        var login = await host.Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = AuthTestHost.Email,
            password = AuthTestHost.Password
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginBody = await login.Content.ReadFromJsonAsync<AuthController.LoginResponse>(AuthTestHost.Json);
        Assert.True(loginBody!.IsAdmin);
    }

    private static string UniqueEmail(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}@example.com";
}
