using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Modules.Users.Infrastructure
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
        {
        }

        public DbSet<Domain.User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("users");

            modelBuilder.Entity<Domain.User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.BranchId).IsRequired();
                entity.Property(e => e.FullName).HasMaxLength(128).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
                entity.Property(e => e.Phone).HasMaxLength(32);
                entity.Property(e => e.Role).HasMaxLength(32).IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.PinHash).HasMaxLength(256);
                entity.Property(e => e.CreatedAtUtc).IsRequired();
                entity.Property(e => e.OrderCount).HasDefaultValue(0);

                entity.HasIndex(e => new { e.BranchId, e.Id });
                entity.HasIndex(e => new { e.BranchId, e.Email }).IsUnique();
                entity.HasIndex(e => new { e.BranchId, e.Role, e.IsActive });
            });
        }
        
    }
}
