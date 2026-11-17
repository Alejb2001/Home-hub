using System.Text.Json;
using System.Text.Json.Serialization;
using HomeHub.Api.Models;

namespace HomeHub.Api.Services;

public interface IWeatherClient
{
    Task<WeatherReading> GetCurrentAsync(long serviceConfigId, CancellationToken ct = default);
}

public class WeatherClient(HttpClient httpClient, string apiKey, string lat, string lon, ILogger<WeatherClient> logger)
    : IWeatherClient
{
    public async Task<WeatherReading> GetCurrentAsync(long serviceConfigId, CancellationToken ct = default)
    {
        var url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={apiKey}&units=metric&lang=es";
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        logger.LogInformation("OWM response for service {Id}: {Json}",
            serviceConfigId, json.Length > 300 ? json[..300] : json);

        var result = JsonSerializer.Deserialize<OwmCurrentResponse>(json)
            ?? throw new InvalidOperationException("Empty response from OpenWeatherMap.");

        return new WeatherReading
        {
            ServiceConfigId = serviceConfigId,
            TimestampUtc = DateTimeOffset.UtcNow,
            TempC = (decimal)(result.Main?.Temp ?? 0),
            CloudsPct = result.Clouds?.All ?? 0,
            WeatherId = result.Weather?.FirstOrDefault()?.Id ?? 800,
            Description = result.Weather?.FirstOrDefault()?.Description ?? "",
            HumidityPct = result.Main?.Humidity ?? 0,
            WindSpeedMs = (decimal)(result.Wind?.Speed ?? 0),
        };
    }
}

// --- OWM response DTOs ---

file class OwmCurrentResponse
{
    [JsonPropertyName("weather")]
    public List<OwmWeatherCondition>? Weather { get; set; }

    [JsonPropertyName("main")]
    public OwmMain? Main { get; set; }

    [JsonPropertyName("wind")]
    public OwmWind? Wind { get; set; }

    [JsonPropertyName("clouds")]
    public OwmClouds? Clouds { get; set; }
}

file class OwmWeatherCondition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

file class OwmMain
{
    [JsonPropertyName("temp")]
    public double Temp { get; set; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }
}

file class OwmWind
{
    [JsonPropertyName("speed")]
    public double Speed { get; set; }
}

file class OwmClouds
{
    [JsonPropertyName("all")]
    public int All { get; set; }
}
