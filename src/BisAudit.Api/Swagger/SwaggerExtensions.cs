using Microsoft.OpenApi.Models;

namespace BisAudit.Api.Swagger;

public static class SwaggerExtensions
{
    public const string RoutePrefix = "swagger";
    public const string DocumentName = "v1";
    public const string BearerSchemeId = "Bearer";

    /// <summary>
    /// Default when the database has no Swagger row. Development/Staging on.
    /// Production off unless <c>Swagger:Enabled</c> / <c>Swagger__Enabled</c>
    /// is set explicitly. Runtime enablement is the admin DB setting.
    /// </summary>
    public static bool IsEnabled(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var configured = configuration.GetValue<bool?>($"{Options.SwaggerOptions.SectionName}:Enabled");
        if (configured.HasValue)
            return configured.Value;

        return environment.IsDevelopment() || environment.IsEnvironment("Staging");
    }

    public static IServiceCollection AddBisAuditSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocumentName, new OpenApiInfo
            {
                Title = "BIS Client IT Audit API",
                Version = DocumentName,
                Description = "Use POST /api/auth/login, then Authorize with the returned JWT (Bearer)."
            });

            options.AddSecurityDefinition(BearerSchemeId, new OpenApiSecurityScheme
            {
                Description = "JWT from POST /api/auth/login. Paste the token only — Swagger sends Authorization: Bearer {token}.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = BearerSchemeId
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        return services;
    }

    /// <summary>
    /// Serves Swagger UI at /swagger when enabled. When disabled, /swagger and
    /// /swagger/* return 404 so the SPA fallback cannot claim those paths.
    /// Pass <paramref name="enabled"/> for a fixed pipeline (tests). Omit it to
    /// read <see cref="Services.ISwaggerEnablement"/> on each Swagger request.
    /// </summary>
    public static IApplicationBuilder UseBisAuditSwagger(this IApplicationBuilder app, bool? enabled = null)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/" + RoutePrefix))
            {
                var on = enabled ?? await context.RequestServices
                    .GetRequiredService<Services.ISwaggerEnablement>()
                    .IsEnabledAsync(context.RequestAborted);
                if (!on)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
            }

            await next();
        });

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/{RoutePrefix}/{DocumentName}/swagger.json", "BIS Client IT Audit API v1");
            options.RoutePrefix = RoutePrefix;
            options.EnablePersistAuthorization();
        });
        return app;
    }

    /// <summary>
    /// Layout A SPA shell. /api and /swagger never fall through to index.html.
    /// </summary>
    public static void MapBisAuditSpaFallback(this WebApplication app)
    {
        var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        var indexHtml = Path.Combine(webRoot, "index.html");
        if (!File.Exists(indexHtml))
            return;

        app.MapFallback(async context =>
        {
            if (context.Request.Path.StartsWithSegments("/api")
                || context.Request.Path.StartsWithSegments("/" + RoutePrefix))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(indexHtml);
        });
    }
}
