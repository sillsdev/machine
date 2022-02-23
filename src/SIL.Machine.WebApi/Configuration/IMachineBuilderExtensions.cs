namespace Microsoft.Extensions.DependencyInjection;

public static class IMachineBuilderExtensions
{
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
		builder.Services.AddSingleton<IComponentFactory<IInteractiveTranslationModel>, ThotSmtModelFactory>();
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
		builder.Services.AddSingleton<IComponentFactory<ITranslationEngine>, TransferEngineFactory>();
		return builder;
	}

	public static IMachineBuilder AddUnigramTruecaser(this IMachineBuilder builder)
	{
		builder.Services.AddSingleton<IComponentFactory<ITruecaser>, UnigramTruecaserFactory>();
		return builder;
	}

	public static IMachineBuilder AddTransferTruecaser(this IMachineBuilder builder)
	{
		builder.Services.AddSingleton<IComponentFactory<ITruecaser>, TransferTruecaserFactory>();
		return builder;
	}

	public static IMachineBuilder AddTextFileTextCorpus(this IMachineBuilder builder)
	{
		builder.Services.AddSingleton<ITextCorpusFactory, TextFileTextCorpusFactory>();
		return builder;
	}

	public static IMachineBuilder AddTextFileTextCorpus(this IMachineBuilder builder,
		Action<TextFileTextCorpusOptions> configureOptions)
	{
		builder.Services.Configure(configureOptions);
		return builder.AddTextFileTextCorpus();
	}

	public static IMachineBuilder AddTextFileTextCorpus(this IMachineBuilder builder,
		IConfiguration config)
	{
		builder.Services.Configure<TextFileTextCorpusOptions>(config);
		return builder.AddTextFileTextCorpus();
	}

	public static IMachineBuilder AddTextCorpus<T>(this IMachineBuilder builder) where T : class, ITextCorpusFactory
	{
		builder.Services.AddSingleton<ITextCorpusFactory, T>();
		return builder;
	}

	public static IMachineBuilder AddMemoryDataAccess(this IMachineBuilder builder)
	{
		builder.Services.AddSingleton<IRepository<Engine>, MemoryRepository<Engine>>();
		builder.Services.AddSingleton<IRepository<Build>, MemoryRepository<Build>>();
		builder.Services.AddSingleton<IRepository<DataFile>, MemoryRepository<DataFile>>();
		return builder;
	}

	public static IMachineBuilder AddMongoDataAccess(this IMachineBuilder builder)
	{
		DataAccessClassMap.RegisterConventions("SIL.Machine.WebApi.Models",
			new CamelCaseElementNameConvention(),
			new EnumRepresentationConvention(BsonType.String),
			new IgnoreIfNullConvention(true),
			new ObjectRefConvention());

		builder.Services.AddMongoRepository<Engine>("engines");
		builder.Services.AddMongoRepository<Build>("builds", indexSetup: indexes =>
			indexes.CreateOrUpdate(new CreateIndexModel<Build>(
				Builders<Build>.IndexKeys.Ascending(b => b.EngineRef))));
		builder.Services.AddMongoRepository<DataFile>("data_files", indexSetup: indexes =>
			indexes.CreateOrUpdate(new CreateIndexModel<DataFile>(
				Builders<DataFile>.IndexKeys.Ascending(b => b.EngineRef))));

		return builder;
	}

	public static IMachineBuilder AddMongoDataAccess(this IMachineBuilder builder,
		Action<MongoDataAccessOptions> configureOptions)
	{
		builder.Services.Configure(configureOptions);
		return builder.AddMongoDataAccess();
	}

	public static IMachineBuilder AddMongoDataAccess(this IMachineBuilder builder, IConfiguration config)
	{
		builder.Services.Configure<MongoDataAccessOptions>(config);
		return builder.AddMongoDataAccess();
	}


	public static void AddMongoRepository<T>(this IServiceCollection services, string collection,
		Action<BsonClassMap<T>> mapSetup = null, Action<IMongoIndexManager<T>> indexSetup = null)
		where T : class, IEntity<T>
	{
		DataAccessClassMap.RegisterClass<T>(cm =>
		{
			cm.MapIdProperty(e => e.Id);
			mapSetup?.Invoke(cm);
		});
		services.AddSingleton<IRepository<T>>(sp => CreateMongoRepository(sp, collection, indexSetup));
	}

	private static MongoRepository<T> CreateMongoRepository<T>(IServiceProvider sp, string collection,
		Action<IMongoIndexManager<T>> indexSetup) where T : class, IEntity<T>
	{
		return new MongoRepository<T>(sp.GetService<IMongoDatabase>().GetCollection<T>(collection),
			c => indexSetup?.Invoke(c.Indexes));
	}
}
