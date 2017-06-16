using System;
using System.Threading;

namespace SIL.Threading
{
	/// <summary>
	/// Provides helper types for <see cref="SynchronizationContext"/>.
	/// </summary>
	public static class SynchronizationContextHelpers
	{
		/// <summary>
		/// Retrieves the current synchronization context, or the default synchronization context if there is no current synchronization context.
		/// </summary>
		public static SynchronizationContext CurrentOrDefault
		{
			get
			{
				return SynchronizationContext.Current ?? new SynchronizationContext();
			}
		}

		/// <summary>
		/// Utility class for temporarily switching <see cref="SynchronizationContext"/> implementations.
		/// </summary>
		public sealed class SynchronizationContextSwitcher : IDisposable
		{
			/// <summary>
			/// The previous <see cref="SynchronizationContext"/>.
			/// </summary>
			private readonly SynchronizationContext _oldContext;
			
			/// <summary>
			/// Whether this object has already been disposed.
			/// </summary>
			private bool _disposed;

			/// <summary>
			/// Initializes a new instance of the <see cref="SynchronizationContextSwitcher"/> class, installing the new <see cref="SynchronizationContext"/>.
			/// </summary>
			/// <param name="newContext">The new <see cref="SynchronizationContext"/>.</param>
			public SynchronizationContextSwitcher(SynchronizationContext newContext)
			{
				_oldContext = SynchronizationContext.Current;
				SynchronizationContext.SetSynchronizationContext(newContext);
			}

			/// <summary>
			/// Restores the old <see cref="SynchronizationContext"/>.
			/// </summary>
			void IDisposable.Dispose()
			{
				if (_disposed)
					return;
				SynchronizationContext.SetSynchronizationContext(_oldContext);
				_disposed = true;
			}
		}
	}
}
