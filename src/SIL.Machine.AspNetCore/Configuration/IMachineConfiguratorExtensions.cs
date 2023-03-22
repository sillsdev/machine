using Serval.Translation.V1;

namespace Microsoft.Extensions.DependencyInjection;

public static class IMachineConfiguratorExtensions
{
    public static IMachineConfigurator AddServiceOptions(
        this IMachineConfigurator configurator,
        Action<ServiceOptions> configureOptions
    )
    {
        configurator.Services.Configure(configureOptions);
        return configurator;
    }

    public static IMachineConfigurator AddServiceOptions(this IMachineConfigurator configurator, IConfiguration config)
    {
        configurator.Services.Configure<ServiceOptions>(config);
        return configurator;
    }

    public static IMachineConfigurator AddSmtTransferEngineOptions(
        this IMachineConfigurator configurator,
        Action<SmtTransferEngineOptions> configureOptions
    )
    {
        configurator.Services.Configure(configureOptions);
        return configurator;
    }

    public static IMachineConfigurator AddSmtTransferEngineOptions(
        this IMachineConfigurator configurator,
        IConfiguration config
    )
    {
        configurator.Services.Configure<SmtTransferEngineOptions>(config);
        return configurator;
    }

    public static IMachineConfigurator AddClearMLNmtEngineOptions(
        this IMachineConfigurator configurator,
        Action<ClearMLNmtEngineOptions> configureOptions
    )
    {
        configurator.Services.Configure(configureOptions);
        return configurator;
    }

    public static IMachineConfigurator AddClearMLNmtEngineOptions(
        this IMachineConfigurator configurator,
        IConfiguration config
    )
    {
        configurator.Services.Configure<ClearMLNmtEngineOptions>(config);
        return configurator;
    }

    public static IMachineConfigurator AddSharedFileOptions(
        this IMachineConfigurator configurator,
        Action<SharedFileOptions> configureOptions
    )
    {
        configurator.Services.Configure(configureOptions);
        return configurator;
    }

    public static IMachineConfigurator AddSharedFileOptions(
        this IMachineConfigurator configurator,
        IConfiguration config
    )
    {
        configurator.Services.Configure<SharedFileOptions>(config);
        return configurator;
    }

    public static IMachineConfigurator AddThotSmtModel(this IMachineConfigurator configurator)
    {
        configurator.Services.AddSingleton<ISmtModelFactory, ThotSmtModelFactory>();
        return configurator;
    }

    public static IMachineConfigurator AddThotSmtModel(
        this IMachineConfigurator configurator,
        Action<ThotSmtModelOptions> configureOptions
    )
    {
        configurator.Services.Configure(configureOptions);
        return configurator.AddThotSmtModel();
    }

    public static IMachineConfigurator AddThotSmtModel(this IMachineConfigurator configurator, IConfiguration config)
    {
        configurator.Services.Configure<ThotSmtModelOptions>(config);
        return configurator.AddThotSmtModel();
    }

    public static IMachineConfigurator AddTransferEngine(this IMachineConfigurator configurator)
    {
        configurator.Services.AddSingleton<ITransferEngineFactory, TransferEngineFactory>();
        return configurator;
    }

    public static IMachineConfigurator AddUnigramTruecaser(this IMachineConfigurator configurator)
    {
        configurator.Services.AddSingleton<ITruecaserFactory, UnigramTruecaserFactory>();
        return configurator;
    }

    public static IMachineConfigurator AddClearMLService(this IMachineConfigurator configurator)
    {
        configurator.Services.AddSingleton<IClearMLService, ClearMLService>();
        return configurator;
    }

