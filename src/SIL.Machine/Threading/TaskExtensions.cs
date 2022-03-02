using System;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.Threading
{
	public static class TaskExtensions
	{
		public static async Task<TResult> Timeout<TResult>(this Task<TResult> task, TimeSpan timeout,
			CancellationToken cancellationToken)
		{
			Task<TResult> t = await Task.WhenAny(task, Delay<TResult>(timeout, cancellationToken));
			return await t;
		}

		public static async Task Timeout(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
		{
			if (timeout == System.Threading.Timeout.InfiniteTimeSpan)
				await task;
			Task t = await Task.WhenAny(task, Task.Delay(timeout, cancellationToken));
			await t;
		}

		private static async Task<TResult> Delay<TResult>(TimeSpan timeout, CancellationToken cancellationToken)
		{
			if (timeout != System.Threading.Timeout.InfiniteTimeSpan)
				await Task.Delay(timeout, cancellationToken);
			return default;
		}
	}
}
