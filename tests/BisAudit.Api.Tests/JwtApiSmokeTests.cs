using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BisAudit.Api.Controllers;

namespace BisAudit.Api.Tests;

public class JwtApiSmokeTests
{
    [Fact]
    public async Task Login_jwt_authorizes_audits_list_and_overview_round_trip()
    {
        await using var host = await AuthTestHost.StartAsync();

        var unauthList = await host.Client.GetAsync("/api/audits");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthList.StatusCode);

        var login = await host.Client.PostAsJsonAsync("/api/auth/login", new { email = AuthTestHost.Email, password = AuthTestHost.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginBody = await login.Content.ReadFromJsonAsync<AuthController.LoginResponse>(AuthTestHost.Json);
        Assert.False(string.IsNullOrWhiteSpace(loginBody?.Token));
        Assert.True(loginBody!.IsAdmin);

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);

        var created = await host.Client.PostAsJsonAsync("/api/audits", new { companyName = "Swagger Smoke Co" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdDoc.RootElement.GetProperty("id").GetGuid();

        var list = await host.Client.GetAsync("/api/audits");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains("Swagger Smoke Co", await list.Content.ReadAsStringAsync());

        var overview = await host.Client.GetAsync($"/api/audits/{id}");
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);

        var put = await host.Client.PutAsJsonAsync($"/api/audits/{id}", new
        {
            companyName = "Swagger Smoke Co",
            industry = "Legal",
            employeeCount = 12,
            address = "1 Harbor Way",
            status = "In Progress",
            auditDate = DateTime.Today,
            preparedBy = "QA",
            auditScope = "Phase 1",
            executiveSummary = "Smoke",
            previousItSupport = "None",
            contacts = Array.Empty<object>(),
            subLocations = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Contains("In Progress", await put.Content.ReadAsStringAsync());

        host.Client.DefaultRequestHeaders.Authorization = null;
        var unauthPut = await host.Client.PutAsJsonAsync($"/api/audits/{id}", new { companyName = "Nope" });
        Assert.Equal(HttpStatusCode.Unauthorized, unauthPut.StatusCode);
    }
}
