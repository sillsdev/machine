using System;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess
{
	public enum BuildLocatorType
	{
		Id,
		Engine
	}

	public static class DataAccessExtensions
	{
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
	}
}
