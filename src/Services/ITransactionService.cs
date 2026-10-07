using BankingTransactions.Api.Contracts;

namespace BankingTransactions.Api.Services;

public interface ITransactionService<TRequest> where TRequest : TransactionRequest
{
    TransactionResponse Create(string accountId, TRequest request);
    TransactionListResponse List(string accountId, int limit, int offset);
    TransactionResponse Get(string accountId, string transactionId);
    TransactionResponse Update(string accountId, string transactionId, TRequest request);
    void Delete(string accountId, string transactionId);
}
