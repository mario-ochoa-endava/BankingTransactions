using System.Text.Json.Serialization;
using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Middleware;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace BankingTransactions.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBankingApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MockApiOptions>().Bind(configuration.GetSection(MockApiOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.BearerToken), "Mock bearer token must not be empty.")
            .Validate(x => x.AvailableBalance >= 0, "Available balance must not be negative.").ValidateOnStart();
        services.AddSingleton<MockTransactionStore>();
        services.AddSingleton(typeof(ITransactionService<>), typeof(MockTransactionService<>));
        services.AddSingleton<IHealthService, MockHealthService>();
        services.AddAuthentication(MockBearerHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, MockBearerHandler>(MockBearerHandler.SchemeName, _ => { });
        //services.AddAuthorization();
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
        });
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var details = context.ModelState.Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(error => new ErrorDetail(x.Key,
                        string.IsNullOrEmpty(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage))).ToArray();
                return new BadRequestObjectResult(ApiErrors.Create(context.HttpContext, "VALIDATION_ERROR",
                    "Request validation failed.", "validation", details));
            };
        });
        return services;
    }
}
