namespace SIL.Machine.WebApi.DataAccess;

public class MongoRepository<T> : IRepository<T> where T : IEntity
{
	private readonly IMongoCollection<T> _collection;
	private readonly Func<IMongoCollection<T>, Task>? _init;
	private readonly IMongoCollection<ChangeEvent>? _changeEvents;

	public MongoRepository(IMongoCollection<T> collection, Func<IMongoCollection<T>, Task>? init = null,
		bool isSubscribable = false)
	{
		_collection = collection;
		_init = init;
		if (isSubscribable)
		{
			string collectionName = _collection.CollectionNamespace.CollectionName;
			_changeEvents = _collection.Database.GetCollection<ChangeEvent>(collectionName + "_log");
		}
	}

	public async Task InitAsync()
	{
		if (_changeEvents is not null)
		{
			string changeEventsName = _changeEvents.CollectionNamespace.CollectionName;
			var filter = new BsonDocument("name", changeEventsName);
			if (!await _changeEvents.Database.ListCollectionNames(new ListCollectionNamesOptions { Filter = filter })
				.AnyAsync())
			{
				await _changeEvents.Database.CreateCollectionAsync(changeEventsName,
					new CreateCollectionOptions { Capped = true, MaxSize = 100 * 1024 });
			}
			await _changeEvents.Indexes.CreateOrUpdateAsync(new CreateIndexModel<ChangeEvent>(
				Builders<ChangeEvent>.IndexKeys.Ascending(ce => ce.EntityRef)));
		}
		if (_init is not null)
			await _init(_collection);
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
		entity.Revision = 1;
		try
		{
			await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
			if (_changeEvents is not null)
			{
				var changeEvent = new ChangeEvent
				{
					EntityRef = entity.Id,
					ChangeType = EntityChangeType.Insert,
					Revision = entity.Revision
				};
				await _changeEvents.InsertOneAsync(changeEvent, cancellationToken: CancellationToken.None);
			}
		}
		catch (MongoWriteException e)
		{
			if (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
				throw new DuplicateKeyException(e);
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
			updateBuilder.Inc(e => e.Revision, 1);
			UpdateDefinition<T> updateDef = updateBuilder.Build();
			T? entity = await _collection.FindOneAndUpdateAsync(filter, updateDef,
				new FindOneAndUpdateOptions<T>
				{
					IsUpsert = upsert,
					ReturnDocument = ReturnDocument.After
				}, cancellationToken);
			if (entity is not null && _changeEvents is not null)
			{
				var changeEvent = new ChangeEvent
				{
					EntityRef = entity.Id,
					ChangeType = EntityChangeType.Update,
					Revision = entity.Revision
				};
				await _changeEvents.InsertOneAsync(changeEvent, cancellationToken: CancellationToken.None);
			}
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
		T? entity = await _collection.FindOneAndDeleteAsync(filter, cancellationToken: cancellationToken);
		if (entity is not null && _changeEvents is not null)
		{
			var changeEvent = new ChangeEvent
			{
				EntityRef = entity.Id,
				ChangeType = EntityChangeType.Delete,
				Revision = entity.Revision + 1
			};
			await _changeEvents.InsertOneAsync(changeEvent, cancellationToken: CancellationToken.None);
		}
		return entity;
	}

	public async Task<int> DeleteAllAsync(Expression<Func<T, bool>> filter,
		CancellationToken cancellationToken = default)
	{
		DeleteResult result = await _collection.DeleteManyAsync(filter, cancellationToken);
		return (int)result.DeletedCount;
	}

	public async Task<ISubscription<T>> SubscribeAsync(Expression<Func<T, bool>> filter,
		CancellationToken cancellationToken = default)
	{
		if (_changeEvents is null)
			throw new NotSupportedException();
		T initialEntity = await _collection.AsQueryable().FirstOrDefaultAsync(filter, cancellationToken);
		var subscription = new MongoSubscription<T>(_collection, _changeEvents, filter.Compile(), initialEntity);
		return subscription;
	}
}
