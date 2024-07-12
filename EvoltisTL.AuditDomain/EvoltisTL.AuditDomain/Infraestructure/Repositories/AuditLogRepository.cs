using EvoltisTL.AuditDomain.Domain.AuditEntryModel;
using EvoltisTL.AuditDomain.Domain.Entities;
using EvoltisTL.AuditDomain.Infraestructure.Persistence;
using EvoltisTL.AuditDomain.Infraestructure.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoltisTL.AuditDomain.Infraestructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly AuditDbContext _dbContext;
        public AuditLogRepository(AuditDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public void SaveChanges(List<AuditEntry> auditEntry)
        {
            foreach(var audit in  auditEntry)
            {
                _dbContext.Add(audit.ToAudit());
            }
            _dbContext.SaveChanges();
        }

        public async Task SaveChangesAsync(List<AuditEntry> auditEntry)
        {
            foreach (var audit in auditEntry)
            {
                await _dbContext.AddAsync(audit.ToAudit());
            }
            await _dbContext.SaveChangesAsync();
        }
    }
}
