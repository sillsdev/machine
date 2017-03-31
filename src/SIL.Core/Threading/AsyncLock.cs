using System;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Threading
{
	public class AsyncLock
	{
		// the semaphore does not need to be disposed, because it never uses AvailableWaitHandle
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
		private readonly Task<IDisposable> _releaser;

		public AsyncLock()
		{
			_releaser = Task.FromResult((IDisposable) new Releaser(this));
		}

		public IDisposable Wait()
		{
			_semaphore.Wait();
			return _releaser.Result;
		}

		public IDisposable Wait(CancellationToken token)
		{
			_semaphore.Wait(token);
			return _releaser.Result;
		}

		public Task<IDisposable> WaitAsync()
		{
			Task wait = _semaphore.WaitAsync();
			return GetReleaser(wait);
		}

		public Task<IDisposable> WaitAsync(CancellationToken token)
		{
			Task wait = _semaphore.WaitAsync(token);
			return GetReleaser(wait);
		}

		private Task<IDisposable> GetReleaser(Task wait)
		{
			return wait.IsCompleted ? _releaser
				: wait.ContinueWith((_, state) => (IDisposable) state,
					_releaser.Result, CancellationToken.None,
					TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}

		private sealed class Releaser : IDisposable
		{
			private readonly AsyncLock _toRelease;

			internal Releaser(AsyncLock toRelease)
			{
				_toRelease = toRelease;
			}

			public void Dispose()
			{
				_toRelease._semaphore.Release();
			}
		}
	}
}
