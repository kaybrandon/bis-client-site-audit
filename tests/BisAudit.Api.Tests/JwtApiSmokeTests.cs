using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BisAudit.Api.Controllers;
using BisAudit.Api.Data;
using BisAudit.Api.Options;
using BisAudit.Api.Services;
using BisAudit.Api.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace BisAudit.Api.Tests;

public class JwtApiSmokeTests
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Login_jwt_authorizes_audits_list_and_overview_round_trip()
    {
        await using var host = await AuthHost.StartAsync();

        var unauthList = await host.Client.GetAsync("/api/audits");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthList.StatusCode);

        var login = await host.Client.PostAsJsonAsync("/api/auth/login", new { email = AuthHost.Email, password = AuthHost.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginBody = await login.Content.ReadFromJsonAsync<AuthController.LoginResponse>(Json);
        Assert.False(string.IsNullOrWhiteSpace(loginBody?.Token));

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.Token);

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

    private sealed class AuthHost : IAsyncDisposable
    {
        public const string Email = "admin@bis.local";
        public const string Password = "Admin!23456";

        private readonly WebApplication _app;
        private readonly string _root;

        public HttpClient Client { get; }

        private AuthHost(WebApplication app, string root, HttpClient client)
        {
            _app = app;
            _root = root;
            Client = client;
        }

        public static async Task<AuthHost> StartAsync()
        {
            var root = Directory.CreateTempSubdirectory("bis-jwt-").FullName;
            var dbName = "BisAuditJwtSmoke-" + Guid.NewGuid().ToString("N");

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(AuditsController).Assembly.GetName().Name,
                ContentRootPath = root,
                EnvironmentName = Environments.Development
            });
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ChangeThisDevSigningKey_AtLeast32Chars!!",
                ["Jwt:Issuer"] = "BisAudit",
                ["Jwt:Audience"] = "BisAudit.Client",
                ["Jwt:ExpiresMinutes"] = "720",
                ["Uploads:RootPath"] = Path.Combine(root, "uploads")
            });

            builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));
            builder.Services.AddDbContextFactory<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
            builder.Services.AddScoped(sp =>
                sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
            builder.Services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.Password.RequiredLength = 8;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            var jwtKey = builder.Configuration["Jwt:Key"]!;
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });
            builder.Services.AddAuthorization();
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddScoped<AuditService>();
            builder.Services.AddScoped<PhotoService>();
            builder.Services.AddControllers()
                .AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            builder.Services.AddBisAuditSwagger();

            var app = builder.Build();
            app.UseBisAuditSwagger(enabled: true);
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.EnsureCreatedAsync();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = new ApplicationUser { UserName = Email, Email = Email, EmailConfirmed = true };
                var result = await users.CreateAsync(user, Password);
                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            await app.StartAsync();
            var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            return new AuthHost(app, root, client);
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
