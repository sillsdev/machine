using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine
{
	public class AnnotationList<TOffset> : BidirList<Annotation<TOffset>>, IDeepCloneable<AnnotationList<TOffset>>, IFreezable, IValueEquatable<AnnotationList<TOffset>>
	{
		private readonly SpanFactory<TOffset> _spanFactory; 
		private int _currentID;
		private readonly Annotation<TOffset> _parent;
		private int _hashCode;

		public AnnotationList(SpanFactory<TOffset> spanFactory)
			: base(new AnnotationComparer(), begin => new Annotation<TOffset>(spanFactory.Empty))
		{
			_spanFactory = spanFactory;
		}

		protected AnnotationList(AnnotationList<TOffset> annList)
			: this(annList._spanFactory)
		{
			AddRange(annList.Select(ann => ann.DeepClone()));
		}

		internal AnnotationList(SpanFactory<TOffset> spanFactory, Annotation<TOffset> parent)
			: this(spanFactory)
		{
			_parent = parent;
		}

		internal Annotation<TOffset> Parent
		{
			get { return _parent; }
		}

		public bool IsFrozen { get; private set; }

		public void Freeze()
		{
			if (IsFrozen)
				return;
			IsFrozen = true;
			_hashCode = 23;
			foreach (Annotation<TOffset> ann in this)
			{
				ann.Freeze();
				_hashCode = _hashCode * 31 + ann.GetFrozenHashCode();
			}
		}

		public bool ValueEquals(AnnotationList<TOffset> other)
		{
			if (other == null)
				return false;

			if (Count != other.Count)
				return false;

			return this.SequenceEqual(other, FreezableEqualityComparer<Annotation<TOffset>>.Default);
		}

		public int GetFrozenHashCode()
		{
			if (!IsFrozen)
				throw new InvalidOperationException("The annotation list does not have a valid hash code, because it is mutable.");
			return _hashCode;
		}

		private void CheckFrozen()
		{
			if (IsFrozen)
				throw new InvalidOperationException("The annotation list is immutable.");
		}

		public Annotation<TOffset> Add(Span<TOffset> span, FeatureStruct fs)
		{
			return Add(span, fs, false);
		}

		public Annotation<TOffset> Add(TOffset start, TOffset end, FeatureStruct fs)
		{
			return Add(start, end, fs, false);
		}

		public Annotation<TOffset> Add(TOffset offset, FeatureStruct fs)
		{
			return Add(_spanFactory.Create(offset), fs);
		}

		public Annotation<TOffset> Add(Span<TOffset> span, FeatureStruct fs, bool optional)
		{
			var ann = new Annotation<TOffset>(span, fs) {Optional = optional};
			Add(ann);
			return ann;
		}

		public Annotation<TOffset> Add(TOffset start, TOffset end, FeatureStruct fs, bool optional)
		{
			return Add(_spanFactory.Create(start, end), fs, optional);
		}

		public Annotation<TOffset> Add(TOffset offset, FeatureStruct fs, bool optional)
		{
			return Add(_spanFactory.Create(offset), fs, optional);
		}

		public override void Add(Annotation<TOffset> node)
		{
			Add(node, true);
		}

		public void Add(Annotation<TOffset> node, bool subsume)
		{
			CheckFrozen();
			if (_parent != null && !_parent.Span.Contains(node.Span))
				throw new ArgumentException("The new annotation must be within the span of the parent annotation.", "node");

			node.Remove(false);
			if (subsume)
			{
				foreach (Annotation<TOffset> ann in GetNodes(node.Span).ToArray())
					node.Children.Add(ann);
				base.Add(node);
				node.ListID = _currentID++;
				for (Annotation<TOffset> ann = node.Prev; ann != Begin; ann = ann.Prev)
				{
					if (ann.Span.Contains(node.Span))
					{
						ann.Children.Add(node);
						break;
					}
				}
			}
			else
			{
				base.Add(node);
				node.ListID = _currentID++;
			}
		}

		public override bool Remove(Annotation<TOffset> node)
		{
			return Remove(node, true);
		}

		public bool Remove(Annotation<TOffset> node, bool preserveChildren)
		{
			CheckFrozen();
			if (base.Remove(node))
			{
				if (preserveChildren)
				{
					foreach (Annotation<TOffset> ann in node.Children.ToArray())
						Add(ann, false);
				}

				return true;
			}

			return false;
		}

		public bool Find(TOffset offset, out Annotation<TOffset> result)
		{
			return Find(offset, Direction.LeftToRight, out result);
		}

		public bool Find(TOffset offset, Direction dir, out Annotation<TOffset> result)
		{
			if (Count == 0)
			{
				result = null;
				return false;
			}

			TOffset lastOffset = GetLast(dir).Span.GetEnd(dir);
			if (!_spanFactory.IsValidSpan(offset, lastOffset, dir))
			{
				result = GetLast(dir);
				return false;
			}

			if (dir == Direction.LeftToRight)
			{
				if (Find(new Annotation<TOffset>(_spanFactory.Create(offset, lastOffset)), out result))
					return true;
			}
			else
			{
				Span<TOffset> offsetSpan = _spanFactory.Create(offset, Direction.RightToLeft);
				if (Find(new Annotation<TOffset>(offsetSpan), Direction.RightToLeft, out result))
					return true;

				if (result != First && result.Prev.Span.Contains(offsetSpan))
					result = result.Prev;
			}

			if (offset.Equals(result.GetNext(dir).Span.GetStart(dir)))
				result = result.GetNext(dir);
			return offset.Equals(result.Span.GetStart(dir));
		}

		public bool FindDepthFirst(TOffset offset, out Annotation<TOffset> result)
		{
			return FindDepthFirst(offset, Direction.LeftToRight, out result);
		}

		public bool FindDepthFirst(TOffset offset, Direction dir, out Annotation<TOffset> result)
		{
			if (Find(offset, dir, out result))
				return true;

			if (!result.IsLeaf && result.Span.Contains(offset, dir))
				return result.Children.FindDepthFirst(offset, dir, out result);

			return false;
		}

		public IEnumerable<Annotation<TOffset>> GetNodes(TOffset start, TOffset end)
		{
			return GetNodes(_spanFactory.Create(start, end));
		}

		public IEnumerable<Annotation<TOffset>> GetNodes(Span<TOffset> span)
		{
			return GetNodes(span, Direction.LeftToRight);
		}

		public IEnumerable<Annotation<TOffset>> GetNodes(TOffset start, TOffset end, Direction dir)
		{
			return GetNodes(_spanFactory.Create(start, end, dir), dir);
		}

		public IEnumerable<Annotation<TOffset>> GetNodes(Span<TOffset> span, Direction dir)
		{
			if (Count == 0)
				return Enumerable.Empty<Annotation<TOffset>>();

			Annotation<TOffset> startAnn;
			if (!Find(span.Start, Direction.LeftToRight, out startAnn))
				startAnn = startAnn.Next;

			if (startAnn == GetEnd(dir))
				return Enumerable.Empty<Annotation<TOffset>>();

			Annotation<TOffset> endAnn;
			if (!Find(span.End, Direction.RightToLeft, out endAnn))
				endAnn = endAnn.Prev;

			if (endAnn == GetBegin(dir))
				return Enumerable.Empty<Annotation<TOffset>>();

			if (startAnn.CompareTo(endAnn) > 0)
				return Enumerable.Empty<Annotation<TOffset>>();

			return this.GetNodes(dir == Direction.LeftToRight ? startAnn : endAnn, dir == Direction.LeftToRight ? endAnn : startAnn, dir).Where(ann => span.Contains(ann.Span));
		}

		public override void Clear()
		{
			CheckFrozen();
			base.Clear();
		}

		public AnnotationList<TOffset> DeepClone()
		{
			return new AnnotationList<TOffset>(this);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append("{");
			bool first = true;
			foreach (Annotation<TOffset> ann in this)
			{
				if (!first)
					sb.Append(", ");
				sb.Append(ann);
				first = false;
			}
			sb.Append("}");
			return sb.ToString();
		}

		private class AnnotationComparer : IComparer<Annotation<TOffset>>
		{
			public int Compare(Annotation<TOffset> x, Annotation<TOffset> y)
			{
				if (x == null)
					return y == null ? 0 : -1;

				return x.CompareTo(y);
			}
		}
	}
}
