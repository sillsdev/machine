using System;
using System.Collections.Generic;
using System.Linq;
using SIL.APRE;
using SIL.APRE.Matching;

namespace SIL.HermitCrab
{
	public sealed class PhoneticShape : BidirList<PhoneticShapeNode>, ICloneable
	{
		private readonly AnnotationList<PhoneticShapeNode> _annotations;
		private readonly CharacterDefinitionTable _charDefTable;
		private readonly ModeType _mode;

		public PhoneticShape(CharacterDefinitionTable charDefTable, ModeType mode)
		{
			_charDefTable = charDefTable;
			_mode = mode;
			_annotations = new AnnotationList<PhoneticShapeNode>();
		}

		public PhoneticShape(CharacterDefinitionTable charDefTable, ModeType mode, IEnumerable<PhoneticShapeNode> nodes)
			: this(charDefTable, mode)
		{
			AddRange(nodes);
		}

		public PhoneticShape(PhoneticShape shape)
			: this(shape._charDefTable, shape._mode, shape.Select(node => node.Clone()))
		{
		}

		public CharacterDefinitionTable CharacterDefinitionTable
		{
			get { return _charDefTable; }
		}

		public ModeType Mode
		{
			get { return _mode; }
		}

		public AnnotationList<PhoneticShapeNode> Annotations
		{
			get
			{
				return _annotations;
			}
		}

		public override void Insert(PhoneticShapeNode newNode, PhoneticShapeNode node, Direction dir)
		{
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
				PhoneticShapeNode curNode = node;
				if (dir == Direction.RightToLeft)
					curNode = curNode == null ? Last : curNode.Prev;

				if (curNode == null)
				{
					if (int.MinValue + 1 == First.Tag)
						RelabelMinimumSparseEnclosingRange(null);
				}
				else if (curNode.Next == null)
				{
					if (curNode.Tag == int.MaxValue)
						RelabelMinimumSparseEnclosingRange(curNode);
				}
				else if (curNode.Tag + 1 == curNode.Next.Tag)
				{
					RelabelMinimumSparseEnclosingRange(curNode);
				}
					
				if (curNode != null && curNode.Next == null)
				{
					newNode.Tag = curNode.Tag == int.MaxValue - 1 ? int.MaxValue : Average(curNode.Tag, int.MaxValue);
				}
				else
				{
					newNode.Tag = Average(curNode == null ? int.MinValue : curNode.Tag, curNode == null ? First.Tag : curNode.Next.Tag);
				}
			}

			base.Insert(newNode, node, dir);

			_annotations.Add(newNode.Annotation);
		}

		public override bool Remove(PhoneticShapeNode node)
		{
			if (base.Remove(node))
			{
				_annotations.Remove(node.Annotation);
				return true;
			}
			return false;
		}

		public override void Clear()
		{
			base.Clear();
			_annotations.Clear();
		}

		private static int Average(int x, int y)
		{
			return (x & y) + (x ^ y) / 2;
		}

		private const int NumBits = (sizeof(int) * 8) - 2;

		private void RelabelMinimumSparseEnclosingRange(PhoneticShapeNode node)
		{
			double T = Math.Pow(Math.Pow(2, NumBits) / Count, 1.0 / NumBits);

			double elementCount = 1.0;

			PhoneticShapeNode left = node;
			PhoneticShapeNode right = node;
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
					while (right == null || (right.Tag < high && right.Next.Tag > right.Tag))
					{
						right = right == null ? First : right.Next;
						elementCount++;
					}
				}
			}
			while (elementCount >= (range * overflowThreshold) && level < NumBits);

			var count = (int)elementCount; //elementCount always fits into an int, size() is an int too

			//note that the base itself can be relabeled, but always gets the same label! (int.MIN_VALUE)
			int pos = low;
			int step = range / count;
			PhoneticShapeNode cursor = left;
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

		object ICloneable.Clone()
		{
			return Clone();
		}

		public PhoneticShape Clone()
		{
			return new PhoneticShape(this);
		}

		public override string ToString()
		{
			return _charDefTable.ToRegexString(this, _mode, true);
		}
	}
}
