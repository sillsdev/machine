namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddMemoryRepository<T>(this IServiceCollection services) where T : IEntity
    {
        services.AddSingleton<IRepository<T>, MemoryRepository<T>>();
        return services;
    }

    public static IServiceCollection AddMongoRepository<T>(
        this IServiceCollection services,
        string collection,
        Action<BsonClassMap<T>>? mapSetup = null,
        Action<IMongoCollection<T>>? init = null,
        bool isSubscribable = false
    ) where T : IEntity
    {
        DataAccessClassMap.RegisterClass<T>(cm => mapSetup?.Invoke(cm));
        services.AddSingleton<IRepository<T>>(sp => CreateMongoRepository(sp, collection, init, isSubscribable));
        return services;
    }

    private static MongoRepository<T> CreateMongoRepository<T>(
        IServiceProvider sp,
        string collection,
        Action<IMongoCollection<T>>? init,
        bool isSubscribable
    ) where T : IEntity
    {
        return new MongoRepository<T>(
            sp.GetService<IMongoDatabase>()!.GetCollection<T>(collection),
            init,
            isSubscribable
        );
    }
}
