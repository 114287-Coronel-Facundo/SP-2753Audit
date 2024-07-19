using EvoltisTL.AuditDomain.Domain.Entities;
using EvoltisTL.AuditDomain.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace EvoltisTL.AuditDomain.Domain.AuditEntryModel
{

    public class AuditEntry
    {
        public AuditEntry(EntityEntry entry, int auditUserId)
        {
            Entry = entry;
            TableName = entry.Metadata.GetTableName();
            UserId = auditUserId;
            AuditType = entry.State switch
            {
                EntityState.Added => AuditType.Create,
                EntityState.Deleted => AuditType.Delete,
                EntityState.Modified => AuditType.Update,
            };
            AddProperties(entry);
        }
        public EntityEntry Entry { get; } = null!;
        public int UserId { get; set; }
        public string TableName { get; set; } = null!;
        public Dictionary<string, object> KeyValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();
        public AuditType AuditType { get; set; }
        public List<string> ChangedColumns { get; } = new List<string>();
        public Audit ToAudit()
        {
            var audit = new Audit();
            audit.UserId = UserId;
            audit.Type = AuditType.ToString();
            audit.TableName = TableName;
            audit.DateTime = DateTime.Now;
            audit.PrimaryKey = JsonSerializer.Serialize(KeyValues);
            audit.OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues);
            audit.NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues);
            audit.AffectedColumns = ChangedColumns.Count == 0 ? null : JsonSerializer.Serialize(ChangedColumns);
            return audit;
        }
        private void AddProperties(EntityEntry entry)
        {
            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        NewValues[propertyName] = property.CurrentValue;
                        break;
                    case EntityState.Deleted:
                        OldValues[propertyName] = property.OriginalValue;
                        break;
                    case EntityState.Modified:
                        if (!Equals(property.OriginalValue, property.CurrentValue))
                        {
                            ChangedColumns.Add(propertyName);
                            OldValues[propertyName] = property.OriginalValue;
                            NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }
        }
    }
}
