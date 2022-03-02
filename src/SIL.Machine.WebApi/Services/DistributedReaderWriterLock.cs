namespace SIL.Machine.WebApi.Services;

public class DistributedReaderWriterLock : IDistributedReaderWriterLock
{
	private readonly IRepository<RWLock> _locks;
	private readonly string _id;

	public DistributedReaderWriterLock(IRepository<RWLock> locks, string id)
	{
		_locks = locks;
		_id = id;
	}

	public async ValueTask<IAsyncDisposable> ReaderLockAsync(TimeSpan? lifetime = default,
		CancellationToken cancellationToken = default)
	{
		string lockId = ObjectId.GenerateNewId().ToString();
		if (!await TryAcquireReaderLock(lockId, lifetime, cancellationToken))
		{
			using ISubscription<RWLock> sub = await _locks.SubscribeAsync(rwl => rwl.Id == _id, cancellationToken);
			do
			{
				RWLock? rwLock = sub.Change.Entity;
				if (rwLock is not null && !rwLock.IsAvailableForReading)
				{
					TimeSpan? timeout = default;
					if (rwLock.WriterLock.ExpiresAt is not null)
					{
						timeout = rwLock.WriterLock.ExpiresAt - DateTime.UtcNow;
						if (timeout < TimeSpan.Zero)
							timeout = TimeSpan.Zero;
					}
					if (timeout != TimeSpan.Zero)
						await sub.WaitForUpdateAsync(timeout, cancellationToken);
				}
			} while (!await TryAcquireReaderLock(lockId, lifetime, cancellationToken));
		}
		return new ReaderLockReleaser(this, lockId);
	}

	public async ValueTask<IAsyncDisposable> WriterLockAsync(TimeSpan? lifetime = default,
		CancellationToken cancellationToken = default)
	{
		string lockId = ObjectId.GenerateNewId().ToString();
		if (!await TryAcquireWriterLock(lockId, lifetime, cancellationToken))
		{
			using ISubscription<RWLock> sub = await _locks.SubscribeAsync(rwl => rwl.Id == _id, cancellationToken);
			do
			{
				RWLock? rwLock = sub.Change.Entity;
				if (rwLock is not null && !rwLock.IsAvailableForWriting)
				{
					List<DateTime> dateTimes = rwLock.ReaderLocks.Where(l => l.ExpiresAt.HasValue)
						.Select(l => l.ExpiresAt.GetValueOrDefault()).ToList();
					if (rwLock.WriterLock.ExpiresAt is not null)
						dateTimes.Add(rwLock.WriterLock.ExpiresAt.Value);
					TimeSpan? timeout = default;
					if (dateTimes.Count > 0)
					{
						timeout = dateTimes.Max() - DateTime.UtcNow;
						if (timeout < TimeSpan.Zero)
							timeout = TimeSpan.Zero;
					}
					if (timeout != TimeSpan.Zero)
						await sub.WaitForUpdateAsync(timeout, cancellationToken);
				}
			} while (!await TryAcquireWriterLock(lockId, lifetime, cancellationToken));
		}
		return new WriterLockReleaser(this, lockId);
	}

	private async Task<bool> TryAcquireWriterLock(string lockId, TimeSpan? lifetime,
		CancellationToken cancellationToken)
	{
		try
		{
			var now = DateTime.UtcNow;
			Expression<Func<RWLock, bool>> filter = rwl => rwl.Id == _id
				&& (!rwl.WriterLock.IsAcquired || (rwl.WriterLock.ExpiresAt != null && rwl.WriterLock.ExpiresAt <= now))
				&& (rwl.ReaderCount == 0 || !rwl.ReaderLocks.Any(l => l.ExpiresAt == null || l.ExpiresAt > now));
			void Update(IUpdateBuilder<RWLock> u)
			{
				u.SetOnInsert(rwl => rwl.Id, _id);
				u.Set(rwl => rwl.WriterLock.IsAcquired, true);
				u.Set(rwl => rwl.WriterLock.Id, lockId);
				if (lifetime.HasValue)
					u.Set(rwl => rwl.WriterLock.ExpiresAt, now + lifetime);
				else
					u.Unset(rwl => rwl.WriterLock.ExpiresAt);
			}
			RWLock? rwLock = await _locks.UpdateAsync(filter, Update, upsert: true, cancellationToken);
			return rwLock is not null;
		}
		catch (DuplicateKeyException)
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
			Expression<Func<RWLock, bool>> filter = rwl => rwl.Id == _id
				&& (!rwl.WriterLock.IsAcquired
					|| (rwl.WriterLock.ExpiresAt != null && rwl.WriterLock.ExpiresAt <= now));
			void Update(IUpdateBuilder<RWLock> u)
			{
				u.SetOnInsert(rwl => rwl.Id, _id);
				u.Inc(rwl => rwl.ReaderCount);
				u.Add(rwl => rwl.ReaderLocks, new Lock
				{
					Id = lockId,
					ExpiresAt = lifetime is null ? null : now + lifetime,
					IsAcquired = true
				});
			}

			RWLock? rwLock = await _locks.UpdateAsync(filter, Update, upsert: true, cancellationToken);
			return rwLock is not null;
		}
		catch (DuplicateKeyException)
		{
			return false;
		}
	}

	private class WriterLockReleaser : AsyncDisposableBase
	{
		private readonly DistributedReaderWriterLock _distributedLock;
		private readonly string _lockId;

		public WriterLockReleaser(DistributedReaderWriterLock distributedLock, string lockId)
		{
			_distributedLock = distributedLock;
			_lockId = lockId;
		}

		protected override async ValueTask DisposeAsyncCore()
		{
			Expression<Func<RWLock, bool>> filter = rwl => rwl.Id == _distributedLock._id
				&& rwl.WriterLock.Id == _lockId;
			await _distributedLock._locks.UpdateAsync(filter, u => u
				.Set(rwl => rwl.WriterLock.IsAcquired, false)
				.Unset(rwl => rwl.WriterLock.ExpiresAt));
		}
	}

	private class ReaderLockReleaser : AsyncDisposableBase
	{
		private readonly DistributedReaderWriterLock _distributedLock;
		private readonly string _lockId;

		public ReaderLockReleaser(DistributedReaderWriterLock distributedLock, string lockId)
		{
			_distributedLock = distributedLock;
			_lockId = lockId;
		}

		protected override async ValueTask DisposeAsyncCore()
		{
			Expression<Func<RWLock, bool>> filter = rwl => rwl.Id == _distributedLock._id
			   && rwl.ReaderLocks.Any(l => l.Id == _lockId);
			await _distributedLock._locks.UpdateAsync(filter, u => u
				.Inc(rwl => rwl.ReaderCount, -1)
				.RemoveAll(rwl => rwl.ReaderLocks, l => l.Id == _lockId));
		}
	}
}
