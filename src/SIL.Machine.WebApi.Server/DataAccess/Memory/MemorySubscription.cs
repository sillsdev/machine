using System;
using System.Collections.Generic;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Utils;

namespace SIL.Machine.WebApi.Server.DataAccess.Memory
{
	internal class MemorySubscription<TKey, TEntity> : Subscription<TKey, TEntity>
		where TEntity : class, IEntity<TEntity>
	{
		private readonly AsyncReaderWriterLock _repoLock;

		public MemorySubscription(AsyncReaderWriterLock repoLock,
			IDictionary<TKey, ISet<Action<EntityChange<TEntity>>>> changeListeners, TKey key,
			Action<EntityChange<TEntity>> listener) : base(changeListeners, key, listener)
		{
			_repoLock = repoLock;
		}

		protected override void RemoveListener()
		{
			using (_repoLock.WriterLock())
				base.RemoveListener();
		}
	}
}
