namespace Microsoft.AspNetCore.Builder;

public static class IEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapServalTranslationService(this IEndpointRouteBuilder builder)
    {
        builder.MapGrpcService<ServalTranslationService>();

        return builder;
    }
}
