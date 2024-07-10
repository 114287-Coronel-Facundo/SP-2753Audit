using Microsoft.EntityFrameworkCore;

namespace EvoltisTL.AuditDomain
{
    public class AuditDbContext : DbContext
    {
        public DbSet<Audit> AuditLogs { get; set; }
        public AuditDbContext()
        {
        }

        public virtual int SaveChanges(int? userId = null)
        {
            OnBeforeSaveChanges(userId);
            base.SaveChanges();
            return SaveChanges();
        }

        public virtual Task<int> SaveChangesAsync(int? userId = null)
        {
            OnBeforeSaveChanges(userId);
            base.SaveChanges();
            return SaveChangesAsync();
        }

        private void OnBeforeSaveChanges(int? userId)
        {
            ChangeTracker.DetectChanges();
            var auditableEntitiesToChange = ChangeTracker.Entries().Where(entry => entry.Entity is AuditableEntity
                                                    && entry.State != EntityState.Detached
                                                    && entry.State != EntityState.Unchanged);
            if (userId == null && auditableEntitiesToChange.Any())
                throw new AuditableEntitySavingException(auditableEntitiesToChange.Select(entity => entity.GetType().Name));

            var auditEntries = new List<AuditEntry>();
            foreach (var entry in auditableEntitiesToChange)
            {
                if (entry.Entity is Audit || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;
                var auditEntry = new AuditEntry(entry);
                auditEntry.TableName = entry.Entity.GetType().Name;
                auditEntry.UserId = userId!.Value;
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
                AuditLogs.Add(auditEntry.ToAudit());
            }
        }        
    }
}