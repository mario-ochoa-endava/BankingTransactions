using BankingTransactions.Api.Configuration;
using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Models;
using Microsoft.Extensions.Options;

namespace BankingTransactions.Api.Services;

public sealed class MockTransactionService<TRequest>(MockTransactionStore store, IOptions<MockApiOptions> options)
    : ITransactionService<TRequest> where TRequest : TransactionRequest
{
    public TransactionResponse Create(string accountId, TRequest request)
    {
        lock (store.SyncRoot)
        {
            EnsureAvailable();
            ValidateReferences(accountId, request);
            var response = Map(request, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow);
            store.Transactions.Add(response.TransactionId, new(accountId, typeof(TRequest), response));
            store.Record(accountId, response.TransactionId, "CREATE");
            return response;
        }
    }

    public TransactionListResponse List(string accountId, int limit, int offset)
    {
        lock (store.SyncRoot)
        {
            EnsureAvailable();
            var records = store.Transactions.Values
                .Where(x => x.AccountId == accountId && x.RequestType == typeof(TRequest))
                .Select(x => x.Response).OrderByDescending(x => x.CreatedAt)
                .ThenBy(x => x.TransactionId, StringComparer.Ordinal).ToArray();
            return new(records.Skip(offset).Take(limit).ToArray(), limit, offset, records.Length);
        }
    }

    public TransactionResponse Get(string accountId, string transactionId)
    {
        lock (store.SyncRoot)
        {
            EnsureAvailable();
            return Find(accountId, transactionId).Response;
        }
    }

    public TransactionResponse Update(string accountId, string transactionId, TRequest request)
    {
        lock (store.SyncRoot)
        {
            EnsureAvailable();
            var record = Find(accountId, transactionId);
            EnsurePending(record);
            ValidateReferences(accountId, request);
            var response = Map(request, transactionId, record.Response.CreatedAt);
            store.Transactions[transactionId] = record with { Response = response };
            store.Record(accountId, transactionId, "UPDATE");
            return response;
        }
    }

    public void Delete(string accountId, string transactionId)
    {
        lock (store.SyncRoot)
        {
            EnsureAvailable();
            EnsurePending(Find(accountId, transactionId));
            store.Transactions.Remove(transactionId);
            store.Record(accountId, transactionId, "DELETE");
        }
    }

    private TransactionRecord Find(string accountId, string transactionId) =>
        store.Transactions.TryGetValue(transactionId, out var record) &&
        record.AccountId == accountId && record.RequestType == typeof(TRequest)
            ? record : throw ApiException.NotFound();

    private static void EnsurePending(TransactionRecord record)
    {
        if (record.Response.Status != "PENDING") throw ApiException.Conflict();
    }

    private void EnsureAvailable()
    {
        if (options.Value.SimulateProcessingFailure)
            throw new ApiException(500, "PROCESSING_ERROR", "Mock transaction processor is unavailable.", "processing");
    }

    private void ValidateReferences(string accountId, TRequest request)
    {
        // The create-payment contract does not declare 402; only withdrawals simulate insufficient funds.
        if (request is WithdrawalRequest && request.Amount > options.Value.AvailableBalance)
            throw new ApiException(402, "INSUFFICIENT_FUNDS", "Account does not have enough available balance.", "processing");
        if (request is RefundRequest refund &&
            (!store.Transactions.TryGetValue(refund.OriginalTransactionId, out var original) || original.AccountId != accountId))
            throw ApiException.NotFound();
    }

    private static TransactionResponse Map(TRequest request, string id, DateTimeOffset createdAt) => new()
    {
        TransactionId = id, Status = "PENDING", Amount = request.Amount!.Value,
        Currency = request.Currency ?? "USD", CreatedAt = createdAt, Description = request.Description,
        Source = (request as DepositRequest)?.Source,
        Channel = (request as WithdrawalRequest)?.Channel,
        OriginalTransactionId = (request as RefundRequest)?.OriginalTransactionId,
        Reason = (request as RefundRequest)?.Reason,
        CheckNumber = (request as CheckRequest)?.CheckNumber,
        Payee = (request as CheckRequest)?.Payee,
        PayeeAccountId = (request as PaymentRequest)?.PayeeAccountId,
        Reference = (request as PaymentRequest)?.Reference
    };
}
