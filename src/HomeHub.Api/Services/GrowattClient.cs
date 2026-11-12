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
    private string? _userId = null;

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

        var response = await httpClient.PostAsync(
            $"{_serverUrl}/login", content, ct);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        logger.LogInformation("Growatt login response: {Json}", json);
        var result = JsonSerializer.Deserialize<GrowattLoginResponse>(json);

        if (result?.Result != 1)
        {
            logger.LogError("Growatt login failed: result={Result}, msg={Message}, raw={Raw}",
                result?.Result, result?.Msg ?? "null", json.Length > 200 ? json[..200] : json);
            throw new GrowattApiException($"Growatt login failed: {result?.Msg}");
        }

        _userId = result.User?.Id;
        _isLoggedIn = true;
        logger.LogInformation("Growatt login successful for user {User}, userId={UserId}", _user, _userId ?? "not-in-response");
    }

    public async Task<GrowattPlantInfo> GetPlantInfoAsync(CancellationToken ct = default)
    {
        await EnsureLoggedInAsync(ct);

        var plantUrl = _userId != null
            ? $"{_serverUrl}/index/getUserPlantList?userId={_userId}"
            : $"{_serverUrl}/index/getUserPlantList";
        var response = await httpClient.GetAsync(plantUrl, ct);

        var plantBody = await response.Content.ReadAsStringAsync(ct);
        logger.LogInformation("getUserPlantList status={Status}, body={Body}",
            (int)response.StatusCode, plantBody.Length > 300 ? plantBody[..300] : plantBody);
        response.EnsureSuccessStatusCode();

        var result = JsonSerializer.Deserialize<GrowattPlantListResponse>(plantBody);

        var plant = result?.Data?.FirstOrDefault()
            ?? throw new GrowattApiException("No plants found on Growatt account.");

        // Get inverter serial for this plant
        var inverterContent = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("plantId", plant.PlantId),
            new KeyValuePair<string, string>("currPage", "1")
        ]);

        var invResponse = await httpClient.PostAsync(
            $"{_serverUrl}/device/getDevicesByPlantList", inverterContent, ct);

        invResponse.EnsureSuccessStatusCode();

        var invJson = await invResponse.Content.ReadAsStringAsync(ct);
        var invResult = JsonSerializer.Deserialize<GrowattInverterListResponse>(invJson);

        var inverter = invResult?.Data?.Datas?.FirstOrDefault()
            ?? throw new GrowattApiException("No inverters found for plant.");

        return new GrowattPlantInfo(plant.PlantId, plant.PlantName, inverter.Sn);
    }

    public async Task<SolarReading> GetCurrentReadingAsync(
        string plantId, string inverterSn, CancellationToken ct = default)
    {
        await EnsureLoggedInAsync(ct);

        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("op", "getData"),
            new KeyValuePair<string, string>("id", inverterSn),
            new KeyValuePair<string, string>("type", "1")
        ]);

        var response = await httpClient.PostAsync(
            $"{_serverUrl}/device/getInverterData", content, ct);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<GrowattInverterDataResponse>(json)
            ?? throw new GrowattApiException("Empty response from Growatt API.");

        return new SolarReading
        {
            PlantId = plantId,
            InverterSn = inverterSn,
            TimestampUtc = DateTimeOffset.UtcNow,
            PowerW = decimal.TryParse(result.Data?.Pac, out var pac) ? pac : 0,
            EnergyTodayKwh = decimal.TryParse(result.Data?.EToday, out var etoday) ? etoday : 0,
            EnergyTotalKwh = decimal.TryParse(result.Data?.ETotal, out var etotal) ? etotal : 0,
            Status = short.TryParse(result.Data?.Status, out var status) ? status : (short)0
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

    [JsonPropertyName("user")]
    public GrowattLoginUser? User { get; set; }
}

file class GrowattLoginUser
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

file class GrowattPlantListResponse
{
    [JsonPropertyName("data")]
    public List<GrowattPlant>? Data { get; set; }
}

file class GrowattPlant
{
    [JsonPropertyName("plantId")]
    public string PlantId { get; set; } = "";

    [JsonPropertyName("plantName")]
    public string PlantName { get; set; } = "";
}

file class GrowattInverterListResponse
{
    [JsonPropertyName("data")]
    public GrowattInverterData? Data { get; set; }
}

file class GrowattInverterData
{
    [JsonPropertyName("datas")]
    public List<GrowattInverter>? Datas { get; set; }
}

file class GrowattInverter
{
    [JsonPropertyName("sn")]
    public string Sn { get; set; } = "";
}

file class GrowattInverterDataResponse
{
    [JsonPropertyName("data")]
    public GrowattInverterValues? Data { get; set; }
}

file class GrowattInverterValues
{
    [JsonPropertyName("pac")]
    public string? Pac { get; set; }

    [JsonPropertyName("etoday")]
    public string? EToday { get; set; }

    [JsonPropertyName("etotal")]
    public string? ETotal { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
