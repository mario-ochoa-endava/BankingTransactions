using BankingTransactions.Api.Contracts;

namespace BankingTransactions.Api.Middleware;

public static class ApiErrors
{
    public static ErrorResponse Create(HttpContext context, string code, string message, string type,
        IReadOnlyList<ErrorDetail>? details = null) =>
        new(new ApiError(code, message, type, context.TraceIdentifier, DateTimeOffset.UtcNow, details));
}
