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
			AddMany(nodes);
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
				if (node == null)
				{
					if (int.MinValue + 1 == First.Tag)
						RelabelMinimumSparseEnclosingRange(null);
				}
				else if (node.Next == null)
				{
					if (node.Tag == int.MaxValue)
						RelabelMinimumSparseEnclosingRange(node);
				}
				else if (node.Tag + 1 == node.Next.Tag)
				{
					RelabelMinimumSparseEnclosingRange(node);
				}
					
				if (node != null && node.Next == null)
				{
					newNode.Tag = node.Tag == int.MaxValue - 1 ? int.MaxValue : Average(node.Tag, int.MaxValue);
				}
				else
				{
					newNode.Tag = Average(node == null ? int.MinValue : node.Tag, node == null ? First.Tag : node.Next.Tag);
				}
			}

			base.Insert(newNode, node, dir);
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

		private void RelabelMinimumSparseEnclosingRange(PhoneticShapeNode atom)
		{
			double T = Math.Pow(Math.Pow(2, NumBits) / Count, 1.0 / NumBits);

			double elementCount = 1.0;

			PhoneticShapeNode left = atom;
			PhoneticShapeNode right = atom;
			int tag = atom == null ? int.MinValue : atom.Tag;
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
					if (atom == cursor)
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
