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
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.OrderCount).HasDefaultValue(0);
                entity.HasIndex(e => new { e.BranchId, e.Id });
            });
        }
        
    }
}
