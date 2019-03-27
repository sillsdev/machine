using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using NoDb;
using SIL.Machine.WebApi.Configuration;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.DataAccess.Memory;
using SIL.Machine.WebApi.DataAccess.Mongo;
using SIL.Machine.WebApi.DataAccess.NoDb;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Services;

namespace Microsoft.Extensions.DependencyInjection
{
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
			builder.Services.AddSingleton<IRuleEngineFactory, TransferEngineFactory>();
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

		public static IMachineBuilder AddNoDbDataAccess(this IMachineBuilder builder)
		{
			builder.Services.AddNoDbForEntity<Engine>();
			builder.Services.AddNoDbForEntity<Build>();
			builder.Services.AddNoDbForEntity<Project>();
			builder.Services.AddSingleton<IEngineRepository>(sp => new MemoryEngineRepository(
				new NoDbEngineRepository(sp.GetService<IBasicCommands<Engine>>(),
					sp.GetService<IBasicQueries<Engine>>())));
			builder.Services.AddSingleton<IBuildRepository>(sp => new MemoryBuildRepository(
				new NoDbBuildRepository(sp.GetService<IBasicCommands<Build>>(),
					sp.GetService<IBasicQueries<Build>>())));
			builder.Services.AddSingleton<IRepository<Project>>(sp => new MemoryRepository<Project>(
				new NoDbRepository<Project>(sp.GetService<IBasicCommands<Project>>(),
					sp.GetService<IBasicQueries<Project>>())));
			return builder;
		}

		public static IMachineBuilder AddNoDbDataAccess(this IMachineBuilder builder,
			Action<NoDbDataAccessOptions> configureOptions)
		{
			builder.Services.Configure(configureOptions);
			return builder.AddNoDbDataAccess();
		}

		public static IMachineBuilder AddNoDbDataAccess(this IMachineBuilder builder,
			IConfiguration config)
		{
			builder.Services.Configure<NoDbDataAccessOptions>(config);
			return builder.AddNoDbDataAccess();
		}

		private static void AddNoDbForEntity<T>(this IServiceCollection services) where T : class
		{
			services.AddSingleton<IBasicCommands<T>, BasicCommands<T>>();
			services.AddSingleton<IBasicQueries<T>, BasicQueries<T>>();
			services.AddSingleton<IStringSerializer<T>, StringSerializer<T>>();
			services.AddSingleton<IStoragePathResolver<T>, MachineStoragePathResolver<T>>();
		}

		public static IMachineBuilder AddMemoryDataAccess(this IMachineBuilder builder)
		{
			builder.Services.AddSingleton<IEngineRepository, MemoryEngineRepository>();
			builder.Services.AddSingleton<IBuildRepository, MemoryBuildRepository>();
			builder.Services.AddSingleton<IRepository<Project>, MemoryRepository<Project>>();
			return builder;
		}

		public static IMachineBuilder AddMongoDataAccess(this IMachineBuilder builder)
		{
			var globalPack = new ConventionPack
			{
				new CamelCaseElementNameConvention(),
				new ObjectRefConvention()
			};
			ConventionRegistry.Register("Machine", globalPack, t => t.Namespace == "SIL.Machine.WebApi.Models");

			RegisterEntity<Engine>(cm =>
			{
				cm.MapMember(e => e.Projects)
					.SetSerializer(new EnumerableInterfaceImplementerSerializer<HashSet<string>, string>(
						new StringSerializer(BsonType.ObjectId)));
			});
			builder.Services.AddSingleton<IEngineRepository, MongoEngineRepository>();

			RegisterEntity<Build>();
			builder.Services.AddSingleton<IBuildRepository, MongoBuildRepository>();

			RegisterEntity<Project>();
			builder.Services.AddSingleton<IRepository<Project>>(
				sp => new MongoRepository<Project>(sp.GetService<IOptions<MongoDataAccessOptions>>(), "projects"));

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
}
