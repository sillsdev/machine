using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Mongo;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using NoDb;
using SIL.Machine.WebApi.Server.DataAccess.Memory;
using SIL.Machine.WebApi.Server.DataAccess.Mongo;
using SIL.Machine.WebApi.Server.DataAccess.NoDb;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Options;
using SIL.Machine.WebApi.Server.Utils;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public enum EngineLocatorType
	{
		Id,
		LanguageTag,
		Project
	}

	public enum BuildLocatorType
	{
		Id,
		Engine
	}

	public static class DataAccessExtensions
	{
		public static async Task<Engine> GetByLocatorAsync(this IEngineRepository engineRepo,
			EngineLocatorType locatorType, string locator, CancellationToken ct = default(CancellationToken))
		{
			switch (locatorType)
			{
				case EngineLocatorType.Id:
					return await engineRepo.GetAsync(locator, ct);
				case EngineLocatorType.LanguageTag:
					int index = locator.IndexOf("_", StringComparison.OrdinalIgnoreCase);
					string sourceLanguageTag = locator.Substring(0, index);
					string targetLanguageTag = locator.Substring(index + 1);
					return await engineRepo.GetByLanguageTagAsync(sourceLanguageTag, targetLanguageTag, ct);
				case EngineLocatorType.Project:
					return await engineRepo.GetByProjectIdAsync(locator, ct);
			}
			return null;
		}

		public static async Task<Build> GetByLocatorAsync(this IBuildRepository buildRepo, BuildLocatorType locatorType,
			string locator, CancellationToken ct = default(CancellationToken))
		{
			switch (locatorType)
			{
				case BuildLocatorType.Id:
					return await buildRepo.GetAsync(locator, ct);
				case BuildLocatorType.Engine:
					return await buildRepo.GetByEngineIdAsync(locator, ct);
			}
			return null;
		}

		public static async Task<T> ConcurrentUpdateAsync<T>(this IRepository<T> repo, string id,
			Action<T> changeAction, CancellationToken ct = default(CancellationToken)) where T : class, IEntity<T>
		{
			T entity = await repo.GetAsync(id, ct);
			if (entity == null)
				return null;
			return await repo.ConcurrentUpdateAsync(entity, changeAction, ct);
		}

		public static async Task<T> ConcurrentUpdateAsync<T>(this IRepository<T> repo, T entity, Action<T> changeAction,
			CancellationToken ct = default(CancellationToken)) where T : class, IEntity<T>
		{
			while (true)
			{
				try
				{
					changeAction(entity);
					await repo.UpdateAsync(entity, true, ct);
					break;
				}
				catch (ConcurrencyConflictException)
				{
					entity = await repo.GetAsync(entity.Id, ct);
					if (entity == null)
						return null;
				}
			}
			return entity;
		}

		public static Task<EntityChange<T>> GetNewerRevisionAsync<T>(this IRepository<T> repo, string id,
			long minRevision, CancellationToken ct = default(CancellationToken)) where T : class, IEntity<T>
		{
			return GetNewerRevisionAsync(repo.SubscribeAsync, id, minRevision, ct);
		}

		public static Task<EntityChange<Build>> GetNewerRevisionByEngineIdAsync(this IBuildRepository repo,
			string engineId, long minRevision, CancellationToken ct = default(CancellationToken))
		{
			return GetNewerRevisionAsync(repo.SubscribeByEngineIdAsync, engineId, minRevision, ct);
		}

		public static Task<EntityChange<Build>> GetNewerRevisionAsync(this IBuildRepository repo,
			BuildLocatorType locatorType, string locator, long minRevision,
			CancellationToken ct = default(CancellationToken))
		{
			switch (locatorType)
			{
				case BuildLocatorType.Id:
					return repo.GetNewerRevisionAsync(locator, minRevision, ct);
				case BuildLocatorType.Engine:
					return repo.GetNewerRevisionByEngineIdAsync(locator, minRevision, ct);
			}
			return null;
		}

		private static async Task<EntityChange<TEntity>> GetNewerRevisionAsync<TKey, TEntity>(
			Func<TKey, CancellationToken, Task<Subscription<TEntity>>> subscribe, TKey key, long minRevision,
			CancellationToken ct) where TEntity : class, IEntity<TEntity>
		{
			using (Subscription<TEntity> subscription = await subscribe(key, ct))
			{
				EntityChange<TEntity> curChange = subscription.Change;
				if (curChange.Type == EntityChangeType.Delete && minRevision > 0)
					return curChange;
				while (true)
				{
					if (curChange.Type != EntityChangeType.Delete && minRevision <= curChange.Entity.Revision)
						return curChange;
					await subscription.WaitForUpdateAsync(ct);
					curChange = subscription.Change;
					if (curChange.Type == EntityChangeType.Delete)
						return curChange;
				}
			}
		}

		public static IServiceCollection AddNoDbDataAccess(this IServiceCollection services, IConfiguration config)
		{
			services.AddHangfire(gc => gc.UseMemoryStorage());
			services.Configure<NoDbDataAccessOptions>(config.GetSection("NoDbDataAccess"));
			services.AddNoDbForEntity<Engine>();
			services.AddNoDbForEntity<Build>();
			services.AddNoDbForEntity<Project>();
			services.AddSingleton<IEngineRepository>(sp => new MemoryEngineRepository(
				new NoDbEngineRepository(sp.GetService<IBasicCommands<Engine>>(),
					sp.GetService<IBasicQueries<Engine>>())));
			services.AddSingleton<IBuildRepository>(sp => new MemoryBuildRepository(
				new NoDbBuildRepository(sp.GetService<IBasicCommands<Build>>(),
					sp.GetService<IBasicQueries<Build>>())));
			services.AddSingleton<IRepository<Project>>(sp => new MemoryRepository<Project>(
				new NoDbRepository<Project>(sp.GetService<IBasicCommands<Project>>(),
					sp.GetService<IBasicQueries<Project>>())));
			return services;
		}

		private static void AddNoDbForEntity<T>(this IServiceCollection services) where T : class
		{
			services.AddSingleton<IBasicCommands<T>, BasicCommands<T>>();
			services.AddSingleton<IBasicQueries<T>, BasicQueries<T>>();
			services.AddSingleton<IStringSerializer<T>, StringSerializer<T>>();
			services.AddSingleton<IStoragePathResolver<T>, MachineStoragePathResolver<T>>();
		}

		public static IServiceCollection AddMemoryDataAccess(this IServiceCollection services)
		{
			services.AddHangfire(gc => gc.UseMemoryStorage());
			services.AddSingleton<IEngineRepository, MemoryEngineRepository>();
			services.AddSingleton<IBuildRepository, MemoryBuildRepository>();
			services.AddSingleton<IRepository<Project>, MemoryRepository<Project>>();
			return services;
		}

		public static IServiceCollection AddMongoDataAccess(this IServiceCollection services,
			IConfiguration configuration)
		{
			IConfigurationSection dataAccessConfig = configuration.GetSection("MongoDataAccess");
			services.Configure<MongoDataAccessOptions>(dataAccessConfig);
			string connectionString = dataAccessConfig.GetValue("ConnectionString", "mongodb://localhost:27017");
			var mongoStorageOptions = new MongoStorageOptions
			{
				MigrationOptions = new MongoMigrationOptions
				{
					Strategy = MongoMigrationStrategy.Migrate
				}
			};
			services.AddHangfire(gc => gc.UseMongoStorage(connectionString, "machine", mongoStorageOptions));

			var mongoClient = new MongoClient(connectionString);
			IMongoDatabase db = mongoClient.GetDatabase("machine");

			var globalPack = new ConventionPack
			{
				new CamelCaseElementNameConvention(),
				new ObjectRefConvention()
			};
			ConventionRegistry.Register("Global", globalPack, t => true);

			RegisterEntity<Engine>(cm =>
				{
					cm.MapMember(e => e.Projects)
						.SetSerializer(new EnumerableInterfaceImplementerSerializer<HashSet<string>, string>(
							new StringSerializer(BsonType.ObjectId)));
				});
			IMongoCollection<Engine> engineCollection = db.GetCollection<Engine>("engines");
			engineCollection.Indexes.CreateOne(Builders<Engine>.IndexKeys
				.Ascending(e => e.SourceLanguageTag)
				.Ascending(e => e.TargetLanguageTag));
			engineCollection.Indexes.CreateOne(Builders<Engine>.IndexKeys.Ascending(e => e.Projects));
			services.AddSingleton<IEngineRepository>(sp => new MongoEngineRepository(engineCollection));

			RegisterEntity<Build>();
			IMongoCollection<Build> buildCollection = db.GetCollection<Build>("builds");
			buildCollection.Indexes.CreateOne(Builders<Build>.IndexKeys.Ascending(b => b.EngineRef));
			services.AddSingleton<IBuildRepository>(sp => new MongoBuildRepository(buildCollection));

			RegisterEntity<Project>();
			IMongoCollection<Project> projectCollection = db.GetCollection<Project>("projects");
			services.AddSingleton<IRepository<Project>>(sp => new MongoRepository<Project>(projectCollection));

			return services;
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

		public static IApplicationBuilder UseDataAccess(this IApplicationBuilder app)
		{
			app.ApplicationServices.GetService<IEngineRepository>().InitAsync().WaitAndUnwrapException();
			app.ApplicationServices.GetService<IBuildRepository>().InitAsync().WaitAndUnwrapException();
			app.ApplicationServices.GetService<IRepository<Project>>().InitAsync().WaitAndUnwrapException();
			return app;
		}
	}
}
