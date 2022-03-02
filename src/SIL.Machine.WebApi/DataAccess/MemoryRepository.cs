namespace SIL.Machine.WebApi.DataAccess;

public class MemoryRepository<T> : IRepository<T> where T : class, IEntity<T>
{
	private static readonly JsonSerializerSettings Settings = new()
	{
		TypeNameHandling = TypeNameHandling.Auto,
		ContractResolver = new WritableContractResolver()
	};

	private readonly Dictionary<string, string> _entities;
	private readonly Func<T, object>[] _uniqueKeySelectors;
	private readonly HashSet<object>[] _uniqueKeys;
	private readonly AsyncLock _lock;
	private readonly Dictionary<MemorySubscription<T>, Func<T, bool>> _subscriptions;

	public MemoryRepository(IEnumerable<T> entities)
		: this(null, entities)
	{
	}

	public MemoryRepository(IEnumerable<Func<T, object>>? uniqueKeySelectors = null, IEnumerable<T>? entities = null)
	{
		_lock = new AsyncLock();
		_uniqueKeySelectors = uniqueKeySelectors?.ToArray() ?? Array.Empty<Func<T, object>>();
		_uniqueKeys = new HashSet<object>[_uniqueKeySelectors.Length];
		for (int i = 0; i < _uniqueKeys.Length; i++)
			_uniqueKeys[i] = new HashSet<object>();

		_entities = new Dictionary<string, string>();
		if (entities != null)
			Add(entities);
		_subscriptions = new Dictionary<MemorySubscription<T>, Func<T, bool>>();
	}

	public void Init() { }

	public void Add(T entity)
	{
		for (int i = 0; i < _uniqueKeySelectors.Length; i++)
		{
			object key = _uniqueKeySelectors[i](entity);
			if (key != null)
				_uniqueKeys[i].Add(key);
		}
		_entities[entity.Id] = JsonConvert.SerializeObject(entity, Settings);
	}

	public void Add(IEnumerable<T> entities)
	{
		foreach (T entity in entities)
			Add(entity);
	}

	public void Remove(T entity)
	{
		for (int i = 0; i < _uniqueKeySelectors.Length; i++)
		{
			object key = _uniqueKeySelectors[i](entity);
			if (key != null)
				_uniqueKeys[i].Remove(key);
		}

		_entities.Remove(entity.Id);
	}

	public void Replace(T entity)
	{
		if (_entities.TryGetValue(entity.Id, out string? existingStr))
		{
			T existing = DeserializeEntity(entity.Id, existingStr);
			Remove(existing);
		}
		Add(entity);
	}

	public bool Contains(string id)
	{
		return _entities.ContainsKey(id);
	}

	public T Get(string id)
	{
		return DeserializeEntity(id, _entities[id]);
	}

	public IEnumerable<T> Entities => _entities.Select(kvp => DeserializeEntity(kvp.Key, kvp.Value));

	public async Task<T?> GetAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
	{
		using (await _lock.LockAsync(cancellationToken))
		{
			return Entities.AsQueryable().FirstOrDefault(filter);
		}
	}

