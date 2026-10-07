using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Models;

namespace BankingTransactions.Api.Services;

// One lock makes lookup, state checks, mutation, and audit recording atomic.
public sealed class MockTransactionStore
{
    internal object SyncRoot { get; } = new();
    internal Dictionary<string, TransactionRecord> Transactions { get; } = new(StringComparer.Ordinal);
    private readonly List<AuditEntry> audit = [];

    public MockTransactionStore()
    {
        Seed<DepositRequest>("deposits-completed");
        Seed<WithdrawalRequest>("withdrawals-completed");
        Seed<RefundRequest>("refunds-completed");
        Seed<CheckRequest>("checks-completed");
        Seed<PaymentRequest>("payments-completed");
    }

    private void Seed<T>(string id) where T : TransactionRequest => Transactions.Add(id,
        new TransactionRecord("ACCOUNT001", typeof(T), new TransactionResponse
        {
            TransactionId = id, Status = "COMPLETED", Amount = 100m, Currency = "USD",
            CreatedAt = DateTimeOffset.UnixEpoch, Description = "Seeded mock transaction",
            OriginalTransactionId = typeof(T) == typeof(RefundRequest) ? "deposits-completed" : null,
            CheckNumber = typeof(T) == typeof(CheckRequest) ? "SEED-1025" : null,
            PayeeAccountId = typeof(T) == typeof(PaymentRequest) ? "PAYEE00001" : null
        }));

    internal void Record(string accountId, string id, string action) =>
        audit.Add(new AuditEntry(accountId, id, action, DateTimeOffset.UtcNow));

    public IReadOnlyList<AuditEntry> GetAuditEntries()
    {
        lock (SyncRoot) return audit.ToArray();
    }
}
