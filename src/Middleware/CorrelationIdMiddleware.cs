using Serilog.Context;

namespace BankingTransactions.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    private const int MaximumLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context.Request.Headers[HeaderName].ToString());
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
            await next(context);
    }

    private static string GetCorrelationId(string value) =>
        value.Length is > 0 and <= MaximumLength && value.All(IsAllowedCharacter)
            ? value
            : Guid.NewGuid().ToString("N");

    private static bool IsAllowedCharacter(char value) =>
        char.IsAsciiLetterOrDigit(value) || value is '-' or '_' or '.';
}
