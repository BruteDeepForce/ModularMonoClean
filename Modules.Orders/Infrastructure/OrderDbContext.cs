using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Modules.Orders.Infrastructure
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

        public DbSet<Domain.Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("orders");
            modelBuilder.Entity<Domain.Order>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.BranchId).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.OrderNo).HasMaxLength(64).IsRequired();
                entity.Property(e => e.OrderType).HasMaxLength(32).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
                entity.Property(e => e.Priority).HasMaxLength(16).IsRequired();
                entity.Property(e => e.Source).HasMaxLength(32).IsRequired();
                entity.Property(e => e.Note).HasMaxLength(1024);
                entity.Property(e => e.SubtotalAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.ServiceAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
                entity.Property(e => e.CancelReason).HasMaxLength(512);
                entity.Property(e => e.CreatedAtUtc).IsRequired();

                entity.HasIndex(e => new { e.BranchId, e.UserId, e.CreatedAtUtc });
                entity.HasIndex(e => new { e.BranchId, e.Status, e.CreatedAtUtc });
                entity.HasIndex(e => new { e.BranchId, e.TableId, e.Status });
                entity.HasIndex(e => new { e.BranchId, e.OrderNo }).IsUnique();
            });
        }

    }
}
