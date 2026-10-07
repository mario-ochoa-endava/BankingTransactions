using BankingTransactions.Api.Contracts;
using BankingTransactions.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingTransactions.Api.Controllers;

[ApiController]
[Route("v1/health")]
public sealed class HealthController(IHealthService service) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<HealthResponse> Get()
    {
        var healthy = service.IsHealthy();
        return StatusCode(healthy ? 200 : 503, new HealthResponse(healthy ? "UP" : "DOWN"));
    }
}
