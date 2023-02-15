namespace Microsoft.AspNetCore.Builder;

public static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder UseServal(this IApplicationBuilder app)
    {
        app.UseRepository<TranslationEngine>();
        app.UseRepository<Build>();
        app.UseRepository<Corpus>();
        app.UseRepository<Webhook>();
        app.UseRepository<Pretranslation>();

        return app;
    }
}
