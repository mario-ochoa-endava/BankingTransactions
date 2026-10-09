using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Controllers;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BankingTransactions.Api.Tests.Controllers;

public sealed class HealthControllerTests
{
    [Theory]
    [InlineData(true, 200, "UP")]
    [InlineData(false, 503, "DOWN")]
    public void Get_returns_status_from_health_service(bool healthy, int code, string status)
    {
        var service = new Mock<IHealthService>(MockBehavior.Strict);
        service.Setup(x => x.IsHealthy()).Returns(healthy);
        var result = Assert.IsType<ObjectResult>(new HealthController(service.Object, NullLogger<HealthController>.Instance).Get().Result);
        Assert.Equal(code, result.StatusCode);
        Assert.Equal(status, Assert.IsType<HealthResponse>(result.Value).Status);
        service.Verify(x => x.IsHealthy(), Times.Once);
        service.VerifyNoOtherCalls();
    }
}