	public async Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>> filter,
		CancellationToken cancellationToken = default)
	{
		using (await _lock.LockAsync(cancellationToken))
		{
			return Entities.AsQueryable().Where(filter).ToList();
		}
	}

	public async Task<bool> ExistsAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
	{
		using (await _lock.LockAsync(cancellationToken))
		{
			return Entities.AsQueryable().Any(filter);
		}
	}

	public async Task InsertAsync(T entity, CancellationToken cancellationToken = default)
	{
		entity.Revision = 1;
		var allSubscriptions = new List<MemorySubscription<T>>();
		using (await _lock.LockAsync(cancellationToken))
		{
			if (string.IsNullOrEmpty(entity.Id))
				entity.Id = ObjectId.GenerateNewId().ToString();

			if (_entities.ContainsKey(entity.Id) || CheckDuplicateKeys(entity))
				throw new DuplicateKeyException();

			Add(entity);
			GetSubscriptions(entity, allSubscriptions);
		}
		SendToSubscribers(allSubscriptions, EntityChangeType.Insert, entity);
	}

	public async Task<T?> UpdateAsync(Expression<Func<T, bool>> filter, Action<IUpdateBuilder<T>> update,
		bool upsert = false, CancellationToken cancellationToken = default)
	{
		var allSubscriptions = new List<MemorySubscription<T>>();
		T? entity;
		Func<T, bool> filterFunc = filter.Compile();
		using (await _lock.LockAsync(cancellationToken))
		{
			entity = Entities.FirstOrDefault(e =>
			{
				try
				{
					return filterFunc(e);
				}
				catch (Exception)
				{
					return false;
				}
			});
			if (entity != null || upsert)
			{
				T? original = default;
				bool isInsert = entity == null;
				if (isInsert)
				{
					entity = (T)Activator.CreateInstance(typeof(T))!;
					string? id = ExpressionHelper.FindEqualsConstantValue<T, string>(e => e.Id, filter.Body);
					if (id is not null && Contains(id))
						throw new DuplicateKeyException();
					entity.Id = id ?? ObjectId.GenerateNewId().ToString();
					entity.Revision = 0;
				}
				else
				{
					original = Entities.AsQueryable().FirstOrDefault(filter);
				}
				Debug.Assert(entity != null);
				var builder = new MemoryUpdateBuilder<T>(filter, entity, isInsert);
				update(builder);
				entity.Revision++;

				if (CheckDuplicateKeys(entity, original))
					throw new DuplicateKeyException();

				Replace(entity);
				GetSubscriptions(entity, allSubscriptions);
			}
		}
		SendToSubscribers(allSubscriptions, EntityChangeType.Update, entity);
		return entity;
	}

	public async Task<T?> DeleteAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
	{
		var allSubscriptions = new List<MemorySubscription<T>>();
		T? entity;
		using (await _lock.LockAsync(cancellationToken))
		{
			entity = Entities.AsQueryable().FirstOrDefault(filter);
			if (entity != null)
			{
				Remove(entity);
				GetSubscriptions(entity, allSubscriptions);
			}
		}
		SendToSubscribers(allSubscriptions, EntityChangeType.Delete, entity);
		return entity;
	}

	public async Task<int> DeleteAllAsync(Expression<Func<T, bool>> filter,
		CancellationToken cancellationToken = default)
	{
		using (await _lock.LockAsync(cancellationToken))
		{
			T[] entities = Entities.AsQueryable().Where(filter).ToArray();
			foreach (T entity in entities)
				Remove(entity);
			return entities.Length;
		}
	}

	public async Task<ISubscription<T>> SubscribeAsync(Expression<Func<T, bool>> filter,
		CancellationToken cancellationToken = default)
	{
		using (await _lock.LockAsync(cancellationToken))
		{
			T? initialEntity = Entities.AsQueryable().FirstOrDefault(filter);
			var subscription = new MemorySubscription<T>(initialEntity, RemoveSubscription);
			_subscriptions[subscription] = filter.Compile();
			return subscription;
		}
	}

	private void RemoveSubscription(MemorySubscription<T> subscription)
	{
		using (_lock.Lock())
		{
			_subscriptions.Remove(subscription);
		}
	}

	private void GetSubscriptions(T entity, IList<MemorySubscription<T>> allSubscriptions)
	{
		foreach (KeyValuePair<MemorySubscription<T>, Func<T, bool>> kvp in _subscriptions)
		{
			if (kvp.Value(entity))
				allSubscriptions.Add(kvp.Key);
		}
	}

	private static void SendToSubscribers(IList<MemorySubscription<T>> allSubscriptions, EntityChangeType type, T? entity)
	{
		foreach (MemorySubscription<T> subscription in allSubscriptions)
			subscription.HandleChange(new EntityChange<T>(type, entity?.Clone()));
	}

	/// <param name="entity">the new or updated entity to be upserted</param>
	/// <param name="original">the original entity, if this is an update (or replacement)</param>
	/// <returns>
	/// true if there is any existing entity, other than the original, that shares any keys with the new or updated
	/// entity
	/// </returns>
	private bool CheckDuplicateKeys(T entity, T? original = default)
	{
		for (int i = 0; i < _uniqueKeySelectors.Length; i++)
		{
			object key = _uniqueKeySelectors[i](entity);
			if (key != null)
			{
				if (_uniqueKeys[i].Contains(key))
				{
					if (original == null || !key.Equals(_uniqueKeySelectors[i](original)))
						return true;
				}
			}
		}
		return false;
	}

	private static T DeserializeEntity(string id, string json)
	{
		var entity = JsonConvert.DeserializeObject<T>(json, Settings)!;
		if (entity.Id == null)
			entity.Id = id;
		return entity;
	}
}
