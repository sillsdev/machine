namespace SIL.Machine.WebApi.Configuration;

public class ApiOptions
{
    public TimeSpan LongPollTimeout { get; set; } = TimeSpan.FromSeconds(40);
}
