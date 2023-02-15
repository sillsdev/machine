using Serval.Engine.Translation.V1;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServalBuilderExtensions
{
    public static IServalBuilder AddCorpusOptions(this IServalBuilder builder, Action<CorpusOptions> configureOptions)
    {
        builder.Services.Configure(configureOptions);
        return builder;
    }

    public static IServalBuilder AddCorpusOptions(this IServalBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<CorpusOptions>(config);
        return builder;
    }

    public static IServalBuilder AddApiOptions(this IServalBuilder builder, Action<ApiOptions> configureOptions)
    {
        builder.Services.Configure(configureOptions);
        return builder;
    }

    public static IServalBuilder AddApiOptions(this IServalBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<ApiOptions>(config);
        return builder;
    }

    public static IServalBuilder AddEngineOptions(this IServalBuilder builder, Action<EngineOptions> configureOptions)
    {
        builder.Services.Configure(configureOptions);
        return builder;
    }

    public static IServalBuilder AddEngineOptions(this IServalBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<EngineOptions>(config);
        return builder;
    }

    public static IServalBuilder AddOptions(this IServalBuilder builder)
    {
        if (builder.Configuration is null)
        {
            builder.AddApiOptions(o => { });
            builder.AddCorpusOptions(o => { });
        }
        else
        {
            builder.AddApiOptions(builder.Configuration.GetSection(ApiOptions.Key));
            builder.AddCorpusOptions(builder.Configuration.GetSection(CorpusOptions.Key));
        }
        return builder;
    }

    public static IServalBuilder AddMemoryDataAccess(this IServalBuilder builder)
    {
        builder.Services.AddMemoryRepository<TranslationEngine>();
        builder.Services.AddMemoryRepository<Build>();
        builder.Services.AddMemoryRepository<Corpus>();
        builder.Services.AddMemoryRepository<Webhook>();
        builder.Services.AddMemoryRepository<Pretranslation>();

        return builder;
    }

    public static IServalBuilder AddMongoDataAccess(this IServalBuilder builder, string connectionString)
    {
        DataAccessClassMap.RegisterConventions(
            "Serval.AspNetCore.Models",
            new StringIdStoredAsObjectIdConvention(),
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(BsonType.String),
            new IgnoreIfNullConvention(true),
            new ObjectRefConvention()
        );

        var mongoUrl = new MongoUrl(connectionString);
        builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoUrl));
        builder.Services.AddSingleton(sp => sp.GetService<IMongoClient>()!.GetDatabase(mongoUrl.DatabaseName));

        builder.Services.AddMongoRepository<TranslationEngine>(
            "translation_engines",
            init: c =>
                c.Indexes.CreateOrUpdate(
                    new CreateIndexModel<TranslationEngine>(
                        Builders<TranslationEngine>.IndexKeys.Ascending(p => p.Owner)
                    )
                )
        );
        builder.Services.AddMongoRepository<Build>(
            "builds",
            init: c =>
                c.Indexes.CreateOrUpdate(
                    new CreateIndexModel<Build>(Builders<Build>.IndexKeys.Ascending(b => b.ParentRef))
                ),
            isSubscribable: true
        );
        builder.Services.AddMongoRepository<Corpus>(
            "corpora",
            init: c =>
                c.Indexes.CreateOrUpdate(
                    new CreateIndexModel<Corpus>(Builders<Corpus>.IndexKeys.Ascending(p => p.Owner))
                )
        );
        builder.Services.AddMongoRepository<Webhook>(
            "hooks",
            init: c =>
            {
                c.Indexes.CreateOrUpdate(
                    new CreateIndexModel<Webhook>(Builders<Webhook>.IndexKeys.Ascending(h => h.Owner))
                );
                c.Indexes.CreateOrUpdate(
                    new CreateIndexModel<Webhook>(Builders<Webhook>.IndexKeys.Ascending(h => h.Events))
                );
            }
        );
        builder.Services.AddMongoRepository<Pretranslation>(
            "pretranslations",
            init: c =>
            {
                c.Indexes.CreateOrUpdate(
                    new CreateIndexModel<Pretranslation>(
                        Builders<Pretranslation>.IndexKeys.Ascending(pt => pt.TranslationEngineRef)
                    )
                );
                c.Indexes.CreateOrUpdate(
                    new CreateIndexModel<Pretranslation>(
                        Builders<Pretranslation>.IndexKeys.Ascending(pt => pt.CorpusRef)
                    )
                );
                c.Indexes.CreateOrUpdate(
                    new CreateIndexModel<Pretranslation>(Builders<Pretranslation>.IndexKeys.Ascending(pt => pt.TextId))
                );
            }
        );

        return builder;
    }

    public static IServalBuilder AddEngineServices(
        this IServalBuilder builder,
        Action<EngineOptions>? configureOptions = null
    )
    {
        var engineOptions = new EngineOptions();
        if (builder.Configuration is not null)
            builder.Configuration.GetSection(EngineOptions.Key).Bind(engineOptions);
        if (configureOptions is not null)
            configureOptions(engineOptions);
        return builder.AddTranslationEngineService(engineOptions.Translation);
    }

    public static IServalBuilder AddTranslationEngineService(this IServalBuilder builder, List<Engine> engines)
    {
        builder.Services.AddSingleton<ITranslationEngineService, TranslationEngineService>();
        foreach (Engine engine in engines)
            builder.Services.AddGrpcClient<TranslationService.TranslationServiceClient>(
                engine.Type,
                o => o.Address = new Uri(engine.Address)
            );
        return builder;
    }
}
