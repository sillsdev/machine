using System.Collections.Generic;

namespace SIL.APRE.Fsa
{
	public enum PriorityType
	{
		Greedy = 0,
		Normal,
		Lazy
	}

	public class Arc<TOffset>
	{
		private readonly State<TOffset> _source;
		private readonly State<TOffset> _target;
		private readonly ArcCondition<TOffset> _condition;
		private readonly int _tag = -1;
		private readonly List<TagMapCommand> _commands;
		private readonly PriorityType _priorityType;

		internal Arc(State<TOffset> source, State<TOffset> target, PriorityType priorityType)
			: this(source, null, target)
		{
			_priorityType = priorityType;
		}

		internal Arc(State<TOffset> source, State<TOffset> target, int tag)
			: this(source, null, target)
		{
			_tag = tag;
		}

		internal Arc(State<TOffset> source, ArcCondition<TOffset> condition, State<TOffset> target, IEnumerable<TagMapCommand> cmds)
			: this(source, condition, target)
		{
			_commands = new List<TagMapCommand>(cmds);
		}

		internal Arc(State<TOffset> source, ArcCondition<TOffset> condition, State<TOffset> target)
		{
			_source = source;
			_condition = condition;
			_target = target;
			_priorityType = PriorityType.Normal;
		}

		public State<TOffset> Source
		{
			get { return _source; }
		}

		public State<TOffset> Target
		{
			get
			{
				return _target;
			}
		} 

		public ArcCondition<TOffset> Condition
		{
			get
			{
				return _condition;
			}
		}

		public int Tag
		{
			get
			{
				return _tag;
			}
		}

		internal IEnumerable<TagMapCommand> Commands
		{
			get
			{
				return _commands;
			}
		}

		public PriorityType PriorityType
		{
			get { return _priorityType; }
		}

		public int Priority { get; set; }

		public override string ToString()
		{
			if (_condition == null)
			{
				if (_tag != -1)
					return string.Format("tag {0}, {1}", _tag, Priority);
				return string.Format("ε, {0}", Priority);
			}

			return string.Format("{0}, {1}", _condition, Priority);
		}
	}
}
