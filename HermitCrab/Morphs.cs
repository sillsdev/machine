using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a left-to-right ordering of morphs in a word.
	/// </summary>
	public class Morphs : KeyedCollection<int, Morph>, ICloneable
	{
		private int _nextPartition;

		/// <summary>
		/// Initializes a new instance of the <see cref="Morphs"/> class.
		/// </summary>
		public Morphs()
		{
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="morphs">The morphs.</param>
		public Morphs(Morphs morphs)
		{
			_nextPartition = morphs._nextPartition;
			foreach (Morph mi in morphs)
				base.InsertItem(Count, mi);
		}

		public bool SameMorphemes(Morphs other)
		{
			if (Count != other.Count)
				return false;

			IEnumerator<Morph> enum1 = GetEnumerator();
			IEnumerator<Morph> enum2 = other.GetEnumerator();
			while (enum1.MoveNext() && enum2.MoveNext())
			{
				if (enum1.Current.Allomorph.Morpheme != enum2.Current.Allomorph.Morpheme)
					return false;
			}
			return true;
		}

		protected override void InsertItem(int index, Morph item)
		{
			if (index < Count)
				throw new NotImplementedException();

			Collection<Morph> morphs = this;
			// check to see if the previous morpheme has the same ID, if so
			// combine the morphemes
			if (index - 1 >= 0 && morphs[index - 1].Allomorph.Morpheme == item.Allomorph.Morpheme
				&& (item.Partition == -1 || Contains(item.Partition) || morphs[index - 1].Partition == item.Partition))
			{
				item.Partition = morphs[index - 1].Partition;
				item.Shape.AddRange(morphs[index - 1].Shape);
				base.SetItem(index - 1, item);
			}
			else
			{

				if (item.Partition == -1 || Contains(item.Partition))
					item.Partition = _nextPartition++;
				base.InsertItem(index, item);
			}
		}

		protected override void SetItem(int index, Morph item)
		{
			throw new NotImplementedException();
		}

		protected override int GetKeyForItem(Morph morph)
		{
			return morph.Partition;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Morphs);
		}

		public bool Equals(Morphs other)
		{
			if (other == null)
				return false;

			if (Count != other.Count)
				return false;

			Collection<Morph> morphs = this;
			Collection<Morph> otherMorphs = other;
			for (int i = 0; i < Count; i++)
			{
				if (!morphs[i].Equals(otherMorphs[i]))
					return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int hashCode = 0;
			foreach (Morph morph in this)
				hashCode ^= morph.GetHashCode();
			return hashCode;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Morphs Clone()
		{
			return new Morphs(this);
		}
	}
}
