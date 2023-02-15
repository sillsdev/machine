using Serval.Platform.BuildStatus.V1;
using Serval.Platform.Metadata.V1;
using Serval.Platform.Result.V1;

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

    public static IMachineBuilder AddOptions(this IMachineBuilder builder)
    {
        if (builder.Configuration is null)
        {
            builder.AddServiceOptions(o => { });
            builder.AddSharedFileOptions(o => { });
            builder.AddTranslationEngineOptions(o => { });
            builder.AddClearMLOptions(o => { });
        }
        else
        {
            builder.AddServiceOptions(builder.Configuration.GetSection(ServiceOptions.Key));
            builder.AddSharedFileOptions(builder.Configuration.GetSection(SharedFileOptions.Key));
            builder.AddTranslationEngineOptions(builder.Configuration.GetSection(TranslationEngineOptions.Key));
            builder.AddClearMLOptions(builder.Configuration.GetSection(ClearMLOptions.Key));
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
        builder.Services.AddMemoryRepository<TranslationEngine>();
        builder.Services.AddMemoryRepository<RWLock>();
        builder.Services.AddMemoryRepository<TrainSegmentPair>();

        return builder;
    }

    public static IMachineBuilder AddMongoDataAccess(this IMachineBuilder builder, string connectionString)
    {
        DataAccessClassMap.RegisterConventions(
            "SIL.Machine.AspNetCore.Models",
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
                        Builders<TranslationEngine>.IndexKeys.Ascending(p => p.EngineId)
                    )
                ),
            isSubscribable: true
        );
        builder.Services.AddMongoRepository<RWLock>(
            "locks",
            init: c =>
            {
                c.Indexes.CreateOrUpdate(
                    new CreateIndexModel<RWLock>(Builders<RWLock>.IndexKeys.Ascending("writerLock._id"))
                );
                c.Indexes.CreateOrUpdate(
                    new CreateIndexModel<RWLock>(Builders<RWLock>.IndexKeys.Ascending("readerLocks._id"))
                );
                c.Indexes.CreateOrUpdate(
                    new CreateIndexModel<RWLock>(Builders<RWLock>.IndexKeys.Ascending("writerQueue._id"))
                );
            },
            isSubscribable: true
        );
        builder.Services.AddMongoRepository<TrainSegmentPair>(
            "train_segment_pairs",
            init: c =>
                c.Indexes.CreateOrUpdate(
                    new CreateIndexModel<TrainSegmentPair>(
                        Builders<TrainSegmentPair>.IndexKeys.Ascending(p => p.TranslationEngineRef)
                    )
                )
        );

        return builder;
    }

    public static IMachineBuilder AddTranslationEngineRuntimeService(this IMachineBuilder builder)
    {
        builder.Services.AddSingleton<ITranslationEngineRuntimeService, TranslationEngineRuntimeService>();
        builder.Services.AddSingleton<ITranslationEngineRuntimeFactory, SmtTransferEngineRuntime.Factory>();
        builder.Services.AddSingleton<ITranslationEngineRuntimeFactory, ClearMLNmtEngineRuntime.Factory>();
        builder.AddThotSmtModel().AddTransferEngine().AddUnigramTruecaser();
        return builder;
    }

    public static IMachineBuilder AddServalOptions(this IMachineBuilder builder, Action<ServalOptions> configureOptions)
    {
        builder.Services.Configure(configureOptions);
        return builder;
    }

    public static IMachineBuilder AddServalOptions(this IMachineBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<ServalOptions>(config);
        return builder;
    }

    public static IMachineBuilder AddServalPlatformService(this IMachineBuilder builder, string servalConnectionString)
    {
        if (builder.Configuration is null)
            builder.AddServalOptions(o => { });
        else
            builder.AddServalOptions(builder.Configuration.GetSection(ServalOptions.Key));
        builder.Services.AddSingleton<IPlatformService, ServalPlatformService>();
        builder.Services.AddGrpcClient<MetadataService.MetadataServiceClient>(
            o => o.Address = new Uri(servalConnectionString)
        );
        builder.Services.AddGrpcClient<BuildStatusService.BuildStatusServiceClient>(
            o => o.Address = new Uri(servalConnectionString)
        );
        builder.Services.AddGrpcClient<ResultService.ResultServiceClient>(
            o => o.Address = new Uri(servalConnectionString)
        );

        return builder;
    }

    public static IMachineBuilder AddServalTranslationService(
        this IMachineBuilder builder,
        string servalConnectionString
    )
    {
        builder.Services.AddGrpc(options => options.Interceptors.Add<UnimplementedInterceptor>());

        builder.AddTranslationEngineRuntimeService();

        builder.AddServalPlatformService(servalConnectionString);

        return builder;
    }
}
