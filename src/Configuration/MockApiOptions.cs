namespace BankingTransactions.Api.Configuration;

public sealed class MockApiOptions
{
    public const string SectionName = "MockApi";
    public bool Healthy { get; set; } = true;
    public bool SimulateProcessingFailure { get; set; }
    public decimal AvailableBalance { get; set; } = 10_000m;
}
