namespace Microsoft.AspNetCore.Builder;

public static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRepository<T>(this IApplicationBuilder app) where T : IEntity
    {
        var repo = app.ApplicationServices.GetService<IRepository<T>>();
        if (repo is null)
            throw new InvalidOperationException("The repository has not been added to the service provider.");
        repo.Init();
        return app;
    }
}
