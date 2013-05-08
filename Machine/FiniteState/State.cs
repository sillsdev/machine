using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Machine.FiniteState
{
	public class State<TData, TOffset> : IFreezable where TData : IData<TOffset>
	{
		private readonly int _index;
		private readonly ArcCollection<TData, TOffset> _arcs;

		private readonly FreezableList<AcceptInfo<TData, TOffset>> _acceptInfos; 
		private readonly List<TagMapCommand> _finishers;
		private readonly bool _isLazy;
		private bool _isAccepting;

		internal State(bool isFsa, int index, bool isAccepting)
			: this(isFsa, index, isAccepting, Enumerable.Empty<AcceptInfo<TData, TOffset>>(), Enumerable.Empty<TagMapCommand>(), false)
		{
		}

		internal State(bool isFsa, int index, IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos)
			: this(isFsa, index, true, acceptInfos, Enumerable.Empty<TagMapCommand>(), false)
		{
		}

		internal State(bool isFsa, int index, IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers, bool isLazy)
			: this(isFsa, index, true, acceptInfos, finishers, isLazy)
		{
		}

		private State(bool isFsa, int index, bool isAccepting, IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers, bool isLazy)
		{
			_index = index;
			IsAccepting = isAccepting;
			_acceptInfos = new FreezableList<AcceptInfo<TData, TOffset>>(acceptInfos);
			_finishers = new List<TagMapCommand>(finishers);
			_isLazy = isLazy;
			_arcs = new ArcCollection<TData, TOffset>(isFsa, this);
		}

		public int Index
		{
			get
			{
				return _index;
			}
		}

		public bool IsAccepting
		{
			get { return _isAccepting; }
			set
			{
				CheckFrozen();
				_isAccepting = value;
			}
		}

		public ArcCollection<TData, TOffset> Arcs
		{
			get { return _arcs; }
		}

		public IList<AcceptInfo<TData, TOffset>> AcceptInfos
		{
			get { return _acceptInfos; }
		}

		public bool IsLazy
		{
			get { return _isLazy; }
		}

		internal List<TagMapCommand> Finishers
		{
			get
			{
				return _finishers;
			}
		}

		public override string ToString()
		{
			return string.Format("State {0}", _index);
		}

		private void CheckFrozen()
		{
			if (IsFrozen)
				throw new InvalidOperationException("The FST is immutable.");
		}

		public bool IsFrozen { get; private set; }

		public void Freeze()
		{
			if (IsFrozen)
				return;

			IsFrozen = true;
			_arcs.Freeze();
			_acceptInfos.Freeze();
		}

		public int GetFrozenHashCode()
		{
			return GetHashCode();
		}
	}
}
