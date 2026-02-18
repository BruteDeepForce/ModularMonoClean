using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Tables
{
    public static class TableModule
    {
        public static IServiceCollection AddTableModule(this IServiceCollection services, IConfiguration configuration)
        {
            //di registrations for tables module
            return services;
        }
        
    }
}