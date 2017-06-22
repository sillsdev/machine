using System;
using System.Collections.Generic;
using SIL.Machine.WebApi.Server.Models;
using SIL.ObjectModel;
using SIL.Threading;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	internal class Subscription<TKey, TEntity> : DisposableBase where TEntity : class, IEntity<TEntity>
	{
		private readonly AsyncReaderWriterLock _repoLock;
		private readonly IDictionary<TKey, Action<EntityChange<TEntity>>> _changeListeners;
		private readonly TKey _key;

		public Subscription(AsyncReaderWriterLock repoLock, IDictionary<TKey, Action<EntityChange<TEntity>>> changeListeners,
			TKey key)
		{
			_repoLock = repoLock;
			_changeListeners = changeListeners;
			_key = key;
		}

		protected override void DisposeManagedResources()
		{
			using (_repoLock.WriterLock())
				_changeListeners.Remove(_key);
		}
	}
}