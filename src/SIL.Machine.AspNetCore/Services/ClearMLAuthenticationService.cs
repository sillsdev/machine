namespace SIL.Machine.AspNetCore.Services;

public class ClearMLAuthenticationService : RecurrentTask, IClearMLAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ClearMLOptions> _options;
    private readonly ILogger<ClearMLAuthenticationService> _logger;
    private readonly AsyncLock _lock = new();

    // technically, the token should be good for 30 days, but let's refresh each hour
    // to know well ahead of time if something is wrong.
    private static readonly TimeSpan RefreshPeriod = TimeSpan.FromSeconds(3600);
    private string _authToken = "";

    public ClearMLAuthenticationService(
        IServiceProvider services,
        HttpClient httpClient,
        IOptionsMonitor<ClearMLOptions> options,
        ILogger<ClearMLAuthenticationService> logger
    )
        : base("ClearML authentication service", services, RefreshPeriod, logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<string> GetAuthTokenAsync(CancellationToken cancellationToken = default)
    {
        using (await _lock.LockAsync(cancellationToken))
        {
            if (_authToken is "")
            {
                //Should only happen once, so no different in cost than previous solution
                _logger.LogInformation("Token was empty; refreshing");
                await AuthorizeAsync(cancellationToken);
            }
        }
        return _authToken;
    }

    protected override async Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            using (await _lock.LockAsync(cancellationToken))
                await AuthorizeAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while refreshing ClearML authentication token.");
        }
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
