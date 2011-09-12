using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE
{
	public class Annotation<TOffset> : SkipListNode<Annotation<TOffset>>, ICloneable, IEquatable<Annotation<TOffset>>, IComparable<Annotation<TOffset>>, IComparable
	{
		private readonly Span<TOffset> _span;

		public Annotation(Span<TOffset> span, FeatureStruct fs)
		{
			_span = span;
			FeatureStruct = fs;
			IsClean = true;
			ListID = -1;
		}

		internal Annotation(Span<TOffset> span)
			: this(span, null)
		{
		}

		public Annotation(Annotation<TOffset> ann)
		{
			_span = ann._span;
			FeatureStruct = ann.FeatureStruct == null ? null : (FeatureStruct) ann.FeatureStruct.Clone();
			IsClean = ann.IsClean;
			IsOptional = ann.IsOptional;
		}

		public Span<TOffset> Span
		{
			get { return _span; }
		}

		public FeatureStruct FeatureStruct { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this annotation is optional.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this annotation is optional, otherwise <c>false</c>.
		/// </value>
		public bool IsOptional { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is clean. This is used
		/// for phonological rules that apply simultaneously. In order to enforce the disjunctive
		/// nature of the subrules, we do not allow another subrule to apply on segment that
		/// has already been altered by another subrule.
		/// </summary>
		/// <value><c>true</c> if this instance is clean, otherwise <c>false</c>.</value>
		public bool IsClean { get; set; }

		internal int ListID { get; set; }

		public override int GetHashCode()
		{
			return _span.GetHashCode() ^ (FeatureStruct == null ? 0 : FeatureStruct.GetHashCode());
		}

		public Annotation<TOffset> Clone()
		{
			return new Annotation<TOffset>(this);
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Annotation<TOffset>);
		}

		public bool Equals(Annotation<TOffset> other)
		{
			if (other == null)
				return false;

			if (FeatureStruct == null)
			{
				if (other.FeatureStruct != null)
					return false;
			}
			else if (!FeatureStruct.Equals(other.FeatureStruct))
			{
				return false;
			}

			return _span.Equals(other._span);
		}

		public int CompareTo(Annotation<TOffset> other)
		{
			return CompareTo(other, Direction.LeftToRight);
		}

		public int CompareTo(Annotation<TOffset> other, Direction dir)
		{
			int res = Span.CompareTo(other.Span, dir);
			if (res != 0)
				return res;

			res = ListID.CompareTo(other.ListID);
			return dir == Direction.LeftToRight ? res : -res;
		}

		int IComparable.CompareTo(object obj)
		{
			return CompareTo(obj as Annotation<TOffset>);
		}

		public override string ToString()
		{
			return string.Format("({0} {1})", _span, FeatureStruct);
		}
	}
}
