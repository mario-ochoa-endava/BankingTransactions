using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingTransactions.Api.Controllers;

[ApiController]
[Route("v1/health")]
public sealed class HealthController(IHealthService service, ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<HealthResponse> Get()
    {
        logger.LogInformation("Checking service health");
        var healthy = service.IsHealthy();
        logger.LogInformation("Service health check returned {HealthStatus}", healthy ? "UP" : "DOWN");
        return StatusCode(healthy ? 200 : 503, new HealthResponse(healthy ? "UP" : "DOWN"));
    }
}
