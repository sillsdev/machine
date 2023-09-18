public class ClearMLHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ClearMLNmtEngineOptions> _options;
    private string _authToken;
    private readonly AsyncLock _lock;

    public ClearMLHealthCheck(HttpClient httpClient, IOptionsMonitor<ClearMLNmtEngineOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
        _authToken = "";
        _lock = new AsyncLock();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using (await _lock.LockAsync())
                if (_authToken == "")
                    _authToken = await GetAuthTokenAsync(cancellationToken);
            if (!await PingAsync(cancellationToken))
                return HealthCheckResult.Unhealthy("ClearML is unresponsive");
            if (!await WorkersAreAssignedToQueue(cancellationToken))
                return HealthCheckResult.Unhealthy("No ClearML agents are available");
            return HealthCheckResult.Healthy("ClearML is available");
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy(exception: e);
        }
    }

    private async Task<string> GetAuthTokenAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.CurrentValue.ApiServer}/auth.login")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        var authenticationString = $"{_options.CurrentValue.AccessKey}:{_options.CurrentValue.SecretKey}";
        var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
        request.Headers.Add("Authorization", $"Basic {base64EncodedAuthenticationString}");
        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string result = await response.Content.ReadAsStringAsync(cancellationToken);
        return (string)((JsonObject?)JsonNode.Parse(result))?["data"]?["token"]!;
    }

    private async Task<JsonObject?> CallAsync(
        string service,
        string action,
        JsonNode body,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.CurrentValue.ApiServer}/{service}.{action}")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_authToken}");
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
            return false;
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
