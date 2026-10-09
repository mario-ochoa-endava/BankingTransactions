using System.Text;
using System.Text.Json.Serialization;
using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Middleware;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BankingTransactions.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBankingApi(this IServiceCollection services, IConfiguration configuration)
    {
        var signingKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 UTF-8 bytes.");

        services.AddOptions<MockApiOptions>().Bind(configuration.GetSection(MockApiOptions.SectionName))
            .Validate(x => x.AvailableBalance >= 0, "Available balance must not be negative.").ValidateOnStart();
        services.AddSingleton<MockTransactionStore>();
        services.AddSingleton(typeof(ITransactionService<>), typeof(MockTransactionService<>));
        services.AddSingleton<IHealthService, MockHealthService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.Headers.WWWAuthenticate = "Bearer";
                        await context.Response.WriteAsJsonAsync(ApiErrors.Create(context.HttpContext,
                            "UNAUTHORIZED", "Missing or invalid bearer token.", "authorization"));
                    }
                };
            });
        services.AddAuthorization();
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
        });
        services.AddSwaggerGen(options =>
            options.SwaggerDoc("v1", new() { Title = "Banking Transactions API", Version = "1.1.0" }));
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
