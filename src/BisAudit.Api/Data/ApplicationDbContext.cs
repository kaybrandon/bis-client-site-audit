using BisAudit.Api.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BisAudit.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ClientAudit> Audits => Set<ClientAudit>();
    public DbSet<AuditContact> Contacts => Set<AuditContact>();
    public DbSet<SubLocation> SubLocations => Set<SubLocation>();
    public DbSet<WorkstationItem> Workstations => Set<WorkstationItem>();
    public DbSet<NetworkItem> NetworkItems => Set<NetworkItem>();
    public DbSet<ServerStorageItem> ServerStorageItems => Set<ServerStorageItem>();
    public DbSet<SecurityAvItem> SecurityAvItems => Set<SecurityAvItem>();
    public DbSet<SoftwareItem> SoftwareItems => Set<SoftwareItem>();
    public DbSet<IssueItem> Issues => Set<IssueItem>();
    public DbSet<PurchaseItem> Purchases => Set<PurchaseItem>();
    public DbSet<SitePhoto> Photos => Set<SitePhoto>();
    public DbSet<DropdownList> DropdownLists => Set<DropdownList>();
    public DbSet<DropdownOption> DropdownOptions => Set<DropdownOption>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ClientAudit>(e =>
        {
            e.ToTable("Audits");
            e.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Industry).HasMaxLength(80);
            e.Property(x => x.Status).HasMaxLength(40);
            e.Property(x => x.PreparedBy).HasMaxLength(120);
            e.Property(x => x.ClientPhotoPath).HasMaxLength(400);
            e.HasMany(x => x.Contacts).WithOne(x => x.Audit).HasForeignKey(x => x.AuditId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.SubLocations).WithOne(x => x.Audit).HasForeignKey(x => x.AuditId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Workstations).WithOne(x => x.Audit).HasForeignKey(x => x.AuditId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.NetworkItems).WithOne(x => x.Audit).HasForeignKey(x => x.AuditId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.ServerStorageItems).WithOne(x => x.Audit).HasForeignKey(x => x.AuditId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.SecurityAvItems).WithOne(x => x.Audit).HasForeignKey(x => x.AuditId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.SoftwareItems).WithOne(x => x.Audit).HasForeignKey(x => x.AuditId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Issues).WithOne(x => x.Audit).HasForeignKey(x => x.AuditId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Purchases).WithOne(x => x.Audit).HasForeignKey(x => x.AuditId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Photos).WithOne(x => x.Audit).HasForeignKey(x => x.AuditId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditContact>(e =>
        {
            e.ToTable("AuditContacts");
            e.Property(x => x.Name).HasMaxLength(160);
            e.Property(x => x.Title).HasMaxLength(120);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(40);
            e.HasIndex(x => new { x.AuditId, x.Role }).IsUnique();
        });

        builder.Entity<SubLocation>(e =>
        {
            e.ToTable("SubLocations");
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
        });

        builder.Entity<WorkstationItem>(e =>
        {
            e.ToTable("Workstations");
            e.Ignore(x => x.DisplayTitle);
            e.Ignore(x => x.DisplayDetail);
            e.Property(x => x.Status).HasMaxLength(60);
        });

        builder.Entity<NetworkItem>(e =>
        {
            e.ToTable("NetworkItems");
            e.Ignore(x => x.DisplayTitle);
            e.Ignore(x => x.DisplayDetail);
        });

        builder.Entity<ServerStorageItem>(e =>
        {
            e.ToTable("ServerStorageItems");
            e.Ignore(x => x.DisplayTitle);
            e.Ignore(x => x.DisplayDetail);
        });

        builder.Entity<SecurityAvItem>(e =>
        {
            e.ToTable("SecurityAvItems");
            e.Ignore(x => x.DisplayTitle);
            e.Ignore(x => x.DisplayDetail);
        });

        builder.Entity<SoftwareItem>(e =>
        {
            e.ToTable("SoftwareItems");
            e.Ignore(x => x.DisplayTitle);
            e.Ignore(x => x.DisplayDetail);
        });

        builder.Entity<IssueItem>(e =>
        {
            e.ToTable("Issues");
            e.Ignore(x => x.DisplayTitle);
            e.Ignore(x => x.DisplayDetail);
            e.Property(x => x.IssueRecommendation).HasMaxLength(400).IsRequired();
            e.Property(x => x.MonthlyCostImpact).HasColumnType("decimal(18,2)");
        });

        builder.Entity<PurchaseItem>(e =>
        {
            e.ToTable("Purchases");
            e.Ignore(x => x.DisplayTitle);
            e.Ignore(x => x.DisplayDetail);
            e.Ignore(x => x.LineInvestment);
            e.Property(x => x.Item).HasMaxLength(200).IsRequired();
            e.Property(x => x.EstimatedUnitCost).HasColumnType("decimal(18,2)");
            e.Property(x => x.MonthlySavings).HasColumnType("decimal(18,2)");
        });

        builder.Entity<SitePhoto>(e =>
        {
            e.ToTable("Photos");
            e.Ignore(x => x.PublicUrl);
            e.Property(x => x.FileName).HasMaxLength(260);
            e.Property(x => x.RelativePath).HasMaxLength(400);
            e.Property(x => x.Category).HasMaxLength(60);
            e.Property(x => x.OwnerType).HasMaxLength(40);
        });

        builder.Entity<DropdownList>(e =>
        {
            e.ToTable("DropdownLists");
            e.HasIndex(x => x.ListKey).IsUnique();
            e.Property(x => x.ListKey).HasMaxLength(80).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
            e.HasMany(x => x.Options).WithOne(x => x.List).HasForeignKey(x => x.ListId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DropdownOption>(e =>
        {
            e.ToTable("DropdownOptions");
            e.Property(x => x.Value).HasMaxLength(120).IsRequired();
        });
    }
}
