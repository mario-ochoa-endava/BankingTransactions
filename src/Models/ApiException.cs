namespace BankingTransactions.Api.Models;

public sealed class ApiException(int statusCode, string code, string message, string errorType) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
    public string ErrorType { get; } = errorType;

    public static ApiException NotFound() => new(404, "TRANSACTION_NOT_FOUND", "Transaction not found.", "not_found");
    public static ApiException Conflict() => new(409, "TRANSACTION_CONFLICT", "Only pending transactions can be changed.", "conflict");
}
