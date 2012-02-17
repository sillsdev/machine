using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.FeatureModel;

namespace SIL.Machine
{
	public class AnnotationList<TOffset> : BidirList<Annotation<TOffset>>, ICloneable<AnnotationList<TOffset>>
	{
		private readonly SpanFactory<TOffset> _spanFactory; 
		private int _currentID;
		private readonly Annotation<TOffset> _parent; 

		public AnnotationList(SpanFactory<TOffset> spanFactory)
			: base(new AnnotationComparer(Direction.LeftToRight), new AnnotationComparer(Direction.RightToLeft))
		{
			_spanFactory = spanFactory;
		}

		public AnnotationList(AnnotationList<TOffset> annList)
			: this(annList._spanFactory)
		{
			AddRange(annList.Select(ann => ann.Clone()));
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

		public void Add(Span<TOffset> span, FeatureStruct fs)
		{
			Add(span, fs, false);
		}

		public void Add(TOffset start, TOffset end, FeatureStruct fs)
		{
			Add(start, end, fs, false);
		}

		public void Add(TOffset offset, FeatureStruct fs)
		{
			Add(offset, offset, fs);
		}

		public void Add(Span<TOffset> span, FeatureStruct fs, bool optional)
		{
			Add(new Annotation<TOffset>(span, fs) {Optional = optional});
		}

		public void Add(TOffset start, TOffset end, FeatureStruct fs, bool optional)
		{
			Add(_spanFactory.Create(start, end), fs, optional);
		}

		public void Add(TOffset offset, FeatureStruct fs, bool optional)
		{
			Add(offset, offset, fs, optional);
		}

		public override void Add(Annotation<TOffset> node)
		{
			Add(node, true);
		}

		public void Add(Annotation<TOffset> node, bool subsume)
		{
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
			if (Find(new Annotation<TOffset>(_spanFactory.Create(_spanFactory.Compare(offset, lastOffset, dir) > 0 ? lastOffset : offset, lastOffset, dir)), dir, out result))
				return true;
			if (result == GetEnd(dir))
				result = GetBegin(dir);

			if (result == GetBegin(dir))
			{
				if (GetFirst(dir).Span.GetStart(dir).Equals(offset))
				{
					result = GetFirst(dir);
					return true;
				}
				return false;
			}

			if (result.GetNext(dir) != GetEnd(dir))
			{
				if (result.GetNext(dir).Span.GetStart(dir).Equals(offset))
					result = result.GetNext(dir);
			}
			return result.Span.GetStart(dir).Equals(offset);
		}

		public bool FindDepthFirst(TOffset offset, out Annotation<TOffset> result)
		{
			return FindDepthFirst(offset, Direction.LeftToRight, out result);
		}

		public bool FindDepthFirst(TOffset offset, Direction dir, out Annotation<TOffset> result)
		{
			if (Find(offset, dir, out result))
				return true;

			if (result != GetBegin(dir) && !result.IsLeaf() && result.Span.Contains(offset))
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
				startAnn = startAnn == Begin ? First : startAnn.Next;

			if (startAnn == GetEnd(dir))
				return Enumerable.Empty<Annotation<TOffset>>();

			Annotation<TOffset> endAnn;
			if (!Find(span.End, Direction.RightToLeft, out endAnn))
				endAnn = endAnn == GetBegin(Direction.RightToLeft) ? GetFirst(Direction.RightToLeft) : endAnn.GetNext(Direction.RightToLeft);

			if (endAnn == GetBegin(dir))
				return Enumerable.Empty<Annotation<TOffset>>();

			return this.GetNodes(dir == Direction.LeftToRight ? startAnn : endAnn, dir == Direction.LeftToRight ? endAnn : startAnn, dir).Where(ann => span.Contains(ann.Span));
		}

		public AnnotationList<TOffset> Clone()
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
				sb.Append(ann.ToString());
				first = false;
			}
			sb.Append("}");
			return sb.ToString();
		}

		class AnnotationComparer : IComparer<Annotation<TOffset>>
		{
			private readonly Direction _dir;

			public AnnotationComparer(Direction dir)
			{
				_dir = dir;
			}

			public int Compare(Annotation<TOffset> x, Annotation<TOffset> y)
			{
				if (x == null)
					return y == null ? 0 : -1;

				return x.CompareTo(y, _dir);
			}
		}
	}
}
