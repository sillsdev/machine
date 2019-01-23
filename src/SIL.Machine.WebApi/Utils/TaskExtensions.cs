using System;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.WebApi.Utils
{
	public static class TaskExtensions
	{
		public static async Task<TResult> Timeout<TResult>(this Task<TResult> task, TimeSpan timeout,
			CancellationToken ct)
		{
			Task<TResult> t = await Task.WhenAny(task, Delay<TResult>(timeout, ct));
			return await t;
		}

		private static async Task<TResult> Delay<TResult>(TimeSpan timeout, CancellationToken ct)
		{
			await Task.Delay(timeout, ct);
			return default(TResult);
		}
	}
}
