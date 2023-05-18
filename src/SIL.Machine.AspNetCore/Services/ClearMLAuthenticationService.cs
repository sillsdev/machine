namespace SIL.Machine.AspNetCore.Services;

public class ClearMLAuthenticationService : IClearMLAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ClearMLNmtEngineOptions> _options;
    private readonly ILogger<ClearMLAuthenticationService> _logger;
    private int _refreshPeriod = 3600; // 1 hour
    private PeriodicTimer? _periodicTimer = null;
    private CancellationTokenSource _ctSource = new CancellationTokenSource();
    private int executionCount = 0;
    private string _authToken = "";

    public ClearMLAuthenticationService(
        HttpClient httpClient,
        IOptionsMonitor<ClearMLNmtEngineOptions> options,
        ILogger<ClearMLAuthenticationService> logger
    )
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public string GetAuthToken()
    {
        return _authToken;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ClearML Authentication Token Refresh service running.");

        InitRefreshAuthorizationTimer();

        return Task.CompletedTask;
    }

    private async void InitRefreshAuthorizationTimer()
    {
        var token = _ctSource.Token;

        // call once during init so that it is populated
        await AuthorizeAsync(expiration_sec: _refreshPeriod * 2 + 10, cancellationToken: token);

        // Let the auth time be twice as long, just in case it fails once.
        _periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(_refreshPeriod));
        try
        {
            while (await _periodicTimer.WaitForNextTickAsync(cancellationToken: token))
                await AuthorizeAsync(expiration_sec: _refreshPeriod * 2 + 10, cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ClearMl Authorization Refresh Token successfully cancelled");
        }
    }

    private async Task AuthorizeAsync(int expiration_sec, CancellationToken cancellationToken)
    {
        var count = Interlocked.Increment(ref executionCount);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.CurrentValue.ApiServer}/auth.login?expiration_sec={_refreshPeriod * 2 + 10}"
        )
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        var authenticationString = $"{_options.CurrentValue.AccessKey}:{_options.CurrentValue.SecretKey}";
        var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
        request.Headers.Add("Authorization", $"Basic {base64EncodedAuthenticationString}");
        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            string result = await response.Content.ReadAsStringAsync();
            _authToken = (string)((JsonObject?)JsonNode.Parse(result))?["data"]?["token"]!;
            _logger.LogInformation("ClearML Authentication Token Refresh Successful. Count: {Count}", count);
        }
        else
        {
            _logger.LogWarning(
                $"ClearML Authentication Token Refresh Unsuccessful. Count: {count}\nError response {response}"
            );
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ClearML Authentication Token Refresh is stopping.");

        _ctSource.Cancel();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _periodicTimer?.Dispose();
    }
}
