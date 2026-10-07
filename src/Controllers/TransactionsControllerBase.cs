using System.ComponentModel.DataAnnotations;
using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingTransactions.Api.Controllers;

[ApiController]
//[Authorize]
[Produces("application/json")]
public abstract class TransactionsControllerBase<TRequest>(ITransactionService<TRequest> service) : ControllerBase
    where TRequest : TransactionRequest
{
    [HttpPost]
    public ActionResult<TransactionResponse> Create(
        [RegularExpression("^[A-Za-z0-9]{10}$")] string accountId, [FromBody] TRequest request)
    {
        var response = service.Create(accountId, request);
        return CreatedAtAction(nameof(Get), new { accountId, transactionId = response.TransactionId }, response);
    }

    [HttpGet]
    public ActionResult<TransactionListResponse> List(
        [RegularExpression("^[A-Za-z0-9]{10}$")] string accountId,
        [FromQuery, Range(1, 100)] int limit = 20, [FromQuery, Range(0, int.MaxValue)] int offset = 0) =>
        Ok(service.List(accountId, limit, offset));

    [HttpGet("{transactionId}")]
    public ActionResult<TransactionResponse> Get(
        [RegularExpression("^[A-Za-z0-9]{10}$")] string accountId,
        [StringLength(64, MinimumLength = 1)] string transactionId) => Ok(service.Get(accountId, transactionId));

    [HttpPut("{transactionId}")]
    public ActionResult<TransactionResponse> Update(
        [RegularExpression("^[A-Za-z0-9]{10}$")] string accountId,
        [StringLength(64, MinimumLength = 1)] string transactionId, [FromBody] TRequest request) =>
        Ok(service.Update(accountId, transactionId, request));

    [HttpDelete("{transactionId}")]
    public IActionResult Delete(
        [RegularExpression("^[A-Za-z0-9]{10}$")] string accountId,
        [StringLength(64, MinimumLength = 1)] string transactionId)
    {
        service.Delete(accountId, transactionId);
        return NoContent();
    }
}
