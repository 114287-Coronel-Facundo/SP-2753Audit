using EvoltisTL.AuditDomain.Infraestructure.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoltisTL.AuditDomain.Application.Service
{
    public static class GetInstanceRepository
    {
        public static IAuditLogRepository GetInstance(IServiceProvider _serviceProvider)
        {
            var algo = _serviceProvider.GetRequiredService<IAuditLogRepository>();
            return algo;
        }
    }
}
