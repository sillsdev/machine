using SIL.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public struct TextSegmentRef : IEquatable<TextSegmentRef>, IComparable<TextSegmentRef>, IComparable
	{
		public TextSegmentRef(params int[] indices)
		{
			Indices = indices;
		}

		public IReadOnlyList<int> Indices { get; }

		public override bool Equals(object obj)
		{
			return Equals((TextSegmentRef) obj);
		}

		public override int GetHashCode()
		{
			return Indices.GetSequenceHashCode();
		}

		public bool Equals(TextSegmentRef other)
		{
			return Indices.SequenceEqual(other.Indices);
		}

		public int CompareTo(TextSegmentRef other)
		{
			for (int i = 0; i < Indices.Count && i < other.Indices.Count; i++)
			{
				int index = Indices[i];
				int otherIndex = other.Indices[i];
				if (index != otherIndex)
					return index.CompareTo(otherIndex);
			}
			return Indices.Count.CompareTo(other.Indices.Count);
		}

		public int CompareTo(object obj)
		{
			if (!(obj is TextSegmentRef))
				throw new ArgumentException("The specified object is not a TextSegmentRef.", nameof(obj));
			return CompareTo((TextSegmentRef) obj);
		}

		public override string ToString()
		{
			return string.Join(".", Indices);
		}
	}
}
