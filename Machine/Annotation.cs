using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine
{
	public class Annotation<TOffset> : BidirListNode<Annotation<TOffset>>, IBidirTreeNode<Annotation<TOffset>>, ICloneable<Annotation<TOffset>>, IComparable<Annotation<TOffset>>, IComparable
	{
		private readonly AnnotationList<TOffset> _children;
		private readonly Span<TOffset> _span;

		public Annotation(Span<TOffset> span, FeatureStruct fs)
		{
			_span = span;
			FeatureStruct = fs;
			ListID = -1;
			_children = new AnnotationList<TOffset>(span.SpanFactory, this);
			Depth = -1;
		}

		internal Annotation(Span<TOffset> span)
			: this(span, null)
		{
		}

		public Annotation(Annotation<TOffset> ann)
		{
			_span = ann._span;
			FeatureStruct = ann.FeatureStruct.Clone();
			Optional = ann.Optional;
		}

		public Annotation<TOffset> Parent { get; private set; }

		public int Depth { get; private set; }

		public AnnotationList<TOffset> Children
		{
			get { return _children; }
		}

		IBidirList<Annotation<TOffset>> IBidirTreeNode<Annotation<TOffset>>.Children
		{
			get { return _children; }
		}

		protected internal override void Clear()
		{
			base.Clear();
			Parent = null;
			Depth = -1;
		}

		protected internal override void Init(BidirList<Annotation<TOffset>> list, bool singleState)
		{
			base.Init(list, singleState);
			Parent = ((AnnotationList<TOffset>) list).Parent;
			Depth = Parent == null ? 0 : Parent.Depth + 1;
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

		public bool Remove(bool preserveChildren)
		{
			if (List == null)
				return false;

			return ((AnnotationList<TOffset>) List).Remove(this, preserveChildren);
		}

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
