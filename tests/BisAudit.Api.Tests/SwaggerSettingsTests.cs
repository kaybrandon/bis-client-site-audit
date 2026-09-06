using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BisAudit.Api.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace BisAudit.Api.Tests;

public class SwaggerSettingsTests
{
    private const string TechPassword = "Tech!23456";

    [Fact]
    public async Task Admin_toggle_on_serves_swagger_off_is_404()
    {
        await using var host = await AuthTestHost.StartAsync();
        await host.UseAdminAsync();

        var off = await host.Client.PutAsJsonAsync("/api/settings/swagger", new { enabled = false });
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        var offBody = await off.Content.ReadFromJsonAsync<SwaggerSettingDto>(AuthTestHost.Json);
        Assert.False(offBody!.Enabled);

        host.ClearAuth();
        foreach (var path in new[] { "/swagger", "/swagger/", "/swagger/index.html", "/swagger/v1/swagger.json" })
        {
            var response = await host.Client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("SPA-SHELL", body);
            Assert.DoesNotContain("swagger-ui", body, StringComparison.OrdinalIgnoreCase);
        }

        await host.UseAdminAsync();
        var on = await host.Client.PutAsJsonAsync("/api/settings/swagger", new { enabled = true });
        Assert.Equal(HttpStatusCode.OK, on.StatusCode);
        var onBody = await on.Content.ReadFromJsonAsync<SwaggerSettingDto>(AuthTestHost.Json);
        Assert.True(onBody!.Enabled);

        var stored = await host.Client.GetFromJsonAsync<SwaggerSettingDto>("/api/settings/swagger", AuthTestHost.Json);
        Assert.True(stored!.Enabled);

        host.ClearAuth();
        var ui = await host.Client.GetAsync("/swagger");
        Assert.Equal(HttpStatusCode.OK, ui.StatusCode);
        Assert.Contains("swagger", await ui.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var spec = await host.Client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, spec.StatusCode);
        using var doc = JsonDocument.Parse(await spec.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
    }

    [Fact]
    public async Task Non_admin_cannot_read_or_toggle_swagger()
    {
        await using var host = await AuthTestHost.StartAsync();

        var anonGet = await host.Client.GetAsync("/api/settings/swagger");
        Assert.Equal(HttpStatusCode.Unauthorized, anonGet.StatusCode);
        var anonPut = await host.Client.PutAsJsonAsync("/api/settings/swagger", new { enabled = true });
        Assert.Equal(HttpStatusCode.Unauthorized, anonPut.StatusCode);

        await host.UseAdminAsync();
        var email = $"tech-{Guid.NewGuid():N}@example.com";
        var created = await host.Client.PostAsJsonAsync("/api/users", new
        {
            email,
            password = TechPassword,
            confirmPassword = TechPassword
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        host.ClearAuth();
        var techToken = await host.LoginAsync(email, TechPassword);
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", techToken);

        var forbiddenGet = await host.Client.GetAsync("/api/settings/swagger");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenGet.StatusCode);
        var forbiddenPut = await host.Client.PutAsJsonAsync("/api/settings/swagger", new { enabled = true });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenPut.StatusCode);

        var audits = await host.Client.GetAsync("/api/audits");
        Assert.Equal(HttpStatusCode.OK, audits.StatusCode);
    }

    [Fact]
    public async Task Enabling_swagger_does_not_open_anonymous_api()
    {
        await using var host = await AuthTestHost.StartAsync();
        await host.UseAdminAsync();
        var enable = await host.Client.PutAsJsonAsync("/api/settings/swagger", new { enabled = true });
        Assert.Equal(HttpStatusCode.OK, enable.StatusCode);

        host.ClearAuth();
        var swagger = await host.Client.GetAsync("/swagger");
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);

        var unauth = await host.Client.GetAsync("/api/audits");
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);

        var token = await host.LoginAsync(AuthTestHost.Email, AuthTestHost.Password);
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var list = await host.Client.GetAsync("/api/audits");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }

    [Fact]
    public async Task Swagger_setting_survives_host_restart()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = "BisAuditSwagger-" + Guid.NewGuid().ToString("N");

        await using (var host = await AuthTestHost.StartAsync(databaseRoot, databaseName))
        {
            await host.UseAdminAsync();
            var put = await host.Client.PutAsJsonAsync("/api/settings/swagger", new { enabled = true });
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        }

        await using (var restarted = await AuthTestHost.StartAsync(databaseRoot, databaseName))
        {
            var ui = await restarted.Client.GetAsync("/swagger");
            Assert.Equal(HttpStatusCode.OK, ui.StatusCode);

            await restarted.UseAdminAsync();
            var setting = await restarted.Client.GetFromJsonAsync<SwaggerSettingDto>("/api/settings/swagger", AuthTestHost.Json);
            Assert.True(setting!.Enabled);

            var off = await restarted.Client.PutAsJsonAsync("/api/settings/swagger", new { enabled = false });
            Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        }

        await using var third = await AuthTestHost.StartAsync(databaseRoot, databaseName);
        var missing = await third.Client.GetAsync("/swagger");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
