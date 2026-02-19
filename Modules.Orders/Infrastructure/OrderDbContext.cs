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
                entity.Property(e => e.CreatedAtUtc).IsRequired();
                entity.HasIndex(e => new { e.BranchId, e.UserId, e.CreatedAtUtc });
            });
        }

    }
}
