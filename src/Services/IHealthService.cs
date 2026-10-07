using BankingTransactions.Api.Configuration;
using Microsoft.Extensions.Options;

namespace BankingTransactions.Api.Services;

public interface IHealthService
{
    bool IsHealthy();
}

public sealed class MockHealthService(IOptionsMonitor<MockApiOptions> options) : IHealthService
{
    public bool IsHealthy() => options.CurrentValue.Healthy;
}
