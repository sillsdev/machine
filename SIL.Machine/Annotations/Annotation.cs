using System;
using System.Linq;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.Annotations
{
	public class Annotation<TOffset> : BidirListNode<Annotation<TOffset>>, IBidirTreeNode<Annotation<TOffset>>, ICloneable<Annotation<TOffset>>, IComparable<Annotation<TOffset>>, IComparable, IFreezable, IValueEquatable<Annotation<TOffset>>
	{
		private AnnotationList<TOffset> _children;
		private int _hashCode;
		private FeatureStruct _fs;
		private bool _optional;
		private object _data;

		public Annotation(Span<TOffset> span, FeatureStruct fs)
		{
			Span = span;
			FeatureStruct = fs;
			ListID = -1;
			Root = this;
		}

		internal Annotation(Span<TOffset> span)
		{
			Span = span;
			ListID = -1;
			Root = this;
		}

		protected Annotation(Annotation<TOffset> ann)
			: this(ann.Span, ann.FeatureStruct.Clone())
		{
			Optional = ann.Optional;
			_data = ann._data;
			if (ann._children != null && ann._children.Count > 0)
				Children.AddRange(ann.Children.Select(node => node.Clone()));
		}

		public object Data
		{
			get { return _data; }
			set
			{
				CheckFrozen();
				_data = value;
			}
		}

		public Annotation<TOffset> Parent { get; private set; }

		public int Depth { get; private set; }

		public bool IsLeaf
		{
			get { return _children == null || _children.Count == 0; }
		}

		public Annotation<TOffset> Root { get; private set; }

		public AnnotationList<TOffset> Children
		{
			get
			{
				if (_children == null)
					_children = new AnnotationList<TOffset>(Span.SpanFactory, this);
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
			Depth = 0;
			Root = this;
		}

		protected internal override void Init(BidirList<Annotation<TOffset>> list, int levels)
		{
			base.Init(list, levels);
			Parent = ((AnnotationList<TOffset>) list).Parent;
			if (Parent != null)
			{
				Depth = Parent.Depth + 1;
				Root = Parent.Root;
			}
		}

		public Span<TOffset> Span { get; internal set; }

		public FeatureStruct FeatureStruct
		{
			get { return _fs; }
			set
			{
				CheckFrozen();
				_fs = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this annotation is optional.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this annotation is optional, otherwise <c>false</c>.
		/// </value>
		public bool Optional
		{
			get { return _optional; }
			set
			{
				CheckFrozen();
				_optional = value;
			}
		}

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
			if (this == other)
				return 0;

			if (List != null)
			{
				if (List.Begin == this)
					return -1;
				if (List.Begin == other)
					return 1;
				if (List.End == this)
					return 1;
				if (List.End == other)
					return -1;
			}

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

		public bool IsFrozen { get; private set; }

		public void Freeze()
		{
			if (IsFrozen)
				return;
			IsFrozen = true;
			_fs.Freeze();
			if (_children != null)
				_children.Freeze();

			_hashCode = 23;
			_hashCode = _hashCode * 31 + _fs.GetFrozenHashCode();
			_hashCode = _hashCode * 31 + (_children == null ? 0 : _children.GetFrozenHashCode());
			_hashCode = _hashCode * 31 + _optional.GetHashCode();
			_hashCode = _hashCode * 31 + Span.GetHashCode();
		}

		public bool ValueEquals(Annotation<TOffset> other)
		{
			if (other == null)
				return false;

			if (IsLeaf != other.IsLeaf)
				return false;

			if (!IsLeaf && !_children.ValueEquals(other._children))
				return false;

			return _fs.ValueEquals(other._fs) && _optional == other._optional && Span == other.Span;
		}

		public int GetFrozenHashCode()
		{
			if (!IsFrozen)
				throw new InvalidOperationException("The annotation does not have a valid hash code, because it is mutable.");
			return _hashCode;
		}

		private void CheckFrozen()
		{
			if (IsFrozen)
				throw new InvalidOperationException("The annotation is immutable.");
		}

		public override string ToString()
		{
			return string.Format("({0} {1})", Span, FeatureStruct);
		}
	}
}
