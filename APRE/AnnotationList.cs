using System;
using System.Collections.Generic;
using System.Linq;

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

		public IBidirList<Annotation<TOffset>> GetView(Span<TOffset> span)
		{
			Annotation<TOffset> startAnn;
			Find(new Annotation<TOffset>(span.StartSpan), Direction.LeftToRight, out startAnn);
			startAnn = startAnn == null ? First : startAnn.Next;

			Annotation<TOffset> endAnn;
			Find(new Annotation<TOffset>(span.EndSpan), Direction.LeftToRight, out endAnn);
			endAnn = endAnn == null ? First : endAnn.Next;

			return this.GetView(startAnn, endAnn);
		}

		public AnnotationList<TOffset> Clone()
		{
			return new AnnotationList<TOffset>(this);
		}

		object ICloneable.Clone()
		{
			return Clone();
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
