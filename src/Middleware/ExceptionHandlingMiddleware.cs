using BankingTransactions.Api.Models;

namespace BankingTransactions.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (ApiException exception) when (!context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode = exception.StatusCode;
            await context.Response.WriteAsJsonAsync(ApiErrors.Create(context, exception.Code, exception.Message, exception.ErrorType));
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            logger.LogError(exception, "Unhandled transaction API error. Request {RequestId}", context.TraceIdentifier);
            context.Response.Clear();
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(ApiErrors.Create(context, "PROCESSING_ERROR", "Transaction could not be processed.", "processing"));
        }
    }
}
