namespace SIL.Machine.AspNetCore.Services;

public class ClearMLHealthCheck(
    IClearMLAuthenticationService clearMLAuthenticationService,
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<ClearMLOptions> options
) : IHealthCheck
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("ClearML-NoRetry");
    private readonly IOptionsMonitor<ClearMLOptions> _options = options;
    private readonly IClearMLAuthenticationService _clearMLAuthenticationService = clearMLAuthenticationService;
    private int _numConsecutiveFailures = 0;
    private readonly AsyncLock _lock = new AsyncLock();

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (!await PingAsync(cancellationToken))
                return HealthCheckResult.Unhealthy("ClearML is unresponsive");
            if (!await WorkersAreAssignedToQueue(cancellationToken))
            {
                return HealthCheckResult.Unhealthy(
                    $"No ClearML agents are available for configured queue \"{_options.CurrentValue.Queue}\""
                );
            }

            using (await _lock.LockAsync())
                _numConsecutiveFailures = 0;
            return HealthCheckResult.Healthy("ClearML is available");
        }
        catch (Exception e)
        {
            using (await _lock.LockAsync())
            {
                _numConsecutiveFailures++;
                return _numConsecutiveFailures > 3
                    ? HealthCheckResult.Unhealthy(exception: e)
                    : HealthCheckResult.Degraded(exception: e);
            }
        }
    }

    private async Task<JsonObject?> CallAsync(
        string service,
        string action,
        JsonNode body,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{service}.{action}")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Add(
            "Authorization",
            $"Bearer {await _clearMLAuthenticationService.GetAuthTokenAsync(cancellationToken)}"
        );
        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string result = await response.Content.ReadAsStringAsync(cancellationToken);
        return (JsonObject?)JsonNode.Parse(result);
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        JsonObject? result = await CallAsync("debug", "ping", new JsonObject(), cancellationToken);
        return result is not null;
    }

    public async Task<bool> WorkersAreAssignedToQueue(CancellationToken cancellationToken = default)
    {
        JsonObject? result = await CallAsync("workers", "get_all", new JsonObject(), cancellationToken);
        JsonNode? workers_node = result?["data"]?["workers"];
        if (workers_node is null)
            throw new InvalidOperationException("Malformed response from ClearML server.");
        JsonArray workers = (JsonArray)workers_node;
        foreach (var worker in workers)
        {
            JsonNode? queues_node = worker?["queues"];
            if (queues_node is null)
                continue;
            JsonArray queues = (JsonArray)queues_node;
            foreach (var queue in queues)
            {
                if ((string?)queue?["name"] == _options.CurrentValue.Queue)
                    return true;
            }
        }
        return false;
    }
}
