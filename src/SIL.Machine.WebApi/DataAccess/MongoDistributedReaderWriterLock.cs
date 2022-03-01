namespace SIL.Machine.WebApi.Utils;

public class Lock
{
	public string Id { get; set; } = default!;
	public DateTime? ExpiresAt { get; set; }
	public bool IsAcquired { get; set; }
}

public class RWLock
{
	public string Id { get; set; } = default!;
	public int Revision { get; set; }
	public Lock WriterLock { get; set; } = new Lock();
	public List<Lock> ReaderLocks { get; set; } = new List<Lock>();
	public int ReaderCount { get; set; }

	public bool IsAvailableForReading
	{
		get
		{
			var now = DateTime.UtcNow;
			return !WriterLock.IsAcquired || WriterLock.ExpiresAt <= now;
		}
	}

	public bool IsAvailableForWriting
	{
		get
		{
			var now = DateTime.UtcNow;
			return (!WriterLock.IsAcquired || WriterLock.ExpiresAt <= now)
				&& (ReaderCount == 0 || !ReaderLocks.Any(l => l.ExpiresAt > now));
		}
	}
}

public class ReleaseSignal
{
	public string LockRef { get; set; } = default!;
	public int Revision { get; set; }
}

public class MongoDistributedReaderWriterLock : IDistributedReaderWriterLock
{
	private readonly IMongoCollection<RWLock> _locks;
	private readonly IMongoCollection<ReleaseSignal> _signals;
	private readonly string _name;

	public MongoDistributedReaderWriterLock(IMongoCollection<RWLock> locks,
		IMongoCollection<ReleaseSignal> signals, string name)
	{
		_locks = locks;
		_signals = signals;
		_name = name;
	}

	public async ValueTask<IAsyncDisposable> ReaderLockAsync(TimeSpan? timeout = default, TimeSpan? lifetime = default,
		CancellationToken cancellationToken = default)
	{
		string lockId = ObjectId.GenerateNewId().ToString();
		while (!await TryAcquireReaderLock(lockId, lifetime, cancellationToken))
		{
			RWLock? rwl = await _locks.AsQueryable().FirstOrDefaultAsync(rwl => rwl.Id == _name,
				cancellationToken);
			if (rwl is not null && !rwl.IsAvailableForReading && !await WaitSignalAsync(rwl.Revision, timeout))
			{
				if (await TryAcquireReaderLock(lockId, lifetime, cancellationToken))
					return new Releaser(this, isWriter: false, lockId);
				throw new TimeoutException();
			}
		}
		return new Releaser(this, isWriter: false, lockId);
	}

	public async ValueTask<IAsyncDisposable> WriterLockAsync(TimeSpan? timeout = default, TimeSpan? lifetime = default,
		CancellationToken cancellationToken = default)
	{
		string lockId = ObjectId.GenerateNewId().ToString();
		while (!await TryAcquireWriterLock(lockId, lifetime, cancellationToken))
		{
			RWLock? rwl = await _locks.AsQueryable().FirstOrDefaultAsync(rwl => rwl.Id == _name,
				cancellationToken);
			if (rwl is not null && !rwl.IsAvailableForWriting && !await WaitSignalAsync(rwl.Revision, timeout))
			{
				if (await TryAcquireWriterLock(lockId, lifetime, cancellationToken))
					return new Releaser(this, isWriter: true, lockId);
				throw new TimeoutException();
			}
		}
		return new Releaser(this, isWriter: true, lockId);
	}

