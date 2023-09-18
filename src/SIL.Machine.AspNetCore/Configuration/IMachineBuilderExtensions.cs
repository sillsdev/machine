using Microsoft.AspNetCore.Http;
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

    public static IMachineBuilder AddClearMLNmtEngineOptions(
        this IMachineBuilder builder,
        Action<ClearMLNmtEngineOptions> configureOptions
    )
    {
        builder.Services.Configure(configureOptions);
        return builder;
    }

    public static IMachineBuilder AddClearMLNmtEngineOptions(this IMachineBuilder builder, IConfiguration config)
    {
        builder.Services.Configure<ClearMLNmtEngineOptions>(config);
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

    public static IMachineBuilder AddClearMLService(this IMachineBuilder builder)
    {
        builder.Services.AddSingleton<IClearMLService, ClearMLService>();
        //Add retry policy; fail after approx. 2 + 4 + 8 = 14 seconds
        builder.Services
            .AddHttpClient<ClearMLService>()
            .AddTransientHttpErrorPolicy(
                b => b.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            );

        // workaround register satisfying the interface and as a hosted service.
        builder.Services.AddSingleton<IClearMLAuthenticationService, ClearMLAuthenticationService>();
        builder.Services.AddHostedService(p => p.GetRequiredService<IClearMLAuthenticationService>());
        //Add retry policy; fail after approx. 2 + 4 + 8 = 14 seconds
        builder.Services
            .AddHttpClient<IClearMLAuthenticationService, ClearMLAuthenticationService>()
            .AddTransientHttpErrorPolicy(
                b => b.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            );

        builder.Services.AddSingleton<S3HealthCheck>();
        builder.Services.AddHealthChecks().AddCheck<ClearMLHealthCheck>("ClearML Health Check");

        return builder;
    }

    public static IMachineBuilder AddMongoBackgroundJobClient(
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
        );
        builder.Services.AddHealthChecks().AddCheck<HangfireHealthCheck>(name: "Hangfire");
        return builder;
    }

    public static IMachineBuilder AddBackgroundJobServer(
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
                    builder.AddClearMLService();
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
                    init: c =>
                        c.Indexes.CreateOrUpdateAsync(
                            new CreateIndexModel<TranslationEngine>(
                                Builders<TranslationEngine>.IndexKeys.Ascending(p => p.EngineId)
                            )
                        )
                );
                o.AddRepository<RWLock>(
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
                    }
                );
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
                    builder.AddClearMLService();
                    builder.Services.AddScoped<ITranslationEngineService, ClearMLNmtEngineService>();
                    break;
            }
        }
        builder.Services.AddGrpcHealthChecks();

        return builder;
    }
}
