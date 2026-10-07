using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using BankingTransactions.Api.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BankingTransactions.Api.Configuration;

// A fixed development token for this mock API. This is deliberately not JWT validation.
public sealed class MockBearerHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder, IOptionsMonitor<MockApiOptions> mockOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "MockBearer";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(header.Parameter))
            return Task.FromResult(AuthenticateResult.NoResult());
        var expected = Encoding.UTF8.GetBytes(mockOptions.CurrentValue.BearerToken);
        var actual = Encoding.UTF8.GetBytes(header.Parameter);
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            return Task.FromResult(AuthenticateResult.Fail("Invalid mock bearer token."));
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "mock-user")], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Bearer";
        return Response.WriteAsJsonAsync(ApiErrors.Create(Context, "UNAUTHORIZED", "Missing or invalid bearer token.", "authorization"));
    }
}
