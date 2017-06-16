using System;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess
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
		public static async Task<Engine> GetByLocatorAsync(this IEngineRepository engineRepo, EngineLocatorType locatorType,
			string locator)
		{
			switch (locatorType)
			{
				case EngineLocatorType.Id:
					return await engineRepo.GetAsync(locator);
				case EngineLocatorType.LanguageTag:
					int index = locator.IndexOf("_", StringComparison.OrdinalIgnoreCase);
					string sourceLanguageTag = locator.Substring(0, index);
					string targetLanguageTag = locator.Substring(index + 1);
					return await engineRepo.GetByLanguageTagAsync(sourceLanguageTag, targetLanguageTag);
				case EngineLocatorType.Project:
					return await engineRepo.GetByProjectIdAsync(locator);
			}
			return null;
		}

		public static async Task<Build> GetByLocatorAsync(this IBuildRepository buildRepo, BuildLocatorType locatorType,
			string locator)
		{
			switch (locatorType)
			{
				case BuildLocatorType.Id:
					return await buildRepo.GetAsync(locator);
				case BuildLocatorType.Engine:
					return await buildRepo.GetByEngineIdAsync(locator);
			}
			return null;
		}

		public static async Task<T> ConcurrentUpdateAsync<T>(this IRepository<T> repo, T entity, Action<T> changeAction)
			where T : class, IEntity<T>
		{
			while (true)
			{
				try
				{
					changeAction(entity);
					await repo.UpdateAsync(entity, true);
					break;
				}
				catch (ConcurrencyConflictException)
				{
					entity = await repo.GetAsync(entity.Id);
					if (entity == null)
						return null;
				}
			}
			return entity;
		}

		public static bool TryConcurrentUpdate<T>(this IRepository<T> repo, T entity, Action<T> changeAction, out T newEntity)
			where T : class, IEntity<T>
		{
			while (true)
			{
				try
				{
					changeAction(entity);
					repo.Update(entity, true);
					break;
				}
				catch (ConcurrencyConflictException)
				{
					if (!repo.TryGet(entity.Id, out entity))
					{
						newEntity = null;
						return false;
					}
				}
			}
			newEntity = entity;
			return true;
		}
	}
}
