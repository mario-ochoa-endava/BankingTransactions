# Banking Transactions API

.NET 10 controller-based mock implementation of `banking-transactions-openapi.yaml`.
All 26 contract operations are available under `/v1`.

## Run

Install the .NET 10 SDK, then run from this directory:

```powershell
dotnet restore BankingTransactions.slnx
dotnet run --project src/BankingTransactions.Api.csproj
```

The launch profile listens at `http://localhost:5080`. Health is available at
`http://localhost:5080/v1/health` without authentication. Transaction requests need:

```http
Authorization: Bearer local-mock-token
```

Import `banking-transactions.postman_collection.json` and
`banking-transactions.local.postman_environment.json` into Postman, then select the
local environment. Create requests capture their returned IDs in collection variables.

## Structure

```text
src/
  Controllers/       Five typed transaction controllers, shared CRUD actions, health
  Services/          Service interfaces, in-memory mock transaction and health services
  Models/            Internal records, audit entries, service exceptions
  Contracts/         Typed request/response DTOs; linked OpenAPI YAML in build output
  Middleware/        Contract error envelopes and exception handling
  Configuration/     Dependency injection, mock options, bearer authentication
  Program.cs
tests/
  BankingTransactions.Api.Tests/
    Controllers/     Isolated controller unit tests using mocked services
    Integration/     HTTP pipeline and OpenAPI route coverage tests
```

The root YAML remains the source contract and is copied into the application output
as `Contracts/banking-transactions-openapi.yaml`.

## Mock behavior

- Every syntactically valid 10-character alphanumeric account ID is recognized.
  Unused accounts have empty transaction lists. No account directory or real banking
  processor is called.
- New transactions are `PENDING`. PUT fully replaces editable fields and preserves
  ID, creation time, and status. Omitted optional fields are cleared; omitted currency
  defaults to `USD` because responses require a currency.
- GET/list include type-specific fields. Lists sort by creation time descending,
  then ID ascending; `limit` defaults to 20 (maximum 100), and `offset` defaults to 0.
- Only pending transactions can be updated or deleted. Other states return 409.
  Deleted transactions disappear from reads/lists; later reads, updates, and deletes
  return 404. Audit entries remain in memory and are not publicly exposed.
- Account `ACCOUNT001` contains a completed record for each type:
  `deposits-completed`, `withdrawals-completed`, `refunds-completed`,
  `checks-completed`, and `payments-completed`. Use these to exercise 409 responses.
  Use `deposits-completed` as a refund's `originalTransactionId`.
- Refunds require an existing original transaction on the same account. All storage,
  including audit history, resets on restart.
- Withdrawals above `MockApi:AvailableBalance` return 402. This is a per-request
  simulated balance check, not a ledger; creating transactions does not debit balances.
  Payments do not simulate insufficient funds because their create operation does
  not declare a 402 response.
- Amount validation follows the YAML literally: `minimum: 0.01` with
  `exclusiveMinimum: true` means `0.01` is invalid and `0.02` is the smallest valid
  cent amount. Deposits are capped at 1,000,000.00.

## Configuration

Defaults are in `src/appsettings.json`. Override them using environment variables:

```powershell
$env:MockApi__BearerToken = 'another-local-token'
$env:MockApi__Healthy = 'false'                  # GET /v1/health returns 503/DOWN
$env:MockApi__SimulateProcessingFailure = 'true' # Transaction operations return 500
$env:MockApi__AvailableBalance = '500'
dotnet run --project src/BankingTransactions.Api.csproj
```

This is a local mock application. Its fixed bearer token intentionally substitutes
for the JWT authentication declared by the contract; it is not a JWT validator.
Replace `MockBearerHandler` and the mock service registrations before connecting
this API to real banking data. Health configuration is independent of simulated
transaction processing failures so each response can be exercised separately.

## Test

```powershell
dotnet test BankingTransactions.slnx
```

Controller unit tests cover create/read/list/update/delete response values, routing
values, service arguments, pagination defaults, and propagation of service errors.
Health controller tests cover both 200/UP and 503/DOWN. Integration tests cover all
26 contract routes, CRUD lifecycles, authentication, validation, pagination,
account/type isolation, conflicts, missing records, error envelopes, and retained
audit records. They use an in-process server and do not call external services.

Controller tests isolate action logic; framework routing, binding, and automatic
validation are exercised through HTTP, following
[Microsoft's controller testing guidance](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/testing?view=aspnetcore-10.0).
