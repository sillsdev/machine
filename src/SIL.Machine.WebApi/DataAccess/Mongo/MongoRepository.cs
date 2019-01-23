using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using SIL.Extensions;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Utils;

namespace SIL.Machine.WebApi.DataAccess.Mongo
{
	public class MongoRepository<T> : IRepository<T> where T : class, IEntity<T>
	{
		private readonly Dictionary<string, ISet<Subscription<T>>> _idSubscriptions;

		public MongoRepository(IMongoCollection<T> collection)
		{
			Collection = collection;
			Lock = new AsyncLock();
			_idSubscriptions = new Dictionary<string, ISet<Subscription<T>>>();
		}

		protected IMongoCollection<T> Collection { get; }
		protected AsyncLock Lock { get; }

		public Task InitAsync(CancellationToken ct = default(CancellationToken))
		{
			return Task.CompletedTask;
		}

		public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default(CancellationToken))
		{
			return await Collection.Find(Builders<T>.Filter.Empty).ToListAsync(ct);
		}

		public Task<T> GetAsync(string id, CancellationToken ct = default(CancellationToken))
		{
			return Collection.Find(e => e.Id == id).FirstOrDefaultAsync(ct);
		}

		public async Task InsertAsync(T entity, CancellationToken ct = default(CancellationToken))
		{
			try
			{
				if (string.IsNullOrEmpty(entity.Id))
					entity.Id = ObjectId.GenerateNewId().ToString();
				await Collection.InsertOneAsync(entity, cancellationToken: ct);

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

		public async Task UpdateAsync(T entity, bool checkConflict = false,
			CancellationToken ct = default(CancellationToken))
		{
			int revision = entity.Revision;
			entity.Revision++;
			if (checkConflict)
			{
				ReplaceOneResult result = await Collection.ReplaceOneAsync(
					e => e.Id == entity.Id && e.Revision == revision, entity, cancellationToken: ct);
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

		public async Task DeleteAsync(T entity, bool checkConflict = false,
			CancellationToken ct = default(CancellationToken))
		{
			if (checkConflict)
			{
				DeleteResult result = await Collection.DeleteOneAsync(
					e => e.Id == entity.Id && e.Revision == entity.Revision, cancellationToken: ct);
				if (result.IsAcknowledged && result.DeletedCount == 0)
					throw new ConcurrencyConflictException("The entity does not exist or has been updated.");
			}
			else
			{
				entity = await Collection.FindOneAndDeleteAsync(e => e.Id == entity.Id, cancellationToken: ct);
			}

			await SendToSubscribersAsync(EntityChangeType.Delete, entity);
		}

		public async Task DeleteAsync(string id, CancellationToken ct = default(CancellationToken))
		{
			T entity = await Collection.FindOneAndDeleteAsync(e => e.Id == id, cancellationToken: ct);

			if (entity != null)
				await SendToSubscribersAsync(EntityChangeType.Delete, entity);
		}

		public Task<Subscription<T>> SubscribeAsync(string id, CancellationToken ct = default(CancellationToken))
		{
			return AddSubscriptionAsync(GetAsync, _idSubscriptions, id, ct);
		}

		protected async Task<Subscription<T>> AddSubscriptionAsync<TKey>(
			Func<TKey, CancellationToken, Task<T>> getEntity, Dictionary<TKey, ISet<Subscription<T>>> keySubscriptions,
			TKey key, CancellationToken ct)
		{
			using (await Lock.LockAsync())
			{
				T initialEntity = await getEntity(key, ct);
				var subscription = new Subscription<T>(key, initialEntity,
					s => RemoveSubscription(keySubscriptions, s));
				if (!keySubscriptions.TryGetValue(key, out ISet<Subscription<T>> subscriptions))
				{
					subscriptions = new HashSet<Subscription<T>>();
					keySubscriptions[key] = subscriptions;
				}
				subscriptions.Add(subscription);
				return subscription;
			}
		}

		private void RemoveSubscription<TKey>(Dictionary<TKey, ISet<Subscription<T>>> keySubscriptions,
			Subscription<T> subscription)
		{
			using (Lock.Lock())
			{
				var key = (TKey) subscription.Key;
				ISet<Subscription<T>> subscriptions = keySubscriptions[key];
				subscriptions.Remove(subscription);
				if (subscriptions.Count == 0)
					keySubscriptions.Remove(key);
			}
		}

		private async Task SendToSubscribersAsync(EntityChangeType type, T entity)
		{
			var allSubscriptions = new List<Subscription<T>>();
			using (await Lock.LockAsync())
				GetSubscriptions(entity, allSubscriptions);
			foreach (Subscription<T> subbscription in allSubscriptions)
				subbscription.HandleChange(new EntityChange<T>(type, entity.Clone()));
		}

		protected virtual void GetSubscriptions(T entity, IList<Subscription<T>> allSubscriptions)
		{
			if (_idSubscriptions.TryGetValue(entity.Id, out ISet<Subscription<T>> subscriptions))
				allSubscriptions.AddRange(subscriptions);
		}
	}
}
