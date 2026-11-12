using System.Collections.Concurrent;

namespace HomeHub.Api.Services;

public class GrowattClientFactory
{
    private readonly ILoggerFactory? _loggerFactory;
    private readonly Func<string, string, IGrowattClient>? _creatorOverride;
    private readonly ConcurrentDictionary<long, IGrowattClient> _clients = new();

    public GrowattClientFactory(ILoggerFactory loggerFactory) => _loggerFactory = loggerFactory;

    // For unit testing — injects a factory function instead of creating real HTTP clients
    internal GrowattClientFactory(Func<string, string, IGrowattClient> creatorOverride) =>
        _creatorOverride = creatorOverride;

    public IGrowattClient GetOrCreate(long serviceConfigId, string username, string password) =>
        _clients.GetOrAdd(serviceConfigId, _ => CreateClient(username, password));

    public void Invalidate(long serviceConfigId) =>
        _clients.TryRemove(serviceConfigId, out _);

    private IGrowattClient CreateClient(string username, string password)
    {
        if (_creatorOverride is not null) return _creatorOverride(username, password);

        var handler = new HttpClientHandler
        {
            CookieContainer = new System.Net.CookieContainer(),
            UseCookies = true,
            AllowAutoRedirect = true,
        };
        var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        return new GrowattClient(httpClient, username, password,
            _loggerFactory!.CreateLogger<GrowattClient>());
    }
}
