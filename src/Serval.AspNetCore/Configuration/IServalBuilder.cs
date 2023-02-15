namespace Microsoft.Extensions.DependencyInjection;

public interface IServalBuilder
{
    IServiceCollection Services { get; }
    IConfiguration? Configuration { get; }
}
