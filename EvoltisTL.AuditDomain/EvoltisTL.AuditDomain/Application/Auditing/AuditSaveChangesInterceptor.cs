using EvoltisTL.AuditDomain.AuditEntryModel;
using EvoltisTL.AuditDomain.Domain.Entities;
using EvoltisTL.AuditDomain.Domain.Enums;
using EvoltisTL.AuditDomain.Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvoltisTL.AuditDomain.Application.Auditing
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly AuditDbContext _auditDbContext;
        //private readonly DomainContext _domainContext;

        public AuditSaveChangesInterceptor(AuditDbContext auditDbContext/*, DomainContext domainContext*/)
        {
            _auditDbContext = auditDbContext;
            //_domainContext = domainContext;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            var dbContext = eventData.Context;
            var audit = dbContext.ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified).ToList();

            var auditEntries = new List<AuditEntry>();
            foreach (var entry in audit)
            {
                if (entry.Entity is Audit || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;
                var auditEntry = new AuditEntry(entry);
                auditEntry.TableName = entry.Entity.GetType().Name;
                //TODO: OBTENER USERID //TODO:VERFICAR USO EN API
                auditEntry.UserId = 111;
                auditEntry.AuditType = entry.State switch
                {
                    EntityState.Added => AuditType.Create,
                    EntityState.Deleted => AuditType.Delete,
                    EntityState.Modified => AuditType.Update,
                };
                auditEntries.Add(auditEntry);
                foreach (var property in entry.Properties)
                {
                    string propertyName = property.Metadata.Name;
                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;
                        case EntityState.Deleted:
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;
                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                auditEntry.ChangedColumns.Add(propertyName);
                                auditEntry.OldValues[propertyName] = property.OriginalValue;
                                auditEntry.NewValues[propertyName] = property.CurrentValue;
                            }
                            break;
                    }
                }
            }
            foreach (var auditEntry in auditEntries)
            {
                _auditDbContext.AuditLogs.Add(auditEntry.ToAudit());
            }

            //base.SaveChanges();
            return base.SavingChanges(eventData, result);
        }


        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var dbContext = eventData.Context;
            var audit = dbContext.ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified).ToList();

            var auditEntries = new List<AuditEntry>();
            foreach (var entry in audit)
            {
                if (entry.Entity is Audit || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;
                var auditEntry = new AuditEntry(entry);
                auditEntry.TableName = entry.Entity.GetType().Name;
                auditEntry.UserId = 111;
                auditEntry.AuditType = entry.State switch
                {
                    EntityState.Added => AuditType.Create,
                    EntityState.Deleted => AuditType.Delete,
                    EntityState.Modified => AuditType.Update,
                };
                auditEntries.Add(auditEntry);
                foreach (var property in entry.Properties)
                {
                    string propertyName = property.Metadata.Name;
                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;
                        case EntityState.Deleted:
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;
                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                auditEntry.ChangedColumns.Add(propertyName);
                                auditEntry.OldValues[propertyName] = property.OriginalValue;
                                auditEntry.NewValues[propertyName] = property.CurrentValue;
                            }
                            break;
                    }
                }
            }
            foreach (var auditEntry in auditEntries)
            {
                _auditDbContext.AuditLogs.Add(auditEntry.ToAudit());
            }

            //base.SaveChanges();
            return base.SavingChangesAsync(eventData, result);
        }

    }
}
