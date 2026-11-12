using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeHub.Api.Models;

namespace HomeHub.Api.Services;

public class GrowattClient(HttpClient httpClient, IConfiguration configuration, ILogger<GrowattClient> logger)
    : IGrowattClient
{
    private static readonly string DefaultServerUrl = "https://server.growatt.com";

    private readonly string _serverUrl =
        configuration["GROWATT_SERVER_URL"] ?? DefaultServerUrl;

    private readonly string _user =
        configuration["GROWATT_USER"]
        ?? throw new InvalidOperationException("GROWATT_USER is not configured.");

    private readonly string _password =
        configuration["GROWATT_PASSWORD"]
        ?? throw new InvalidOperationException("GROWATT_PASSWORD is not configured.");

    private bool _isLoggedIn = false;

    private async Task EnsureLoggedInAsync(CancellationToken ct)
    {
        if (_isLoggedIn) return;

        var passwordHash = ComputeMd5(_password);

        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("account", _user),
            new KeyValuePair<string, string>("password", ""),
            new KeyValuePair<string, string>("validateCode", ""),
            new KeyValuePair<string, string>("loginType", "0"),
            new KeyValuePair<string, string>("isReadPact", "0"),
            new KeyValuePair<string, string>("passwordCrc", passwordHash)
        ]);

        var response = await httpClient.PostAsync($"{_serverUrl}/login", content, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<GrowattLoginResponse>(json);

        if (result?.Result != 1)
        {
            logger.LogError("Growatt login failed: result={Result}, msg={Message}, raw={Raw}",
                result?.Result, result?.Msg ?? "null", json.Length > 200 ? json[..200] : json);
            throw new GrowattApiException($"Growatt login failed: {result?.Msg}");
        }

        _isLoggedIn = true;
        logger.LogInformation("Growatt login successful for user {User}", _user);
    }

    // Returns the first plant's ID and name. InverterSn is populated later via GetCurrentReadingAsync.
    public async Task<GrowattPlantInfo> GetPlantInfoAsync(CancellationToken ct = default)
    {
        await EnsureLoggedInAsync(ct);

        // POST /index/getPlantListTitle — returns a JSON array of plants (session-based, no userId needed)
        var response = await httpClient.PostAsync(
            $"{_serverUrl}/index/getPlantListTitle",
            new FormUrlEncodedContent([]), ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        logger.LogInformation("getPlantListTitle: {Json}", json.Length > 300 ? json[..300] : json);

        var plants = JsonSerializer.Deserialize<List<GrowattPlantTitle>>(json);
        var plant = plants?.FirstOrDefault()
            ?? throw new GrowattApiException("No plants found on Growatt account.");

        return new GrowattPlantInfo(plant.Id, plant.PlantName, "");
    }

    // Fetches all device data for the plant in one call.
    // inverterSn param is unused — the response provides the actual SN.
    public async Task<SolarReading> GetCurrentReadingAsync(
        string plantId, string inverterSn, CancellationToken ct = default)
    {
        await EnsureLoggedInAsync(ct);

        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("currPage", "1"),
            new KeyValuePair<string, string>("plantId", plantId)
        ]);

        var response = await httpClient.PostAsync(
            $"{_serverUrl}/panel/getDevicesByPlantList", content, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        logger.LogInformation("getDevicesByPlantList: {Json}", json.Length > 400 ? json[..400] : json);

        var result = JsonSerializer.Deserialize<GrowattDeviceListResponse>(json)
            ?? throw new GrowattApiException("Empty response from getDevicesByPlantList.");

        var device = result.Obj?.Datas?.FirstOrDefault()
            ?? throw new GrowattApiException("No devices found in plant.");

        return new SolarReading
        {
            PlantId = plantId,
            InverterSn = device.Sn ?? inverterSn,
            TimestampUtc = DateTimeOffset.UtcNow,
            PowerW = decimal.TryParse(device.Pac, out var pac) ? pac : 0,
            EnergyTodayKwh = decimal.TryParse(device.EToday, out var etoday) ? etoday : 0,
            EnergyTotalKwh = decimal.TryParse(device.ETotal, out var etotal) ? etotal : 0,
            Status = short.TryParse(device.Status, out var status) ? status : (short)0
        };
    }

    private static string ComputeMd5(string input)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLower();
    }
}

public class GrowattApiException(string message) : Exception(message);

// --- Response DTOs (private to this file) ---

file class GrowattLoginResponse
{
    [JsonPropertyName("result")]
    public int Result { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }
}

// POST /index/getPlantListTitle returns a JSON array of these
file class GrowattPlantTitle
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("plantName")]
    public string PlantName { get; set; } = "";
}

// POST /panel/getDevicesByPlantList
file class GrowattDeviceListResponse
{
    [JsonPropertyName("result")]
    public int Result { get; set; }

    [JsonPropertyName("obj")]
    public GrowattDeviceListObj? Obj { get; set; }
}

file class GrowattDeviceListObj
{
    [JsonPropertyName("datas")]
    public List<GrowattDeviceData>? Datas { get; set; }
}

file class GrowattDeviceData
{
    [JsonPropertyName("sn")]
    public string? Sn { get; set; }

    [JsonPropertyName("pac")]
    public string? Pac { get; set; }

    [JsonPropertyName("eToday")]
    public string? EToday { get; set; }

    [JsonPropertyName("eTotal")]
    public string? ETotal { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
