using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine
{
	public class Annotation<TOffset> : BidirListNode<Annotation<TOffset>>, IBidirTreeNode<Annotation<TOffset>>, ICloneable<Annotation<TOffset>>, IComparable<Annotation<TOffset>>, IComparable
	{
		private AnnotationList<TOffset> _children;
		private readonly Span<TOffset> _span;

		public Annotation(Span<TOffset> span, FeatureStruct fs)
		{
			_span = span;
			FeatureStruct = fs;
			ListID = -1;
			Depth = -1;
		}

		internal Annotation(Span<TOffset> span)
		{
			_span = span;
			ListID = -1;
			Depth = -1;
		}

		public Annotation(Annotation<TOffset> ann)
			: this(ann._span, ann.FeatureStruct.Clone())
		{
			Optional = ann.Optional;
			if (ann._children != null && ann._children.Count > 0)
				Children.AddRange(ann.Children.Clone());
		}

		public Annotation<TOffset> Parent { get; private set; }

		public int Depth { get; private set; }

		public AnnotationList<TOffset> Children
		{
			get
			{
				if (_children == null)
					_children = new AnnotationList<TOffset>(_span.SpanFactory, this);
				return _children;
			}
		}

		IBidirList<Annotation<TOffset>> IBidirTreeNode<Annotation<TOffset>>.Children
		{
			get { return Children; }
		}

		protected internal override void Clear()
		{
			base.Clear();
			Parent = null;
			Depth = -1;
		}

		protected internal override void Init(BidirList<Annotation<TOffset>> list, int levels)
		{
			base.Init(list, levels);
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
			int res = Span.CompareTo(other.Span);
			if (res != 0)
				return res;

			if (ListID == -1 || other.ListID == -1)
				return 0;
			return ListID.CompareTo(other.ListID);
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
