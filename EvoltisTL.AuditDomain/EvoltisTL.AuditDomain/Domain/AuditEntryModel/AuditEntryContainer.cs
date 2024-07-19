using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoltisTL.AuditDomain.Domain.AuditEntryModel
{
    public class AuditEntryContainer
    {
        public List<AuditEntry> TemporaryAuditEntries { get; private set; }
        public ConcurrentDictionary<Guid, bool> ExplicitTransactions { get; private set; }

        public AuditEntryContainer()
        {
            TemporaryAuditEntries = new List<AuditEntry>();
            ExplicitTransactions = new ConcurrentDictionary<Guid, bool>();
        }

        public void AddAuditEntry(AuditEntry entry)
        {
            TemporaryAuditEntries.Add(entry);
        }

        public void ClearAuditEntry()
        {
            TemporaryAuditEntries.Clear();
        }

        public void AddExplicitTransaction(Guid transaction)
        {
            ExplicitTransactions.TryAdd(transaction, true);
        }

        public void RemoveExplicitTransaction(Guid transaction)
        {
            ExplicitTransactions.TryRemove(transaction, out _);
        }
        public bool ContainsExplicitTransaction(Guid? transaction)
        {
            if (transaction == null)
            {
                return false;
            }

            bool value;
            return ExplicitTransactions.TryGetValue(transaction.Value, out value) && value;
        }
    }
}
