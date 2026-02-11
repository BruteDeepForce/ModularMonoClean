using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Orders
{
    public static class OrderModule
    {
        public static IServiceCollection AddOrderModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<Infrastructure.OrderDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
                
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(OrderModule).Assembly);
                cfg.LicenseKey = configuration["MediatR:LicenseKey"];
            });

            return services;
        }
        
    }
}