using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using SIL.Extensions;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Utils;

namespace SIL.Machine.WebApi.Server.DataAccess.Mongo
{
	public class MongoRepository<T> : IRepository<T> where T : class, IEntity<T>
	{
		private readonly Dictionary<string, ISet<Action<EntityChange<T>>>> _changeListeners;

		public MongoRepository(IMongoCollection<T> collection)
		{
			Collection = collection;
			Lock = new AsyncLock();
			_changeListeners = new Dictionary<string, ISet<Action<EntityChange<T>>>>();
		}

		protected IMongoCollection<T> Collection { get; }
		protected AsyncLock Lock { get; }

		public Task InitAsync()
		{
			return Task.CompletedTask;
		}

		public async Task<IEnumerable<T>> GetAllAsync()
		{
			return await Collection.Find(Builders<T>.Filter.Empty).ToListAsync();
		}

		public Task<T> GetAsync(string id)
		{
			return Collection.Find(e => e.Id == id).FirstOrDefaultAsync();
		}

		public async Task InsertAsync(T entity)
		{
			try
			{
				if (string.IsNullOrEmpty(entity.Id))
					entity.Id = ObjectId.GenerateNewId().ToString();
				await Collection.InsertOneAsync(entity);

				await SendToSubscribersAsync(EntityChangeType.Insert, entity);
			}
			catch (AggregateException ae)
			{
				bool keyExists = false;
				ae.Handle(e =>
					{
						var mwe = e as MongoWriteException;
						if (mwe != null && mwe.WriteError.Category == ServerErrorCategory.DuplicateKey)
						{
							keyExists = true;
							return true;
						}
						return false;
					});

				if (keyExists)
					throw new KeyAlreadyExistsException("An entity with the same identifier already exists.");
			}
		}

		public async Task UpdateAsync(T entity, bool checkConflict = false)
		{
			int revision = entity.Revision;
			entity.Revision++;
			if (checkConflict)
			{
				ReplaceOneResult result = await Collection.ReplaceOneAsync(
					e => e.Id == entity.Id && e.Revision == revision, entity);
				if (result.IsAcknowledged && result.MatchedCount == 0)
				{
					entity.Revision--;
					throw new ConcurrencyConflictException("The entity does not exist or has been updated.");
				}
			}
			else
			{
				await Collection.ReplaceOneAsync(e => e.Id == entity.Id, entity,
					new UpdateOptions { IsUpsert = true });
			}

			await SendToSubscribersAsync(EntityChangeType.Update, entity);
		}

		public async Task DeleteAsync(T entity, bool checkConflict = false)
		{
			if (checkConflict)
			{
				DeleteResult result = await Collection.DeleteOneAsync(
					e => e.Id == entity.Id && e.Revision == entity.Revision);
				if (result.IsAcknowledged && result.DeletedCount == 0)
					throw new ConcurrencyConflictException("The entity does not exist or has been updated.");
			}
			else
			{
				entity = await Collection.FindOneAndDeleteAsync(e => e.Id == entity.Id);
			}

			await SendToSubscribersAsync(EntityChangeType.Delete, entity);
		}

		public async Task DeleteAsync(string id)
		{
			T entity = await Collection.FindOneAndDeleteAsync(e => e.Id == id);

			if (entity != null)
				await SendToSubscribersAsync(EntityChangeType.Delete, entity);
		}

		public async Task<IDisposable> SubscribeAsync(string id, Action<EntityChange<T>> listener)
		{
			using (await Lock.LockAsync())
				return new MongoSubscription<string, T>(Lock, _changeListeners, id, listener);
		}

		private async Task SendToSubscribersAsync(EntityChangeType type, T entity)
		{
			var changeListeners = new List<Action<EntityChange<T>>>();
			using (await Lock.LockAsync())
				GetChangeListeners(entity, changeListeners);
			foreach (Action<EntityChange<T>> changeListener in changeListeners)
				changeListener(new EntityChange<T>(type, entity.Clone()));
		}

		protected virtual void GetChangeListeners(T entity, IList<Action<EntityChange<T>>> changeListeners)
		{
			if (_changeListeners.TryGetValue(entity.Id, out ISet<Action<EntityChange<T>>> listeners))
				changeListeners.AddRange(listeners);
		}
	}
}
