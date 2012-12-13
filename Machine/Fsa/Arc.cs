using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Collections;

namespace SIL.Machine.Fsa
{
	public enum ArcPriorityType
	{
		High = 0,
		Medium,
		Low,
		VeryLow
	}

	public class Arc<TData, TOffset, TResult> where TData : IData<TOffset>
	{
		private readonly State<TData, TOffset, TResult> _source;
		private readonly int _tag = -1;
		private readonly List<TagMapCommand> _commands;
		private readonly ArcPriorityType _priorityType;
		private readonly ReadOnlyList<Predicate> _outputs;

		internal Arc(State<TData, TOffset, TResult> source, State<TData, TOffset, TResult> target, ArcPriorityType priorityType)
			: this(source, null, target)
		{
			_priorityType = priorityType;
		}

		internal Arc(State<TData, TOffset, TResult> source, State<TData, TOffset, TResult> target, int tag)
			: this(source, null, target, new TagMapCommand(tag, TagMapCommand.CurrentPosition).ToEnumerable())
		{
			_tag = tag;
		}

		internal Arc(State<TData, TOffset, TResult> source, Predicate input, State<TData, TOffset, TResult> target, IEnumerable<TagMapCommand> cmds)
			: this(source, input, Enumerable.Empty<Predicate>(), target, cmds)
		{
		}

		internal Arc(State<TData, TOffset, TResult> source, Predicate input, State<TData, TOffset, TResult> target)
			: this(source, input, Enumerable.Empty<Predicate>(), target)
		{
		}

		internal Arc(State<TData, TOffset, TResult> source, Predicate input, IEnumerable<Predicate> output, State<TData, TOffset, TResult> target)
			: this(source, input, output, target, Enumerable.Empty<TagMapCommand>())
		{
		}

		private Arc(State<TData, TOffset, TResult> source, Predicate input, IEnumerable<Predicate> output, State<TData, TOffset, TResult> target, IEnumerable<TagMapCommand> cmds)
		{
			_source = source;
			Input = input;
			_outputs = output.ToList().AsReadOnlyList();
			Target = target;
			_priorityType = ArcPriorityType.Medium;
			Priority = -1;
			_commands = cmds.ToList();
		}

		public State<TData, TOffset, TResult> Source
		{
			get { return _source; }
		}

		public State<TData, TOffset, TResult> Target { get; internal set; }

		public Predicate Input { get; internal set; }

		public IReadOnlyList<Predicate> Outputs
		{
			get { return _outputs; }
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
			if (Input == null)
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
				sb.Append(Input);
			}

			if (_outputs.Count > 0)
			{
				sb.Append(":");
				sb.Append(string.Concat(_outputs.Select(o => o.ToString())));
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
