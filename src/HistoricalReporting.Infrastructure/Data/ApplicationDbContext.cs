using HistoricalReporting.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HistoricalReporting.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Report> Reports => Set<Report>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Manager> Managers => Set<Manager>();
    public DbSet<ManagerDepartment> ManagerDepartments => Set<ManagerDepartment>();
    public DbSet<OrganisationUser> OrganisationUsers => Set<OrganisationUser>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();
    public DbSet<NlpQueryLog> NlpQueryLogs => Set<NlpQueryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Report configuration
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Query).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(e => e.Name);
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasOne(e => e.Manager)
                .WithOne(m => m.User)
                .HasForeignKey<User>(e => e.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Organisation configuration
        modelBuilder.Entity<Organisation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(10);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Department configuration
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Organisation)
                .WithMany(o => o.Departments)
                .HasForeignKey(e => e.OrganisationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ParentDepartment)
                .WithMany(d => d.SubDepartments)
                .HasForeignKey(e => e.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganisationId, e.Name }).IsUnique();
        });

        // Manager configuration
        modelBuilder.Entity<Manager>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Organisation)
                .WithMany(o => o.Managers)
                .HasForeignKey(e => e.OrganisationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ManagerDepartment junction table
        modelBuilder.Entity<ManagerDepartment>(entity =>
        {
            entity.HasKey(e => new { e.ManagerId, e.DepartmentId });
            entity.HasOne(e => e.Manager)
                .WithMany(m => m.ManagedDepartments)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Department)
                .WithMany(d => d.ManagerDepartments)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // OrganisationUser configuration
        modelBuilder.Entity<OrganisationUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Organisation)
                .WithMany(o => o.Users)
                .HasForeignKey(e => e.OrganisationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(e => e.OrganisationId);
            entity.HasIndex(e => e.DepartmentId);
        });

        // AuditRecord configuration
        modelBuilder.Entity<AuditRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditRecords)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.Status);
        });

        // NlpQueryLog configuration
        modelBuilder.Entity<NlpQueryLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NaturalLanguageQuery).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.GeneratedSql).HasMaxLength(8000);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasOne(e => e.Manager)
                .WithMany()
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Organisation)
                .WithMany()
                .HasForeignKey(e => e.OrganisationId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(e => e.ManagerId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
