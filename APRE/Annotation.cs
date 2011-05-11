using System;

namespace SIL.APRE
{
	public class Annotation<TOffset> : SkipListNode<Annotation<TOffset>>, ICloneable, IComparable<Annotation<TOffset>>, IComparable, IEquatable<Annotation<TOffset>>
	{
		private readonly string _type;
		private readonly Span<TOffset> _span;

		public Annotation(string type, Span<TOffset> span, FeatureStructure fs)
		{
			_type = type;
			_span = span;
			FeatureStructure = fs;
			IsClean = true;
		}

		public Annotation(Annotation<TOffset> ann)
		{
			_type = ann._type;
			_span = ann._span;
			FeatureStructure = (FeatureStructure) ann.FeatureStructure.Clone();
			IsClean = ann.IsClean;
			IsOptional = ann.IsOptional;
		}

		internal Annotation(Span<TOffset> span)
			: this(null, span, null)
		{
		}

		public string Type
		{
			get { return _type; }
		}

		public Span<TOffset> Span
		{
			get { return _span; }
		}

		public FeatureStructure FeatureStructure { get; set; }

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

		public override int GetHashCode()
		{
			return _span.GetHashCode() ^ (_type == null ? 0 : _type.GetHashCode());
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
			return _span.Equals(other._span) && _type == other._type;
		}

		public int CompareTo(Annotation<TOffset> other, Direction dir)
		{
			int res = _span.CompareTo(other._span, dir);
			if (res != 0)
				return res;
			if (_type == null && other._type == null)
				return 0;
			if (_type == null)
				return -1;
			return _type.CompareTo(other._type);
		}

		public int CompareTo(Annotation<TOffset> other)
		{
			return CompareTo(other, Direction.LeftToRight);
		}

		int IComparable.CompareTo(object obj)
		{
			if (!(obj is Annotation<TOffset>))
				throw new ArgumentException();
			return CompareTo((Annotation<TOffset>) obj);
		}

		public override string ToString()
		{
			return string.Format("({0} {1})", _type, _span);
		}
	}
}
