using Dummy.Core.Model;
using EvoltisTL.AuditDomain.AuditEntryModel;
using EvoltisTL.AuditDomain.Model;
using EvoltisTL.AuditDomain.Model.ModelBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace EvoltisTL.AuditDomain.Interceptor
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

        //public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        //{
        //    //PerformAudit(eventData.Context);
        //    return base.SavedChanges(eventData, result);
        //}

        //public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        //{
        //    //PerformAudit(eventData.Context);
        //    return await base.SavedChangesAsync(eventData, result, cancellationToken);
        //}

        private void PerformAudit(DbContext context)
        {
            //var auditableEntities = context.ChangeTracker.Entries()
            //    .Where(e => e.Entity is AuditableEntity &&
            //                e.State != EntityState.Detached &&
            //                e.State != EntityState.Unchanged)
            //    .Select(e => e.Entity as AuditableEntity)
            //    .ToList();

            var auditableEntities = context.ChangeTracker.Entries().ToList();
    //.Where(e => e.Entity is AuditableEntity &&
    //            e.State != EntityState.Detached &&
    //            e.State != EntityState.Unchanged)
    //.Select(e => e.Entity as AuditableEntity)
    //.ToList();


            context.ChangeTracker.DetectChanges();
            var auditableEntitiesToChange = context.ChangeTracker.Entries().Where(entry => /*entry.Entity is AuditableEntity
                                                    &&*/ entry.State != EntityState.Detached
                                                    && entry.State != EntityState.Unchanged);

            foreach( var entity in context.ChangeTracker.Entries())
            {
                var enti = entity.Entity;
            }

            //_auditDbContext.OnBeforeSaveChanges(auditableEntities, 1123);
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
                auditEntry.UserId = 111;
                auditEntry.AuditType = entry.State switch
                {
                    EntityState.Added => Enums.AuditType.Create,
                    EntityState.Deleted => Enums.AuditType.Delete,
                    EntityState.Modified => Enums.AuditType.Update,
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

    }
}
