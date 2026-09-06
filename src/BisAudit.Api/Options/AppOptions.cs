namespace BisAudit.Api.Options;

public class UploadOptions
{
    public const string SectionName = "Uploads";
    public string RootPath { get; set; } = "wwwroot/uploads";
}

public class SeedOptions
{
    public const string SectionName = "Seed";
    public string AdminEmail { get; set; } = "admin@bis.local";
    public string AdminPassword { get; set; } = "Admin!23456";
    public bool LoadSampleAudit { get; set; } = true;
}

/// <summary>
/// Initial OpenAPI / Swagger UI default when no <c>AppSettings</c> row exists.
/// Runtime on/off is the admin Settings toggle (database). When
/// <see cref="Enabled"/> is null, Development and Staging are on and Production
/// is off. <c>Swagger__Enabled</c> only seeds that first row — it is not written
/// back from the app.
/// </summary>
public class SwaggerOptions
{
    public const string SectionName = "Swagger";

    public bool? Enabled { get; set; }
}
