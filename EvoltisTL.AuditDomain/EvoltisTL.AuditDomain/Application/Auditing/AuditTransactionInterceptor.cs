using EvoltisTL.AuditDomain.Domain.AuditEntryModel;
using EvoltisTL.AuditDomain.Infraestructure.Repositories;
using EvoltisTL.AuditDomain.Infraestructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoltisTL.AuditDomain.Application.Auditing
{
    public class AuditTransactionInterceptor : DbTransactionInterceptor
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly AuditEntryContainer _auditEntryContainer;
        public AuditTransactionInterceptor(IAuditLogRepository auditLogRepository, AuditEntryContainer auditEntryContainer)
        {
            _auditLogRepository = auditLogRepository;
            _auditEntryContainer = auditEntryContainer;
        }


        public override DbTransaction TransactionStarted(
            DbConnection connection,
            TransactionEndEventData eventData,
            DbTransaction result)
        {
            _auditEntryContainer.AddExplicitTransaction(eventData.TransactionId);
            return base.TransactionStarted(connection, eventData, result);
        }

        public override async ValueTask<DbTransaction> TransactionStartedAsync(
                DbConnection connection,
                TransactionEndEventData eventData,
                DbTransaction result,
                CancellationToken cancellationToken = default)
        {
            _auditEntryContainer.AddExplicitTransaction(eventData.TransactionId);
            return await base.TransactionStartedAsync(connection, eventData, result, cancellationToken);
        }

        public override void TransactionCommitted(
                        DbTransaction transaction,
                        TransactionEndEventData eventData)
        {
            var isImplicit = _auditEntryContainer.ContainsExplicitTransaction(eventData.TransactionId);
            if (isImplicit)
            {
                _auditLogRepository.SaveChanges(_auditEntryContainer.TemporaryAuditEntries);
            }

            if (_auditEntryContainer.ContainsExplicitTransaction(eventData.TransactionId))
                _auditLogRepository.SaveChanges(_auditEntryContainer.TemporaryAuditEntries);
        }

        public override Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (!_auditEntryContainer.TemporaryAuditEntries.Any())
                return base.TransactionCommittedAsync(transaction, eventData, cancellationToken);

            var isImplicit = _auditEntryContainer.ContainsExplicitTransaction(eventData.TransactionId);
            if (isImplicit)
            {
                _auditLogRepository.SaveChanges(_auditEntryContainer.TemporaryAuditEntries);
            }

            return base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
        }

        public override void TransactionRolledBack(
                                DbTransaction transaction,
                                TransactionEndEventData eventData)
        {
            ClearAllContainer(eventData);
        }

        public override Task TransactionRolledBackAsync(
                        DbTransaction transaction,
                        TransactionEndEventData eventData,
                        CancellationToken cancellationToken = default)
        {
            ClearAllContainer(eventData);
            return base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
        }


        private void ClearAllContainer(TransactionEndEventData transaction)
        {
            _auditEntryContainer.ClearAuditEntry();
            _auditEntryContainer.RemoveExplicitTransaction(transaction.TransactionId);
        }
    }
}
