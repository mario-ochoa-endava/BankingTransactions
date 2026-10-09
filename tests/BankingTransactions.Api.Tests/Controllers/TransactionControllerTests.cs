using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Controllers;
using BankingTransactions.Api.Models;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BankingTransactions.Api.Tests.Controllers;

public abstract class TransactionControllerTests<TRequest> where TRequest : TransactionRequest
{
    private const string Account = "ACCOUNT001";
    private const string Id = "transaction-123";
    private readonly Mock<ITransactionService<TRequest>> service = new(MockBehavior.Strict);
    protected abstract TransactionsControllerBase<TRequest> Controller(ITransactionService<TRequest> service);
    protected abstract TRequest Request();
    private static TransactionResponse Response() => new()
    {
        TransactionId = Id, Status = "PENDING", Amount = 25m, Currency = "USD", CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public void Create_returns_201_with_location_values_and_service_response()
    {
        var request = Request();
        var response = Response();
        service.Setup(x => x.Create(Account, request)).Returns(response);
        var result = Assert.IsType<CreatedAtActionResult>(Controller(service.Object).Create(Account, request).Result);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("Get", result.ActionName);
        Assert.Equal(Account, result.RouteValues!["accountId"]);
        Assert.Equal(Id, result.RouteValues["transactionId"]);
        Assert.Same(response, result.Value);
        service.Verify(x => x.Create(Account, request), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public void List_forwards_account_and_pagination_and_returns_200()
    {
        var response = new TransactionListResponse([Response()], 5, 10, 11);
        service.Setup(x => x.List(Account, 5, 10)).Returns(response);
        var result = Assert.IsType<OkObjectResult>(Controller(service.Object).List(Account, 5, 10).Result);
        Assert.Same(response, result.Value);
        service.Verify(x => x.List(Account, 5, 10), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public void List_uses_contract_pagination_defaults()
    {
        service.Setup(x => x.List(Account, 20, 0)).Returns(new TransactionListResponse([], 20, 0, 0));
        Assert.IsType<OkObjectResult>(Controller(service.Object).List(Account).Result);
        service.Verify(x => x.List(Account, 20, 0), Times.Once);
    }

    [Fact]
    public void Get_forwards_account_and_id_and_returns_200()
    {
        var response = Response();
        service.Setup(x => x.Get(Account, Id)).Returns(response);
        Assert.Same(response, Assert.IsType<OkObjectResult>(Controller(service.Object).Get(Account, Id).Result).Value);
        service.Verify(x => x.Get(Account, Id), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public void Update_forwards_replacement_and_returns_200()
    {
        var request = Request();
        var response = Response();
        service.Setup(x => x.Update(Account, Id, request)).Returns(response);
        Assert.Same(response, Assert.IsType<OkObjectResult>(Controller(service.Object).Update(Account, Id, request).Result).Value);
        service.Verify(x => x.Update(Account, Id, request), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public void Delete_calls_service_and_returns_204_without_body()
    {
        service.Setup(x => x.Delete(Account, Id));
        Assert.IsType<NoContentResult>(Controller(service.Object).Delete(Account, Id));
        service.Verify(x => x.Delete(Account, Id), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public void Missing_transaction_error_is_forwarded_to_middleware()
    {
        var error = ApiException.NotFound();
        service.Setup(x => x.Get(Account, Id)).Throws(error);
        Assert.Same(error, Assert.Throws<ApiException>(() => Controller(service.Object).Get(Account, Id)));
    }

    [Fact]
    public void Update_conflict_is_forwarded_to_middleware()
    {
        var request = Request();
        var error = ApiException.Conflict();
        service.Setup(x => x.Update(Account, Id, request)).Throws(error);
        Assert.Same(error, Assert.Throws<ApiException>(() => Controller(service.Object).Update(Account, Id, request)));
    }

    [Fact]
    public void Delete_conflict_is_forwarded_to_middleware()
    {
        var error = ApiException.Conflict();
        service.Setup(x => x.Delete(Account, Id)).Throws(error);
        Assert.Same(error, Assert.Throws<ApiException>(() => Controller(service.Object).Delete(Account, Id)));
    }
}

public sealed class DepositsControllerTests : TransactionControllerTests<DepositRequest>
{
    protected override TransactionsControllerBase<DepositRequest> Controller(ITransactionService<DepositRequest> service) =>
        new DepositsController(service, NullLogger<TransactionsControllerBase<DepositRequest>>.Instance);
    protected override DepositRequest Request() => new() { Amount = 25m, Currency = "USD", Source = "External transfer" };
}

public sealed class WithdrawalsControllerTests : TransactionControllerTests<WithdrawalRequest>
{
    protected override TransactionsControllerBase<WithdrawalRequest> Controller(ITransactionService<WithdrawalRequest> service) =>
        new WithdrawalsController(service, NullLogger<TransactionsControllerBase<WithdrawalRequest>>.Instance);
    protected override WithdrawalRequest Request() => new() { Amount = 25m, Currency = "USD", Channel = "MOBILE" };
}

public sealed class RefundsControllerTests : TransactionControllerTests<RefundRequest>
{
    protected override TransactionsControllerBase<RefundRequest> Controller(ITransactionService<RefundRequest> service) =>
        new RefundsController(service, NullLogger<TransactionsControllerBase<RefundRequest>>.Instance);
    protected override RefundRequest Request() => new() { Amount = 25m, Currency = "USD", OriginalTransactionId = "deposits-completed" };
}

public sealed class ChecksControllerTests : TransactionControllerTests<CheckRequest>
{
    protected override TransactionsControllerBase<CheckRequest> Controller(ITransactionService<CheckRequest> service) =>
        new ChecksController(service, NullLogger<TransactionsControllerBase<CheckRequest>>.Instance);
    protected override CheckRequest Request() => new() { Amount = 25m, Currency = "USD", CheckNumber = "1025" };
}

public sealed class PaymentsControllerTests : TransactionControllerTests<PaymentRequest>
{
    protected override TransactionsControllerBase<PaymentRequest> Controller(ITransactionService<PaymentRequest> service) =>
        new PaymentsController(service, NullLogger<TransactionsControllerBase<PaymentRequest>>.Instance);
    protected override PaymentRequest Request() => new() { Amount = 25m, Currency = "USD", PayeeAccountId = "PAYEE00001" };
}
