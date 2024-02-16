namespace SIL.Machine.AspNetCore.Services;

public class ClearMLAuthenticationService(
    IServiceProvider services,
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<ClearMLOptions> options,
    ILogger<ClearMLAuthenticationService> logger
) : RecurrentTask("ClearML authentication service", services, RefreshPeriod, logger), IClearMLAuthenticationService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("ClearML");
    private readonly IOptionsMonitor<ClearMLOptions> _options = options;
    private readonly ILogger<ClearMLAuthenticationService> _logger = logger;
    private readonly AsyncLock _lock = new();

    // technically, the token should be good for 30 days, but let's refresh each hour
    // to know well ahead of time if something is wrong.
    private static readonly TimeSpan RefreshPeriod = TimeSpan.FromSeconds(3600);
    private string _authToken = "";

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
            if (_authToken is "")
            {
                _logger.LogError(e, "Error occurred while acquiring ClearML authentication token for the first time.");
                // The ClearML token never was set.  We can't continue without it.
                throw;
            }
            else
            {
                _logger.LogError(e, "Error occurred while refreshing ClearML authentication token.");
            }
        }
    }

    private async Task AuthorizeAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "auth.login")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        var authenticationString = $"{_options.CurrentValue.AccessKey}:{_options.CurrentValue.SecretKey}";
        var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
        request.Headers.Add("Authorization", $"Basic {base64EncodedAuthenticationString}");
        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string result = await response.Content.ReadAsStringAsync(cancellationToken);
        string? refreshedToken = (string?)((JsonObject?)JsonNode.Parse(result))?["data"]?["token"];
        if (refreshedToken is null || refreshedToken is "")
            throw new Exception($"ClearML authentication failed - {response.StatusCode}: {response.ReasonPhrase}");
        _authToken = refreshedToken;
        _logger.LogInformation("ClearML Authentication Token Refresh Successful.");
    }
}
