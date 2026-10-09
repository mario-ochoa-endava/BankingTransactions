using System.ComponentModel.DataAnnotations;
using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingTransactions.Api.Controllers;

[Route("v1/accounts/{accountId}/transactions/deposits")]
public sealed class DepositsController(ITransactionService<DepositRequest> service,
    ILogger<TransactionsControllerBase<DepositRequest>> logger)
    : TransactionsControllerBase<DepositRequest>(service, logger)
{
    [HttpPost]
    [Authorize(Roles = "admin")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public override ActionResult<TransactionResponse> Create(
        [RegularExpression("^[A-Za-z0-9]{10}$")] string accountId, [FromBody] DepositRequest request) =>
        base.Create(accountId, request);
}
