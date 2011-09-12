using System.Collections.Generic;

namespace SIL.APRE.Fsa
{
	public class Arc<TOffset>
	{
		private readonly State<TOffset> _source;
		private readonly State<TOffset> _target;
		private readonly ArcCondition<TOffset> _condition;
		private readonly int _tag = -1;
		private readonly List<TagMapCommand> _commands;
		private readonly int _priority = -1;

		internal Arc(State<TOffset> source, State<TOffset> target)
			: this(source, null, target)
		{
		}

		internal Arc(State<TOffset> source, State<TOffset> target, int tag, int priority)
		{
			_source = source;
			_target = target;
			_tag = tag;
			_priority = priority;
		}

		internal Arc(State<TOffset> source, ArcCondition<TOffset> condition, State<TOffset> target)
		{
			_source = source;
			_condition = condition;
			_target = target;
		}

		internal Arc(State<TOffset> source, ArcCondition<TOffset> condition, State<TOffset> target, IEnumerable<TagMapCommand> cmds)
			: this(source, condition, target)
		{
			_commands = new List<TagMapCommand>(cmds);
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

		public int Priority
		{
			get
			{
				return _priority;
			}
		}

		public override string ToString()
		{
			if (_condition == null)
			{
				if (_tag != -1)
					return string.Format("Tag {0}", _tag);
				return "";
			}

			return _condition.ToString();
		}
	}
}
