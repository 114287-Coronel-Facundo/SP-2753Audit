using EvoltisTL.AuditDomain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Reflection;

namespace EvoltisTL.AuditDomain.Infraestructure.Persistence
{
    public class AuditDbContext : DbContext
    {
        public DbSet<Audit> AuditLogs { get; set; }

        //public AuditDbContext()
        //{
            
        //}

        public AuditDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}