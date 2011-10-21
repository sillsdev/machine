using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.APRE.FeatureModel;

namespace SIL.APRE
{
	public class AnnotationList<TOffset> : SkipList<Annotation<TOffset>>, ICloneable<AnnotationList<TOffset>>
	{
		private readonly SpanFactory<TOffset> _spanFactory; 
		private int _currentID;
		private readonly Dictionary<string, List<Annotation<TOffset>>> _typeIndex;

		public AnnotationList(SpanFactory<TOffset> spanFactory)
			: base(new AnnotationComparer(Direction.LeftToRight), new AnnotationComparer(Direction.RightToLeft))
		{
			_spanFactory = spanFactory;
			_typeIndex = new Dictionary<string, List<Annotation<TOffset>>>();
		}

		public AnnotationList(AnnotationList<TOffset> annList)
			: this(annList._spanFactory)
		{
			AddRange(annList.Select(ann => ann.Clone()));
		}

		public void Add(string type, Span<TOffset> span, FeatureStruct fs)
		{
			Add(type, span, fs, false);
		}

		public void Add(string type, TOffset start, TOffset end, FeatureStruct fs)
		{
			Add(type, start, end, fs, false);
		}

		public void Add(string type, Span<TOffset> span, FeatureStruct fs, bool optional)
		{
			Add(new Annotation<TOffset>(type, span, fs) {Optional = optional});
		}

		public void Add(string type, TOffset start, TOffset end, FeatureStruct fs, bool optional)
		{
			Add(type, _spanFactory.Create(start, end), fs, optional);
		}

		public override void Add(Annotation<TOffset> node)
		{
			base.Add(node);
			node.ListID = _currentID++;

			List<Annotation<TOffset>> annotations;
			if (!_typeIndex.TryGetValue(node.Type, out annotations))
			{
				annotations = new List<Annotation<TOffset>>();
				_typeIndex[node.Type] = annotations;
			}
			annotations.Add(node);
		}

		public override bool Remove(Annotation<TOffset> node)
		{
			if (base.Remove(node))
			{
				List<Annotation<TOffset>> annotations = _typeIndex[node.Type];
				annotations.Remove(node);
				return true;
			}

			return false;
		}

		public override void Clear()
		{
			base.Clear();
			_typeIndex.Clear();
		}

		public bool Find(TOffset offset, out Annotation<TOffset> result)
		{
			return Find(offset, Direction.LeftToRight, out result);
		}

		public bool Find(TOffset offset, Direction dir, out Annotation<TOffset> result)
		{
			Find(new Annotation<TOffset>(_spanFactory.Create(offset, GetLast(dir).Span.GetEnd(dir), dir)), dir, out result);
			result = result == GetEnd(dir) ? GetFirst(dir) : (result.GetNext(dir) == GetEnd(dir) ? result : result.GetNext(dir));
			return result.Span.GetStart(dir).Equals(offset);
		}

		public IEnumerable<Annotation<TOffset>> GetNodes(string type)
		{
			return GetNodes(type, Direction.LeftToRight);
		}

		public IEnumerable<Annotation<TOffset>> GetNodes(string type, Direction dir)
		{
			List<Annotation<TOffset>> annotations;
			if (_typeIndex.TryGetValue(type, out annotations))
				return annotations;
			return Enumerable.Empty<Annotation<TOffset>>();
		}

		public IEnumerable<Annotation<TOffset>> GetNodes(Span<TOffset> span)
		{
			return GetNodes(span, Direction.LeftToRight);
		}

		public IEnumerable<Annotation<TOffset>> GetNodes(Span<TOffset> span, Direction dir)
		{
			Annotation<TOffset> startAnn;
			Find(span.Start, Direction.LeftToRight, out startAnn);

			Annotation<TOffset> endAnn;
			Find(span.End, Direction.RightToLeft, out endAnn);

			return this.GetNodes(dir == Direction.LeftToRight ? startAnn : endAnn, dir == Direction.LeftToRight ? endAnn : startAnn, dir);
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
