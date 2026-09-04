using BisAudit.Web.Components;
using BisAudit.Web.Components.Account;
using BisAudit.Web.Data;
using BisAudit.Web.Data.Seed;
using BisAudit.Web.Options;
using BisAudit.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// Future authentication hooks (not enabled in Phase 1):
//   Windows Auth / IIS: builder.Services.AddAuthentication(Microsoft.AspNetCore.Server.IISIntegration.IISDefaults.AuthenticationScheme);
//   Entra ID: builder.Services.AddAuthentication().AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<PhotoService>();
builder.Services.AddScoped<DropdownService>();

builder.Services.AddSignalR(o => o.MaximumReceiveMessageSize = 12 * 1024 * 1024);

var app = builder.Build();

await DatabaseSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var uploadOptions = app.Services.GetRequiredService<IOptions<UploadOptions>>().Value;
var uploadRoot = Path.IsPathRooted(uploadOptions.RootPath)
    ? uploadOptions.RootPath
    : Path.Combine(app.Environment.ContentRootPath, uploadOptions.RootPath);
Directory.CreateDirectory(uploadRoot);
var normalizedUpload = uploadOptions.RootPath.Replace('\\', '/');
if (Path.IsPathRooted(uploadOptions.RootPath) || !normalizedUpload.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadRoot),
        RequestPath = "/uploads"
    });
}

app.UseAntiforgery();

app.MapGet("/api/audits/{id:guid}/contacts.csv", async (Guid id, AuditService audits) =>
{
    var audit = await audits.GetAsync(id);
    if (audit is null) return Results.NotFound();
    var csv = ContactExport.ToCsv(audit);
    var safe = string.Concat(audit.CompanyName.Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)));
    if (string.IsNullOrWhiteSpace(safe)) safe = "contacts";
    return Results.File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"{safe}-contacts.csv");
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.Run();
