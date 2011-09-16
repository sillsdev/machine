using System.Collections.Generic;
using System.Linq;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Fsa
{
	public class State<TOffset>
	{
		private readonly int _index;
		private readonly List<Arc<TOffset>> _outgoingArcs;
		private readonly List<Arc<TOffset>> _incomingArcs;

		private readonly bool _isAccepting;
		private readonly List<AcceptInfo<TOffset>> _acceptInfos; 
		private readonly List<TagMapCommand> _finishers;
		private readonly bool _isLazy;

		internal State(int index, bool isAccepting)
			: this(index, isAccepting, Enumerable.Empty<AcceptInfo<TOffset>>(), Enumerable.Empty<TagMapCommand>(), false)
		{
		}

		internal State(int index, IEnumerable<AcceptInfo<TOffset>> acceptInfos)
			: this(index, true, acceptInfos, Enumerable.Empty<TagMapCommand>(), false)
		{
		}

		internal State(int index, IEnumerable<AcceptInfo<TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers, bool isLazy)
			: this(index, true, acceptInfos, finishers, isLazy)
		{
		}

		private State(int index, bool isAccepting, IEnumerable<AcceptInfo<TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers, bool isLazy)
		{
			_index = index;
			_isAccepting = isAccepting;
			_acceptInfos = new List<AcceptInfo<TOffset>>(acceptInfos);
			_finishers = new List<TagMapCommand>(finishers);
			_isLazy = isLazy;
			_outgoingArcs = new List<Arc<TOffset>>();
			_incomingArcs = new List<Arc<TOffset>>();
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

		public IEnumerable<Arc<TOffset>> OutgoingArcs
		{
			get { return _outgoingArcs; }
		}

		public IEnumerable<Arc<TOffset>> IncomingArcs
		{
			get { return _incomingArcs; }
		}

		public IEnumerable<AcceptInfo<TOffset>> AcceptInfos
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

		public State<TOffset> AddArc(State<TOffset> target)
		{
			return AddArc(target, ArcPriorityType.Medium);
		}

		public State<TOffset> AddArc(State<TOffset> target, ArcPriorityType priorityType)
		{
			return AddArc(new Arc<TOffset>(this, target, priorityType));
		}

		public State<TOffset> AddArc(FeatureStruct condition, State<TOffset> target)
		{
			return AddArc(new Arc<TOffset>(this, condition, target));
		}

		internal State<TOffset> AddArc(State<TOffset> target, int tag)
		{
			return AddArc(new Arc<TOffset>(this, target, tag));
		}

		internal State<TOffset> AddArc(FeatureStruct condition, State<TOffset> target, IEnumerable<TagMapCommand> cmds)
		{
			return AddArc(new Arc<TOffset>(this, condition, target, cmds));
		}

		private State<TOffset> AddArc(Arc<TOffset> arc)
		{
			_outgoingArcs.Add(arc);
			arc.Target._incomingArcs.Add(arc);
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
