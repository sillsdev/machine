using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace SIL.APRE
{
	public class IDBearerSet<T> : KeyedCollection<string, T> where T : IIDBearer
	{
		public IDBearerSet()
		{
		}

		public IDBearerSet(IEnumerable<T> items)
		{
			AddMany(items);
		}

		public void AddMany(IEnumerable<T> items)
		{
			foreach (T item in items)
				Add(item);
		}

		public bool TryGetValue(string id, out T value)
		{
			if (Dictionary == null)
			{
				value = default(T);
				return false;
			}

			return Dictionary.TryGetValue(id, out value);
		}

		public IDBearerSet<T> Intersection(IEnumerable<T> items)
		{
			var result = new IDBearerSet<T>();
			foreach (T item in items)
			{
				if (Contains(item))
					result.Add(item);
			}
			return result;
		}

		public IDBearerSet<T> Union(IEnumerable<T> items)
		{
			var result = new IDBearerSet<T>(this);
			result.AddMany(items);
			return result;
		}

		public IDBearerSet<T> Difference(IEnumerable<T> items)
		{
			var result = new IDBearerSet<T>();
			foreach (T item in items)
			{
				if (!Contains(item))
					result.Add(item);
			}
			return result;
		}

		protected override string GetKeyForItem(T item)
		{
			return item.ID;
		}

		protected override void InsertItem(int index, T item)
		{
			if (Contains(item.ID))
			{
				int oldIndex = IndexOf(item);
				Remove(item.ID);
				if (oldIndex < index)
					index--;
			}
			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, T item)
		{
			if (Contains(item.ID))
			{
				int oldIndex = IndexOf(item);
				if (oldIndex != index)
				{
					Remove(item.ID);
					if (oldIndex < index)
						index--;
				}
			}
			base.SetItem(index, item);
		}

		public override string ToString()
		{
			bool firstItem = true;
			var sb = new StringBuilder();
			foreach (T item in this)
			{
				if (!firstItem)
					sb.Append(", ");
				sb.Append(item.Description);
				firstItem = false;
			}
			return sb.ToString();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as IDBearerSet<T>);
		}

		public bool Equals(IDBearerSet<T> other)
		{
			if (other == null)
				return false;

			if (Count != other.Count)
				return false;

			return this.All(other.Contains);
		}

		public override int GetHashCode()
		{
			return this.Aggregate(0, (current, item) => current ^ item.GetHashCode());
		}
	}
}
