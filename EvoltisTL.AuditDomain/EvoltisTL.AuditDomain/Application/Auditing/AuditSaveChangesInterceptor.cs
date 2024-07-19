using EvoltisTL.AuditDomain.Domain.AuditEntryModel;
using EvoltisTL.AuditDomain.Domain.Entities;
using EvoltisTL.AuditDomain.Domain.Enums;
using EvoltisTL.AuditDomain.Domain.Exceptions;
using EvoltisTL.AuditDomain.Domain.ModelBase;
using EvoltisTL.AuditDomain.Infraestructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading;

namespace EvoltisTL.AuditDomain.Application.Auditing
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly AuditEntryContainer _auditEntryContainer;

        public AuditSaveChangesInterceptor(IAuditLogRepository auditLogRepository, AuditEntryContainer auditEntryContainer)
        {
            _auditLogRepository = auditLogRepository;
            _auditEntryContainer = auditEntryContainer;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            _auditEntryContainer.ClearAuditEntry();
            var auditEntries = GetAuditEntries(eventData);

            if (!auditEntries.Any())
                return base.SavingChanges(eventData, result);

            foreach (var entry in auditEntries)
            {
                _auditEntryContainer.AddAuditEntry(entry);
            }
            return base.SavingChanges(eventData, result);
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            _auditEntryContainer.ClearAuditEntry();
            var auditEntries = GetAuditEntries(eventData);

            if (!auditEntries.Any())
                return await base.SavingChangesAsync(eventData, result, cancellationToken);

            foreach (var entry in auditEntries)
            {
                _auditEntryContainer.AddAuditEntry(entry);
            }
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            if (!_auditEntryContainer.TemporaryAuditEntries.Any())
                return base.SavedChanges(eventData, result);
            if (_auditEntryContainer.ContainsExplicitTransaction(eventData.Context.Database.CurrentTransaction.TransactionId))
                return base.SavedChanges(eventData, result);

            CompleteAuditEntries();
            _auditLogRepository.SaveChanges(_auditEntryContainer.TemporaryAuditEntries);
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (!_auditEntryContainer.TemporaryAuditEntries.Any())
                return await base.SavedChangesAsync(eventData, result, cancellationToken);

            if (_auditEntryContainer.ContainsExplicitTransaction(eventData.Context.Database.CurrentTransaction.TransactionId))
                return await base.SavedChangesAsync(eventData, result, cancellationToken);


            CompleteAuditEntries();
            await _auditLogRepository.SaveChangesAsync(_auditEntryContainer.TemporaryAuditEntries);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        private List<AuditEntry> GetAuditEntries(DbContextEventData eventData)
        {
            var dbContext = eventData.Context;

            var auditableEntries = dbContext.ChangeTracker.Entries()
                            .Where(entry => entry.Entity is AuditableEntity && entry.State != EntityState.Detached && entry.State != EntityState.Unchanged)
                            .ToList();

            if (!auditableEntries.Any())
                return new List<AuditEntry>();

            var auditUserId = GetUserId(auditableEntries.Select(p => (AuditableEntity)p.Entity));

            return auditableEntries.Select(p => new AuditEntry(p, auditUserId)).ToList();
        }

        private void CompleteAuditEntries()
        {
            foreach (var entry in _auditEntryContainer.TemporaryAuditEntries)
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

        private int GetUserId(IEnumerable<AuditableEntity> auditableEntries)
        {
            if (auditableEntries.All(p => p.AuditUserId == null))
                throw new AuditableEntitySavingException("User ID for audit was not found, need the user id to be saved, add UserId to call the method");

            return auditableEntries.First(p => p.AuditUserId != null).AuditUserId;
        }
    }

}
