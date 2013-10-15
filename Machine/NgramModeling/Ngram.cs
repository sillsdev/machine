using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Machine.NgramModeling
{
	public class Ngram<TItem> : IReadOnlyList<TItem>, IStructuralEquatable
	{
		public static implicit operator Ngram<TItem>(TItem item)
		{
			if (item == null)
				return new Ngram<TItem>();

			return new Ngram<TItem>(item);
		}

		private readonly TItem[] _items; 

		public Ngram(params TItem[] items)
		{
			_items = items;
		}

		public Ngram(IEnumerable<TItem> items)
		{
			_items = items.ToArray();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _items.GetEnumerator();
		}

		IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
		{
			return ((IEnumerable<TItem>) _items).GetEnumerator();
		}

		public int Count
		{
			get { return _items.Length; }
		}

		public TItem this[int index]
		{
			get { return _items[index]; }
		}

		public TItem First
		{
			get
			{
				if (_items.Length == 0)
					throw new InvalidOperationException("The n-gram is empty.");
				return _items[0];
			}
		}

		public TItem GetFirst(Direction dir)
		{
			if (_items.Length == 0)
				throw new InvalidOperationException("The n-gram is empty.");
			return dir == Direction.LeftToRight ? First : Last;
		}

		public TItem Last
		{
			get
			{
				if (_items.Length == 0)
					throw new InvalidOperationException("The n-gram is empty.");
				return _items[_items.Length - 1];
			}
		}

		public TItem GetLast(Direction dir)
		{
			if (_items.Length == 0)
				throw new InvalidOperationException("The n-gram is empty.");

			return dir == Direction.LeftToRight ? Last : First;
		}

		public Ngram<TItem> TakeAllExceptLast()
		{
			return TakeAllExceptLast(Direction.LeftToRight);
		}

		public Ngram<TItem> TakeAllExceptLast(Direction dir)
		{
			return new Ngram<TItem>(dir == Direction.LeftToRight ? _items.Take(_items.Length - 1) : _items.Skip(1));
		}

		public Ngram<TItem> SkipFirst()
		{
			return SkipFirst(Direction.LeftToRight);
		}

		public Ngram<TItem> SkipFirst(Direction dir)
		{
			return new Ngram<TItem>(dir == Direction.LeftToRight ? _items.Skip(1) : _items.Take(_items.Length - 1));
		}

		public Ngram<TItem> Concat(TItem item)
		{
			return Concat(item, Direction.LeftToRight);
		}

		public Ngram<TItem> Concat(TItem item, Direction dir)
		{
			return new Ngram<TItem>(dir == Direction.LeftToRight ? _items.Concat(item) : item.ToEnumerable().Concat(_items));
		}

		public Ngram<TItem> Concat(Ngram<TItem> ngram)
		{
			return Concat(ngram, Direction.LeftToRight);
		}

		public Ngram<TItem> Concat(Ngram<TItem> ngram, Direction dir)
		{
			return new Ngram<TItem>(dir == Direction.LeftToRight ? _items.Concat(ngram) : ngram.Concat(_items));
		}

		public override bool Equals(object obj)
		{
			return ((IStructuralEquatable) this).Equals(obj, EqualityComparer<TItem>.Default);
		}

		public override int GetHashCode()
		{
			return ((IStructuralEquatable) this).GetHashCode(EqualityComparer<TItem>.Default);
		}

		bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
		{
			if (other == null)
				return false;

			var ngram = other as Ngram<TItem>;
			return ngram != null && _items.SequenceEqual(ngram._items, comparer.ToTypesafe<TItem>());
		}

		int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
		{
			return _items.GetSequenceHashCode(comparer.ToTypesafe<TItem>());
		}

		public override string ToString()
		{
			if (_items.Length == 0)
				return "-";

			return string.Concat(_items.Select(item => item.ToString()));
		}
	}
}
