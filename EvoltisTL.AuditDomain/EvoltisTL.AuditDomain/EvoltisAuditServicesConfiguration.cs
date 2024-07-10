using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoltisTL.AuditDomain
{
    public static class EvoltisAuditServicesConfiguration
    {
        public static IServiceCollection ConfigureAudit(this IServiceCollection services, string connectionString)
        {

            services.AddDbContext<AuditDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
            //services.AddAutoMapper((new BaseModel()).GetType().Assembly);

            return services;
        }
    }
}
