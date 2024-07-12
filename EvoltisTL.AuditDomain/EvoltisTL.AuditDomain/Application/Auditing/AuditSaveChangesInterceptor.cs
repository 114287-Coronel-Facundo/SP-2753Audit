using EvoltisTL.AuditDomain.Application.Service;
using EvoltisTL.AuditDomain.Domain.AuditEntryModel;
using EvoltisTL.AuditDomain.Domain.Entities;
using EvoltisTL.AuditDomain.Domain.Enums;
using EvoltisTL.AuditDomain.Infraestructure.Persistence;
using EvoltisTL.AuditDomain.Infraestructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvoltisTL.AuditDomain.Application.Auditing
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IServiceProvider _serviceProvider;

        public AuditSaveChangesInterceptor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }   

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            var auditEntries = GetAuditEntries(eventData);
            var logRepository = GetInstanceRepository.GetInstance(_serviceProvider);

            logRepository.SaveChanges(auditEntries);

            return base.SavingChanges(eventData, result);
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var auditEntries = GetAuditEntries(eventData);
            var logRepository = GetInstanceRepository.GetInstance(_serviceProvider);

            logRepository.SaveChanges(auditEntries);

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private List<AuditEntry> GetAuditEntries(DbContextEventData eventData)
        {
            var dbContext = eventData.Context;
            var auditEntries = new List<AuditEntry>();

            var auditEntities = dbContext.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .ToList();

            foreach (var entry in auditEntities)
            {
                if (entry.Entity is Audit || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditEntry(entry)
                {
                    TableName = entry.Entity.GetType().Name,
                    UserId = 111, // TODO: OBTENER USERID
                    AuditType = entry.State switch
                    {
                        EntityState.Added => AuditType.Create,
                        EntityState.Deleted => AuditType.Delete,
                        EntityState.Modified => AuditType.Update,
                    }
                };

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

                auditEntries.Add(auditEntry);
            }

            return auditEntries;
        }

    }
}
