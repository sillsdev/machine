namespace SIL.Machine.WebApi.Services;

[TestFixture]
public class DistributedReaderWriterLockTests
{
	[Test]
	public async Task ReaderLockAsync_NoLockAcquired()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock(locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		RWLock entity;
		await using (await rwLock.ReaderLockAsync())
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading, Is.True);
			Assert.That(entity.IsAvailableForWriting, Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading, Is.True);
		Assert.That(entity.IsAvailableForWriting, Is.True);
	}

	[Test]
	public async Task ReaderLockAsync_ReaderLockAcquired()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock(locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		RWLock entity;
		await using (await rwLock.ReaderLockAsync())
		{
			await using (await rwLock.ReaderLockAsync())
			{
				entity = locks.Get("test");
				Assert.That(entity.IsAvailableForReading, Is.True);
				Assert.That(entity.IsAvailableForWriting, Is.False);
			}
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading, Is.True);
		Assert.That(entity.IsAvailableForWriting, Is.True);
	}

	[Test]
	public async Task ReaderLockAsync_WriterLockAcquiredAndNotReleased()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock(locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		await using (await rwLock.WriterLockAsync())
		{
			var task = rwLock.ReaderLockAsync().AsTask();
			await AssertNeverCompletesAsync(task);
		}

		RWLock entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading, Is.True);
		Assert.That(entity.IsAvailableForWriting, Is.True);
	}

	[Test]
	public async Task ReaderLockAsync_WriterLockAcquiredAndReleased()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock(locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		Task<IAsyncDisposable> task;
		await using (await rwLock.WriterLockAsync())
		{
			task = rwLock.ReaderLockAsync().AsTask();
			Assert.That(task.IsCompleted, Is.False);
		}

		RWLock entity;
		await using (await task)
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading, Is.True);
			Assert.That(entity.IsAvailableForWriting, Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading, Is.True);
		Assert.That(entity.IsAvailableForWriting, Is.True);
	}

	[Test]
	public async Task ReaderLockAsync_WriterLockAcquiredAndExpired()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock(locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		RWLock entity;
		await using (await rwLock.WriterLockAsync(TimeSpan.FromMilliseconds(400)))
		{
			var task = rwLock.ReaderLockAsync().AsTask();
			await Task.Delay(500);
			await using (await task)
			{
				entity = locks.Get("test");
				Assert.That(entity.IsAvailableForReading, Is.True);
				Assert.That(entity.IsAvailableForWriting, Is.False);
			}
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading, Is.True);
		Assert.That(entity.IsAvailableForWriting, Is.True);
	}

	[Test]
	public async Task ReaderLockAsync_Cancelled()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock(locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		Task<IAsyncDisposable> task;
		await using (await rwLock.WriterLockAsync())
		{
			var cts = new CancellationTokenSource();
			task = rwLock.ReaderLockAsync(cancellationToken: cts.Token).AsTask();
			cts.Cancel();
			Assert.CatchAsync<OperationCanceledException>(async () => await task);
		}

		RWLock entity;
		await using (await rwLock.ReaderLockAsync())
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading, Is.True);
			Assert.That(entity.IsAvailableForWriting, Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading, Is.True);
		Assert.That(entity.IsAvailableForWriting, Is.True);
	}

	[Test]
	public async Task WriterLockAsync_NoLockAcquired()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock(locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		RWLock entity;
		await using (await rwLock.WriterLockAsync())
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading, Is.False);
			Assert.That(entity.IsAvailableForWriting, Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading, Is.True);
		Assert.That(entity.IsAvailableForWriting, Is.True);
	}

	[Test]
	public async Task WriterLockAsync_ReaderLockAcquiredAndNotReleased()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock(locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		await using (await rwLock.ReaderLockAsync())
		{
			var task = rwLock.WriterLockAsync().AsTask();
			await AssertNeverCompletesAsync(task);
		}

		RWLock entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading, Is.True);
		Assert.That(entity.IsAvailableForWriting, Is.True);
	}

	[Test]
	public async Task WriterLockAsync_ReaderLockAcquiredAndReleased()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock(locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		Task<IAsyncDisposable> task;
		await using (await rwLock.ReaderLockAsync())
		{
			task = rwLock.WriterLockAsync().AsTask();
			Assert.That(task.IsCompleted, Is.False);
		}

		RWLock entity;
		await using (await task)
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading, Is.False);
			Assert.That(entity.IsAvailableForWriting, Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading, Is.True);
		Assert.That(entity.IsAvailableForWriting, Is.True);
	}

	[Test]
	public async Task WriterLockAsync_WriterLockAcquiredAndNeverReleased()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock(locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		RWLock entity;
		await using (await rwLock.WriterLockAsync())
		{
			var task = rwLock.WriterLockAsync().AsTask();
			await AssertNeverCompletesAsync(task);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading, Is.True);
		Assert.That(entity.IsAvailableForWriting, Is.True);
	}

	[Test]
	public async Task WriterLockAsync_WriterLockAcquiredAndReleased()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock(locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		Task<IAsyncDisposable> task;
		await using (await rwLock.WriterLockAsync())
		{
			task = rwLock.WriterLockAsync().AsTask();
			Assert.That(task.IsCompleted, Is.False);
		}

		RWLock entity;
		await using (await task)
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading, Is.False);
			Assert.That(entity.IsAvailableForWriting, Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading, Is.True);
		Assert.That(entity.IsAvailableForWriting, Is.True);
	}

	[Test]
	public async Task WriterLockAsync_WriterLockAcquiredAndExpired()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock(locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		RWLock entity;
		await using (await rwLock.WriterLockAsync(TimeSpan.FromMilliseconds(400)))
		{
			var task = rwLock.WriterLockAsync().AsTask();
			await Task.Delay(500);
			await using (await task)
			{
				entity = locks.Get("test");
				Assert.That(entity.IsAvailableForReading, Is.False);
				Assert.That(entity.IsAvailableForWriting, Is.False);
			}
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading, Is.True);
		Assert.That(entity.IsAvailableForWriting, Is.True);
	}

	[Test]
	public async Task WriterLockAsync_Cancelled()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock(locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		Task<IAsyncDisposable> task;
		await using (await rwLock.WriterLockAsync())
		{
			var cts = new CancellationTokenSource();
			task = rwLock.WriterLockAsync(cancellationToken: cts.Token).AsTask();
			cts.Cancel();
			Assert.CatchAsync<OperationCanceledException>(async () => await task);
		}

		RWLock entity;
		await using (await rwLock.WriterLockAsync())
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading, Is.False);
			Assert.That(entity.IsAvailableForWriting, Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading, Is.True);
		Assert.That(entity.IsAvailableForWriting, Is.True);
	}

	public static async Task AssertNeverCompletesAsync(Task task, int timeout = 500)
	{
		if (task.IsCompleted)
			Assert.Fail("Task completed unexpectedly.");
		Task completedTask = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
		if (completedTask == task)
			Assert.Fail("Task completed unexpectedly.");
		Task _ = task.ContinueWith(_ => Assert.Fail("Task completed unexpectedly."), TaskScheduler.Default);
	}
}
