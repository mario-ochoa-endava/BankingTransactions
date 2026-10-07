using BankingTransactions.Api.Contracts;

namespace BankingTransactions.Api.Models;

public sealed record TransactionRecord(string AccountId, Type RequestType, TransactionResponse Response);
public sealed record AuditEntry(string AccountId, string TransactionId, string Action, DateTimeOffset Timestamp);
