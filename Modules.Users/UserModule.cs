using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Users
{
    public static class UserModule
    {
        public static IServiceCollection AddUserModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<Infrastructure.UserDbContext>(options =>
             options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
             
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(UserModule).Assembly);
                cfg.LicenseKey = configuration["MediatR:LicenseKey"];
            });
            return services;
        }
    }
}