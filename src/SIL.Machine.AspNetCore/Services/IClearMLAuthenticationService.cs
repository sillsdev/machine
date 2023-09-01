namespace SIL.Machine.AspNetCore.Services;

public interface IClearMLAuthenticationService : IHostedService
{
    public Task<string> GetAuthToken(CancellationToken cancellationToken = default);
}
