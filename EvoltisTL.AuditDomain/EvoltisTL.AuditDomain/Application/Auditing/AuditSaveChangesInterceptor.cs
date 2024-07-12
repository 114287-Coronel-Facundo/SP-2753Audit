using Dummy.Core.Model.Classes;
using EvoltisTL.AuditDomain.Domain.AuditEntryModel;
using EvoltisTL.AuditDomain.Domain.Entities;
using EvoltisTL.AuditDomain.Domain.Enums;
using EvoltisTL.AuditDomain.Infraestructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvoltisTL.AuditDomain.Application.Auditing
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private List<AuditEntry> _temporaryAuditEntries;

        public AuditSaveChangesInterceptor(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
            _temporaryAuditEntries = new List<AuditEntry>();
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            _temporaryAuditEntries = GetAuditEntries(eventData);
            return base.SavingChanges(eventData, result);
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            _temporaryAuditEntries = GetAuditEntries(eventData);
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            CompleteAuditEntries();
            _auditLogRepository.SaveChanges(_temporaryAuditEntries);
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            CompleteAuditEntries();
            await _auditLogRepository.SaveChangesAsync(_temporaryAuditEntries);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        private List<AuditEntry> GetAuditEntries(DbContextEventData eventData)
        {
            var dbContext = eventData.Context;
            var auditEntries = new List<AuditEntry>();

            foreach (var entry in dbContext.ChangeTracker.Entries())
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

        private void CompleteAuditEntries()
        {
            foreach (var entry in _temporaryAuditEntries)
            {
                foreach (var property in entry.Entry.Properties)
                {
                    if (property.Metadata.IsPrimaryKey() && entry.AuditType == AuditType.Create)
                    {
                        entry.KeyValues[property.Metadata.Name] = property.CurrentValue;
                    }
                }
            }
        }
    }

}
