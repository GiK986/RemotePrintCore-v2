namespace RemotePrintCore.Web.Data;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RemotePrintCore.Web.Models.Entities;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Printer> Printers { get; set; } = null!;

    public DbSet<Banner> Banners { get; set; } = null!;

    public DbSet<DocumentTemplate> DocumentTemplates { get; set; } = null!;

    public DbSet<PrintLog> PrintLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Global query filter for soft delete on BaseEntity subclasses
        builder.Entity<Printer>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Banner>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<DocumentTemplate>().HasQueryFilter(e => !e.IsDeleted);

        // PrintLog index on CreatedOn for fast queries
        builder.Entity<PrintLog>()
            .HasIndex(e => e.CreatedOn);
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditInfo()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedOn = DateTime.UtcNow;
            }
            else
            {
                entity.ModifiedOn = DateTime.UtcNow;
            }
        }

        // Handle soft deletes
        var deletedEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && e.State == EntityState.Deleted);

        foreach (var entry in deletedEntries)
        {
            entry.State = EntityState.Modified;
            var entity = (BaseEntity)entry.Entity;
            entity.IsDeleted = true;
            entity.DeletedOn = DateTime.UtcNow;
        }

        // PrintLog timestamp
        var printLogEntries = ChangeTracker.Entries<PrintLog>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in printLogEntries)
        {
            entry.Entity.CreatedOn = DateTime.UtcNow;
        }
    }
}
