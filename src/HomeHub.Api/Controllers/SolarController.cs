using HomeHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHub.Api.Controllers;

[ApiController]
[Route("api/solar")]
[Authorize]
public class SolarController(ISolarService solarService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await solarService.GetSummaryAsync(ct);
        if (summary is null)
            return Ok(new { message = "No data available yet." });

        return Ok(summary);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken ct)
    {
        if (from >= to)
            return BadRequest(new { message = "\"from\" must be before \"to\"." });

        var readings = await solarService.GetHistoryAsync(from, to, ct);
        return Ok(readings);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] string period = "day",
        CancellationToken ct = default)
    {
        var validPeriods = new[] { "day", "week", "month", "year" };
        if (!validPeriods.Contains(period))
            return BadRequest(new { message = $"period must be one of: {string.Join(", ", validPeriods)}" });

        var stats = await solarService.GetStatsAsync(period, ct);
        return Ok(stats);
    }
}
