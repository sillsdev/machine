using System.Collections.Generic;
using System.Text;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Fsa
{
	public enum ArcPriorityType
	{
		High = 0,
		Medium,
		Low,
		VeryLow
	}

	public class Arc<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly State<TData, TOffset> _source;
		private readonly State<TData, TOffset> _target;
		private readonly FeatureStruct _condition;
		private readonly int _tag = -1;
		private readonly List<TagMapCommand> _commands;
		private readonly ArcPriorityType _priorityType;

		internal Arc(State<TData, TOffset> source, State<TData, TOffset> target, ArcPriorityType priorityType)
			: this(source, null, target)
		{
			_priorityType = priorityType;
		}

		internal Arc(State<TData, TOffset> source, State<TData, TOffset> target, int tag)
			: this(source, null, target)
		{
			_tag = tag;
		}

		internal Arc(State<TData, TOffset> source, FeatureStruct condition, State<TData, TOffset> target, IEnumerable<TagMapCommand> cmds)
			: this(source, condition, target)
		{
			_commands = new List<TagMapCommand>(cmds);
		}

		internal Arc(State<TData, TOffset> source, FeatureStruct condition, State<TData, TOffset> target)
		{
			_source = source;
			_condition = condition;
			_target = target;
			_priorityType = ArcPriorityType.Medium;
			Priority = -1;
		}

		public State<TData, TOffset> Source
		{
			get { return _source; }
		}

		public State<TData, TOffset> Target
		{
			get
			{
				return _target;
			}
		} 

		public FeatureStruct Condition
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

		internal List<TagMapCommand> Commands
		{
			get
			{
				return _commands;
			}
		}

		public ArcPriorityType PriorityType
		{
			get { return _priorityType; }
		}

		public int Priority { get; internal set; }

		public override string ToString()
		{
			var sb = new StringBuilder();
			if (_condition == null)
			{
				if (_tag != -1)
				{
					sb.Append("tag ");
					sb.Append(_tag);
				}
				else
				{
					sb.Append("ε");
				}
			}
			else
			{
				sb.Append(_condition.ToString());
			}

			if (Priority != -1)
			{
				sb.Append(", ");
				sb.Append(Priority);
			}

			return sb.ToString();
		}
	}
}
