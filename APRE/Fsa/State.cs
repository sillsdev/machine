using System.Collections.Generic;

namespace SIL.APRE.Fsa
{
	public class State<TOffset>
	{
		private readonly int _index;
		private readonly bool _isAccepting;
		private readonly List<Arc<TOffset>> _arcs;
		private readonly List<TagMapCommand> _finishers;

		internal State(int index, bool isAccepting)
			: this(index, isAccepting, null)
		{
		}

		internal State(int index, IEnumerable<TagMapCommand> finishers)
			: this(index, true, finishers)
		{
		}

		State(int index, bool isAccepting, IEnumerable<TagMapCommand> finishers)
		{
			_isAccepting = isAccepting;
			_index = index;
			_arcs = new List<Arc<TOffset>>();
			_finishers = finishers == null ? new List<TagMapCommand>() : new List<TagMapCommand>(finishers);
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

		internal IEnumerable<TagMapCommand> Finishers
		{
			get
			{
				return _finishers;
			}
		}

		public void AddArc(Arc<TOffset> arc)
		{
			_arcs.Add(arc);
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
