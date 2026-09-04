using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BisAudit.Api.Data;
using BisAudit.Api.Data.Seed;
using BisAudit.Api.Options;
using BisAudit.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var logsDir = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Logging:File:Path"] ?? "logs");
Directory.CreateDirectory(logsDir);
var retainDays = builder.Configuration.GetValue("Logging:File:RetainedFileCountLimit", 30);

builder.Host.UseSerilog((_, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logsDir, "bisaudit-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: retainDays,
        shared: true));

builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 12_000_000);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
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

// Future authentication hooks (not enabled in Phase 1):
//   Windows Auth / IIS: builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);
//   Entra ID: builder.Services.AddAuthentication().AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<PhotoService>();
builder.Services.AddScoped<DropdownService>();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["http://localhost:5173", "http://127.0.0.1:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("vite", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await DatabaseSeeder.SeedAsync(app.Services);

var detailedErrors = app.Configuration.GetValue("Logging:DetailedErrors", false)
                     || app.Environment.IsDevelopment();
if (detailedErrors)
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var ex = context.Features.Get<IExceptionHandlerPathFeature>();
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Pipeline");
            logger.LogError(ex?.Error, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { message = "An error occurred." });
        });
    });
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseSerilogRequestLogging();

app.UseCors("vite");
app.UseAuthentication();
app.UseAuthorization();
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

app.MapControllers();

var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (File.Exists(Path.Combine(webRoot, "index.html")))
    app.MapFallbackToFile("index.html");

app.Run();
