using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Tables.Infrastructure.DesignTime;

public sealed class TableDbContextFactory : IDesignTimeDbContextFactory<TableDbContext>
{
    public TableDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=modulardb;Username=modularuser;Password=modularpass";

        var optionsBuilder = new DbContextOptionsBuilder<TableDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new TableDbContext(optionsBuilder.Options);
    }
}
