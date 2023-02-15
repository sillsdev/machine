namespace Microsoft.AspNetCore.Builder;

public static class IEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapServalPlatformServices(this IEndpointRouteBuilder builder)
    {
        builder.MapGrpcService<ServalBuildStatusService>();
        builder.MapGrpcService<ServalMetadataService>();
        builder.MapGrpcService<ServalResultService>();

        return builder;
    }
}
