using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BankingTransactions.Api.Tests.Integration;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private const string TestSigningKey = "test-only-signing-key-that-is-at-least-32-bytes";

    public Dictionary<string, string?> Settings { get; } = new()
    {
        ["Jwt:SigningKey"] = TestSigningKey
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(Settings));
    }

    public HttpClient AuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken());
        return client;
    }

    public static string CreateToken(string signingKey = TestSigningKey, DateTime? expires = null, string role = "admin")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.NameIdentifier, "integration-test"), new Claim(ClaimTypes.Role, role)],
            expires: expires ?? DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