	private async Task<bool> TryAcquireWriterLock(string lockId, TimeSpan? lifetime,
		CancellationToken cancellationToken)
	{
		try
		{
			var now = DateTime.UtcNow;
			Expression<Func<RWLock, bool>> filter = rwl => rwl.Id == _name
				&& (!rwl.WriterLock.IsAcquired || rwl.WriterLock.ExpiresAt <= now)
				&& (rwl.ReaderCount == 0 || !rwl.ReaderLocks.Any(l => l.ExpiresAt > now));
			var update = Builders<RWLock>.Update
				.SetOnInsert(rwl => rwl.Id, _name)
				.Set(rwl => rwl.WriterLock.IsAcquired, true)
				.Set(rwl => rwl.WriterLock.Id, lockId)
				.Inc(rwl => rwl.Revision, 1);
			if (lifetime.HasValue)
				update.Set(rwl => rwl.WriterLock.ExpiresAt, now + lifetime);
			UpdateResult result = await _locks.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true },
				cancellationToken);
			return result.IsAcknowledged;
		}
		catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
		{
			return false;
		}
	}

	private async Task<bool> TryAcquireReaderLock(string lockId, TimeSpan? lifetime,
		CancellationToken cancellationToken)
	{
		try
		{
			var now = DateTime.UtcNow;
			Expression<Func<RWLock, bool>> filter = rwl => rwl.Id == _name
				&& (!rwl.WriterLock.IsAcquired || rwl.WriterLock.ExpiresAt <= now);
			var readerLock = new Lock
			{
				Id = lockId,
				ExpiresAt = lifetime is null ? null : now + lifetime,
				IsAcquired = true
			};
			var update = Builders<RWLock>.Update
				.SetOnInsert(rwl => rwl.Id, _name)
				.Inc(rwl => rwl.ReaderCount, 1)
				.Push(rwl => rwl.ReaderLocks, readerLock)
				.Inc(rwl => rwl.Revision, 1);
			UpdateResult result = await _locks.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true },
				cancellationToken);
			return result.IsAcknowledged;
		}
		catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
		{
			return false;
		}
	}

	private async Task<bool> WaitSignalAsync(int revision, TimeSpan? timeout)
	{
		using IAsyncCursor<ReleaseSignal> cursor = await _signals.Find(x => x.LockRef == _name && x.Revision > revision,
			new FindOptions { MaxAwaitTime = timeout, CursorType = CursorType.TailableAwait }).ToCursorAsync();
		DateTime started = DateTime.UtcNow;

		while (await cursor.MoveNextAsync())
		{
			if (cursor.Current.Any())
				return true;
			if (timeout.HasValue && DateTime.UtcNow - started >= timeout)
				return false;
		}

		return false;
	}

	private class Releaser : AsyncDisposableBase
	{
		private readonly MongoDistributedReaderWriterLock _distributedLock;
		private readonly bool _isWriter;
		private readonly string _lockId;

		public Releaser(MongoDistributedReaderWriterLock distributedLock, bool isWriter, string lockId)
		{
			_distributedLock = distributedLock;
			_isWriter = isWriter;
			_lockId = lockId;
		}

		protected override async ValueTask DisposeAsyncCore()
		{
			Expression<Func<RWLock, bool>> filter;
			UpdateDefinition<RWLock> update;
			if (_isWriter)
			{
				filter = rwl => rwl.Id == _distributedLock._name && rwl.WriterLock.Id == _lockId;
				update = Builders<RWLock>.Update
					.Set(rwl => rwl.WriterLock.IsAcquired, false)
					.Inc(rwl => rwl.Revision, 1);
			}
			else
			{
				filter = rwl => rwl.Id == _distributedLock._name && rwl.ReaderLocks.Any(l => l.Id == _lockId);
				update = Builders<RWLock>.Update
					.PullFilter(rwl => rwl.ReaderLocks, l => l.Id == _lockId)
					.Inc(rwl => rwl.ReaderCount, -1)
					.Inc(rwl => rwl.Revision, 1);
			}

			RWLock? rwLock = await _distributedLock._locks.FindOneAndUpdateAsync(filter, update,
				new FindOneAndUpdateOptions<RWLock> { ReturnDocument = ReturnDocument.After });
			if (rwLock is not null)
			{
				await _distributedLock._signals.InsertOneAsync(new ReleaseSignal
				{
					LockRef = _distributedLock._name,
					Revision = rwLock.Revision
				});
			}
		}
	}
}
