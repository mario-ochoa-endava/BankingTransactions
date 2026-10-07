namespace BankingTransactions.Api.Contracts;

public sealed record TransactionResponse
{
    public required string TransactionId { get; init; }
    public required string Status { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? Message { get; init; }
    public string? Description { get; init; }
    public string? Source { get; init; }
    public string? Channel { get; init; }
    public string? OriginalTransactionId { get; init; }
    public string? Reason { get; init; }
    public string? CheckNumber { get; init; }
    public string? Payee { get; init; }
    public string? PayeeAccountId { get; init; }
    public string? Reference { get; init; }
}

public sealed record TransactionListResponse(IReadOnlyList<TransactionResponse> Items, int Limit, int Offset, int Total);
public sealed record HealthResponse(string Status);
public sealed record ErrorResponse(ApiError Error);
public sealed record ApiError(string Code, string Message, string Type, string RequestId,
    DateTimeOffset Timestamp, IReadOnlyList<ErrorDetail>? Details = null);
public sealed record ErrorDetail(string Field, string Issue, string? RejectedValue = null);
