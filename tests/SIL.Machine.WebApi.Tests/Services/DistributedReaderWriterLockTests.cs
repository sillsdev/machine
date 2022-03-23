namespace SIL.Machine.WebApi.Services;

[TestFixture]
public class DistributedReaderWriterLockTests
{
	[Test]
	public async Task ReaderLockAsync_NoLockAcquired()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		RWLock entity;
		await using (await rwLock.ReaderLockAsync())
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading(), Is.True);
			Assert.That(entity.IsAvailableForWriting(), Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading(), Is.True);
		Assert.That(entity.IsAvailableForWriting(), Is.True);
	}

	[Test]
	public async Task ReaderLockAsync_ReaderLockAcquired()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		RWLock entity;
		await using (await rwLock.ReaderLockAsync())
		{
			await using (await rwLock.ReaderLockAsync())
			{
				entity = locks.Get("test");
				Assert.That(entity.IsAvailableForReading(), Is.True);
				Assert.That(entity.IsAvailableForWriting(), Is.False);
			}
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading(), Is.True);
		Assert.That(entity.IsAvailableForWriting(), Is.True);
	}

	[Test]
	public async Task ReaderLockAsync_WriterLockAcquiredAndNotReleased()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		await rwLock.WriterLockAsync();
		var task = rwLock.ReaderLockAsync();
		await AssertNeverCompletesAsync(task);
	}

	[Test]
	public async Task ReaderLockAsync_WriterLockAcquiredAndReleased()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		Task<IAsyncDisposable> task;
		await using (await rwLock.WriterLockAsync())
		{
			task = rwLock.ReaderLockAsync();
			Assert.That(task.IsCompleted, Is.False);
		}

		RWLock entity;
		await using (await task)
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading(), Is.True);
			Assert.That(entity.IsAvailableForWriting(), Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading(), Is.True);
		Assert.That(entity.IsAvailableForWriting(), Is.True);
	}

	[Test]
	public async Task ReaderLockAsync_WriterLockAcquiredAndExpired()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		RWLock entity;
		await using (await rwLock.WriterLockAsync(TimeSpan.FromMilliseconds(400)))
		{
			var task = rwLock.ReaderLockAsync();
			await Task.Delay(500);
			await using (await task)
			{
				entity = locks.Get("test");
				Assert.That(entity.IsAvailableForReading(), Is.True);
				Assert.That(entity.IsAvailableForWriting(), Is.False);
			}
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading(), Is.True);
		Assert.That(entity.IsAvailableForWriting(), Is.True);
	}

	[Test]
	public async Task ReaderLockAsync_Cancelled()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		Task<IAsyncDisposable> task;
		await using (await rwLock.WriterLockAsync())
		{
			var cts = new CancellationTokenSource();
			task = rwLock.ReaderLockAsync(cancellationToken: cts.Token);
			cts.Cancel();
			Assert.CatchAsync<OperationCanceledException>(async () => await task);
		}

		RWLock entity;
		await using (await rwLock.ReaderLockAsync())
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading(), Is.True);
			Assert.That(entity.IsAvailableForWriting(), Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading(), Is.True);
		Assert.That(entity.IsAvailableForWriting(), Is.True);
	}

	[Test]
	public async Task WriterLockAsync_NoLockAcquired()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		RWLock entity;
		await using (await rwLock.WriterLockAsync())
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading(), Is.False);
			Assert.That(entity.IsAvailableForWriting(), Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading(), Is.True);
		Assert.That(entity.IsAvailableForWriting(), Is.True);
	}

	[Test]
	public async Task WriterLockAsync_ReaderLockAcquiredAndNotReleased()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		await rwLock.ReaderLockAsync();
		var task = rwLock.WriterLockAsync();
		await AssertNeverCompletesAsync(task);
	}

	[Test]
	public async Task WriterLockAsync_ReaderLockAcquiredAndReleased()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		Task<IAsyncDisposable> task;
		await using (await rwLock.ReaderLockAsync())
		{
			task = rwLock.WriterLockAsync();
			Assert.That(task.IsCompleted, Is.False);
		}

		RWLock entity;
		await using (await task)
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading(), Is.False);
			Assert.That(entity.IsAvailableForWriting(), Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading(), Is.True);
		Assert.That(entity.IsAvailableForWriting(), Is.True);
	}

	[Test]
	public async Task WriterLockAsync_WriterLockAcquiredAndNeverReleased()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		await rwLock.WriterLockAsync();
		var task = rwLock.WriterLockAsync();
		await AssertNeverCompletesAsync(task);
	}

	[Test]
	public async Task WriterLockAsync_WriterLockAcquiredAndReleased()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		Task<IAsyncDisposable> task;
		await using (await rwLock.WriterLockAsync())
		{
			task = rwLock.WriterLockAsync();
			Assert.That(task.IsCompleted, Is.False);
		}

		RWLock entity;
		await using (await task)
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading(), Is.False);
			Assert.That(entity.IsAvailableForWriting(), Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading(), Is.True);
		Assert.That(entity.IsAvailableForWriting(), Is.True);
	}

	[Test]
	public async Task WriterLockAsync_WriterLockTakesPriorityOverReaderLock()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		Task<IAsyncDisposable> writeTask, readTask;
		await using (await rwLock.WriterLockAsync())
		{
			readTask = rwLock.ReaderLockAsync();
			Assert.That(readTask.IsCompleted, Is.False);
			writeTask = rwLock.WriterLockAsync();
			Assert.That(writeTask.IsCompleted, Is.False);
		}

		await writeTask;
		await AssertNeverCompletesAsync(readTask);
	}

	[Test]
	public async Task WriterLockAsync_FirstWriterLockHasPriority()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		Task<IAsyncDisposable> task1, task2;
		await using (await rwLock.WriterLockAsync())
		{
			task1 = rwLock.WriterLockAsync();
			Assert.That(task1.IsCompleted, Is.False);
			task2 = rwLock.WriterLockAsync();
			Assert.That(task2.IsCompleted, Is.False);
		}

		await task1;
		await AssertNeverCompletesAsync(task2);
	}

	[Test]
	public async Task WriterLockAsync_WriterLockAcquiredAndExpired()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		RWLock entity;
		await using (await rwLock.WriterLockAsync(TimeSpan.FromMilliseconds(400)))
		{
			var task = rwLock.WriterLockAsync();
			await Task.Delay(500);
			await using (await task)
			{
				entity = locks.Get("test");
				Assert.That(entity.IsAvailableForReading(), Is.False);
				Assert.That(entity.IsAvailableForWriting(), Is.False);
			}
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading(), Is.True);
		Assert.That(entity.IsAvailableForWriting(), Is.True);
	}

	[Test]
	public async Task WriterLockAsync_Cancelled()
	{
		var locks = new MemoryRepository<RWLock>();
		var rwLock = new DistributedReaderWriterLock("host", locks, "test");

		Assert.That(locks.Contains("test"), Is.False);

		Task<IAsyncDisposable> task;
		await using (await rwLock.WriterLockAsync())
		{
			var cts = new CancellationTokenSource();
			task = rwLock.WriterLockAsync(cancellationToken: cts.Token);
			cts.Cancel();
			Assert.CatchAsync<OperationCanceledException>(async () => await task);
		}

		RWLock entity;
		await using (await rwLock.WriterLockAsync())
		{
			entity = locks.Get("test");
			Assert.That(entity.IsAvailableForReading(), Is.False);
			Assert.That(entity.IsAvailableForWriting(), Is.False);
		}

		entity = locks.Get("test");
		Assert.That(entity.IsAvailableForReading(), Is.True);
		Assert.That(entity.IsAvailableForWriting(), Is.True);
	}

	public static async Task AssertNeverCompletesAsync(Task task, int timeout = 100)
	{
		if (task.IsCompleted)
			Assert.Fail("Task completed unexpectedly.");
		Task completedTask = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
		if (completedTask == task)
			Assert.Fail("Task completed unexpectedly.");
		var _ = task.ContinueWith(_ => Assert.Fail("Task completed unexpectedly."), TaskScheduler.Default);
	}
}
