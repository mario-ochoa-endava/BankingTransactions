using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankingTransactions.Api.Controllers;

[Route("v1/accounts/{accountId}/transactions/deposits")]
public sealed class DepositsController(ITransactionService<DepositRequest> service)
    : TransactionsControllerBase<DepositRequest>(service);
