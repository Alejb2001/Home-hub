using System.Security.Claims;
using HomeHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHub.Api.Controllers;

[ApiController]
[Route("api/solar")]
[Authorize]
public class SolarController(ISolarService solarService) : ControllerBase
{
    private long GetUserId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await solarService.GetSummaryAsync(GetUserId(), ct);
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
        var readings = await solarService.GetHistoryAsync(GetUserId(), from, to, ct);
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
        var stats = await solarService.GetStatsAsync(GetUserId(), period, ct);
        return Ok(stats);
    }
}
