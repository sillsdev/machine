using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;

namespace SIL.Machine.Corpora
{
	public struct RowRef : IEquatable<RowRef>, IComparable<RowRef>, IComparable
	{
		public RowRef(params string[] keys)
			: this((IEnumerable<string>)keys)
		{
		}

		public RowRef(IEnumerable<string> keys)
		{
			Keys = keys.ToArray();
		}

		public RowRef(params int[] keys)
			: this((IEnumerable<int>)keys)
		{
		}

		public RowRef(IEnumerable<int> keys)
		{
			Keys = keys.Select(i => i.ToString()).ToArray();
		}

		public IReadOnlyList<string> Keys { get; }

		public override bool Equals(object obj)
		{
			return Equals((RowRef)obj);
		}

		public override int GetHashCode()
		{
			return Keys.GetSequenceHashCode();
		}

		public bool Equals(RowRef other)
		{
			return Keys.SequenceEqual(other.Keys);
		}

		public int CompareTo(RowRef other)
		{
			for (int i = 0; i < Keys.Count && i < other.Keys.Count; i++)
			{
				string key = Keys[i];
				string otherKey = other.Keys[i];
				if (key != otherKey)
				{
					// if both keys are numbers, compare numerically
					if (int.TryParse(key, out int intKey) && int.TryParse(otherKey, out int intOtherKey))
						return intKey.CompareTo(intOtherKey);
					return key.CompareTo(otherKey);
				}
			}
			return Keys.Count.CompareTo(other.Keys.Count);
		}

		public int CompareTo(object obj)
		{
			if (!(obj is RowRef))
				throw new ArgumentException("The specified object is not a TextSegmentRef.", nameof(obj));
			return CompareTo((RowRef)obj);
		}

		public override string ToString()
		{
			return string.Join(".", Keys);
		}
	}
}
