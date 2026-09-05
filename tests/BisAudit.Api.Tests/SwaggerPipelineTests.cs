using System.Net;
using System.Text.Json;
using BisAudit.Api.Controllers;
using BisAudit.Api.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BisAudit.Api.Tests;

public class SwaggerPipelineTests
{
    [Fact]
    public async Task Enabled_serves_ui_assets_and_jwt_openapi()
    {
        await using var host = await PipelineHost.StartAsync(swaggerEnabled: true);

        var ui = await host.Client.GetAsync("/swagger");
        Assert.Equal(HttpStatusCode.OK, ui.StatusCode);
        var uiHtml = await ui.Content.ReadAsStringAsync();
        Assert.Contains("swagger", uiHtml, StringComparison.OrdinalIgnoreCase);

        var index = await host.Client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, index.StatusCode);

        var spec = await host.Client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, spec.StatusCode);
        using var doc = JsonDocument.Parse(await spec.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.True(root.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out var bearer));
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());

        var paths = root.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/auth/login", out _));
        Assert.True(paths.TryGetProperty("/api/audits", out var audits));
        Assert.True(audits.TryGetProperty("get", out _));
        Assert.True(paths.TryGetProperty("/api/audits/{id}", out var overview));
        Assert.True(overview.TryGetProperty("get", out _));
        Assert.True(overview.TryGetProperty("put", out _));

        var spa = await host.Client.GetAsync("/dashboard");
        Assert.Equal(HttpStatusCode.OK, spa.StatusCode);
        Assert.Equal("SPA-SHELL", (await spa.Content.ReadAsStringAsync()).Trim());
    }

    [Fact]
    public async Task Disabled_swagger_is_404_not_spa_shell()
    {
        await using var host = await PipelineHost.StartAsync(swaggerEnabled: false);

        foreach (var path in new[] { "/swagger", "/swagger/", "/swagger/index.html", "/swagger/v1/swagger.json", "/swagger/swagger-ui.css" })
        {
            var response = await host.Client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("SPA-SHELL", body);
        }

        var spa = await host.Client.GetAsync("/dashboard");
        Assert.Equal(HttpStatusCode.OK, spa.StatusCode);
        Assert.Equal("SPA-SHELL", (await spa.Content.ReadAsStringAsync()).Trim());

        var apiMiss = await host.Client.GetAsync("/api/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, apiMiss.StatusCode);
        Assert.DoesNotContain("SPA-SHELL", await apiMiss.Content.ReadAsStringAsync());
    }

    private sealed class PipelineHost : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly string _root;

        public HttpClient Client { get; }

        private PipelineHost(WebApplication app, string root, HttpClient client)
        {
            _app = app;
            _root = root;
            Client = client;
        }

        public static async Task<PipelineHost> StartAsync(bool swaggerEnabled)
        {
            var root = Directory.CreateTempSubdirectory("bis-swagger-").FullName;
            var webRoot = Path.Combine(root, "wwwroot");
            Directory.CreateDirectory(webRoot);
            await File.WriteAllTextAsync(Path.Combine(webRoot, "index.html"), "SPA-SHELL");

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(AuditsController).Assembly.GetName().Name,
                ContentRootPath = root,
                WebRootPath = webRoot,
                EnvironmentName = Environments.Production
            });
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Services.AddControllers();
            builder.Services.AddBisAuditSwagger();

            var app = builder.Build();
            app.UseBisAuditSwagger(swaggerEnabled);
            app.UseStaticFiles();
            app.MapControllers();
            app.MapBisAuditSpaFallback();
            await app.StartAsync();

            var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            return new PipelineHost(app, root, client);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.StopAsync();
            await _app.DisposeAsync();
            try { Directory.Delete(_root, recursive: true); }
            catch (IOException) { }
        }
    }
}
