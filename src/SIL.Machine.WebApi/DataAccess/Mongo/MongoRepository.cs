namespace SIL.Machine.WebApi.DataAccess.Mongo;

public class MongoRepository<T> : IRepository<T> where T : class, IEntity<T>
{
	private readonly Dictionary<string, ISet<Subscription<T>>> _idSubscriptions;

	public MongoRepository(IOptions<MongoDataAccessOptions> options, string collectionName)
	{
		var client = new MongoClient(options.Value.ConnectionString);
		IMongoDatabase database = client.GetDatabase(options.Value.MachineDatabaseName);
		Collection = database.GetCollection<T>(collectionName);
		Lock = new AsyncLock();
		_idSubscriptions = new Dictionary<string, ISet<Subscription<T>>>();
	}

	protected IMongoCollection<T> Collection { get; }
	protected AsyncLock Lock { get; }

	public virtual void Init()
	{
	}

	public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default(CancellationToken))
	{
		return await Collection.Find(Builders<T>.Filter.Empty).ToListAsync(ct);
	}

	public Task<T> GetAsync(string id, CancellationToken ct = default(CancellationToken))
	{
		return Collection.Find(e => e.Id == id).FirstOrDefaultAsync(ct);
	}

	public async Task<bool> ExistsAsync(string id, CancellationToken ct = default(CancellationToken))
	{
		return await Collection.Find(e => e.Id == id).Limit(1).CountDocumentsAsync(ct) > 0;
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

	protected void CreateOrUpdateIndex(CreateIndexModel<T> indexModel)
	{
		try
		{
			Collection.Indexes.CreateOne(indexModel);
		}
		catch (MongoCommandException ex)
		{
			if (ex.CodeName == "IndexOptionsConflict")
			{
				string name = ex.Command["indexes"][0]["name"].AsString;
				Collection.Indexes.DropOne(name);
				Collection.Indexes.CreateOne(indexModel);
			}
			else
			{
				throw;
			}
		}
	}

	protected virtual void GetSubscriptions(T entity, IList<Subscription<T>> allSubscriptions)
	{
		if (_idSubscriptions.TryGetValue(entity.Id, out ISet<Subscription<T>> subscriptions))
			allSubscriptions.AddRange(subscriptions);
	}

	protected void SendToSubscribers(List<Subscription<T>> allSubscriptions, EntityChangeType type, T entity)
	{
		foreach (Subscription<T> subscription in allSubscriptions)
			subscription.HandleChange(new EntityChange<T>(type, entity.Clone()));
	}

	private void RemoveSubscription<TKey>(Dictionary<TKey, ISet<Subscription<T>>> keySubscriptions,
		Subscription<T> subscription)
	{
		using (Lock.Lock())
		{
			var key = (TKey)subscription.Key;
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
		SendToSubscribers(allSubscriptions, type, entity);
	}
}
