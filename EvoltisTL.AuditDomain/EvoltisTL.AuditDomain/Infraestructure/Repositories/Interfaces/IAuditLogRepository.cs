using EvoltisTL.AuditDomain.Domain.AuditEntryModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoltisTL.AuditDomain.Infraestructure.Repositories.Interfaces
{
    public interface IAuditLogRepository
    {
        public void SaveChanges(List<AuditEntry> auditEntry);
        public Task SaveChangesAsync(List<AuditEntry> auditEntry);
    }
}
