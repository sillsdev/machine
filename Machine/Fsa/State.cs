using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Fsa
{
	public class State<TData, TOffset> : IEquatable<State<TData, TOffset>>  where TData : IData<TOffset>
	{
		private readonly int _index;
		private readonly ArcCollection<TData, TOffset> _arcs;

		private readonly List<AcceptInfo<TData, TOffset>> _acceptInfos; 
		private readonly List<TagMapCommand> _finishers;
		private readonly bool _isLazy;

		internal State(int index, bool isAccepting)
			: this(index, isAccepting, Enumerable.Empty<AcceptInfo<TData, TOffset>>(), Enumerable.Empty<TagMapCommand>(), false)
		{
		}

		internal State(int index, IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos)
			: this(index, true, acceptInfos, Enumerable.Empty<TagMapCommand>(), false)
		{
		}

		internal State(int index, IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers, bool isLazy)
			: this(index, true, acceptInfos, finishers, isLazy)
		{
		}

		private State(int index, bool isAccepting, IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers, bool isLazy)
		{
			_index = index;
			IsAccepting = isAccepting;
			_acceptInfos = new List<AcceptInfo<TData, TOffset>>(acceptInfos);
			_finishers = new List<TagMapCommand>(finishers);
			_isLazy = isLazy;
			_arcs = new ArcCollection<TData, TOffset>(this);
		}

		public int Index
		{
			get
			{
				return _index;
			}
		}

		public bool IsAccepting { get; set; }

		public ArcCollection<TData, TOffset> Arcs
		{
			get { return _arcs; }
		}

		public ICollection<AcceptInfo<TData, TOffset>> AcceptInfos
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

		public override int GetHashCode()
		{
			return _index;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as State<TData, TOffset>);
		}

		public bool Equals(State<TData, TOffset> other)
		{
			if (other == null)
				return false;

			return _index == other._index;
		}

		public override string ToString()
		{
			return string.Format("State {0}", _index);
		}
	}
}
