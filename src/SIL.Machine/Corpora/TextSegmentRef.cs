using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;

namespace SIL.Machine.Corpora
{
	public struct TextSegmentRef : IEquatable<TextSegmentRef>, IComparable<TextSegmentRef>, IComparable
	{
		public TextSegmentRef(params string[] keys)
			: this((IEnumerable<string>) keys)
		{
		}

		public TextSegmentRef(IEnumerable<string> keys)
		{
			Keys = keys.ToArray();
		}

		public TextSegmentRef(params int[] keys)
			: this((IEnumerable<int>) keys)
		{
		}

		public TextSegmentRef(IEnumerable<int> keys)
		{
			Keys = keys.Select(i => i.ToString()).ToArray();
		}

		public IReadOnlyList<string> Keys { get; }

		public override bool Equals(object obj)
		{
			return Equals((TextSegmentRef) obj);
		}

		public override int GetHashCode()
		{
			return Keys.GetSequenceHashCode();
		}

		public bool Equals(TextSegmentRef other)
		{
			return Keys.SequenceEqual(other.Keys);
		}

		public int CompareTo(TextSegmentRef other)
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
			if (!(obj is TextSegmentRef))
				throw new ArgumentException("The specified object is not a TextSegmentRef.", nameof(obj));
			return CompareTo((TextSegmentRef) obj);
		}

		public override string ToString()
		{
			return string.Join(".", Keys);
		}
	}
}
