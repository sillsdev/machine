using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.Annotations
{
	public class Shape : OrderedBidirList<ShapeNode>, IAnnotatedData<ShapeNode>, ICloneable<Shape>, IFreezable, IValueEquatable<Shape>
	{
		private readonly Func<bool, ShapeNode> _marginSelector;
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly AnnotationList<ShapeNode> _annotations;
		private int _hashCode;

		public Shape(SpanFactory<ShapeNode> spanFactory, Func<bool, ShapeNode> marginSelector)
			: this(spanFactory, marginSelector, new AnnotationList<ShapeNode>(spanFactory))
		{
		}

		public Shape(SpanFactory<ShapeNode> spanFactory, Func<bool, ShapeNode> marginSelector, AnnotationList<ShapeNode> annotations)
			: base(EqualityComparer<ShapeNode>.Default, marginSelector)
		{
			_marginSelector = marginSelector;
			_spanFactory = spanFactory;
			_annotations = annotations;
			Begin.Tag = int.MinValue;
			End.Tag = int.MaxValue;
			_annotations.Add(Begin.Annotation, false);
			_annotations.Add(End.Annotation, false);
		}

		protected Shape(Shape shape)
			: this(shape.SpanFactory, shape._marginSelector)
		{
			shape.CopyTo(this);
		}

		public SpanFactory<ShapeNode> SpanFactory
		{
			get { return _spanFactory; }
		}

		public Span<ShapeNode> Span
		{
			get { return _spanFactory.Create(Begin, End); }
		}

		public AnnotationList<ShapeNode> Annotations
		{
			get { return _annotations; }
		}

		public bool IsFrozen { get; private set; }

		public void Freeze()
		{
			if (IsFrozen)
				return;

			IsFrozen = true;
			Begin.Freeze();
			int i = 0;
			foreach (ShapeNode node in this)
			{
				node.Tag = i++;
				node.Freeze();
			}
			End.Freeze();

			_annotations.Freeze();

			_hashCode = 23;
			_hashCode = _hashCode * 31 + Count;
			_hashCode = _hashCode * 31 + _annotations.GetFrozenHashCode();
		}

		private void CheckFrozen()
		{
			if (IsFrozen)
				throw new InvalidOperationException("The shape is immutable.");
		}

		public ShapeNode Add(FeatureStruct fs)
		{
			return Add(fs, false);
		}

		public ShapeNode Add(FeatureStruct fs, bool optional)
		{
			var newNode = new ShapeNode(_spanFactory, fs);
			newNode.Annotation.Optional = optional;
			Add(newNode);
			return newNode;
		}

		public Span<ShapeNode> CopyTo(Shape dest)
		{
			if (Count == 0)
				return _spanFactory.Empty;
			return CopyTo(First, Last, dest);
		}

		public Span<ShapeNode> CopyTo(ShapeNode srcStart, ShapeNode srcEnd, Shape dest)
		{
			return CopyTo(_spanFactory.Create(srcStart, srcEnd), dest);
		}

		public Span<ShapeNode> CopyTo(Span<ShapeNode> srcSpan, Shape dest)
		{
			ShapeNode startNode = null;
			ShapeNode endNode = null;
			foreach (ShapeNode node in GetNodes(srcSpan))
			{
				ShapeNode newNode = node.Clone();
				if (startNode == null)
					startNode = newNode;
				endNode = newNode;
				dest.Add(newNode);
			}

			Span<ShapeNode> destSpan = dest.SpanFactory.Create(startNode, endNode);
			Dictionary<ShapeNode, ShapeNode> mapping = GetNodes(srcSpan).Zip(dest.GetNodes(destSpan)).ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
			foreach (Annotation<ShapeNode> ann in _annotations.GetNodes(srcSpan))
				CopyAnnotations(dest._annotations, ann, mapping);

			return destSpan;
		}

		private void CopyAnnotations(AnnotationList<ShapeNode> destList, Annotation<ShapeNode> ann, Dictionary<ShapeNode, ShapeNode> mapping)
		{
			if (ann.Span.Start.Annotation == ann)
			{
				destList.Add(mapping[ann.Span.Start].Annotation, false);
			}
			else
			{
				var newAnn = new Annotation<ShapeNode>(_spanFactory.Create(mapping[ann.Span.Start], mapping[ann.Span.End]), ann.FeatureStruct.Clone());
				destList.Add(newAnn, false);
				if (!ann.IsLeaf)
				{
					foreach (Annotation<ShapeNode> child in ann.Children)
						CopyAnnotations(newAnn.Children, child, mapping);
				}
			}
		}

		public ShapeNode AddAfter(ShapeNode node, FeatureStruct fs)
		{
			return AddAfter(node, fs, Direction.LeftToRight);
		}

		public ShapeNode AddAfter(ShapeNode node, FeatureStruct fs, bool optional)
		{
			return AddAfter(node, fs, optional, Direction.LeftToRight);
		}

		public ShapeNode AddAfter(ShapeNode node, FeatureStruct fs, Direction dir)
		{
			return AddAfter(node, fs, false, dir);
		}

		public ShapeNode AddAfter(ShapeNode node, FeatureStruct fs, bool optional, Direction dir)
		{
			var newNode = new ShapeNode(_spanFactory, fs);
			newNode.Annotation.Optional = optional;
			AddAfter(node, newNode, dir);
			return newNode;
		}

		public override void AddAfter(ShapeNode node, ShapeNode newNode, Direction dir)
		{
			CheckFrozen();
			if (newNode.List == this)
				throw new ArgumentException("newNode is already a member of this collection.", "newNode");
			if (node != null && node.List != this)
				throw new ArgumentException("node is not a member of this collection.", "node");

			if (Count == 0)
			{
				newNode.Tag = 0;
			}
			else
			{
				ShapeNode curNode = node;
				if (dir == Direction.RightToLeft)
					curNode = curNode == null ? Last : curNode.Prev;

				if (curNode == null)
				{
					if (First.Tag == int.MinValue + 1)
						RelabelMinimumSparseEnclosingRange(null);
				}
				else if (curNode.Next == null)
				{
					if (curNode.Tag == int.MaxValue - 1)
						RelabelMinimumSparseEnclosingRange(curNode);
				}
				else if (curNode.Tag + 1 == curNode.Next.Tag)
				{
					RelabelMinimumSparseEnclosingRange(curNode);
				}
					
				if (curNode != null && curNode.Next == null)
				{
					newNode.Tag = Average(curNode.Tag, int.MaxValue);
				}
				else
				{
					newNode.Tag = Average(curNode == null ? int.MinValue : curNode.Tag, curNode == null ? First.Tag : curNode.Next.Tag);
				}
			}

			base.AddAfter(node, newNode, dir);

			_annotations.Add(newNode.Annotation);
		}

		public override bool Remove(ShapeNode node)
		{
			CheckFrozen();
			if (node.List != this)
				return false;

			node.Annotation.Remove();
			UpdateAnnotations(_annotations, node);
			return base.Remove(node);
		}

		private void UpdateAnnotations(AnnotationList<ShapeNode> annList, ShapeNode node)
		{
			if (annList.Count == 0)
				return;

			Annotation<ShapeNode> startAnn;
			annList.Find(node, Direction.LeftToRight, out startAnn);
			if (startAnn == annList.Begin)
				startAnn = annList.First;

			Annotation<ShapeNode> endAnn;
			annList.Find(node, Direction.RightToLeft, out endAnn);
			if (endAnn == annList.End)
				endAnn = annList.Last;

			if (startAnn.CompareTo(endAnn) > 0)
				return;

			foreach (Annotation<ShapeNode> ann in annList.GetNodes(startAnn, endAnn).Where(ann => ann.Span.Contains(node)).ToArray())
			{
				if (!ann.IsLeaf)
					UpdateAnnotations(ann.Children, node);

				if (ann.Span.Start == node && ann.Span.End == node)
				{
					annList.Remove(ann);
				}
				else if (ann.Span.Start == node || ann.Span.End == node)
				{
					Span<ShapeNode> span = ann.Span.Start == node ? _spanFactory.Create(node.Next, ann.Span.End) : _spanFactory.Create(ann.Span.Start, node.Prev);
					var newAnn = new Annotation<ShapeNode>(span, ann.FeatureStruct.Clone()) {Optional = ann.Optional};
					if (!ann.IsLeaf)
					{
						foreach (Annotation<ShapeNode> child in ann.Children.ToArray())
							newAnn.Children.Add(child, false);
					}
					annList.Remove(ann, false);
					annList.Add(newAnn, false);
				}
			}
		}

		public override void Clear()
		{
			CheckFrozen();
			base.Clear();
			_annotations.Clear();
			_annotations.Add(Begin.Annotation);
			_annotations.Add(End.Annotation);
		}

		private static int Average(int x, int y)
		{
			return (x & y) + (x ^ y) / 2;
		}

		private const int NumBits = (sizeof(int) * 8) - 2;

		private void RelabelMinimumSparseEnclosingRange(ShapeNode node)
		{
			double T = Math.Pow(Math.Pow(2, NumBits) / Count, 1.0 / NumBits);

			double elementCount = 1.0;

			ShapeNode left = node;
			ShapeNode right = node;
			int tag = node == null ? int.MinValue : node.Tag;
			int low = tag;
			int high = tag;

			int level = 0;
			double overflowThreshold = 1.0;
			int range = 1;
			do
			{
				int toggleBit = 1 << level++;
				overflowThreshold /= T;
				range <<= 1;

				bool expandToLeft = (tag & toggleBit) != 0;
				if (expandToLeft)
				{
					low ^= toggleBit;
					while (left != null && left.Tag > low)
					{
						left = left.Prev;
						elementCount++;
					}
				}
				else
				{
					high ^= toggleBit;
					while (right == null || (right.Tag < high && (right.Next != null && right.Next.Tag > right.Tag)))
					{
						right = right == null ? First : right.Next;
						elementCount++;
					}
				}
			}
			while (elementCount >= (range * overflowThreshold) && level < NumBits);

			var count = (int) elementCount; //elementCount always fits into an int, size() is an int too

			//note that the base itself can be relabeled, but always gets the same label! (int.MIN_VALUE)
			int pos = low;
			int step = range / count;
			ShapeNode cursor = left;
			if (step > 1)
			{
				for (int i = 0; i < count; i++)
				{
					if (cursor != null)
						cursor.Tag = pos;
					pos += step;
					cursor = cursor == null ? First : cursor.Next;
				}
			}
			else
			{   //handle degenerate case here (step == 1)
				//make sure that this and next are separated by distance of at least 2
				int slack = range - count;
				for (int i = 0; i < elementCount; i++)
				{
					if (cursor != null)
						cursor.Tag = pos;
					pos++;
					if (node == cursor)
						pos += slack;
					cursor = cursor == null ? First : cursor.Next;
				}
			}
		}

		public IEnumerable<ShapeNode> GetNodes(Span<ShapeNode> span)
		{
			return GetNodes(span, Direction.LeftToRight);
		}

		public IEnumerable<ShapeNode> GetNodes(Span<ShapeNode> span, Direction dir)
		{
			return this.GetNodes(span.GetStart(dir), span.GetEnd(dir), dir);
		}

		public bool ValueEquals(Shape other)
		{
			if (Count != other.Count)
				return false;

			return _annotations.ValueEquals(other._annotations);
		}

		public int GetFrozenHashCode()
		{
			if (!IsFrozen)
				throw new InvalidOperationException("The shape does not have a valid hash code, because it is mutable.");

			return _hashCode;
		}

		public Shape Clone()
		{
			return new Shape(this);
		}
	}
}
