namespace SIL.Machine.AspNetCore.Services;

public interface IClearMLAuthenticationService : IHostedService
{
    public Task<string> GetAuthTokenAsync(CancellationToken cancellationToken = default);
}
