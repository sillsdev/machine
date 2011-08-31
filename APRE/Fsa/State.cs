using System;
using System.Collections.Generic;

namespace SIL.APRE.Fsa
{
	public class State<TOffset>
	{
		private readonly int _index;
		private readonly List<Arc<TOffset>> _arcs;

		private readonly bool _isAccepting;
		private readonly List<AcceptInfo<TOffset>> _acceptInfos; 
		private readonly List<TagMapCommand> _finishers;

		internal State(int index, bool isAccepting)
		{
			_index = index;
			_isAccepting = isAccepting;
			_arcs = new List<Arc<TOffset>>();
		}

		internal State(int index, string id, Func<IBidirList<Annotation<TOffset>>, bool> accept)
			: this(index, true)
		{
			_acceptInfos = new List<AcceptInfo<TOffset>> {new AcceptInfo<TOffset>(id, accept)};
		}

		internal State(int index, IEnumerable<AcceptInfo<TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers)
			: this(index, true)
		{
			_acceptInfos = new List<AcceptInfo<TOffset>>(acceptInfos);
			_finishers = new List<TagMapCommand>(finishers);
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
			get
			{
				return _isAccepting;
			}
		}

		public IEnumerable<Arc<TOffset>> Arcs
		{
			get
			{
				return _arcs;
			}
		}

		public IEnumerable<AcceptInfo<TOffset>> AcceptInfos
		{
			get { return _acceptInfos; }
		}

		internal IEnumerable<TagMapCommand> Finishers
		{
			get
			{
				return _finishers;
			}
		}

		public State<TOffset> AddArc(Arc<TOffset> arc)
		{
			_arcs.Add(arc);
			return arc.Target;
		}

		public override int GetHashCode()
		{
			return _index;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as State<TOffset>);
		}

		public bool Equals(State<TOffset> other)
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
