namespace Microsoft.Extensions.DependencyInjection;

public static class IMachineBuilderExtensions
{
	public static IMachineBuilder AddServiceOptions(this IMachineBuilder builder,
		Action<ServiceOptions> configureOptions)
	{
		builder.Services.Configure(configureOptions);
		return builder;
	}

	public static IMachineBuilder AddServiceOptions(this IMachineBuilder builder,
		IConfiguration config)
	{
		builder.Services.Configure<ServiceOptions>(config);
		return builder;
	}

	public static IMachineBuilder AddEngineOptions(this IMachineBuilder builder,
		Action<EngineOptions> configureOptions)
	{
		builder.Services.Configure(configureOptions);
		return builder;
	}

	public static IMachineBuilder AddEngineOptions(this IMachineBuilder builder,
		IConfiguration config)
	{
		builder.Services.Configure<EngineOptions>(config);
		return builder;
	}

	public static IMachineBuilder AddDataFileOptions(this IMachineBuilder builder,
		Action<DataFileOptions> configureOptions)
	{
		builder.Services.Configure(configureOptions);
		return builder;
	}

	public static IMachineBuilder AddDataFileOptions(this IMachineBuilder builder,
		IConfiguration config)
	{
		builder.Services.Configure<DataFileOptions>(config);
		return builder;
	}

	public static IMachineBuilder AddThotSmtModel(this IMachineBuilder builder)
	{
		builder.Services.AddSingleton<ISmtModelFactory, ThotSmtModelFactory>();
		return builder;
	}

	public static IMachineBuilder AddThotSmtModel(this IMachineBuilder builder,
		Action<ThotSmtModelOptions> configureOptions)
	{
		builder.Services.Configure(configureOptions);
		return builder.AddThotSmtModel();
	}

	public static IMachineBuilder AddThotSmtModel(this IMachineBuilder builder,
		IConfiguration config)
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

	public static IMachineBuilder AddTransferTruecaser(this IMachineBuilder builder)
	{
		builder.Services.AddSingleton<ITruecaserFactory, TransferTruecaserFactory>();
		return builder;
	}

	public static IMachineBuilder AddMemoryDataAccess(this IMachineBuilder builder)
	{
		builder.Services.AddSingleton<IRepository<Engine>, MemoryRepository<Engine>>();
		builder.Services.AddSingleton<IRepository<Build>, MemoryRepository<Build>>();
		builder.Services.AddSingleton<IRepository<DataFile>, MemoryRepository<DataFile>>();
		builder.Services.AddSingleton<IRepository<RWLock>, MemoryRepository<RWLock>>();

		return builder;
	}

	public static IMachineBuilder AddMongoDataAccess(this IMachineBuilder builder, string connectionString)
	{
		DataAccessClassMap.RegisterConventions("SIL.Machine.WebApi.Models",
			new StringIdStoredAsObjectIdConvention(),
			new CamelCaseElementNameConvention(),
			new EnumRepresentationConvention(BsonType.String),
			new IgnoreIfNullConvention(true),
			new ObjectRefConvention());

		var mongoUrl = new MongoUrl(connectionString);
		builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoUrl));
		builder.Services.AddSingleton(sp => sp.GetService<IMongoClient>()!.GetDatabase(mongoUrl.DatabaseName));


		builder.Services.AddMongoRepository<Engine>("engines");
		builder.Services.AddMongoRepository<Build>("builds", indexSetup: indexes =>
			indexes.CreateOrUpdate(new CreateIndexModel<Build>(
				Builders<Build>.IndexKeys.Ascending(b => b.EngineRef))), isSubscribable: true);
		builder.Services.AddMongoRepository<DataFile>("files", indexSetup: indexes =>
			indexes.CreateOrUpdate(new CreateIndexModel<DataFile>(
				Builders<DataFile>.IndexKeys.Ascending(f => f.EngineRef))));
		builder.Services.AddMongoRepository<RWLock>("locks", indexSetup: indexes =>
		{
			indexes.CreateOrUpdate(new CreateIndexModel<RWLock>(
				Builders<RWLock>.IndexKeys.Ascending(rwl => rwl.WriterLock.Id)));
			indexes.CreateOrUpdate(new CreateIndexModel<RWLock>(
				Builders<RWLock>.IndexKeys.Ascending("readerLocks._id")));
		}, isSubscribable: true);

		return builder;
	}


	public static void AddMongoRepository<T>(this IServiceCollection services, string collection,
		Action<BsonClassMap<T>>? mapSetup = null, Action<IMongoIndexManager<T>>? indexSetup = null,
		bool isSubscribable = false) where T : class, IEntity<T>
	{
		DataAccessClassMap.RegisterClass<T>(cm => mapSetup?.Invoke(cm));
		services.AddSingleton<IRepository<T>>(sp => CreateMongoRepository(sp, collection, indexSetup, isSubscribable));
	}

	private static MongoRepository<T> CreateMongoRepository<T>(IServiceProvider sp, string collection,
		Action<IMongoIndexManager<T>>? indexSetup, bool isSubscribable) where T : class, IEntity<T>
	{
		return new MongoRepository<T>(sp.GetService<IMongoDatabase>()!.GetCollection<T>(collection),
			c => indexSetup?.Invoke(c.Indexes), isSubscribable);
	}
}
