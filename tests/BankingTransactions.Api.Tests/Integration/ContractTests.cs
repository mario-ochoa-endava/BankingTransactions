using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BankingTransactions.Api.Tests.Integration;

public sealed class ContractTests
{
    public static TheoryData<string> Kinds => new() { "deposits", "withdrawals", "refunds", "checks", "payments" };
    private static string Collection(string kind, string account = "ACCOUNT001") => $"/v1/accounts/{account}/transactions/{kind}";
    private static Dictionary<string, object?> Body(string kind, decimal amount = 25m) => new()
    {
        ["amount"] = amount, ["currency"] = "USD", ["description"] = "Example",
        [kind switch { "deposits" => "source", "withdrawals" => "channel", "refunds" => "originalTransactionId", "checks" => "checkNumber", _ => "payeeAccountId" }] =
            kind switch { "deposits" => "External transfer", "withdrawals" => "MOBILE", "refunds" => "deposits-completed", "checks" => "1025", _ => "PAYEE00001" }
    };

    [Theory, MemberData(nameof(Kinds))]
    public async Task Full_crud_preserves_identity_clears_optional_fields_and_retains_audit(string kind)
    {
        using var app = new ApiFactory();
        using var client = app.AuthenticatedClient();
        var collection = Collection(kind);
        var created = await client.PostAsJsonAsync(collection, Body(kind));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var original = (await created.Content.ReadFromJsonAsync<TransactionResponse>())!;
        Assert.Equal("PENDING", original.Status);
        Assert.Equal(25m, original.Amount);
        Assert.Equal("USD", original.Currency);
        Assert.NotNull(created.Headers.Location);
        Assert.EndsWith(collection + "/" + original.TransactionId, created.Headers.Location.ToString());
        var location = created.Headers.Location!;
        var read = await client.GetFromJsonAsync<TransactionResponse>(location);
        Assert.Equal(original, read);
        var list = (await client.GetFromJsonAsync<TransactionListResponse>(collection + "?limit=1&offset=0"))!;
        Assert.Equal(2, list.Total);
        Assert.Equal(original.TransactionId, Assert.Single(list.Items).TransactionId);
        Assert.Equal(1, list.Limit);
        Assert.Equal(0, list.Offset);
        var replacement = Body(kind, 50m);
        replacement.Remove("description");
        replacement.Remove("currency");
        if (kind == "deposits") replacement.Remove("source");
        if (kind == "withdrawals") replacement.Remove("channel");
        var updated = await client.PutAsJsonAsync(location, replacement);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var result = (await updated.Content.ReadFromJsonAsync<TransactionResponse>())!;
        Assert.Equal(original.TransactionId, result.TransactionId);
        Assert.Equal(original.CreatedAt, result.CreatedAt);
        Assert.Equal("PENDING", result.Status);
        Assert.Equal(50m, result.Amount);
        Assert.Equal("USD", result.Currency);
        Assert.Null(result.Description);
        Assert.Null(result.Source);
        Assert.Null(result.Channel);
        var deleted = await client.DeleteAsync(location);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await deleted.Content.ReadAsByteArrayAsync());
        await AssertError(await client.GetAsync(location), 404, "not_found");
        await AssertError(await client.PutAsJsonAsync(location, Body(kind)), 404, "not_found");
        await AssertError(await client.DeleteAsync(location), 404, "not_found");
        var after = (await client.GetFromJsonAsync<TransactionListResponse>(collection))!;
        Assert.Equal(1, after.Total);
        Assert.DoesNotContain(after.Items, x => x.TransactionId == original.TransactionId);
        var audit = app.Services.GetRequiredService<MockTransactionStore>().GetAuditEntries();
        Assert.Equal(new[] { "CREATE", "UPDATE", "DELETE" }, audit.Select(x => x.Action));
    }

    [Fact]
    public async Task Create_requires_admin_role_while_read_endpoints_allow_other_roles()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", ApiFactory.CreateToken(role: "user"));
        var collection = Collection("deposits");

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(collection)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync(collection, Body("deposits"))).StatusCode);
    }

    [Fact]
    public async Task Correlation_id_is_returned_and_used_in_error_response()
    {
        const string correlationId = "integration-test-correlation_123";
        using var app = new ApiFactory();
        using var client = app.AuthenticatedClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

        var response = await client.GetAsync(Collection("deposits") + "/missing");
        Assert.Equal(correlationId, Assert.Single(response.Headers.GetValues("X-Correlation-Id")));
        var error = (await response.Content.ReadFromJsonAsync<ErrorResponse>())!;
        Assert.Equal(correlationId, error.Error.RequestId);
    }

    [Fact]
    public async Task Missing_correlation_id_is_generated_and_returned()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/v1/health");
        var correlationId = Assert.Single(response.Headers.GetValues("X-Correlation-Id"));

        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Theory, MemberData(nameof(Kinds))]
    public async Task Completed_transactions_are_immutable(string kind)
    {
        using var app = new ApiFactory();
        using var client = app.AuthenticatedClient();
        var path = Collection(kind) + "/" + kind + "-completed";
        await AssertError(await client.PutAsJsonAsync(path, Body(kind)), 409, "conflict");
        await AssertError(await client.DeleteAsync(path), 409, "conflict");
        Assert.Equal("COMPLETED", (await client.GetFromJsonAsync<TransactionResponse>(path))!.Status);
    }

    [Theory, MemberData(nameof(Kinds))]
    public async Task Missing_or_invalid_authentication_returns_contract_error_for_every_action(string kind)
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        var collection = Collection(kind);
        await AssertError(await client.GetAsync(collection), 401, "authorization");
        await AssertError(await client.PostAsJsonAsync(collection, Body(kind)), 401, "authorization");
        await AssertError(await client.GetAsync(collection + "/missing"), 401, "authorization");
        await AssertError(await client.PutAsJsonAsync(collection + "/missing", Body(kind)), 401, "authorization");
        await AssertError(await client.DeleteAsync(collection + "/missing"), 401, "authorization");
        client.DefaultRequestHeaders.Authorization = new("Bearer", "invalid-token");
        var response = await client.GetAsync(collection);
        await AssertError(response, 401, "authorization");
        Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).Scheme);

        foreach (var token in new[]
                 {
                     ApiFactory.CreateToken(signingKey: "different-signing-key-that-is-over-32-bytes"),
                     ApiFactory.CreateToken(expires: DateTime.UtcNow.AddMinutes(-10))
                 })
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
            await AssertError(await client.GetAsync(collection), 401, "authorization");
        }
    }

    [Theory, MemberData(nameof(Kinds))]
    public async Task Invalid_amount_and_missing_amount_are_rejected(string kind)
    {
        using var app = new ApiFactory();
        using var client = app.AuthenticatedClient();
        foreach (var amount in new[] { -1m, 0m, 0.01m, 1.001m })
            await AssertError(await client.PostAsJsonAsync(Collection(kind), Body(kind, amount)), 400, "validation");
        var missing = Body(kind);
        missing.Remove("amount");
        await AssertError(await client.PostAsJsonAsync(Collection(kind), missing), 400, "validation");
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(Collection(kind), Body(kind, 0.02m))).StatusCode);
    }

    [Theory]
    [InlineData("?limit=0")]
    [InlineData("?limit=101")]
    [InlineData("?offset=-1")]
    [InlineData("?limit=abc")]
    public async Task Invalid_pagination_returns_400(string query)
    {
        using var app = new ApiFactory();
        using var client = app.AuthenticatedClient();
        await AssertError(await client.GetAsync(Collection("deposits") + query), 400, "validation");
    }

    [Fact]
    public async Task Account_and_type_scope_are_enforced_and_missing_updates_do_not_create()
    {
        using var app = new ApiFactory();
        using var client = app.AuthenticatedClient();
        await AssertError(await client.GetAsync(Collection("deposits", "ACCOUNT002") + "/deposits-completed"), 404, "not_found");
        await AssertError(await client.GetAsync(Collection("payments") + "/deposits-completed"), 404, "not_found");
        await AssertError(await client.PutAsJsonAsync(Collection("deposits") + "/missing", Body("deposits")), 404, "not_found");
        await AssertError(await client.GetAsync(Collection("deposits", "short")), 400, "validation");
        await AssertError(await client.GetAsync(Collection("deposits") + "/" + new string('x', 65)), 400, "validation");
        var empty = (await client.GetFromJsonAsync<TransactionListResponse>(Collection("deposits", "ACCOUNT002")))!;
        Assert.Empty(empty.Items);
        Assert.Equal(0, empty.Total);
        Assert.Equal(20, empty.Limit);
    }

    [Fact]
    public async Task Type_specific_constraints_and_refund_reference_are_validated()
    {
        using var app = new ApiFactory();
        using var client = app.AuthenticatedClient();
        await AssertError(await client.PostAsJsonAsync(Collection("deposits"), Body("deposits", 1_000_000.01m)), 400, "validation");
        foreach (var (kind, field, value) in new[] {
            ("withdrawals", "channel", "INVALID"), ("withdrawals", "channel", ""),
            ("deposits", "currency", ""), ("deposits", "currency", "usd"),
            ("payments", "payeeAccountId", "short"), ("checks", "checkNumber", ""),
            ("refunds", "originalTransactionId", "") })
        {
            var body = Body(kind);
            body[field] = value;
            await AssertError(await client.PostAsJsonAsync(Collection(kind), body), 400, "validation");
        }
        await AssertError(await client.PostAsJsonAsync(Collection("refunds", "ACCOUNT002"), Body("refunds")), 404, "not_found");
        await AssertError(await client.PostAsJsonAsync(Collection("withdrawals"), Body("withdrawals", 10_000.01m)), 402, "processing");
        var malformed = new StringContent("{invalid", Encoding.UTF8, "application/json");
        await AssertError(await client.PostAsync(Collection("deposits"), malformed), 400, "validation");
    }

    [Theory]
    [InlineData(true, 200, "UP")]
    [InlineData(false, 503, "DOWN")]
    public async Task Health_is_anonymous_and_reports_configured_readiness(bool healthy, int code, string status)
    {
        using var app = new ApiFactory();
        app.Settings["MockApi:Healthy"] = healthy.ToString();
        using var client = app.CreateClient();
        var response = await client.GetAsync("/v1/health");
        Assert.Equal(code, (int)response.StatusCode);
        Assert.Equal(status, (await response.Content.ReadFromJsonAsync<HealthResponse>())!.Status);
    }

    [Fact]
    public async Task Simulated_processing_failure_uses_contract_error_envelope()
    {
        using var app = new ApiFactory();
        app.Settings["MockApi:SimulateProcessingFailure"] = "true";
        using var client = app.AuthenticatedClient();
        await AssertError(await client.GetAsync(Collection("deposits")), 500, "processing");
    }

    [Fact]
    public void Every_contract_operation_has_a_controller_route()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        var contract = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Contracts", "banking-transactions-openapi.yaml"));
        var expected = new HashSet<string>();
        string? path = null;
        foreach (var line in contract)
        {
            if (line == "components:") break;
            var pathMatch = Regex.Match(line, @"^  (/.*):$");
            if (pathMatch.Success) path = pathMatch.Groups[1].Value;
            var methodMatch = Regex.Match(line, @"^    (get|post|put|delete):$");
            if (methodMatch.Success) expected.Add(methodMatch.Groups[1].Value.ToUpperInvariant() + " v1" + path);
        }
        var actions = app.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items;
        var actual = actions.SelectMany(action => action.ActionConstraints!.OfType<HttpMethodActionConstraint>()
            .SelectMany(constraint => constraint.HttpMethods.Select(method => method + " " + action.AttributeRouteInfo!.Template))).ToHashSet();
        Assert.Equal(26, expected.Count);
        Assert.True(expected.SetEquals(actual), "Controller routes must exactly match the OpenAPI operations.");
    }

    private static async Task AssertError(HttpResponseMessage response, int status, string type)
    {
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        var envelope = (await response.Content.ReadFromJsonAsync<ErrorResponse>())!;
        Assert.Equal(type, envelope.Error.Type);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Error.Code));
        Assert.False(string.IsNullOrWhiteSpace(envelope.Error.Message));
        Assert.False(string.IsNullOrWhiteSpace(envelope.Error.RequestId));
        Assert.NotEqual(default, envelope.Error.Timestamp);
        if (status == 400) Assert.NotEmpty(envelope.Error.Details!);
    }
}
