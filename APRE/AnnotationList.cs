using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE
{
	public sealed class AnnotationList<TOffset> : SkipList<Annotation<TOffset>>, ICloneable
	{
		private int _currentID;

		public AnnotationList()
			: base(new AnnotationComparer(Direction.LeftToRight), new AnnotationComparer(Direction.RightToLeft))
		{
		}

		public AnnotationList(AnnotationList<TOffset> annList)
			: this()
		{
			AddMany(annList.Select(ann => ann.Clone()));
		}

		public override void Add(Annotation<TOffset> node)
		{
			base.Add(node);
			node.ListID = _currentID++;
		}

		public AnnotationList<TOffset> Clone()
		{
			return new AnnotationList<TOffset>(this);
		}

		object ICloneable.Clone()
		{
			return Clone();
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

				int res = x.Span.CompareTo(y.Span, _dir);
				if (res != 0)
					return res;
				return x.ListID.CompareTo(y.ListID);
			}
		}
	}
}
