namespace Microsoft.Extensions.DependencyInjection;

public static class IMachineBuilderExtensions
{
    public static IMachineBuilder AddServiceOptions(
        this IMachineBuilder builder,
        Action<ServiceOptions> configureOptions
    )
    {
        builder.Services.Configure(configureOptions);
        return builder;
    }

    public static IMachineBuilder AddServiceOptions(this IMachineBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<ServiceOptions>(config);
        return builder;
    }

    public static IMachineBuilder AddTranslationEngineOptions(
        this IMachineBuilder builder,
        Action<TranslationEngineOptions> configureOptions
    )
    {
        builder.Services.Configure(configureOptions);
        return builder;
    }

    public static IMachineBuilder AddTranslationEngineOptions(this IMachineBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<TranslationEngineOptions>(config);
        return builder;
    }

    public static IMachineBuilder AddCorpusOptions(this IMachineBuilder builder, Action<CorpusOptions> configureOptions)
    {
        builder.Services.Configure(configureOptions);
        return builder;
    }

    public static IMachineBuilder AddCorpusOptions(this IMachineBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<CorpusOptions>(config);
        return builder;
    }

    public static IMachineBuilder AddApiOptions(this IMachineBuilder builder, Action<ApiOptions> configureOptions)
    {
        builder.Services.Configure(configureOptions);
        return builder;
    }

    public static IMachineBuilder AddApiOptions(this IMachineBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<ApiOptions>(config);
        return builder;
    }

    public static IMachineBuilder AddClearMLOptions(
        this IMachineBuilder builder,
        Action<ClearMLOptions> configureOptions
    )
    {
        builder.Services.Configure(configureOptions);
        return builder;
    }

    public static IMachineBuilder AddClearMLOptions(this IMachineBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<ClearMLOptions>(config);
        return builder;
    }

    public static IMachineBuilder AddSharedFileOptions(
        this IMachineBuilder builder,
        Action<SharedFileOptions> configureOptions
    )
    {
        builder.Services.Configure(configureOptions);
        return builder;
    }

    public static IMachineBuilder AddSharedFileOptions(this IMachineBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<SharedFileOptions>(config);
        return builder;
    }

    public static IMachineBuilder AddOptions(this IMachineBuilder builder, IConfiguration? config = null)
    {
        if (config is null)
        {
            builder.AddApiOptions(o => { });
            builder.AddCorpusOptions(o => { });
            builder.AddServiceOptions(o => { });
            builder.AddSharedFileOptions(o => { });
            builder.AddTranslationEngineOptions(o => { });
            builder.AddClearMLOptions(o => { });
        }
        else
        {
            builder.AddApiOptions(config.GetSection(ApiOptions.Key));
            builder.AddCorpusOptions(config.GetSection(CorpusOptions.Key));
            builder.AddServiceOptions(config.GetSection(ServiceOptions.Key));
            builder.AddSharedFileOptions(config.GetSection(SharedFileOptions.Key));
            builder.AddTranslationEngineOptions(config.GetSection(TranslationEngineOptions.Key));
            builder.AddClearMLOptions(config.GetSection(ClearMLOptions.Key));
        }
        return builder;
    }

    public static IMachineBuilder AddThotSmtModel(this IMachineBuilder builder)
    {
        builder.Services.AddSingleton<ISmtModelFactory, ThotSmtModelFactory>();
        return builder;
    }

    public static IMachineBuilder AddThotSmtModel(
        this IMachineBuilder builder,
        Action<ThotSmtModelOptions> configureOptions
    )
    {
        builder.Services.Configure(configureOptions);
        return builder.AddThotSmtModel();
    }

    public static IMachineBuilder AddThotSmtModel(this IMachineBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<ThotSmtModelOptions>(config);
        return builder.AddThotSmtModel();
    }

    public static IMachineBuilder AddTransferEngine(this IMachineBuilder builder)
    {
        builder.Services.AddSingleton<ITransferEngineFactory, TransferEngineFactory>();
        return builder;
    }

    public static IMachineBuilder AddUnigramTruecaser(this IMachineBuilder builder)
    {
        builder.Services.AddSingleton<ITruecaserFactory, UnigramTruecaserFactory>();
        return builder;
    }

