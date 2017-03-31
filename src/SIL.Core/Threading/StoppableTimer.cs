using System;
using System.Threading;
using SIL.ObjectModel;

namespace SIL.Threading
{
	public class StoppableTimer : DisposableBase
	{
		private readonly Timer _timer;
		private readonly Action _callback;
		private readonly object _syncObject = new object();
		private bool _running;

		public StoppableTimer(Action callback)
		{
			_callback = callback;
			_timer = new Timer(s => ((StoppableTimer) s).FireTimer(), this, Timeout.Infinite, Timeout.Infinite);
		}

		public void Start(TimeSpan period)
		{
			lock (_syncObject)
			{
				_running = true;
				_timer.Change(period, period);
			}
		}

		private void FireTimer()
		{
			lock (_syncObject)
			{
				if (_running)
					_callback();
			}
		}

		public void Stop()
		{
			lock (_syncObject)
			{
				// FireTimer is *not* running _callback (since we got the lock)
				_timer.Change(Timeout.Infinite, Timeout.Infinite);
				_running = false;
			}
			// Now FireTimer will *never* run _callback
		}

		protected override void DisposeManagedResources()
		{
			Stop();
			_timer.Dispose();
		}
	}
}
