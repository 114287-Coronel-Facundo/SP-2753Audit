using EvoltisTL.AuditDomain.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvoltisTL.AuditDomain.Infraestructure.Persistence
{
    public class AuditDbContext : DbContext
    {
        public DbSet<Audit> AuditLogs { get; set; }
        public AuditDbContext()
        {
        }
    }
}