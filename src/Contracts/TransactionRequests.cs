using System.ComponentModel.DataAnnotations;

namespace BankingTransactions.Api.Contracts;

public abstract record TransactionRequest : IValidatableObject
{
    [Required]
    public decimal? Amount { get; init; }

    [RegularExpression("^[A-Z]{3}$")]
    public string? Currency { get; init; }

    [StringLength(200)]
    public string? Description { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // OpenAPI 3.0: minimum 0.01 + exclusiveMinimum true excludes 0.01 itself.
        if (Amount is decimal amount && (amount <= 0.01m || amount % 0.01m != 0))
            yield return new ValidationResult("Amount must exceed 0.01 and be a multiple of 0.01.", [nameof(Amount)]);
        if (Currency is not null && !System.Text.RegularExpressions.Regex.IsMatch(Currency, "^[A-Z]{3}$"))
            yield return new ValidationResult("Currency must contain exactly three uppercase letters.", [nameof(Currency)]);
        if (this is DepositRequest && Amount > 1_000_000m)
            yield return new ValidationResult("Deposit amount cannot exceed 1,000,000.00.", [nameof(Amount)]);
        if (this is WithdrawalRequest { Channel: not null } withdrawal &&
            withdrawal.Channel is not ("ATM" or "BRANCH" or "MOBILE" or "ONLINE"))
            yield return new ValidationResult("Channel must be ATM, BRANCH, MOBILE, or ONLINE.", [nameof(WithdrawalRequest.Channel)]);
    }
}

public sealed record DepositRequest : TransactionRequest
{
    [StringLength(64)] public string? Source { get; init; }
}

public sealed record WithdrawalRequest : TransactionRequest
{
    public string? Channel { get; init; }
}

public sealed record RefundRequest : TransactionRequest
{
    [Required, StringLength(64, MinimumLength = 1)]
    public string OriginalTransactionId { get; init; } = "";
    [StringLength(200)] public string? Reason { get; init; }
}

public sealed record CheckRequest : TransactionRequest
{
    [Required, StringLength(20, MinimumLength = 1)]
    public string CheckNumber { get; init; } = "";
    [StringLength(120)] public string? Payee { get; init; }
}

public sealed record PaymentRequest : TransactionRequest
{
    [Required, RegularExpression("^[A-Za-z0-9]{10}$")]
    public string PayeeAccountId { get; init; } = "";
    [StringLength(64)] public string? Reference { get; init; }
}
