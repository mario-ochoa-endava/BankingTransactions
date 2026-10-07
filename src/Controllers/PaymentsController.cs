using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankingTransactions.Api.Controllers;

[Route("v1/accounts/{accountId}/transactions/payments")]
public sealed class PaymentsController(ITransactionService<PaymentRequest> service)
    : TransactionsControllerBase<PaymentRequest>(service);
