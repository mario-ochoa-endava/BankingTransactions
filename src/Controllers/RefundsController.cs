using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankingTransactions.Api.Controllers;

[Route("v1/accounts/{accountId}/transactions/refunds")]
public sealed class RefundsController(ITransactionService<RefundRequest> service,
    ILogger<TransactionsControllerBase<RefundRequest>> logger)
    : TransactionsControllerBase<RefundRequest>(service, logger);
