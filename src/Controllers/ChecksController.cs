using System;
using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BankingTransactions.Api.Controllers;

[Route("v1/accounts/{accountId}/transactions/checks")]
[Obsolete("ChecksController is deprecated. Use DepositsController instead.")]
public sealed class ChecksController(ITransactionService<CheckRequest> service,
    ILogger<TransactionsControllerBase<CheckRequest>> logger)
    : TransactionsControllerBase<CheckRequest>(service, logger);
