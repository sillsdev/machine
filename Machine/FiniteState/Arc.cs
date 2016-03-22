using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.ObjectModel;

namespace SIL.Machine.FiniteState
{
	public enum ArcPriorityType
	{
		High = 0,
		Medium,
		Low,
		VeryLow
	}

	public class Arc<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		private readonly State<TData, TOffset> _source;
		private readonly int _tag = -1;
		private readonly List<TagMapCommand> _commands;
		private readonly ArcPriorityType _priorityType;
		private readonly ReadOnlyList<Output<TData, TOffset>> _outputs;

		internal Arc(State<TData, TOffset> source, State<TData, TOffset> target, ArcPriorityType priorityType)
			: this(source, new Input(0), target)
		{
			_priorityType = priorityType;
		}

		internal Arc(State<TData, TOffset> source, State<TData, TOffset> target, int tag)
			: this(source, new Input(0), target, new TagMapCommand(tag, TagMapCommand.CurrentPosition).ToEnumerable())
		{
			_tag = tag;
		}

		internal Arc(State<TData, TOffset> source, Input input, State<TData, TOffset> target, IEnumerable<TagMapCommand> cmds)
			: this(source, input, Enumerable.Empty<Output<TData, TOffset>>(), target, cmds)
		{
		}

		internal Arc(State<TData, TOffset> source, Input input, State<TData, TOffset> target)
			: this(source, input, Enumerable.Empty<Output<TData, TOffset>>(), target)
		{
		}

		internal Arc(State<TData, TOffset> source, Input input, IEnumerable<Output<TData, TOffset>> output, State<TData, TOffset> target)
			: this(source, input, output, target, Enumerable.Empty<TagMapCommand>())
		{
		}

		internal Arc(State<TData, TOffset> source, Input input, IEnumerable<Output<TData, TOffset>> output, State<TData, TOffset> target, IEnumerable<TagMapCommand> cmds)
		{
			_source = source;
			Input = input;
			_outputs = output.ToList().ToReadOnlyList();
			Target = target;
			_priorityType = ArcPriorityType.Medium;
			Priority = -1;
			_commands = cmds.ToList();
		}

		public State<TData, TOffset> Source
		{
			get { return _source; }
		}

		public State<TData, TOffset> Target { get; internal set; }

		public Input Input { get; internal set; }

		public IReadOnlyList<Output<TData, TOffset>> Outputs
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

		internal int Priority { get; set; }

		public override string ToString()
		{
			var sb = new StringBuilder();
			if (Input.IsEpsilon && _tag != -1)
			{
				sb.Append("tag ");
				sb.Append(_tag);
			}
			else
			{
				sb.Append(Input);
			}

			if (_outputs.Count > 0)
			{
				sb.Append(":");
				sb.Append(string.Join(",", _outputs.Select(o => o.ToString())));
			}

			return sb.ToString();
		}
	}
}
