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
/// OpenAPI / Swagger UI. When <see cref="Enabled"/> is null, Development and Staging
/// are on and Production is off. Override with <c>Swagger__Enabled</c>.
/// </summary>
public class SwaggerOptions
{
    public const string SectionName = "Swagger";

    public bool? Enabled { get; set; }
}
