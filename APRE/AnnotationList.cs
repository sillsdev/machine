using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE
{
	public class AnnotationList<TOffset> : SkipList<Annotation<TOffset>>, ICloneable<AnnotationList<TOffset>> 
	{
		private int _currentID;

		public AnnotationList()
			: base(new AnnotationComparer(Direction.LeftToRight), new AnnotationComparer(Direction.RightToLeft))
		{
		}

		public AnnotationList(AnnotationList<TOffset> annList)
			: this()
		{
			AddRange(annList.Select(ann => ann.Clone()));
		}

		public override void Add(Annotation<TOffset> node)
		{
			base.Add(node);
			node.ListID = _currentID++;
		}

		public IEnumerable<Annotation<TOffset>> GetNodes(Span<TOffset> span)
		{
			return GetNodes(span, Direction.LeftToRight);
		}

		public IEnumerable<Annotation<TOffset>> GetNodes(Span<TOffset> span, Direction dir)
		{
			Annotation<TOffset> startAnn;
			Find(new Annotation<TOffset>(span), Direction.LeftToRight, out startAnn);
			startAnn = startAnn == null ? First : (startAnn.Next ?? startAnn);

			Annotation<TOffset> endAnn;
			Find(new Annotation<TOffset>(span), Direction.RightToLeft, out endAnn);
			endAnn = endAnn == null ? Last : (endAnn.Prev ?? endAnn);

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
