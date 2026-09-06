using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BisAudit.Api.Controllers;
using BisAudit.Api.Data;
using BisAudit.Api.Identity;
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

internal sealed class AuthTestHost : IAsyncDisposable
{
    public const string Email = "admin@bis.local";
    public const string Password = "Admin!23456";

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplication _app;
    private readonly string _root;

    public HttpClient Client { get; }

    private AuthTestHost(WebApplication app, string root, HttpClient client)
    {
        _app = app;
        _root = root;
        Client = client;
    }

    public static async Task<AuthTestHost> StartAsync()
    {
        var root = Directory.CreateTempSubdirectory("bis-auth-").FullName;
        var dbName = "BisAuditAuth-" + Guid.NewGuid().ToString("N");

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
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                                     ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                        if (string.IsNullOrEmpty(userId))
                        {
                            context.Fail("Invalid token.");
                            return;
                        }

                        var user = await userManager.FindByIdAsync(userId);
                        if (user is null || await userManager.IsLockedOutAsync(user))
                            context.Fail("User is disabled.");
                    }
                };
            });
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<TokenService>();
        builder.Services.AddScoped<UserService>();
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
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roles.RoleExistsAsync(AppRoles.Admin))
                await roles.CreateAsync(new IdentityRole(AppRoles.Admin));
            var user = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                EmailConfirmed = true,
                LockoutEnabled = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await users.CreateAsync(user, Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(user, AppRoles.Admin);
        }

        await app.StartAsync();
        var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        return new AuthTestHost(app, root, client);
    }

    public async Task<string> LoginAsync(string email, string password)
    {
        var login = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<AuthController.LoginResponse>(Json);
        if (string.IsNullOrWhiteSpace(body?.Token))
            throw new InvalidOperationException("Login did not return a token.");
        return body.Token;
    }

    public async Task UseAdminAsync()
    {
        var token = await LoginAsync(Email, Password);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearAuth() => Client.DefaultRequestHeaders.Authorization = null;

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        try { Directory.Delete(_root, recursive: true); }
        catch (IOException) { }
    }
}
