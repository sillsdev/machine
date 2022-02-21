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
		builder.Services.AddSingleton<IEngineRepository, MemoryEngineRepository>();
		builder.Services.AddSingleton<IBuildRepository, MemoryBuildRepository>();
		return builder;
	}

	public static IMachineBuilder AddMongoDataAccess(this IMachineBuilder builder)
	{
		var globalPack = new ConventionPack
			{
				new CamelCaseElementNameConvention(),
				new ObjectRefConvention(),
				new IgnoreIfNullConvention(true)
			};
		ConventionRegistry.Register("Machine", globalPack, t => t.Namespace == "SIL.Machine.WebApi.Models");

		RegisterEntity<Engine>();
		builder.Services.AddSingleton<IEngineRepository, MongoEngineRepository>();

		RegisterEntity<Build>();
		builder.Services.AddSingleton<IBuildRepository, MongoBuildRepository>();

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

	private static void RegisterEntity<T>(Action<BsonClassMap<T>> setup = null) where T : class, IEntity<T>
	{
		BsonClassMap.RegisterClassMap<T>(cm =>
		{
			cm.AutoMap();
			cm.MapIdProperty(e => e.Id)
				.SetIdGenerator(StringObjectIdGenerator.Instance)
				.SetSerializer(new StringSerializer(BsonType.ObjectId));
			setup?.Invoke(cm);
		});
	}
}
