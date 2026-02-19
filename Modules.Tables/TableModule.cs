using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Tables
{
    public static class TableModule
    {
        public static IServiceCollection AddTableModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<Infrastructure.TableDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(TableModule).Assembly);
                cfg.LicenseKey = configuration["MediatR:LicenseKey"];
            });

            return services;
        }
        
    }
}
