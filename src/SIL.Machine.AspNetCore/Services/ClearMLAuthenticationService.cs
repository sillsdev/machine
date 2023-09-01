﻿namespace SIL.Machine.AspNetCore.Services;

public class ClearMLAuthenticationService : BackgroundService, IClearMLAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ClearMLNmtEngineOptions> _options;
    private readonly ILogger<ClearMLAuthenticationService> _logger;

    // technically, the token should be good for 30 days, but let's refresh each hour
    // to know well ahead of time if something is wrong.
    private const int RefreshPeriod = 3600;
    private int _consecutiveFailureCount = 0;
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

    public async Task<string> GetAuthToken(CancellationToken cancellationToken = default)
    {
        if (_authToken is "")
        {
            //Should only happen once, so no different in cost than previous solution
            _logger.LogInformation("Token was empty; refreshing");
            await AuthorizeAsync(cancellationToken);
        }
        return _authToken;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ClearML Authentication Token Refresh service running - and has initial token.");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(RefreshPeriod), stoppingToken);
                await AuthorizeAsync(stoppingToken);
            }
        }
        catch (TaskCanceledException) { }
        _logger.LogInformation("ClearML Authentication Token Refresh service successfully stopped");
    }

    private async Task AuthorizeAsync(CancellationToken cancellationToken)
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
        _authToken = (string)((JsonObject?)JsonNode.Parse(result))?["data"]?["token"]!;
        _logger.LogInformation("ClearML Authentication Token Refresh Successful.");
    }
}
