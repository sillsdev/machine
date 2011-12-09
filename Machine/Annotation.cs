using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine
{
	public class Annotation<TOffset> : SkipListNode<Annotation<TOffset>>, ICloneable<Annotation<TOffset>>, IComparable<Annotation<TOffset>>, IComparable
	{
		private readonly Span<TOffset> _span;

		public Annotation(Span<TOffset> span, FeatureStruct fs)
			: this(null, span, fs)
		{
		}

		public Annotation(string type, Span<TOffset> span, FeatureStruct fs)
		{
			_span = span;
			FeatureStruct = fs;
			if (!string.IsNullOrEmpty(type))
				FeatureStruct.AddValue(AnnotationFeatureSystem.Type, type);
			ListID = -1;
		}

		internal Annotation(Span<TOffset> span)
			: this(null, span, null)
		{
		}

		public Annotation(Annotation<TOffset> ann)
		{
			_span = ann._span;
			FeatureStruct = ann.FeatureStruct.Clone();
			Optional = ann.Optional;
		}

		public string Type
		{
			get
			{
				StringFeatureValue sfv;
				if (FeatureStruct.TryGetValue(AnnotationFeatureSystem.Type, out sfv))
					return (string) sfv;
				return null;
			}
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
		public bool Optional { get; set; }

		internal int ListID { get; set; }

		public Annotation<TOffset> Clone()
		{
			return new Annotation<TOffset>(this);
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
