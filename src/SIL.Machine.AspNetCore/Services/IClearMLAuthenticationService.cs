namespace SIL.Machine.AspNetCore.Services;

public interface IClearMLAuthenticationService : IHostedService, IDisposable
{
    public string GetAuthToken();
}
