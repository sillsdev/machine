namespace SIL.Machine.WebApi.DataAccess;

public class MongoRepository<T> : IRepository<T> where T : class, IEntity<T>
{
	private readonly IMongoCollection<T> _collection;
	private readonly Action<IMongoCollection<T>> _init;
	private readonly AsyncLock _lock;
	private readonly Dictionary<Subscription<T>, Func<T, bool>> _subscriptions;

	public MongoRepository(IMongoCollection<T> collection, Action<IMongoCollection<T>> init)
	{
		_collection = collection;
		_init = init;
		_lock = new AsyncLock();
		_subscriptions = new Dictionary<Subscription<T>, Func<T, bool>>();
	}

	public void Init()
	{
		_init(_collection);
	}

	public async Task<T?> GetAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
	{
		return await _collection.AsQueryable().FirstOrDefaultAsync(filter, cancellationToken);
	}

	public async Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>> filter,
		CancellationToken cancellationToken = default)
	{
		return await _collection.AsQueryable().Where(filter).ToListAsync(cancellationToken);
	}

	public Task<bool> ExistsAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
	{
		return _collection.AsQueryable().AnyAsync(filter, cancellationToken);
	}

	public async Task InsertAsync(T entity, CancellationToken cancellationToken = default)
	{
		try
		{
			await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
			await SendToSubscribersAsync(EntityChangeType.Insert, entity);
		}
		catch (MongoWriteException e)
		{
			if (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
				throw new DuplicateKeyException(e);
			throw;
		}

	}

	public async Task<bool> ReplaceAsync(T entity, bool upsert = false, CancellationToken cancellationToken = default)
	{
		try
		{
			ReplaceOneResult result = await _collection.ReplaceOneAsync(e => e.Id == entity.Id, entity,
				new UpdateOptions { IsUpsert = upsert }, cancellationToken);
			if (result.IsAcknowledged && (upsert || result.MatchedCount > 0))
			{
				await SendToSubscribersAsync(EntityChangeType.Update, entity);
				return true;
			}
			return false;
		}
		catch (MongoWriteException e)
		{
			if (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
				throw new DuplicateKeyException();
			throw;
		}
	}

	public async Task<T?> UpdateAsync(Expression<Func<T, bool>> filter, Action<IUpdateBuilder<T>> update,
		bool upsert = false, CancellationToken cancellationToken = default)
	{
		try
		{
			var updateBuilder = new MongoUpdateBuilder<T>();
			update(updateBuilder);
			UpdateDefinition<T> updateDef = updateBuilder.Build();
			T entity = await _collection.FindOneAndUpdateAsync(filter, updateDef,
				new FindOneAndUpdateOptions<T>
				{
					IsUpsert = upsert,
					ReturnDocument = ReturnDocument.After
				}, cancellationToken);
			await SendToSubscribersAsync(EntityChangeType.Update, entity);
			return entity;
		}
		catch (MongoWriteException e)
		{
			if (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
				throw new DuplicateKeyException();
			throw;
		}
	}

	public async Task<T?> DeleteAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
	{
		T entity = await _collection.FindOneAndDeleteAsync(filter, cancellationToken: cancellationToken);
		await SendToSubscribersAsync(EntityChangeType.Delete, entity);
		return entity;
	}

	public async Task<int> DeleteAllAsync(Expression<Func<T, bool>> filter,
		CancellationToken cancellationToken = default)
	{
		DeleteResult result = await _collection.DeleteManyAsync(filter, cancellationToken);
		return (int)result.DeletedCount;
	}

	public async Task<Subscription<T>> SubscribeAsync(Expression<Func<T, bool>> filter,
		CancellationToken cancellationToken = default)
	{
		using (await _lock.LockAsync(cancellationToken))
		{
			T initialEntity = await _collection.AsQueryable().FirstOrDefaultAsync(filter, cancellationToken);
			var subscription = new Subscription<T>(initialEntity, RemoveSubscription);
			_subscriptions[subscription] = filter.Compile();
			return subscription;
		}
	}

	private void RemoveSubscription(Subscription<T> subscription)
	{
		using (_lock.Lock())
		{
			_subscriptions.Remove(subscription);
		}
	}

	private async Task SendToSubscribersAsync(EntityChangeType type, T entity)
	{
		var allSubscriptions = new List<Subscription<T>>();
		using (await _lock.LockAsync())
			GetSubscriptions(entity, allSubscriptions);
		SendToSubscribers(allSubscriptions, type, entity);
	}

	private void GetSubscriptions(T entity, IList<Subscription<T>> allSubscriptions)
	{
		foreach (KeyValuePair<Subscription<T>, Func<T, bool>> kvp in _subscriptions)
		{
			if (kvp.Value(entity))
				allSubscriptions.Add(kvp.Key);
		}
	}

	private static void SendToSubscribers(IList<Subscription<T>> allSubscriptions, EntityChangeType type, T entity)
	{
		foreach (Subscription<T> subscription in allSubscriptions)
			subscription.HandleChange(new EntityChange<T>(type, entity?.Clone()));
	}
}
