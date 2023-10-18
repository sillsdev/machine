using Serval.Translation.V1;

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

    public static IMachineBuilder AddSmtTransferEngineOptions(
        this IMachineBuilder builder,
        Action<SmtTransferEngineOptions> configureOptions
    )
    {
        builder.Services.Configure(configureOptions);
        return builder;
    }

    public static IMachineBuilder AddSmtTransferEngineOptions(this IMachineBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<SmtTransferEngineOptions>(config);
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

    public static IMachineBuilder AddThotSmtModel(this IMachineBuilder builder)
    {
        if (builder.Configuration is null)
            return builder.AddThotSmtModel(o => { });
        else
            return builder.AddThotSmtModel(builder.Configuration.GetSection(ThotSmtModelOptions.Key));
    }

    public static IMachineBuilder AddThotSmtModel(
        this IMachineBuilder builder,
        Action<ThotSmtModelOptions> configureOptions
    )
    {
        builder.Services.Configure(configureOptions);
        builder.Services.AddSingleton<ISmtModelFactory, ThotSmtModelFactory>();
        return builder;
    }

    public static IMachineBuilder AddThotSmtModel(this IMachineBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<ThotSmtModelOptions>(config);
        builder.Services.AddSingleton<ISmtModelFactory, ThotSmtModelFactory>();
        return builder;
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

    public static IMachineBuilder AddClearMLService(this IMachineBuilder builder, string? connectionString = null)
    {
        connectionString ??= builder.Configuration.GetConnectionString("ClearML");
        builder.Services
            .AddHttpClient("ClearML")
            .ConfigureHttpClient(httpClient => httpClient.BaseAddress = new Uri(connectionString))
            // Add retry policy; fail after approx. 2 + 4 + 8 = 14 seconds
            .AddTransientHttpErrorPolicy(
                b => b.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            );

        builder.Services.AddSingleton<IClearMLService, ClearMLService>();

        // workaround register satisfying the interface and as a hosted service.
        builder.Services.AddSingleton<IClearMLAuthenticationService, ClearMLAuthenticationService>();
        builder.Services.AddHostedService(p => p.GetRequiredService<IClearMLAuthenticationService>());

        builder.Services
            .AddHttpClient("ClearML-NoRetry")
            .ConfigureHttpClient(httpClient => httpClient.BaseAddress = new Uri(connectionString));

        builder.Services.AddHealthChecks().AddCheck<ClearMLHealthCheck>("ClearML Health Check");

        return builder;
    }

    private static IMachineBuilder AddClearMLBuildJobRunner(this IMachineBuilder builder)
    {
        builder.Services.AddScoped<IBuildJobRunner, ClearMLBuildJobRunner>();
        builder.Services.AddScoped<IClearMLBuildJobFactory, NmtClearMLBuildJobFactory>();
        builder.Services.AddSingleton<ClearMLMonitorService>();
        builder.Services.AddHostedService(p => p.GetRequiredService<ClearMLMonitorService>());

        return builder;
    }

    private static IMachineBuilder AddHangfireBuildJobRunner(this IMachineBuilder builder)
    {
        builder.Services.AddScoped<IBuildJobRunner, HangfireBuildJobRunner>();

        builder.Services.AddScoped<IHangfireBuildJobFactory, SmtTransferHangfireBuildJobFactory>();
        builder.Services.AddScoped<IHangfireBuildJobFactory, NmtHangfireBuildJobFactory>();

        return builder;
    }

    public static IMachineBuilder AddMongoHangfireJobClient(
        this IMachineBuilder builder,
        string? connectionString = null
    )
    {
        builder.Services.AddHangfire(
            c =>
                c.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseMongoStorage(
                        connectionString ?? builder.Configuration.GetConnectionString("Hangfire"),
                        new MongoStorageOptions
                        {
                            MigrationOptions = new MongoMigrationOptions
                            {
                                MigrationStrategy = new MigrateMongoMigrationStrategy(),
                                BackupStrategy = new CollectionMongoBackupStrategy()
                            },
                            CheckConnection = true,
                            CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection,
                        }
                    )
                    .UseFilter(new AutomaticRetryAttribute { Attempts = 0 })
        );
        builder.Services.AddHealthChecks().AddCheck<HangfireHealthCheck>(name: "Hangfire");
        return builder;
    }

    public static IMachineBuilder AddHangfireJobServer(
        this IMachineBuilder builder,
        IEnumerable<TranslationEngineType>? engineTypes = null
    )
    {
        engineTypes ??=
            builder.Configuration?.GetSection("TranslationEngines").Get<TranslationEngineType[]?>()
            ?? new[] { TranslationEngineType.SmtTransfer, TranslationEngineType.Nmt };
        var queues = new List<string>();
        foreach (TranslationEngineType engineType in engineTypes.Distinct())
        {
            switch (engineType)
            {
                case TranslationEngineType.SmtTransfer:
                    builder.AddThotSmtModel().AddTransferEngine().AddUnigramTruecaser();
                    queues.Add("smt_transfer");
                    break;
                case TranslationEngineType.Nmt:
                    queues.Add("nmt");
                    break;
            }
        }

        builder.Services.AddHangfireServer(o =>
        {
            o.Queues = queues.ToArray();
        });
        return builder;
    }

    public static IMachineBuilder AddMemoryDataAccess(this IMachineBuilder builder)
    {
        builder.Services.AddMemoryDataAccess(o =>
        {
            o.AddRepository<TranslationEngine>();
            o.AddRepository<RWLock>();
            o.AddRepository<TrainSegmentPair>();
        });

        return builder;
    }

    public static IMachineBuilder AddMongoDataAccess(this IMachineBuilder builder, string? connectionString = null)
    {
        connectionString ??= builder.Configuration.GetConnectionString("Mongo");
        builder.Services.AddMongoDataAccess(
            connectionString,
            "SIL.Machine.AspNetCore.Models",
            o =>
            {
                o.AddRepository<TranslationEngine>(
                    "translation_engines",
                    mapSetup: m => m.SetIgnoreExtraElements(true),
                    init: async c =>
                    {
                        await c.Indexes.CreateOrUpdateAsync(
                            new CreateIndexModel<TranslationEngine>(
                                Builders<TranslationEngine>.IndexKeys
                                    .Ascending(e => e.EngineId)
                                    .Ascending("currentBuild._id")
                            )
                        );
                        await c.Indexes.CreateOrUpdateAsync(
                            new CreateIndexModel<TranslationEngine>(
                                Builders<TranslationEngine>.IndexKeys.Ascending(e => e.CurrentBuild!.JobRunner)
                            )
                        );
                    }
                );
                o.AddRepository<RWLock>("locks");
                o.AddRepository<TrainSegmentPair>(
                    "train_segment_pairs",
                    init: c =>
                        c.Indexes.CreateOrUpdateAsync(
                            new CreateIndexModel<TrainSegmentPair>(
                                Builders<TrainSegmentPair>.IndexKeys.Ascending(p => p.TranslationEngineRef)
                            )
                        )
                );
            }
        );
        builder.Services.AddHealthChecks().AddMongoDb(connectionString, name: "Mongo");

        return builder;
    }

    public static IMachineBuilder AddServalPlatformService(
        this IMachineBuilder builder,
        string? connectionString = null
    )
    {
        builder.Services.AddScoped<IPlatformService, ServalPlatformService>();
        builder.Services
            .AddGrpcClient<TranslationPlatformApi.TranslationPlatformApiClient>(o =>
            {
                o.Address = new Uri(connectionString ?? builder.Configuration.GetConnectionString("Serval"));
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
                            RetryPolicy = new Grpc.Net.Client.Configuration.RetryPolicy
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

        return builder;
    }

    public static IMachineBuilder AddServalTranslationEngineService(
        this IMachineBuilder builder,
        string? connectionString = null,
        IEnumerable<TranslationEngineType>? engineTypes = null
    )
    {
        builder.Services.AddGrpc(options => options.Interceptors.Add<UnimplementedInterceptor>());
        builder.AddServalPlatformService(connectionString ?? builder.Configuration.GetConnectionString("Serval"));
        engineTypes ??=
            builder.Configuration?.GetSection("TranslationEngines").Get<TranslationEngineType[]?>()
            ?? new[] { TranslationEngineType.SmtTransfer, TranslationEngineType.Nmt };
        foreach (TranslationEngineType engineType in engineTypes.Distinct())
        {
            switch (engineType)
            {
                case TranslationEngineType.SmtTransfer:
                    builder.Services.AddSingleton<SmtTransferEngineStateService>();
                    builder.Services.AddHostedService<SmtTransferEngineCommitService>();
                    builder.AddThotSmtModel().AddTransferEngine().AddUnigramTruecaser();
                    builder.Services.AddScoped<ITranslationEngineService, SmtTransferEngineService>();
                    break;
                case TranslationEngineType.Nmt:
                    builder.Services.AddScoped<ITranslationEngineService, NmtEngineService>();
                    break;
            }
        }
        builder.Services.AddGrpcHealthChecks();

        return builder;
    }

    public static IMachineBuilder AddBuildJobService(
        this IMachineBuilder builder,
        Action<BuildJobOptions> configureOptions
    )
    {
        builder.Services.Configure(configureOptions);
        var options = new BuildJobOptions();
        configureOptions(options);
        return builder.AddBuildJobService(options);
    }

    public static IMachineBuilder AddBuildJobService(this IMachineBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<BuildJobOptions>(config);
        var options = config.Get<BuildJobOptions>();
        return builder.AddBuildJobService(options);
    }

    public static IMachineBuilder AddBuildJobService(this IMachineBuilder builder)
    {
        if (builder.Configuration is null)
            builder.AddBuildJobService(o => { });
        else
            builder.AddBuildJobService(builder.Configuration.GetSection(BuildJobOptions.Key));
        return builder;
    }

    private static IMachineBuilder AddBuildJobService(this IMachineBuilder builder, BuildJobOptions options)
    {
        builder.Services.AddScoped<IBuildJobService, BuildJobService>();

        foreach (BuildJobRunner runnerType in options.Runners.Values.Distinct())
        {
            switch (runnerType)
            {
                case BuildJobRunner.ClearML:
                    builder.AddClearMLBuildJobRunner();
                    break;
                case BuildJobRunner.Hangfire:
                    builder.AddHangfireBuildJobRunner();
                    break;
            }
        }
        return builder;
    }
}