    public static IMachineBuilder AddMongoBackgroundJobClient(this IMachineBuilder builder, string connectionString)
    {
        builder.Services.AddHangfire(
            c =>
                c.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseMongoStorage(
                        connectionString,
                        new MongoStorageOptions
                        {
                            MigrationOptions = new MongoMigrationOptions
                            {
                                MigrationStrategy = new MigrateMongoMigrationStrategy(),
                                BackupStrategy = new CollectionMongoBackupStrategy()
                            },
                            CheckConnection = true,
                            CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection
                        }
                    )
        );
        return builder;
    }

    public static IMachineBuilder AddMemoryBackgroundJobClient(this IMachineBuilder builder)
    {
        builder.Services.AddHangfire(
            c =>
                c.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseMemoryStorage()
        );
        return builder;
    }

    public static IMachineBuilder AddBackgroundJobServer(this IMachineBuilder builder, string[]? queues = null)
    {
        builder.Services.AddHangfireServer(
            o =>
            {
                if (queues is not null)
                    o.Queues = queues;
            }
        );
        builder.AddThotSmtModel().AddTransferEngine().AddUnigramTruecaser();
        return builder;
    }

    public static IMachineBuilder AddMemoryDataAccess(this IMachineBuilder builder)
    {
        builder.Services.AddSingleton<IRepository<TranslationEngine>, MemoryRepository<TranslationEngine>>();
        builder.Services.AddSingleton<IRepository<Build>, MemoryRepository<Build>>();
        builder.Services.AddSingleton<IRepository<Corpus>, MemoryRepository<Corpus>>();
        builder.Services.AddSingleton<IRepository<RWLock>, MemoryRepository<RWLock>>();
        builder.Services.AddSingleton<IRepository<TrainSegmentPair>, MemoryRepository<TrainSegmentPair>>();
        builder.Services.AddSingleton<IRepository<Webhook>, MemoryRepository<Webhook>>();
        builder.Services.AddSingleton<IRepository<Pretranslation>, MemoryRepository<Pretranslation>>();

        return builder;
    }

    public static IMachineBuilder AddMongoDataAccess(this IMachineBuilder builder, string connectionString)
    {
        DataAccessClassMap.RegisterConventions(
            "SIL.Machine.WebApi.Models",
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
            init: async c =>
                await c.Indexes.CreateOrUpdateAsync(
                    new CreateIndexModel<TranslationEngine>(
                        Builders<TranslationEngine>.IndexKeys.Ascending(p => p.Owner)
                    )
                )
        );
        builder.Services.AddMongoRepository<Build>(
            "builds",
            init: async c =>
                await c.Indexes.CreateOrUpdateAsync(
                    new CreateIndexModel<Build>(Builders<Build>.IndexKeys.Ascending(b => b.ParentRef))
                ),
            isSubscribable: true
        );
        builder.Services.AddMongoRepository<Corpus>(
            "corpora",
            init: async c =>
                await c.Indexes.CreateOrUpdateAsync(
                    new CreateIndexModel<Corpus>(Builders<Corpus>.IndexKeys.Ascending(p => p.Owner))
                )
        );
        builder.Services.AddMongoRepository<RWLock>(
            "locks",
            init: async c =>
            {
                await c.Indexes.CreateOrUpdateAsync(
                    new CreateIndexModel<RWLock>(Builders<RWLock>.IndexKeys.Ascending("writerLock._id"))
                );
                await c.Indexes.CreateOrUpdateAsync(
                    new CreateIndexModel<RWLock>(Builders<RWLock>.IndexKeys.Ascending("readerLocks._id"))
                );
                await c.Indexes.CreateOrUpdateAsync(
                    new CreateIndexModel<RWLock>(Builders<RWLock>.IndexKeys.Ascending("writerQueue._id"))
                );
            },
            isSubscribable: true
        );
        builder.Services.AddMongoRepository<TrainSegmentPair>(
            "train_segment_pairs",
            init: async c =>
                await c.Indexes.CreateOrUpdateAsync(
                    new CreateIndexModel<TrainSegmentPair>(
                        Builders<TrainSegmentPair>.IndexKeys.Ascending(p => p.TranslationEngineRef)
                    )
                )
        );
        builder.Services.AddMongoRepository<Webhook>(
            "hooks",
            init: async c =>
            {
                await c.Indexes.CreateOrUpdateAsync(
                    new CreateIndexModel<Webhook>(Builders<Webhook>.IndexKeys.Ascending(h => h.Owner))
                );
                await c.Indexes.CreateOrUpdateAsync(
                    new CreateIndexModel<Webhook>(Builders<Webhook>.IndexKeys.Ascending(h => h.Events))
                );
            }
        );
        builder.Services.AddMongoRepository<Pretranslation>(
            "pretranslations",
            init: async c =>
            {
                await c.Indexes.CreateOrUpdateAsync(
                    new CreateIndexModel<Pretranslation>(
                        Builders<Pretranslation>.IndexKeys.Ascending(pt => pt.TranslationEngineRef)
                    )
                );
                await c.Indexes.CreateOrUpdateAsync(
                    new CreateIndexModel<Pretranslation>(
                        Builders<Pretranslation>.IndexKeys.Ascending(pt => pt.CorpusRef)
                    )
                );
                await c.Indexes.CreateOrUpdateAsync(
                    new CreateIndexModel<Pretranslation>(Builders<Pretranslation>.IndexKeys.Ascending(pt => pt.TextId))
                );
            }
        );

        return builder;
    }

