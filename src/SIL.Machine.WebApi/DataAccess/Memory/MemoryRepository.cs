namespace SIL.Machine.WebApi.DataAccess.Memory;

public class MemoryRepository<T> : IRepository<T> where T : class, IEntity<T>
{
	private readonly Dictionary<string, ISet<Subscription<T>>> _idSubscriptions;

	public MemoryRepository(IRepository<T> persistenceRepo = null)
	{
		Lock = new AsyncReaderWriterLock();
		Entities = new Dictionary<string, T>();
		PersistenceRepository = persistenceRepo;
		_idSubscriptions = new Dictionary<string, ISet<Subscription<T>>>();
	}

	protected IRepository<T> PersistenceRepository { get; }
	protected AsyncReaderWriterLock Lock { get; }
	protected IDictionary<string, T> Entities { get; }

	public virtual void Init()
	{
		if (PersistenceRepository != null)
		{
			PersistenceRepository.Init();
			foreach (T entity in PersistenceRepository.GetAllAsync().WaitAndUnwrapException())
				Entities[entity.Id] = entity;
		}
	}

	public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default(CancellationToken))
	{
		using (await Lock.ReaderLockAsync(ct))
			return Entities.Values.Select(e => e.Clone()).ToArray();
	}

	public async Task<T> GetAsync(string id, CancellationToken ct = default(CancellationToken))
	{
		using (await Lock.ReaderLockAsync(ct))
		{
			if (Entities.TryGetValue(id, out T e))
				return e.Clone();

			return null;
		}
	}

	public async Task<bool> ExistsAsync(string id, CancellationToken ct = default(CancellationToken))
	{
		using (await Lock.ReaderLockAsync(ct))
		{
			return Entities.ContainsKey(id);
		}
	}

	public async Task InsertAsync(T entity, CancellationToken ct = default(CancellationToken))
	{
		var allSubscriptions = new List<Subscription<T>>();
		T internalEntity;
		using (await Lock.WriterLockAsync(ct))
		{
			if (string.IsNullOrEmpty(entity.Id))
				entity.Id = ObjectId.GenerateNewId().ToString();
			if (Entities.ContainsKey(entity.Id))
				throw new KeyAlreadyExistsException("An entity with the same identifier already exists.");
			OnBeforeEntityChanged(EntityChangeType.Insert, entity);

			internalEntity = entity.Clone();
			Entities.Add(entity.Id, internalEntity);

			OnEntityChanged(EntityChangeType.Insert, null, internalEntity, allSubscriptions);

			if (PersistenceRepository != null)
				await PersistenceRepository.InsertAsync(entity, ct);
		}
		SendToSubscribers(allSubscriptions, EntityChangeType.Insert, internalEntity);
	}

	public async Task UpdateAsync(T entity, bool checkConflict = false,
		CancellationToken ct = default(CancellationToken))
	{
		var allSubscriptions = new List<Subscription<T>>();
		T internalEntity;
		using (await Lock.WriterLockAsync(ct))
		{
			OnBeforeEntityChanged(EntityChangeType.Update, entity);

			if (checkConflict)
				CheckForConcurrencyConflict(entity);

			entity.Revision++;
			internalEntity = entity.Clone();
			T oldEntity = Entities[entity.Id];
			Entities[entity.Id] = internalEntity;

			GetIdSubscriptions(entity.Id, allSubscriptions);

			OnEntityChanged(EntityChangeType.Update, oldEntity, internalEntity, allSubscriptions);

			if (PersistenceRepository != null)
				await PersistenceRepository.UpdateAsync(entity, false, ct);
		}
		SendToSubscribers(allSubscriptions, EntityChangeType.Update, internalEntity);
	}

	public async Task DeleteAsync(T entity, bool checkConflict = false,
		CancellationToken ct = default(CancellationToken))
	{
		var allSubscriptions = new List<Subscription<T>>();
		T internalEntity;
		using (await Lock.WriterLockAsync(ct))
		{
			if (checkConflict)
				CheckForConcurrencyConflict(entity);

			internalEntity = DeleteEntity(entity.Id, allSubscriptions);

			if (PersistenceRepository != null)
				await PersistenceRepository.DeleteAsync(entity, false, ct);
		}
		SendToSubscribers(allSubscriptions, EntityChangeType.Delete, internalEntity);
	}

	public async Task DeleteAsync(string id, CancellationToken ct = default(CancellationToken))
	{
		var allSubscriptions = new List<Subscription<T>>();
		T internalEntity;
		using (await Lock.WriterLockAsync(ct))
		{
			internalEntity = DeleteEntity(id, allSubscriptions);

			if (PersistenceRepository != null)
				await PersistenceRepository.DeleteAsync(id, ct);
		}
		if (internalEntity != null)
			SendToSubscribers(allSubscriptions, EntityChangeType.Delete, internalEntity);
	}

	public async Task<Subscription<T>> SubscribeAsync(string id, CancellationToken ct = default(CancellationToken))
	{
		using (await Lock.WriterLockAsync(ct))
		{
			if (!Entities.TryGetValue(id, out T initialEntity))
				initialEntity = null;
			var subscription = new Subscription<T>(id, initialEntity?.Clone(), RemoveSubscription);
			if (!_idSubscriptions.TryGetValue(id, out ISet<Subscription<T>> subscriptions))
			{
				subscriptions = new HashSet<Subscription<T>>();
				_idSubscriptions[id] = subscriptions;
			}
			subscriptions.Add(subscription);
			return subscription;
		}
	}

	protected T DeleteEntity(string id, IList<Subscription<T>> allSubscriptions)
	{
		if (Entities.TryGetValue(id, out T oldEntity))
		{
			Entities.Remove(id);

			GetIdSubscriptions(id, allSubscriptions);

			OnEntityChanged(EntityChangeType.Delete, oldEntity, null, allSubscriptions);
			return oldEntity;
		}
		return null;
	}

	protected void SendToSubscribers(IList<Subscription<T>> allSubscriptions, EntityChangeType type, T entity)
	{
		foreach (Subscription<T> subscription in allSubscriptions)
			subscription.HandleChange(new EntityChange<T>(type, entity.Clone()));
	}

	private void RemoveSubscription(Subscription<T> subscription)
	{
		using (Lock.WriterLock())
		{
			var key = (string)subscription.Key;
			ISet<Subscription<T>> subscriptions = _idSubscriptions[key];
			subscriptions.Remove(subscription);
			if (subscriptions.Count == 0)
				_idSubscriptions.Remove(key);
		}
	}

	private void CheckForConcurrencyConflict(T entity)
	{
		if (!Entities.TryGetValue(entity.Id, out T internalEntity))
			throw new ConcurrencyConflictException("The entity does not exist.");

		if (entity.Revision != internalEntity.Revision)
			throw new ConcurrencyConflictException("The entity has been updated.");
	}

	private void GetIdSubscriptions(string id, IList<Subscription<T>> allSubscriptions)
	{
		if (_idSubscriptions.TryGetValue(id, out ISet<Subscription<T>> subscriptions))
			allSubscriptions.AddRange(subscriptions);
	}

	protected virtual void OnBeforeEntityChanged(EntityChangeType type, T entity)
	{
	}

	protected virtual void OnEntityChanged(EntityChangeType type, T oldEntity, T newEntity,
		IList<Subscription<T>> allSubscriptions)
	{
	}
}
