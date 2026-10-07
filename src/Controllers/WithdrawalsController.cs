using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankingTransactions.Api.Controllers;

[Route("v1/accounts/{accountId}/transactions/withdrawals")]
public sealed class WithdrawalsController(ITransactionService<WithdrawalRequest> service)
    : TransactionsControllerBase<WithdrawalRequest>(service);
