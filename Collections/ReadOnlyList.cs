using System;
using System.Collections.Generic;

namespace SIL.Collections
{
	public class ReadOnlyList<T> : SimpleReadOnlyCollection<T>, IReadOnlyList<T>, IList<T>
	{
		private readonly IList<T> _list; 

		public ReadOnlyList(IList<T> list)
			: base(list)
		{
			_list = list;
		}

		public int IndexOf(T item)
		{
			return _list.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			throw new NotSupportedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		T IList<T>.this[int index]
		{
			get { return _list[index]; }
			set { throw new NotSupportedException(); }
		}

		public T this[int index]
		{
			get { return _list[index]; }
		}
	}
}
