using Microsoft.EntityFrameworkCore;

namespace Modules.Tables.Infrastructure;

public class TableDbContext : DbContext
{
    public TableDbContext(DbContextOptions<TableDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Table> Tables { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("tables");

        modelBuilder.Entity<Domain.Table>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.BranchId).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(64);
            entity.Property(e => e.Hall).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            entity.Property(e => e.MinCapacity).IsRequired();
            entity.Property(e => e.MaxCapacity).IsRequired();
            entity.Property(e => e.PositionX).HasColumnType("numeric(10,2)");
            entity.Property(e => e.PositionY).HasColumnType("numeric(10,2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Note).HasMaxLength(512);
            entity.Property(e => e.QrToken).HasMaxLength(128);
            entity.Property(e => e.CreatedAtUtc).IsRequired();

            entity.HasIndex(e => new { e.BranchId, e.Hall });
            entity.HasIndex(e => new { e.BranchId, e.Status });
            entity.HasIndex(e => new { e.BranchId, e.Code }).IsUnique();
            entity.HasIndex(e => new { e.BranchId, e.QrToken }).IsUnique();
        });
    }
}
