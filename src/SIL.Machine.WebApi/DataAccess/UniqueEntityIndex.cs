using System;
using System.Collections.Generic;
using SIL.Extensions;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess
{
	public class UniqueEntityIndex<T> where T : class, IEntity<T>
	{
		private readonly Dictionary<object, T> _index;
		private readonly Func<T, IEnumerable<object>> _keySelector;
		private readonly Func<T, bool> _filter;

		public UniqueEntityIndex(string name, Func<T, object> keySelector, Func<T, bool> filter = null)
			: this(name, e => keySelector(e).ToEnumerable(), filter)
		{
		}

		public UniqueEntityIndex(string name, Func<T, IEnumerable<object>> keySelector, Func<T, bool> filter = null)
		{
			Name = name;
			_index = new Dictionary<object, T>();
			_keySelector = keySelector;
			_filter = filter;
		}

		public string Name { get; }

		public bool TryGetEntity(object key, out T entity)
		{
			if (_index.TryGetValue(key, out T e))
			{
				entity = e.Clone();
				return true;
			}

			entity = null;
			return false;
		}

		public void CheckKeyConflict(T entity)
		{
			if (_filter != null && !_filter(entity))
				return;

			foreach (object key in _keySelector(entity))
			{
				if (_index.TryGetValue(key, out T otherEntity))
				{
					if (entity.Id != otherEntity.Id)
					{
						throw new KeyAlreadyExistsException("An entity with the same key already exists.")
						{
							IndexName = Name,
							Entity = otherEntity
						};
					}
				}
			}
		}

		public void EntityUpdated(T entity)
		{
			if (_filter != null && !_filter(entity))
				return;

			foreach (object key in _keySelector(entity))
				_index[key] = entity;
		}

		public void EntityDeleted(T entity)
		{
			if (_filter != null && !_filter(entity))
				return;

			foreach (object key in _keySelector(entity))
				_index.Remove(key);
		}
	}
}

