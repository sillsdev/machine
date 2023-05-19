namespace SIL.Machine.AspNetCore.Services;

public interface IClearMLAuthenticationService : IHostedService
{
    public string GetAuthToken();
}
