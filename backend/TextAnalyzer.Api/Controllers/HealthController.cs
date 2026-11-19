using Microsoft.AspNetCore.Mvc;
using TextAnalyzer.Api.Data;

namespace TextAnalyzer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly TextAnalyzerDbContext _dbContext;

    public HealthController(TextAnalyzerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "Healthy" });
    }

    [HttpGet("database")]
    public async Task<IActionResult> GetDatabaseStatus()
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(
            HttpContext.RequestAborted);

        if (!canConnect)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { status = "Unhealthy" });
        }

        return Ok(new { status = "Healthy" });
    }
}
