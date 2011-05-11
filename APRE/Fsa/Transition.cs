using System.Collections.Generic;

namespace SIL.APRE.Fsa
{
	public class Transition<TOffset, TData>
	{
		private readonly State<TOffset, TData> _target;
		private readonly ITransitionCondition<TOffset, TData> _condition;
		private readonly int _tag = -1;
		private readonly List<TagMapCommand> _commands;
		private readonly int _priority = -1;

		public Transition(State<TOffset, TData> target)
			: this(null, target)
		{
		}

		internal Transition(State<TOffset, TData> target, int tag, int priority)
		{
			_target = target;
			_tag = tag;
			_priority = priority;
		}

		public Transition(ITransitionCondition<TOffset, TData> condition, State<TOffset, TData> target)
		{
			_condition = condition;
			_target = target;
		}

		internal Transition(ITransitionCondition<TOffset, TData> condition, State<TOffset, TData> target, IEnumerable<TagMapCommand> cmds)
			: this(condition, target)
		{
			_commands = new List<TagMapCommand>(cmds);
		}

		public State<TOffset, TData> Target
		{
			get
			{
				return _target;
			}
		}

		public ITransitionCondition<TOffset, TData> Condition
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
