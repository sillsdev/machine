using System;
using System.Collections.Generic;
using SIL.Machine.WebApi.Server.Models;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	internal class Subscription<TKey, TEntity> : DisposableBase where TEntity : class, IEntity<TEntity>
	{
		private readonly IDictionary<TKey, ISet<Action<EntityChange<TEntity>>>> _changeListeners;
		private readonly TKey _key;
		private readonly Action<EntityChange<TEntity>> _listener;

		public Subscription(IDictionary<TKey, ISet<Action<EntityChange<TEntity>>>> changeListeners, TKey key,
			Action<EntityChange<TEntity>> listener)
		{
			_changeListeners = changeListeners;
			_key = key;
			_listener = listener;

			if (!_changeListeners.TryGetValue(_key, out ISet<Action<EntityChange<TEntity>>> listeners))
			{
				listeners = new HashSet<Action<EntityChange<TEntity>>>();
				_changeListeners[_key] = listeners;
			}
			listeners.Add(listener);
		}

		protected override void DisposeManagedResources()
		{
			RemoveListener();
		}

		protected virtual void RemoveListener()
		{
			ISet<Action<EntityChange<TEntity>>> listeners = _changeListeners[_key];
			listeners.Remove(_listener);
			if (listeners.Count == 0)
				_changeListeners.Remove(_key);
		}
	}
}