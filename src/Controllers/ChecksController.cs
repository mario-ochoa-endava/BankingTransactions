using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankingTransactions.Api.Controllers;

[Route("v1/accounts/{accountId}/transactions/checks")]
public sealed class ChecksController(ITransactionService<CheckRequest> service,
    ILogger<TransactionsControllerBase<CheckRequest>> logger)
    : TransactionsControllerBase<CheckRequest>(service, logger);
