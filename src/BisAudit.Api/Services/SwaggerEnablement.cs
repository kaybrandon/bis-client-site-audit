using BisAudit.Api.Data;
using BisAudit.Api.Data.Entities;
using BisAudit.Api.Swagger;
using Microsoft.EntityFrameworkCore;

namespace BisAudit.Api.Services;

public interface ISwaggerEnablement
{
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);
    Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}

/// <summary>
/// Swagger UI on/off from the <c>AppSettings</c> table. Missing row falls back
/// to the environment default (Development/Staging on, Production off, or
/// <c>Swagger:Enabled</c> if set). Writes persist across process restarts.
/// </summary>
public sealed class SwaggerEnablement(
    IDbContextFactory<ApplicationDbContext> factory,
    IConfiguration configuration,
    IHostEnvironment environment) : ISwaggerEnablement
{
    public const string SettingKey = "Swagger.Enabled";

    private const int Unknown = -1;
    private int _cached = Unknown;

    public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        var cached = Volatile.Read(ref _cached);
        if (cached != Unknown)
            return cached == 1;

        var enabled = await ReadAsync(cancellationToken);
        Volatile.Write(ref _cached, enabled ? 1 : 0);
        return enabled;
    }

    public async Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == SettingKey, cancellationToken);
        if (row is null)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = SettingKey,
                Value = Format(enabled),
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            row.Value = Format(enabled);
            row.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        Volatile.Write(ref _cached, enabled ? 1 : 0);
    }

    private async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == SettingKey, cancellationToken);
        if (row is not null && bool.TryParse(row.Value, out var stored))
            return stored;

        return SwaggerExtensions.IsEnabled(configuration, environment);
    }

    private static string Format(bool enabled) => enabled ? bool.TrueString : bool.FalseString;
}
