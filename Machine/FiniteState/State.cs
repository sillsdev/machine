using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.FiniteState
{
	public class State<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly int _index;
		private readonly ArcCollection<TData, TOffset> _arcs;

		private readonly List<AcceptInfo<TData, TOffset>> _acceptInfos; 
		private readonly List<TagMapCommand> _finishers;
		private readonly bool _isLazy;

		internal State(IFstOperations<TData, TOffset> operations, int index, bool isAccepting)
			: this(operations, index, isAccepting, Enumerable.Empty<AcceptInfo<TData, TOffset>>(), Enumerable.Empty<TagMapCommand>(), false)
		{
		}

		internal State(IFstOperations<TData, TOffset> operations, int index, IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos)
			: this(operations, index, true, acceptInfos, Enumerable.Empty<TagMapCommand>(), false)
		{
		}

		internal State(IFstOperations<TData, TOffset> operations, int index, IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers, bool isLazy)
			: this(operations, index, true, acceptInfos, finishers, isLazy)
		{
		}

		private State(IFstOperations<TData, TOffset> operations, int index, bool isAccepting, IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers, bool isLazy)
		{
			_index = index;
			IsAccepting = isAccepting;
			_acceptInfos = new List<AcceptInfo<TData, TOffset>>(acceptInfos);
			_finishers = new List<TagMapCommand>(finishers);
			_isLazy = isLazy;
			_arcs = new ArcCollection<TData, TOffset>(operations, this);
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

		public override string ToString()
		{
			return string.Format("State {0}", _index);
		}
	}
}
