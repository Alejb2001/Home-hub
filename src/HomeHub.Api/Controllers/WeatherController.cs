using System.Security.Claims;
using System.Text.Json;
using HomeHub.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeHub.Api.Controllers;

[ApiController]
[Route("api/weather")]
[Authorize]
public class WeatherController(AppDbContext db) : ControllerBase
{
    private long GetUserId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var userId = GetUserId();
        var home = await db.Homes
            .Include(h => h.Services)
            .FirstOrDefaultAsync(h => h.UserId == userId, ct);

        if (home is null) return Ok(null);

        var weatherSvc = home.Services.FirstOrDefault(s => s.Type == "weather" && s.Enabled);
        if (weatherSvc is null) return Ok(null);

        var latest = await db.WeatherReadings
            .Where(w => w.ServiceConfigId == weatherSvc.Id)
            .OrderByDescending(w => w.TimestampUtc)
            .FirstOrDefaultAsync(ct);

        if (latest is null) return Ok(null);

        return Ok(new
        {
            tempC = latest.TempC,
            cloudsPct = latest.CloudsPct,
            weatherId = latest.WeatherId,
            description = latest.Description,
            humidityPct = latest.HumidityPct,
            windSpeedMs = latest.WindSpeedMs,
            timestampUtc = latest.TimestampUtc,
        });
    }
}