    public static IMachineConfigurator AddMongoBackgroundJobClient(
        this IMachineConfigurator configurator,
        string? connectionString = null
    )
    {
        configurator.Services.AddHangfire(
            c =>
                c.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseMongoStorage(
                        connectionString ?? configurator.Configuration.GetConnectionString("Hangfire"),
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
        return configurator;
    }

    public static IMachineConfigurator AddMemoryBackgroundJobClient(this IMachineConfigurator builder)
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

    public static IMachineConfigurator AddBackgroundJobServer(
        this IMachineConfigurator configurator,
        IEnumerable<TranslationEngineType>? engineTypes = null
    )
    {
        engineTypes ??=
            configurator.Configuration.GetValue<TranslationEngineType[]?>("TranslationEngines")
            ?? new[] { TranslationEngineType.SmtTransfer, TranslationEngineType.Nmt };
        var queues = new List<string>();
        foreach (TranslationEngineType engineType in engineTypes.Distinct())
        {
            switch (engineType)
            {
                case TranslationEngineType.SmtTransfer:
                    configurator.AddThotSmtModel().AddTransferEngine().AddUnigramTruecaser();
                    queues.Add("smt_transfer");
                    break;
                case TranslationEngineType.Nmt:
                    configurator.AddClearMLService();
                    queues.Add("nmt");
                    break;
            }
        }

        configurator.Services.AddHangfireServer(o =>
        {
            o.Queues = queues.ToArray();
        });
        return configurator;
    }

    public static IMachineConfigurator AddMemoryDataAccess(this IMachineConfigurator configurator)
    {
        configurator.Services.AddMemoryDataAccess(o =>
        {
            o.AddRepository<TranslationEngine>();
            o.AddRepository<RWLock>();
            o.AddRepository<TrainSegmentPair>();
        });

        return configurator;
    }

    public static IMachineConfigurator AddMongoDataAccess(
        this IMachineConfigurator configurator,
        string? connectionString = null
    )
    {
        configurator.Services.AddMongoDataAccess(
            connectionString ?? configurator.Configuration.GetConnectionString("Mongo"),
            "SIL.Machine.AspNetCore.Models",
            o =>
            {
                o.AddRepository<TranslationEngine>(
                    "translation_engines",
                    init: c =>
                        c.Indexes.CreateOrUpdate(
                            new CreateIndexModel<TranslationEngine>(
                                Builders<TranslationEngine>.IndexKeys.Ascending(p => p.EngineId)
                            )
                        )
                );
                o.AddRepository<RWLock>(
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
                    }
                );
                o.AddRepository<TrainSegmentPair>(
                    "train_segment_pairs",
                    init: c =>
                        c.Indexes.CreateOrUpdate(
                            new CreateIndexModel<TrainSegmentPair>(
                                Builders<TrainSegmentPair>.IndexKeys.Ascending(p => p.TranslationEngineRef)
                            )
                        )
                );
            }
        );

        return configurator;
    }

    public static IMachineConfigurator AddServalPlatformService(
        this IMachineConfigurator configurator,
        string? connectionString = null
    )
    {
        configurator.Services.AddScoped<IPlatformService, ServalPlatformService>();
        configurator.Services
            .AddGrpcClient<TranslationPlatformApi.TranslationPlatformApiClient>(o =>
            {
                o.Address = new Uri(connectionString ?? configurator.Configuration.GetConnectionString("Serval"));
            })
            .ConfigureChannel(o =>
            {
                o.MaxRetryAttempts = null;
                o.ServiceConfig = new ServiceConfig
                {
                    MethodConfigs =
                    {
                        new MethodConfig
                        {
                            Names = { MethodName.Default },
                            RetryPolicy = new RetryPolicy
                            {
                                MaxAttempts = 10,
                                InitialBackoff = TimeSpan.FromSeconds(1),
                                MaxBackoff = TimeSpan.FromSeconds(5),
                                BackoffMultiplier = 1.5,
                                RetryableStatusCodes = { StatusCode.Unavailable }
                            }
                        },
                        new MethodConfig
                        {
                            Names =
                            {
                                new MethodName
                                {
                                    Service = "serval.translation.v1.TranslationPlatformApi",
                                    Method = "UpdateBuildStatus"
                                }
                            }
                        },
                    }
                };
            });

        return configurator;
    }

    public static IMachineConfigurator AddServalTranslationEngineService(
        this IMachineConfigurator configurator,
        string? connectionString = null,
        IEnumerable<TranslationEngineType>? engineTypes = null
    )
    {
        configurator.Services.AddGrpc(options => options.Interceptors.Add<UnimplementedInterceptor>());
        configurator.AddServalPlatformService(
            connectionString ?? configurator.Configuration.GetConnectionString("Serval")
        );
        engineTypes ??=
            configurator.Configuration.GetValue<TranslationEngineType[]?>("TranslationEngines")
            ?? new[] { TranslationEngineType.SmtTransfer, TranslationEngineType.Nmt };
        foreach (TranslationEngineType engineType in engineTypes.Distinct())
        {
            switch (engineType)
            {
                case TranslationEngineType.SmtTransfer:
                    configurator.Services.AddSingleton<SmtTransferEngineStateService>();
                    configurator.Services.AddHostedService<SmtTransferEngineCommitService>();
                    configurator.AddThotSmtModel().AddTransferEngine().AddUnigramTruecaser();
                    configurator.Services.AddScoped<ITranslationEngineService, SmtTransferEngineService>();
                    break;
                case TranslationEngineType.Nmt:
                    configurator.AddClearMLService();
                    configurator.Services.AddScoped<ITranslationEngineService, ClearMLNmtEngineService>();
                    break;
            }
        }

        return configurator;
    }
}
