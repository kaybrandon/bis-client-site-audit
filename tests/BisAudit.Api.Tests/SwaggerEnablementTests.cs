using BisAudit.Api.Swagger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace BisAudit.Api.Tests;

public class SwaggerEnablementTests
{
    [Theory]
    [InlineData("Development", null, true)]
    [InlineData("Staging", null, true)]
    [InlineData("Production", null, false)]
    [InlineData("Production", "true", true)]
    [InlineData("Production", "false", false)]
    [InlineData("Development", "false", false)]
    [InlineData("Staging", "false", false)]
    [InlineData("Development", "true", true)]
    public void Environment_and_explicit_flag(string environmentName, string? enabled, bool expected)
    {
        var values = new Dictionary<string, string?>();
        if (enabled is not null)
            values["Swagger:Enabled"] = enabled;

        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var env = new FakeHostEnvironment { EnvironmentName = environmentName };

        Assert.Equal(expected, SwaggerExtensions.IsEnabled(config, env));
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "";
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