    public static IMachineBuilder AddTranslationEngineServer(this IMachineBuilder builder)
    {
        builder.Services.AddCodeFirstGrpc(
            o =>
            {
                o.Interceptors.Add<UnimplementedInterceptor>();
            }
        );
        builder.Services.AddSingleton<ITranslationEngineRuntimeService, TranslationEngineRuntimeService>();
        builder.Services.AddSingleton<ITranslationEngineRuntimeFactory, SmtTransferEngineRuntime.Factory>();
        builder.Services.AddSingleton<ITranslationEngineRuntimeFactory, ClearMLNmtEngineRuntime.Factory>();
        builder.AddThotSmtModel().AddTransferEngine().AddUnigramTruecaser();
        return builder;
    }

    public static IMachineBuilder AddTranslationEngineClient(this IMachineBuilder builder, string connectionString)
    {
        builder.Services.AddSingleton<ITranslationEngineService, TranslationEngineService>();
        builder.Services.AddCodeFirstGrpcClient<IGrpcTranslationEngineService>(
            o =>
            {
                o.Address = new Uri(connectionString);
            }
        );
        builder.Services.AddSingleton<ITranslationEngineRuntimeService, RemoteTranslationEngineRuntimeService>();
        return builder;
    }

    public static IMachineBuilder AddTranslationEngineService(this IMachineBuilder builder)
    {
        builder.Services.AddSingleton<ITranslationEngineService, TranslationEngineService>();
        builder.Services.AddSingleton<ITranslationEngineRuntimeService, TranslationEngineRuntimeService>();
        builder.Services.AddSingleton<ITranslationEngineRuntimeFactory, SmtTransferEngineRuntime.Factory>();
        builder.Services.AddSingleton<ITranslationEngineRuntimeFactory, ClearMLNmtEngineRuntime.Factory>();
        builder.AddThotSmtModel().AddTransferEngine().AddUnigramTruecaser();
        return builder;
    }

    private static void AddMongoRepository<T>(
        this IServiceCollection services,
        string collection,
        Action<BsonClassMap<T>>? mapSetup = null,
        Func<IMongoCollection<T>, Task>? init = null,
        bool isSubscribable = false
    ) where T : IEntity
    {
        DataAccessClassMap.RegisterClass<T>(cm => mapSetup?.Invoke(cm));
        services.AddSingleton<IRepository<T>>(sp => CreateMongoRepository(sp, collection, init, isSubscribable));
    }

    private static MongoRepository<T> CreateMongoRepository<T>(
        IServiceProvider sp,
        string collection,
        Func<IMongoCollection<T>, Task>? init,
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
