using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BisAudit.Api.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                 ?? "Server=localhost,1433;Database=BisAudit;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
        options.UseSqlServer(cs);
        return new ApplicationDbContext(options.Options);
    }
}
