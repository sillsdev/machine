using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class Fst<TData, TOffset> where TData : IData<TOffset>, IDeepCloneable<TData>
	{
		private int _nextTag;
		private readonly Dictionary<string, int> _groups;
		private readonly List<TagMapCommand> _initializers;
		private int _registerCount;
		private readonly Direction _dir;
		private readonly Func<Annotation<TOffset>, bool> _filter;
		private readonly List<State<TData, TOffset>> _states;
		private readonly SimpleReadOnlyCollection<State<TData, TOffset>> _readonlyStates;
		private readonly IEqualityComparer<FstResult<TData, TOffset>> _fsaMatchComparer;
		private readonly IFstOperations<TData, TOffset> _operations;
		private readonly IEqualityComparer<TOffset> _offsetComparer; 

		public Fst()
			: this(Direction.LeftToRight)
		{
		}

		public Fst(IEqualityComparer<TOffset> offsetComparer)
			: this(Direction.LeftToRight, offsetComparer)
		{
		}

		public Fst(IFstOperations<TData, TOffset> operations)
			: this(operations, Direction.LeftToRight)
		{
		}

		public Fst(IFstOperations<TData, TOffset> operations, IEqualityComparer<TOffset> offsetComparer)
			: this(operations, Direction.LeftToRight, offsetComparer)
		{
		}

		public Fst(Direction dir)
			: this(dir, ann => true)
		{
		}

		public Fst(Direction dir, IEqualityComparer<TOffset> offsetComparer)
			: this(dir, ann => true, offsetComparer)
		{
		}

		public Fst(IFstOperations<TData, TOffset> operations, Direction dir)
			: this(operations, dir, ann => true)
		{
		}

		public Fst(IFstOperations<TData, TOffset> operations, Direction dir, IEqualityComparer<TOffset> offsetComparer)
			: this(operations, dir, ann => true, offsetComparer)
		{
		}

		public Fst(Func<Annotation<TOffset>, bool> filter)
			: this(Direction.LeftToRight, filter)
		{
		}

		public Fst(Func<Annotation<TOffset>, bool> filter, IEqualityComparer<TOffset> offsetComparer)
			: this(Direction.LeftToRight, filter, offsetComparer)
		{
		}

		public Fst(IFstOperations<TData, TOffset> operations, Func<Annotation<TOffset>, bool> filter)
			: this(operations, Direction.LeftToRight, filter)
		{
		}

		public Fst(IFstOperations<TData, TOffset> operations, Func<Annotation<TOffset>, bool> filter, IEqualityComparer<TOffset> offsetComparer)
			: this(operations, Direction.LeftToRight, filter, offsetComparer)
		{
		}
		
		public Fst(Direction dir, Func<Annotation<TOffset>, bool> filter)
			: this(null, dir, filter)
		{
		}

		public Fst(Direction dir, Func<Annotation<TOffset>, bool> filter, IEqualityComparer<TOffset> offsetComparer)
			: this(null, dir, filter, offsetComparer)
		{
		}

		public Fst(IFstOperations<TData, TOffset> operations, Direction dir, Func<Annotation<TOffset>, bool> filter)
			: this(operations, dir, filter, EqualityComparer<TOffset>.Default)
		{
		}

		public Fst(IFstOperations<TData, TOffset> operations, Direction dir, Func<Annotation<TOffset>, bool> filter, IEqualityComparer<TOffset> offsetComparer)
		{
			_states = new List<State<TData, TOffset>>();
			_readonlyStates = _states.AsSimpleReadOnlyCollection();
			_operations = operations;
			_initializers = new List<TagMapCommand>();
			_groups = new Dictionary<string, int>();
			_dir = dir;
			_filter = filter;
			_fsaMatchComparer = AnonymousEqualityComparer.Create<FstResult<TData, TOffset>>(FstResultEquals, FstResultGetHashCode);
			_offsetComparer = offsetComparer;
		}

		private bool FstResultEquals(FstResult<TData, TOffset> x, FstResult<TData, TOffset> y)
		{
			if (x.ID != y.ID)
				return false;

			for (int i = 0; i < _registerCount; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					if (x.Registers[i, j].HasValue != y.Registers[i, j].HasValue)
						return false;

					if (x.Registers[i, j].HasValue && !_offsetComparer.Equals(x.Registers[i, j].Value, x.Registers[i, j].Value))
						return false;
				}
			}

			return EqualityComparer<TData>.Default.Equals(x.Output, y.Output);
		}

		private int FstResultGetHashCode(FstResult<TData, TOffset> m)
		{
			int code = 23;
			code = code * 31 + (m.ID == null ? 0 : m.ID.GetHashCode());
			for (int i = 0; i < _registerCount; i++)
			{
				for (int j = 0; j < 2; j++)
					code = code * 31 + (m.Registers[i, j].HasValue && m.Registers[i, j].Value != null ? _offsetComparer.GetHashCode(m.Registers[i, j].Value) : 0);
			}
			code = code * 31 * EqualityComparer<TData>.Default.GetHashCode(m.Output);
			return code;
		}

		public bool IsDeterministic { get; private set; }

		public State<TData, TOffset> CreateAcceptingState()
		{
			var state = new State<TData, TOffset>(_operations == null, _states.Count, true);
			_states.Add(state);
			return state;
		}

		protected State<TData, TOffset> CreateAcceptingState(IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos)
		{
			var state = new State<TData, TOffset>(_operations == null, _states.Count, acceptInfos);
			_states.Add(state);
			return state;
		}

		public State<TData, TOffset> CreateAcceptingState(string id, Func<TData, FstResult<TData, TOffset>,  bool> acceptable, int priority)
		{
			var state = new State<TData, TOffset>(_operations == null, _states.Count, new AcceptInfo<TData, TOffset>(id, acceptable, priority).ToEnumerable());
			_states.Add(state);
			return state;
		}

		public State<TData, TOffset> CreateState()
		{
			var state = new State<TData, TOffset>(_operations == null, _states.Count, false);
			_states.Add(state);
			return state;
		}

		public IEnumerable<string> GroupNames
		{
			get { return _groups.Keys; }
		}

		public bool IsAcceptor
		{
			get { return _operations == null; }
		}

		public bool GetOffsets(string groupName, NullableValue<TOffset>[,] registers, out TOffset start, out TOffset end)
		{
			int tag = _groups[groupName];
			NullableValue<TOffset> startValue = registers[tag, 0];
			NullableValue<TOffset> endValue = registers[tag + 1, 1];
			if (startValue.HasValue && endValue.HasValue)
			{
				if (_dir == Direction.LeftToRight)
				{
					start = startValue.Value;
					end = endValue.Value;
				}
				else
				{
					start = endValue.Value;
					end = startValue.Value;
				}
				return true;
			}

			start = default(TOffset);
			end = default(TOffset);
			return false;
		}

		public State<TData, TOffset> CreateTag(State<TData, TOffset> source, State<TData, TOffset> target, string groupName, bool isStart)
		{
			int tag = GetTag(groupName, isStart);
			return source.Arcs.Add(target, tag);
		}

		private int GetTag(string groupName, bool isStart)
		{
			int tag;
			if (!_groups.TryGetValue(groupName, out tag))
			{
				tag = _nextTag;
				_nextTag += 2;
				_registerCount += 2;
				_groups.Add(groupName, tag);
			}
			return isStart ? tag : tag + 1;
		}

		private State<TData, TOffset> CreateAcceptingState(IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers, bool isLazy)
		{
			var state = new State<TData, TOffset>(_operations == null, _states.Count, acceptInfos, finishers, isLazy);
			_states.Add(state);
			return state;
		}

		public State<TData, TOffset> StartState { get; set; }

		public Direction Direction
		{
			get { return _dir; }
		}

		public IReadOnlyCollection<State<TData, TOffset>> States
		{
			get { return _readonlyStates; }
		}

		public IFstOperations<TData, TOffset> Operations
		{
			get { return _operations; }
		}

		public void Reset()
		{
			_states.Clear();
			StartState = null;
			IsDeterministic = false;
		}

		private class FstInstance
		{
			private readonly NullableValue<TOffset>[,] _registers;
			private readonly TData _output;
			private readonly IDictionary<Annotation<TOffset>, Annotation<TOffset>> _mappings;
			private readonly Queue<Annotation<TOffset>> _queue;
			private readonly VariableBindings _varBindings;
			private readonly ISet<State<TData, TOffset>> _visited;
			private readonly State<TData, TOffset> _state;
			private readonly Annotation<TOffset> _annotation;
			private readonly int _depth;
			private readonly int[] _priorities; 

			public FstInstance(State<TData, TOffset> state, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output,
				IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings, Queue<Annotation<TOffset>> queue, VariableBindings varBindings,
				ISet<State<TData, TOffset>> visited, int depth, int[] priorities)
			{
				_state = state;
				_annotation = ann;
				_registers = registers;
				_output = output;
				_mappings = mappings;
				_queue = queue;
				_varBindings = varBindings;
				_visited = visited;
				_depth = depth;
				_priorities = priorities;
			}

			public State<TData, TOffset> State
			{
				get { return _state; }
			}

			public Annotation<TOffset> Annotation
			{
				get { return _annotation; }
			}

			public TData Output
			{
				get { return _output; }
			}

			public NullableValue<TOffset>[,] Registers
			{
				get { return _registers; }
			}

			public IDictionary<Annotation<TOffset>, Annotation<TOffset>> Mappings
			{
				get { return _mappings; }
			}

			public Queue<Annotation<TOffset>> Queue
			{
				get { return _queue; }
			}

			public VariableBindings VariableBindings
			{
				get { return _varBindings; }
			}

			public ISet<State<TData, TOffset>> Visited
			{
				get { return _visited; }
			}

			public int Depth
			{
				get { return _depth; }
			}

			public int[] Priorities
			{
				get { return _priorities; }
			}
		}

		public bool Transduce(TData data, Annotation<TOffset> start, bool startAnchor, bool endAnchor, bool useDefaults, out IEnumerable<FstResult<TData, TOffset>> results)
		{
			return Transduce(data, start, startAnchor, endAnchor, true, useDefaults, out results);
		}

		public bool Transduce(TData data, Annotation<TOffset> start, bool startAnchor, bool endAnchor, bool useDefaults, out FstResult<TData, TOffset> result)
		{
			IEnumerable<FstResult<TData, TOffset>> results;
			if (Transduce(data, start, startAnchor, endAnchor, false, useDefaults, out results))
			{
				result = results.First();
				return true;
			}
			result = null;
			return false;
		}

		private bool Transduce(TData data, Annotation<TOffset> start, bool startAnchor, bool endAnchor, bool allMatches, bool useDefaults, out IEnumerable<FstResult<TData, TOffset>> results)
		{
			var from = new ConcurrentStack<FstInstance>();

			List<FstResult<TData, TOffset>> resultList = null;

			Annotation<TOffset> ann = start;

			var initAnns = new HashSet<Annotation<TOffset>>();
			while (ann != data.Annotations.GetEnd(_dir))
			{
				var initRegisters = new NullableValue<TOffset>[_registerCount, 2];

				var cmds = new List<TagMapCommand>();
				foreach (TagMapCommand cmd in _initializers)
				{
					if (cmd.Dest == 0)
						initRegisters[cmd.Dest, 0].Value = ann.Span.GetStart(_dir);
					else
						cmds.Add(cmd);
				}

				ann = InitializeStack(data, ann, initRegisters, cmds, from, initAnns, 0);

				var curResults = new List<FstResult<TData, TOffset>>();
				while (!from.IsEmpty)
				{
					var to = new ConcurrentStack<FstInstance>();
					Parallel.ForEach(from, inst =>
						{
							if (inst.Annotation == null)
								return;

							var taskResults = new List<FstResult<TData, TOffset>>();
							VariableBindings varBindings = null;
							foreach (Arc<TData, TOffset> arc in inst.State.Arcs)
							{
								if (arc.Input.IsEpsilon)
								{
									if (!inst.Visited.Contains(arc.Target))
									{
										TData output = inst.Output;
										IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings = inst.Mappings;
										Queue<Annotation<TOffset>> queue = inst.Queue;
										NullableValue<TOffset>[,] registers = inst.Registers;
										ISet<State<TData, TOffset>> visited = inst.Visited;
										if (IsInstanceReuseable(inst))
										{
											if (varBindings == null)
												varBindings = inst.VariableBindings;
										}
										else
										{
											registers = (NullableValue<TOffset>[,]) inst.Registers.Clone();

											if (_operations != null)
											{
												output = inst.Output.DeepClone();

												Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
													.Zip(output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
												mappings = inst.Mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
												queue = new Queue<Annotation<TOffset>>(inst.Queue);
											}
											if (varBindings == null)
												varBindings = inst.VariableBindings.DeepClone();
											visited = new HashSet<State<TData, TOffset>>(inst.Visited);
										}
										if (_operations != null)
											ExecuteOutputs(arc.Outputs, output, mappings, queue);

										to.Push(EpsilonAdvanceFst(data, endAnchor, inst.Annotation, registers, output, mappings, queue, varBindings, visited, arc, taskResults,
											inst.Depth, inst.Priorities));
										varBindings = null;
									}
								}
								else
								{
									if (varBindings == null)
										varBindings = IsInstanceReuseable(inst) ? inst.VariableBindings : inst.VariableBindings.DeepClone();
									if (inst.Annotation != data.Annotations.GetEnd(_dir) && arc.Input.Matches(inst.Annotation.FeatureStruct, useDefaults, varBindings))
									{
										TData output = inst.Output;
										IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings = inst.Mappings;
										Queue<Annotation<TOffset>> queue = inst.Queue;
										if (_operations != null)
										{
											if (!IsInstanceReuseable(inst))
											{
												output = inst.Output.DeepClone();

												Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
													.Zip(output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
												mappings = inst.Mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
												queue = new Queue<Annotation<TOffset>>(inst.Queue);
											}

											for (int i = 0; i < arc.Input.EnqueueCount; i++)
												queue.Enqueue(inst.Annotation);

											ExecuteOutputs(arc.Outputs, output, mappings, queue);
										}

										FstInstance[] instances = AdvanceFst(data, endAnchor, inst.Annotation, inst.Registers, output, mappings, queue, varBindings, arc,
											taskResults, inst.Depth, inst.Priorities).ToArray();
										to.PushRange(instances);

										if (IsDeterministic)
											break;
										varBindings = null;
									}
								}
							}

							if (taskResults.Count > 0)
							{
								lock (curResults)
								{
									curResults.AddRange(taskResults);
								}
							}
						});
					from = to;
				}

				if (curResults.Count > 0)
				{
					if (resultList == null)
						resultList = new List<FstResult<TData, TOffset>>();
					curResults.Sort(ResultCompare);
					resultList.AddRange(curResults);
					if (!allMatches)
						break;
				}

				if (startAnchor)
					break;
			}

			if (resultList == null)
			{
				results = null;
				return false;
			}

			results = allMatches ? resultList.Distinct(_fsaMatchComparer) : resultList;
			return true;
		}

		private bool IsInstanceReuseable(FstInstance inst)
		{
			if (inst.State.Arcs.Count <= 1)
				return true;

			return IsDeterministic && inst.State.Arcs.All(a => !a.Input.IsEpsilon);
		}

		private void ExecuteOutputs(IEnumerable<Output<TData, TOffset>> outputs, TData output, IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings,
			Queue<Annotation<TOffset>> queue)
		{
			foreach (Output<TData, TOffset> outputAction in outputs)
			{
				Annotation<TOffset> inputAnn = queue.Dequeue();
				Annotation<TOffset> outputAnn = mappings[inputAnn];
				outputAction.UpdateOutput(output, outputAnn, _operations);
			}
		}

		private int ResultCompare(FstResult<TData, TOffset> x, FstResult<TData, TOffset> y)
		{
			int compare = x.Priority.CompareTo(y.Priority);
			if (compare != 0)
				return compare;

			compare = -x.Depth.CompareTo(y.Depth);
			if (IsDeterministic)
			{
				compare = x.IsLazy ? -compare : compare;
			}
			else if (compare == 0)
			{
				foreach (Tuple<int, int> priorityPair in x.Priorities.Zip(y.Priorities))
				{
					compare = priorityPair.Item1.CompareTo(priorityPair.Item2);
					if (compare != 0)
						break;
				}
			}
			return compare;
		}

		private Annotation<TOffset> InitializeStack(TData data, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
			List<TagMapCommand> cmds, ConcurrentStack<FstInstance> from, HashSet<Annotation<TOffset>> initAnns, int depth)
		{
			TOffset offset = ann.Span.GetStart(_dir);

			var newRegisters = (NullableValue<TOffset>[,]) registers.Clone();
			ExecuteCommands(newRegisters, cmds, new NullableValue<TOffset>(ann.Span.GetStart(_dir)), new NullableValue<TOffset>());

			int curDepth = depth;
			for (Annotation<TOffset> a = ann; a != data.Annotations.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(offset); a = a.GetNextDepthFirst(_dir, _filter))
			{
				if (a.Optional)
				{
					Annotation<TOffset> nextAnn = a.GetNextDepthFirst(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
					if (nextAnn != null)
						InitializeStack(data, nextAnn, registers, cmds, from, initAnns, curDepth);
				}
				depth++;
			}

			curDepth = depth;
			bool cloneRegisters = false;
			for (; ann != data.Annotations.GetEnd(_dir) && ann.Span.GetStart(_dir).Equals(offset); ann = ann.GetNextDepthFirst(_dir, _filter))
			{
				if (!initAnns.Contains(ann))
				{
					TData o = data;
					Dictionary<Annotation<TOffset>, Annotation<TOffset>> m = null;
					Queue<Annotation<TOffset>> q = null;
					if (_operations != null)
					{
						o = data.DeepClone();
						m = data.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
							.Zip(o.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
						q = new Queue<Annotation<TOffset>>();
					}
					from.Push(new FstInstance(StartState, ann, cloneRegisters ? (NullableValue<TOffset>[,]) newRegisters.Clone() : newRegisters,
						o, m, q, new VariableBindings(), new HashSet<State<TData, TOffset>>(), curDepth, IsDeterministic ? null : new int[0]));
					initAnns.Add(ann);
					cloneRegisters = true;
				}
				curDepth++;
			}

			return ann;
		}

		private static void ExecuteCommands(NullableValue<TOffset>[,] registers, IEnumerable<TagMapCommand> cmds,
			NullableValue<TOffset> start, NullableValue<TOffset> end)
		{
			foreach (TagMapCommand cmd in cmds)
			{
				if (cmd.Src == TagMapCommand.CurrentPosition)
				{
					registers[cmd.Dest, 0] = start;
					registers[cmd.Dest, 1] = end;
				}
				else
				{
					registers[cmd.Dest, 0] = registers[cmd.Src, 0];
					registers[cmd.Dest, 1] = registers[cmd.Src, 1];
				}
			}
		}

		private IEnumerable<FstInstance> AdvanceFst(TData data, bool endAnchor, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output,
			IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings, Queue<Annotation<TOffset>> queue, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int depth, int[] priorities)
		{
			Annotation<TOffset> nextAnn = ann.GetNextDepthFirst(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
			TOffset nextOffset = nextAnn == data.Annotations.GetEnd(_dir) ? data.Annotations.GetLast(_dir, _filter).Span.GetEnd(_dir) : nextAnn.Span.GetStart(_dir);
			TOffset end = ann.Span.GetEnd(_dir);
			var newRegisters = (NullableValue<TOffset>[,]) registers.Clone();
			ExecuteCommands(newRegisters, arc.Commands, new NullableValue<TOffset>(nextOffset), new NullableValue<TOffset>(end));

			CheckAccepting(data, nextAnn, endAnchor, newRegisters, output, varBindings, arc, curResults, depth, priorities);

			if (nextAnn != data.Annotations.GetEnd(_dir))
			{
				int curDepth = depth + 1;
				var anns = new List<Annotation<TOffset>>();
				bool cloneOutputs = false;
				for (Annotation<TOffset> curAnn = nextAnn; curAnn != data.Annotations.GetEnd(_dir) && curAnn.Span.GetStart(_dir).Equals(nextOffset); curAnn = curAnn.GetNextDepthFirst(_dir, _filter))
				{
					if (curAnn.Optional)
					{
						foreach (FstInstance ni in AdvanceFst(data, endAnchor, curAnn, registers, output, mappings, queue, varBindings, arc, curResults, curDepth, priorities))
						{
							yield return ni;
							cloneOutputs = true;
						}
					}
					curDepth++;
					anns.Add(curAnn);
				}

				curDepth = depth + 1;
				bool cloneRegisters = false;
				foreach (Annotation<TOffset> curAnn in anns)
				{
					TData o = output;
					IDictionary<Annotation<TOffset>, Annotation<TOffset>> m = mappings;
					Queue<Annotation<TOffset>> q = queue;
					if (_operations != null && cloneOutputs)
					{
						o = output.DeepClone();

						Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
							.Zip(o.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
						m = mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
						q = new Queue<Annotation<TOffset>>(queue);
					}

					yield return new FstInstance(arc.Target, curAnn, cloneRegisters ?  (NullableValue<TOffset>[,]) newRegisters.Clone() : newRegisters, o, m, q,
						cloneOutputs ? varBindings.DeepClone() : varBindings, new HashSet<State<TData, TOffset>>(), curDepth, UpdatePriorities(priorities, arc.Priority));

					curDepth++;
					cloneOutputs = true;
					cloneRegisters = true;
				}
			}
			else
			{
				yield return new FstInstance(arc.Target, nextAnn, newRegisters, output, mappings, queue, varBindings, new HashSet<State<TData, TOffset>>(),
					depth + 1, UpdatePriorities(priorities, arc.Priority));
			}
		}

		private static int[] UpdatePriorities(int[] priorities, int priority)
		{
			int[] p = null;
			if (priorities != null)
			{
				p = new int[priorities.Length + 1];
				priorities.CopyTo(p, 0);
				p[p.Length - 1] = priority;
			}
			return p;
		}

		private FstInstance EpsilonAdvanceFst(TData data, bool endAnchor, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output, IDictionary<Annotation<TOffset>,
			Annotation<TOffset>> mappings, Queue<Annotation<TOffset>> queue, VariableBindings varBindings, ISet<State<TData, TOffset>> visited, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int depth, int[] priorities)
		{
			Annotation<TOffset> prevAnn = ann.GetPrevDepthFirst(_dir, (cur, prev) => !cur.Span.Overlaps(prev.Span) && _filter(prev));
			ExecuteCommands(registers, arc.Commands, new NullableValue<TOffset>(ann.Span.GetStart(_dir)), new NullableValue<TOffset>(prevAnn.Span.GetEnd(_dir)));
			CheckAccepting(data, ann, endAnchor, registers, output, varBindings, arc, curResults, depth, priorities);
			visited.Add(arc.Target);
			return new FstInstance(arc.Target, ann, registers, output, mappings, queue, varBindings, visited, depth, UpdatePriorities(priorities, arc.Priority));
		}

		private void CheckAccepting(TData data, Annotation<TOffset> ann, bool endAnchor, NullableValue<TOffset>[,] registers, TData output,
			VariableBindings varBindings, Arc<TData, TOffset> arc, List<FstResult<TData, TOffset>> curResults, int depth, int[] priorities)
		{
			if (arc.Target.IsAccepting && (!endAnchor || ann == data.Annotations.GetEnd(_dir)))
			{
				var matchRegisters = (NullableValue<TOffset>[,]) registers.Clone();
				ExecuteCommands(matchRegisters, arc.Target.Finishers, new NullableValue<TOffset>(), new NullableValue<TOffset>());
				if (arc.Target.AcceptInfos.Count > 0)
				{
					foreach (AcceptInfo<TData, TOffset> acceptInfo in arc.Target.AcceptInfos)
					{
						var candidate = new FstResult<TData, TOffset>(acceptInfo.ID, matchRegisters, _operations != null ? output.DeepClone() : output, varBindings.DeepClone(),
							acceptInfo.Priority, arc.Target.IsLazy, ann, depth, UpdatePriorities(priorities, arc.Priority));
						if (acceptInfo.Acceptable == null || acceptInfo.Acceptable(data, candidate))
							curResults.Add(candidate);
					}
				}
				else
				{
					curResults.Add(new FstResult<TData, TOffset>(null, matchRegisters, output.DeepClone(), varBindings.DeepClone(), -1, arc.Target.IsLazy, ann,
						depth, UpdatePriorities(priorities, arc.Priority)));
				}
			}
		}

		public Fst<TData, TOffset> Determinize()
		{
			return Optimize(DeterministicGetArcs, true);
		}

		public bool TryDeterminize(out Fst<TData, TOffset> fst)
		{
			if (IsDeterminizable)
			{
				fst = Determinize();
				return true;
			}

			fst = null;
			return false;
		}

		public Fst<TData, TOffset> EpsilonRemoval()
		{
			return Optimize(EpsilonRemovalGetArcs, false);
		}

		private class NfaStateInfo : IEquatable<NfaStateInfo>, IComparable<NfaStateInfo>
		{
			private readonly State<TData, TOffset> _nfaState;
			private readonly List<Output<TData, TOffset>> _outputs;
			private readonly Dictionary<int, int> _tags;
			private readonly int _lastPriority;
			private readonly int _maxPriority;

			public NfaStateInfo(State<TData, TOffset> nfaState, IEnumerable<Output<TData, TOffset>> outputs, int maxPriority = 0, int lastPriority = 0, IDictionary<int, int> tags = null)
			{
				_nfaState = nfaState;
				_outputs = outputs.ToList();
				_maxPriority = maxPriority;
				_lastPriority = lastPriority;
				_tags = tags == null ? new Dictionary<int, int>() : new Dictionary<int, int>(tags);
			}

			public State<TData, TOffset> NfaState
			{
				get
				{
					return _nfaState;
				}
			}

			public List<Output<TData, TOffset>> Outputs
			{
				get { return _outputs; }
			}

			public int MaxPriority
			{
				get { return _maxPriority; }
			}

			public int LastPriority
			{
				get { return _lastPriority; }
			}

			public Dictionary<int, int> Tags
			{
				get
				{
					return _tags;
				}
			}

			public override int GetHashCode()
			{
				int code = 23;
				code = code * 31 + _nfaState.GetHashCode();
				code = code * 31 + _outputs.GetSequenceHashCode();
				code = code * 31 + _tags.Keys.Aggregate(0, (current, tag) => current ^ tag);
				return code;
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as NfaStateInfo);
			}

			public bool Equals(NfaStateInfo other)
			{
				if (other == null)
					return false;

				if (_tags.Count != other._tags.Count)
					return false;

				if (_tags.Keys.Any(tag => !other._tags.ContainsKey(tag)))
					return false;

				return _nfaState == other._nfaState && _outputs.SequenceEqual(other._outputs);
			}

			public int CompareTo(NfaStateInfo other)
			{
				if (other == null)
					return 1;
				int res = _maxPriority.CompareTo(other._maxPriority);
				if (res != 0)
					return res;

				return _lastPriority.CompareTo(other._lastPriority);
			}

			public override string ToString()
			{
				return string.Format("State {0}", _nfaState.Index);
			}
		}

		private class SubsetState : IEquatable<SubsetState>
		{
			private readonly HashSet<NfaStateInfo> _nfaStates;

			public SubsetState(IEnumerable<NfaStateInfo> nfaStates)
			{
				_nfaStates = new HashSet<NfaStateInfo>(nfaStates);
			}

			public IEnumerable<NfaStateInfo> NfaStates
			{
				get { return _nfaStates; }
			}

			public State<TData, TOffset> State { get; set; }

			public override bool Equals(object obj)
			{
				var other = obj as SubsetState;
				return other != null && Equals(other);
			}

			public bool Equals(SubsetState other)
			{
				return other != null && _nfaStates.SetEquals(other._nfaStates);
			}

			public override int GetHashCode()
			{
				return _nfaStates.Aggregate(0, (code, state) => code ^ state.GetHashCode());
			}
		}

		private static IEnumerable<Tuple<SubsetState, Input, IEnumerable<Output<TData, TOffset>>, int>> DeterministicGetArcs(SubsetState from)
		{
			ILookup<Input, NfaStateInfo> conditions = from.NfaStates
				.SelectMany(state => state.NfaState.Arcs, (state, arc) => new { State = state, Arc = arc} )
				.Where(stateArc => !stateArc.Arc.Input.IsEpsilon)
				.ToLookup(stateArc => stateArc.Arc.Input, stateArc => new NfaStateInfo(stateArc.Arc.Target, stateArc.State.Outputs.Concat(stateArc.Arc.Outputs),
					Math.Max(stateArc.Arc.Priority, stateArc.State.MaxPriority), stateArc.Arc.Priority, stateArc.State.Tags));

			var preprocessedConditions = new List<Tuple<FeatureStruct, IEnumerable<FeatureStruct>, IEnumerable<NfaStateInfo>>>
				{
					Tuple.Create((FeatureStruct) null, Enumerable.Empty<FeatureStruct>(), Enumerable.Empty<NfaStateInfo>())
				};
			foreach (IGrouping<Input, NfaStateInfo> cond in conditions)
			{
				var temp = new List<Tuple<FeatureStruct, IEnumerable<FeatureStruct>, IEnumerable<NfaStateInfo>>>();
				foreach (Tuple<FeatureStruct, IEnumerable<FeatureStruct>, IEnumerable<NfaStateInfo>> preprocessedCond in preprocessedConditions)
				{
					FeatureStruct newCond;
					if (preprocessedCond.Item1 == null)
						newCond = cond.Key.FeatureStruct;
					else if (!preprocessedCond.Item1.Unify(cond.Key.FeatureStruct, false, new VariableBindings(), out newCond))
						newCond = null;
					if (newCond != null)
						temp.Add(Tuple.Create(newCond, preprocessedCond.Item2, preprocessedCond.Item3.Concat(cond)));

					if (!cond.Key.FeatureStruct.IsEmpty)
						temp.Add(Tuple.Create(preprocessedCond.Item1, preprocessedCond.Item2.Concat(cond.Key.FeatureStruct), preprocessedCond.Item3));
				}
				preprocessedConditions = temp;
			}

			foreach (Tuple<FeatureStruct, IEnumerable<FeatureStruct>, IEnumerable<NfaStateInfo>> preprocessedCond in preprocessedConditions.Where(pc => pc.Item1 != null))
			{
				IGrouping<NfaStateInfo, NfaStateInfo>[] groups = preprocessedCond.Item3.GroupBy(s => s).ToArray();
				if (groups.Length > 0)
				{
					preprocessedCond.Item1.Freeze();
					var input = new Input(preprocessedCond.Item1, preprocessedCond.Item2, 1);
					if (input.IsSatisfiable)
					{
						foreach (Tuple<SubsetState, Input, IEnumerable<Output<TData, TOffset>>, int> arc in GetAllArcsForInput(input, from, groups, 0, Enumerable.Empty<NfaStateInfo>()))
							yield return arc;
					}
				}
			}
		}

		private static IEnumerable<Tuple<SubsetState, Input, IEnumerable<Output<TData, TOffset>>, int>> GetAllArcsForInput(Input input, SubsetState curSubsetState, IGrouping<NfaStateInfo, NfaStateInfo>[] groups, int index, IEnumerable<NfaStateInfo> states)
		{
			if (index == groups.Length)
			{
				NfaStateInfo[] targetStates = EpsilonClosure(states.Select(s => new NfaStateInfo(s.NfaState, s.Outputs, s.MaxPriority, s.LastPriority, s.Tags)), GetFirstFreeIndex(curSubsetState)).ToArray();
				if (targetStates.Length == 0)
					yield break;
				var commonPrefix = new List<Output<TData, TOffset>>();
				bool first = true;
				int enqueueCount = input.EnqueueCount + targetStates.Select(s => s.NfaState.Arcs.Where(a => a.Input.IsEpsilon).Select(a => a.Input.EnqueueCount).Concat(0).Max()).Sum();
				foreach (NfaStateInfo state in targetStates)
				{
					for (int i = 0; i < state.Outputs.Count; i++)
					{
						if (first)
							commonPrefix.Add(state.Outputs[i]);
						else if (i < commonPrefix.Count && !commonPrefix[i].Equals(state.Outputs[i]))
							commonPrefix.RemoveRange(i, commonPrefix.Count - i);
					}

					int dequeueCount = enqueueCount - state.Outputs.Count;
					for (int i = 0; i < dequeueCount; i++)
						state.Outputs.Add(new NullOutput<TData, TOffset>());
					first = false;
				}

				if (commonPrefix.Count > 0)
				{
					foreach (NfaStateInfo state in targetStates)
						state.Outputs.RemoveRange(0, commonPrefix.Count);
				}

				yield return Tuple.Create(new SubsetState(targetStates), new Input(input.FeatureStruct, input.NegatedFeatureStructs, enqueueCount), (IEnumerable<Output<TData, TOffset>>) commonPrefix, 0);
			}
			else
			{
				foreach (NfaStateInfo state in groups[index])
				{
					foreach (Tuple<SubsetState, Input, IEnumerable<Output<TData, TOffset>>, int> arc in GetAllArcsForInput(input, curSubsetState, groups, index + 1, states.Concat(state)))
						yield return arc;
				}
			}
		}

		private static IEnumerable<Tuple<SubsetState, Input, IEnumerable<Output<TData, TOffset>>, int>> EpsilonRemovalGetArcs(SubsetState from)
		{
			foreach (NfaStateInfo q in from.NfaStates)
			{
				foreach (Arc<TData, TOffset> arc in q.NfaState.Arcs.Where(a => !a.Input.IsEpsilon))
				{
					var nsi = new NfaStateInfo(arc.Target, Enumerable.Empty<Output<TData, TOffset>>(), Math.Max(q.MaxPriority, arc.Priority), arc.Priority, q.Tags);
					var target = new SubsetState(EpsilonClosure(nsi.ToEnumerable(), GetFirstFreeIndex(from)));
					yield return Tuple.Create(target, arc.Input, (IEnumerable<Output<TData, TOffset>>) arc.Outputs, target.NfaStates.Min(s => s.MaxPriority));
				}
			}
		}

		private void MarkArcPriorities()
		{
			var visited = new HashSet<State<TData, TOffset>>();
			var todo = new Stack<Arc<TData, TOffset>>(StartState.Arcs.Reverse());
			int nextPriority = 0;
			while (todo.Count > 0)
			{
				Arc<TData, TOffset> arc = todo.Pop();
				arc.Priority = nextPriority++;
				if (!visited.Contains(arc.Target))
				{
					visited.Add(arc.Target);
					foreach (Arc<TData, TOffset> nextArc in arc.Target.Arcs)
						todo.Push(nextArc);
				}
			}
		}

		private Fst<TData, TOffset> Optimize(Func<SubsetState, IEnumerable<Tuple<SubsetState, Input, IEnumerable<Output<TData, TOffset>>, int>>> arcsSelector, bool deterministic)
		{
			MarkArcPriorities();

			var newFst = new Fst<TData, TOffset>(_operations, _dir, _filter) {IsDeterministic = deterministic, _nextTag = _nextTag};
			foreach (KeyValuePair<string, int> kvp in _groups)
				newFst._groups[kvp.Key] = kvp.Value;

			var registerIndices = new Dictionary<Tuple<int, int>, int>();

			var startState = new NfaStateInfo(StartState, Enumerable.Empty<Output<TData, TOffset>>());
			var subsetStart = new SubsetState(EpsilonClosure(startState.ToEnumerable(), 0));

			CreateOptimizedState(newFst, subsetStart, registerIndices);
			newFst.StartState = subsetStart.State;

			var cmdTags = new Dictionary<int, int>();
			foreach (NfaStateInfo state in subsetStart.NfaStates)
			{
				foreach (KeyValuePair<int, int> kvp in state.Tags)
					cmdTags[kvp.Key] = kvp.Value;
			}
			newFst._initializers.AddRange(from kvp in cmdTags
										  select new TagMapCommand(GetRegisterIndex(registerIndices, kvp.Key, kvp.Value), TagMapCommand.CurrentPosition));

			var subsetStates = new Dictionary<SubsetState, SubsetState> { {subsetStart, subsetStart} };
			var unmarkedSubsetStates = new Queue<SubsetState>();
			unmarkedSubsetStates.Enqueue(subsetStart);

			while (unmarkedSubsetStates.Count != 0)
			{
				SubsetState curSubsetState = unmarkedSubsetStates.Dequeue();

				foreach (Tuple<SubsetState, Input, IEnumerable<Output<TData, TOffset>>, int> t in arcsSelector(curSubsetState))
					CreateOptimizedArc(newFst, subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, t.Item1, t.Item2, t.Item3, t.Item4, deterministic);
			}

			var regNums = new Dictionary<int, int>();
			for (newFst._registerCount = 0; newFst._registerCount < _nextTag; newFst._registerCount++)
				regNums[newFst._registerCount] = newFst._registerCount;

			foreach (State<TData, TOffset> state in newFst._states)
			{
				RenumberCommands(newFst, regNums, state.Finishers);
				foreach (Arc<TData, TOffset> arc in state.Arcs)
					RenumberCommands(newFst, regNums, arc.Commands);
			}

			return newFst;
		}

		private void RenumberCommands(Fst<TData, TOffset> newFst, Dictionary<int, int> regNums, List<TagMapCommand> cmds)
		{
			foreach (TagMapCommand cmd in cmds)
			{
				if (cmd.Src != TagMapCommand.CurrentPosition && !regNums.ContainsKey(cmd.Src))
					regNums[cmd.Src] = newFst._registerCount++;
				if (!regNums.ContainsKey(cmd.Dest))
					regNums[cmd.Dest] = newFst._registerCount++;
				if (cmd.Src != TagMapCommand.CurrentPosition)
					cmd.Src = regNums[cmd.Src];
				cmd.Dest = regNums[cmd.Dest];
			}
			cmds.Sort();
		}

		private void CreateOptimizedArc(Fst<TData, TOffset> newFst, Dictionary<SubsetState, SubsetState> subsetStates, Queue<SubsetState> unmarkedSubsetStates,
			Dictionary<Tuple<int, int>, int> registerIndices, SubsetState curSubsetState, SubsetState target, Input input, IEnumerable<Output<TData, TOffset>> outputs, int priority, bool deterministic)
		{
			var cmdTags = new Dictionary<int, int>();
			foreach (NfaStateInfo targetState in target.NfaStates)
			{
				foreach (KeyValuePair<int, int> tag in targetState.Tags)
				{
					bool found = false;
					foreach (NfaStateInfo curState in curSubsetState.NfaStates)
					{
						if (curState.Tags.Contains(tag))
						{
							found = true;
							break;
						}
					}

					if (!found)
						cmdTags[tag.Key] = tag.Value;
				}
			}

			var cmds = new List<TagMapCommand>();
			//if (!deterministic)
			//{
			//    var tags = new HashSet<int>();
			//    foreach (NfaStateInfo srcState in curSubsetState.NfaStates)
			//    {
			//        foreach (KeyValuePair<int, int> tag in srcState.Tags)
			//        {
			//            if (tag.Value > 0 && !tags.Contains(tag.Key))
			//            {
			//                tags.Add(tag.Key);
			//                int src = GetRegisterIndex(registerIndices, tag.Key, tag.Value);
			//                int dest = GetRegisterIndex(registerIndices, tag.Key, 0);
			//                cmds.Add(new TagMapCommand(dest, src));
			//            }
			//        }
			//    }
			//}

			SubsetState subsetState;
			if (subsetStates.TryGetValue(target, out subsetState))
			{
				ReorderTagIndices(target, subsetState, registerIndices, cmds);
				target = subsetState;
			}
			else
			{
				subsetStates.Add(target, target);
				unmarkedSubsetStates.Enqueue(target);
				CreateOptimizedState(newFst, target, registerIndices);
			}

			foreach (KeyValuePair<int, int> tag in cmdTags)
			{
				int reg = GetRegisterIndex(registerIndices, tag.Key, tag.Value);
				bool found = false;
				foreach (TagMapCommand cmd in cmds)
				{
					if (cmd.Src == reg)
					{
						found = true;
						cmd.Src = TagMapCommand.CurrentPosition;
						break;
					}
				}
				if (!found)
					cmds.Add(new TagMapCommand(reg, TagMapCommand.CurrentPosition));
			}

			curSubsetState.State.Arcs.Add(input, outputs, target.State, cmds, priority);
		}

		private void CreateOptimizedState(Fst<TData, TOffset> newFst, SubsetState subsetState, Dictionary<Tuple<int, int>, int> registerIndices)
		{
			NfaStateInfo[] acceptingStates = (from state in subsetState.NfaStates
											  where state.NfaState.IsAccepting
											  orderby state descending
											  select state).ToArray();
			if (acceptingStates.Length > 0)
			{
				AcceptInfo<TData, TOffset>[] acceptInfos = acceptingStates.SelectMany(state => state.NfaState.AcceptInfos).ToArray();
				bool isLazy = IsLazyAcceptingState(subsetState);

				var finishers = new List<TagMapCommand>();
				var finishedTags = new HashSet<int>();
				var remaining = new List<NfaStateInfo>();
				bool accepting = false;
				foreach (NfaStateInfo acceptingState in acceptingStates)
				{
					if (acceptingState.Outputs.Count == 0)
						accepting = true;
					else
						remaining.Add(acceptingState);

					foreach (KeyValuePair<int, int> tag in acceptingState.Tags)
					{
						if (tag.Value > 0 && !finishedTags.Contains(tag.Key))
						{
							finishedTags.Add(tag.Key);
							int src = GetRegisterIndex(registerIndices, tag.Key, tag.Value);
							int dest = GetRegisterIndex(registerIndices, tag.Key, 0);
							finishers.Add(new TagMapCommand(dest, src));
						}
					}
				}
				if (accepting)
				{
					subsetState.State = newFst.CreateAcceptingState(acceptInfos, finishers, isLazy);
				}
				else
				{
					subsetState.State = newFst.CreateState();
					if (remaining.Count > 0)
					{
						foreach (NfaStateInfo state in remaining)
							subsetState.State.Arcs.Add(new Input(0), state.Outputs, newFst.CreateAcceptingState(acceptInfos, finishers, isLazy));
					}
				}
			}
			else
			{
				subsetState.State = newFst.CreateState();
			}
		}

		private bool IsLazyAcceptingState(SubsetState state)
		{
			//Arc<TData, TOffset> arc = state.NfaStates.SelectMany(s => s.NfaState.Arcs).MinBy(a => a.Priority);
			//State<TData, TOffset> curState = arc.Target;
			//while (!curState.IsAccepting)
			//{
			//    Arc<TData, TOffset> highestPriArc = curState.Arcs.MinBy(a => a.Priority);
			//    if (highestPriArc.Condition != null)
			//        break;
			//    curState = highestPriArc.Target;
			//}
			//return curState.IsAccepting;

			//foreach (Arc<TData, TOffset> arc in state.NfaStates.Min().NfaState.Arcs)
			//{
				//State<TData, TOffset> curState = arc.Target;
				State<TData, TOffset> curState = state.NfaStates.Min().NfaState;
				while (!curState.IsAccepting)
				{
					Arc<TData, TOffset> highestPriArc = curState.Arcs.MinBy(a => a.Priority);
					if (!highestPriArc.Input.IsEpsilon)
						break;
					curState = highestPriArc.Target;
				}

				if (curState.IsAccepting)
				{
					if ((from s in state.NfaStates
						 from tran in s.NfaState.Arcs
						 where !tran.Input.IsEpsilon
						 select tran.Input).Any())
					{
						return true;
					}
					//break;
				}
			//}
			return false;
		}

		private void ReorderTagIndices(SubsetState from, SubsetState to, Dictionary<Tuple<int, int>, int> registerIndices,
			List<TagMapCommand> cmds)
		{
			var newCmds = new List<TagMapCommand>();
			var reorderedIndices = new Dictionary<Tuple<int, int>, int>();
			var reorderedStates = new Dictionary<Tuple<int, int>, NfaStateInfo>();

			foreach (NfaStateInfo fromState in from.NfaStates)
			{
				foreach (NfaStateInfo toState in to.NfaStates)
				{
					if (!toState.NfaState.Equals(fromState.NfaState))
						continue;

					foreach (KeyValuePair<int, int> fromTag in fromState.Tags)
					{
						Tuple<int, int> tagIndex = Tuple.Create(fromTag.Key, toState.Tags[fromTag.Key]);

						int index;
						if (reorderedIndices.TryGetValue(tagIndex, out index))
						{
							NfaStateInfo state = reorderedStates[tagIndex];
							if (index != fromTag.Value && (state.MaxPriority <= fromState.MaxPriority && state.LastPriority <= fromState.LastPriority))
								continue;

							int src = GetRegisterIndex(registerIndices, fromTag.Key, index);
							int dest = GetRegisterIndex(registerIndices, fromTag.Key, tagIndex.Item2);
							newCmds.RemoveAll(cmd => cmd.Src == src && cmd.Dest == dest);
						}

						if (tagIndex.Item2 != fromTag.Value)
						{
							int src = GetRegisterIndex(registerIndices, fromTag.Key, fromTag.Value);
							int dest = GetRegisterIndex(registerIndices, fromTag.Key, tagIndex.Item2);
							newCmds.Add(new TagMapCommand(dest, src));
						}

						reorderedIndices[tagIndex] = fromTag.Value;
						reorderedStates[tagIndex] = fromState;
					}
				}

			}
			cmds.AddRange(newCmds);
		}

		private static IEnumerable<NfaStateInfo> EpsilonClosure(IEnumerable<NfaStateInfo> from, int index)
		{
			var stack = new Stack<NfaStateInfo>();
			var closure = new Dictionary<NfaStateInfo, NfaStateInfo>(new AnonymousEqualityComparer<NfaStateInfo>((nsi1, nsi2) => nsi1.NfaState == nsi2.NfaState && nsi1.Outputs.SequenceEqual(nsi2.Outputs),
				nsi => (23 * 31 + nsi.NfaState.GetHashCode()) * 31 + nsi.Outputs.GetSequenceHashCode()));
			foreach (NfaStateInfo state in from)
			{
				stack.Push(state);
				closure[state] = state;
			}

			while (stack.Count != 0)
			{
				NfaStateInfo topState = stack.Pop();

				foreach (Arc<TData, TOffset> arc in topState.NfaState.Arcs)
				{
					if (arc.Input.IsEpsilon)
					{
						var newState = new NfaStateInfo(arc.Target, topState.Outputs.Concat(arc.Outputs), Math.Max(arc.Priority, topState.MaxPriority), arc.Priority, topState.Tags);
						NfaStateInfo temp;
						if (closure.TryGetValue(newState, out temp))
						{
							if (temp.MaxPriority < newState.MaxPriority)
								continue;
							if (temp.MaxPriority == newState.MaxPriority && temp.LastPriority <= newState.LastPriority)
								continue;
						}

						if (arc.Tag != -1)
							newState.Tags[arc.Tag] = index;

						closure[newState] = newState;
						stack.Push(newState);
					}
				}
			}

			return closure.Values;
		}

		private static int GetFirstFreeIndex(SubsetState subsetState)
		{
			int firstFreeIndex = -1;
			foreach (NfaStateInfo state in subsetState.NfaStates)
			{
				foreach (KeyValuePair<int, int> tag in state.Tags)
					firstFreeIndex = Math.Max(firstFreeIndex, tag.Value);
			}
			return firstFreeIndex + 1;
		}

		private int GetRegisterIndex(Dictionary<Tuple<int, int>, int> registerIndices, int tag, int index)
		{
			if (index == 0)
				return tag;

			Tuple<int, int> key = Tuple.Create(tag, index);
			int registerIndex;
			if (registerIndices.TryGetValue(key, out registerIndex))
				return registerIndex;

			registerIndex = _nextTag + registerIndices.Count;
			registerIndices[key] = registerIndex;
			return registerIndex;
		}

		public bool IsDeterminizable
		{
			get
			{
				if (HasEpsilonLoop)
					return false;

				return !HasUnboundedLoopsWithNonidenticalOutput();
			}
		}

		public bool HasEpsilonLoop
		{
			get
			{
				var visitedStates = new HashSet<State<TData, TOffset>>();
				var visitedArcs = new HashSet<Arc<TData, TOffset>>();
				foreach (State<TData, TOffset> state in _states)
				{
					if (HasEpsilonLoopImpl(state, state, visitedStates, visitedArcs))
						return true;
				}
				return false;
			}
		}

		private static bool HasEpsilonLoopImpl(State<TData, TOffset> state1, State<TData, TOffset> state2,
			HashSet<State<TData, TOffset>> visitedStates, HashSet<Arc<TData, TOffset>> visitedArcs)
		{
			if (visitedStates.Contains(state2))
				return true;
			visitedStates.Add(state2);
			foreach (Arc<TData, TOffset> arc in state2.Arcs)
			{
				if (arc.Input == null && !visitedArcs.Contains(arc))
				{
					visitedArcs.Add(arc);
					if (arc.Target.Equals(state1))
						return true;
					if (HasEpsilonLoopImpl(state1, arc.Target, visitedStates, visitedArcs))
						return true;
				}
				visitedArcs.Remove(arc);
			}
			visitedStates.Remove(state1);
			return false;
		}

		private static IEnumerable<Tuple<Arc<TData, TOffset>, Arc<TData, TOffset>>> GetAmbiguousArcs(State<TData, TOffset> state)
		{
			for (int i = 0; i < state.Arcs.Count - 1; i++)
			{
				FeatureStruct[] inputs1 = ClosuredInputs(state.Arcs[i]).ToArray();
				for (int j = i + 1; j < state.Arcs.Count; j++)
				{
					bool ambiguous = false;
					foreach (FeatureStruct input2 in ClosuredInputs(state.Arcs[j]))
					{
						foreach (FeatureStruct input1 in inputs1)
						{
							if (input1.IsUnifiable(input2))
							{
								ambiguous = true;
								break;
							}
						}
						if (ambiguous)
							break;
					}

					if (ambiguous)
						yield return Tuple.Create(state.Arcs[i], state.Arcs[j]);
				}
			}
		}

		private bool HasUnboundedLoopsWithNonidenticalOutput()
		{
			foreach (State<TData, TOffset> state in _states)
			{
				foreach (Tuple<Arc<TData, TOffset>, Arc<TData, TOffset>> arcs in GetAmbiguousArcs(state))
				{
					Fst<TData, TOffset> fst1 = ExtractTransducer(state, arcs.Item1);
					Fst<TData, TOffset> fst2 = ExtractTransducer(state, arcs.Item2);

					if (fst1.HasLoop && fst2.HasLoop)
					{
						Fst<TData, TOffset> intersection = fst1.Intersect(fst2);
						if (intersection.HasLoop)
						{
							Fst<TData, TOffset> outputFsa1 = fst1.GetOutputAcceptor().Determinize();
							Fst<TData, TOffset> outputFsa2 = fst2.GetOutputAcceptor().Determinize();

							if (!outputFsa1.IsEquivalentTo(outputFsa2))
								return true;
						}
					}
				}
			}
			return false;
		}

		private Fst<TData, TOffset> ExtractTransducer(State<TData, TOffset> startState, Arc<TData, TOffset> arc)
		{
			var fst = new Fst<TData, TOffset>(_operations, _dir, _filter);
			fst.StartState = startState.IsAccepting ? fst.CreateAcceptingState(startState.AcceptInfos) : fst.CreateState();
			var copies = new Dictionary<State<TData, TOffset>, State<TData, TOffset>>();
			copies[startState] = fst.StartState;
			fst.StartState.Arcs.Add(arc.Input, arc.Outputs, Copy(fst, arc.Target, CopyAddArc, copies));
			return fst;
		}

		private static void CopyAddArc(State<TData, TOffset> state, Arc<TData, TOffset> arc, State<TData, TOffset> target)
		{
			if (arc.Input.EnqueueCount == 0)
			{
				if (arc.Tag != -1)
					state.Arcs.Add(target, arc.Tag);
				else
					state.Arcs.Add(target, arc.PriorityType);
			}
			else
			{
				state.Arcs.Add(arc.Input, arc.Outputs, target);
			}
		}

		private static State<TData, TOffset> Copy(Fst<TData, TOffset> fst, State<TData, TOffset> source,
			Action<State<TData, TOffset>, Arc<TData, TOffset>, State<TData, TOffset>> addArc, IDictionary<State<TData, TOffset>, State<TData, TOffset>> copies)
		{
			State<TData, TOffset> newState;
			if (copies.TryGetValue(source, out newState))
				return newState;

			newState = source.IsAccepting ? fst.CreateAcceptingState(source.AcceptInfos) : fst.CreateState();
			copies[source] = newState;
			foreach (Arc<TData, TOffset> arc in source.Arcs)
			{
				State<TData, TOffset> childState = Copy(fst, arc.Target, addArc, copies);
				addArc(newState, arc, childState);
			}
			return newState;
		}

		public bool HasLoop
		{
			get { return HasLoopImpl(StartState, new HashSet<State<TData, TOffset>>()); }
		}

		private static bool HasLoopImpl(State<TData, TOffset> state, HashSet<State<TData, TOffset>> visited)
		{
			if (visited.Contains(state))
				return true;
			visited.Add(state);
			foreach (Arc<TData, TOffset> arc in state.Arcs)
			{
				if (HasLoopImpl(arc.Target, visited))
					return true;
			}
			return false;
		}

		private static IEnumerable<FeatureStruct> ClosuredInputs(Arc<TData, TOffset> arc)
		{
			return EpsilonClosure(arc.Target).SelectMany(s => s.Arcs, (s, a) => a.Input).Concat(arc.Input)
				.Where(input => input != null).Select(input => input.FeatureStruct).Distinct(ValueEqualityComparer<FeatureStruct>.Instance);
		}

		private static IEnumerable<State<TData, TOffset>> EpsilonClosure(State<TData, TOffset> state)
		{
			var stack = new Stack<State<TData, TOffset>>();
			stack.Push(state);
			var closure = new HashSet<State<TData, TOffset>> {state};

			while (stack.Count != 0)
			{
				State<TData, TOffset> topState = stack.Pop();

				foreach (Arc<TData, TOffset> arc in topState.Arcs)
				{
					if (arc.Input == null)
					{
						closure.Add(arc.Target);
						stack.Push(arc.Target);
					}
				}
			}

			return closure;
		}

		public Fst<TData, TOffset> GetOutputAcceptor()
		{
			var fst = new Fst<TData, TOffset>(_dir, _filter) {_nextTag = _nextTag, _registerCount = _registerCount};
			foreach (KeyValuePair<string, int> kvp in _groups)
				fst._groups[kvp.Key] = kvp.Value;
			fst.StartState = Copy(fst, StartState, OutputAcceptorAddArc, new Dictionary<State<TData, TOffset>, State<TData, TOffset>>());
			return fst;
		}

		private static void OutputAcceptorAddArc(State<TData, TOffset> state, Arc<TData, TOffset> arc, State<TData, TOffset> target)
		{
			if (arc.Input.EnqueueCount == 0)
			{
				if (arc.Tag != -1)
					state.Arcs.Add(target, arc.Tag);
				else
					state.Arcs.Add(target, arc.PriorityType);
			}
			else if (arc.Outputs[0] is RemoveOutput<TData, TOffset>)
			{
				state.Arcs.Add(target);
			}
			else
			{
				state.Arcs.Add(arc.Outputs[0].FeatureStruct, target);
			}
		}

		public Fst<TData, TOffset> GetInputAcceptor()
		{
			var fst = new Fst<TData, TOffset>(_dir, _filter) {_nextTag = _nextTag, _registerCount = _registerCount};
			foreach (KeyValuePair<string, int> kvp in _groups)
				fst._groups[kvp.Key] = kvp.Value;
			fst.StartState = Copy(fst, StartState, InputAcceptorAddArc, new Dictionary<State<TData, TOffset>, State<TData, TOffset>>());
			return fst;
		}

		private static void InputAcceptorAddArc(State<TData, TOffset> state, Arc<TData, TOffset> arc, State<TData, TOffset> target)
		{
			if (arc.Input.EnqueueCount == 0)
			{
				if (arc.Tag != -1)
					state.Arcs.Add(target, arc.Tag);
				else
					state.Arcs.Add(target, arc.PriorityType);
			}
			else
			{
				state.Arcs.Add(arc.Input.FeatureStruct, target);
			}
		}

		public bool IsEquivalentTo(Fst<TData, TOffset> otherFst)
		{
			if (!IsDeterministic)
				throw new InvalidOperationException("The FSA must be deterministic.");
			if (!otherFst.IsDeterministic)
				throw new ArgumentException("otherFst must be deterministic.", "otherFst");

			var sets = new List<HashSet<State<TData, TOffset>>> {new HashSet<State<TData, TOffset>> {StartState, otherFst.StartState}};

			var stack = new Stack<Tuple<State<TData, TOffset>, State<TData, TOffset>>>();
			stack.Push(Tuple.Create(StartState, otherFst.StartState));
			while (stack.Count != 0)
			{
				Tuple<State<TData, TOffset>, State<TData, TOffset>> pair = stack.Pop();
				if (pair.Item1.IsAccepting != pair.Item2.IsAccepting)
					return false;

				var arcs2 = new HashSet<Arc<TData, TOffset>>(pair.Item2.Arcs);
				foreach (Arc<TData, TOffset> arc1 in pair.Item1.Arcs)
				{
					bool found = false;
					foreach (Arc<TData, TOffset> arc2 in arcs2)
					{
						int r1 = -1;
						int r2 = -1;
						if (arc1.Input.Equals(arc2.Input))
						{
							for (int i = 0; i < sets.Count; i++)
							{
								if (sets[i].Contains(arc1.Target))
									r1 = i;
								if (sets[i].Contains(arc2.Target))
									r2 = i;
								if (r1 != -1 && r2 != -1)
									break;
							}

							if (r1 == -1 && r2 == -1)
							{
								sets.Add(new HashSet<State<TData, TOffset>> {arc1.Target, arc2.Target});
								stack.Push(Tuple.Create(arc1.Target, arc2.Target));
							}
							else if (r1 == -1 && r2 != -1)
							{
								sets[r2].Add(arc1.Target);
								stack.Push(Tuple.Create(arc1.Target, arc2.Target));
							}
							else if (r1 != -1 && r2 == -1)
							{
								sets[r1].Add(arc2.Target);
								stack.Push(Tuple.Create(arc1.Target, arc2.Target));
							}
							else if (r1 != r2)
							{
								sets[r1].UnionWith(sets[r2]);
								sets.RemoveAt(r2);
								stack.Push(Tuple.Create(arc1.Target, arc2.Target));
							}
							arcs2.Remove(arc2);
							found = true;
							break;
						}
					}

					if (!found)
						return false;
				}

				if (arcs2.Count > 0)
					return false;
			}

			return true;
		}

		public void Minimize()
		{
		    if (!IsDeterministic)
		        throw new InvalidOperationException("The FSA must be deterministic to be minimized.");

			var acceptingStates = new HashSet<State<TData, TOffset>>();
			var nonacceptingStates = new HashSet<State<TData, TOffset>>();
			foreach (State<TData, TOffset> state in _states)
			{
				if (state.IsAccepting)
					acceptingStates.Add(state);
				else
					nonacceptingStates.Add(state);
			}

			var partitions = new List<HashSet<State<TData, TOffset>>> {nonacceptingStates};
			var waiting = new List<HashSet<State<TData, TOffset>>>();
			foreach (IGrouping<MinimizeStateInfo, State<TData, TOffset>> acceptingPartition in acceptingStates.GroupBy(s => new MinimizeStateInfo(s)))
			{
				var set = new HashSet<State<TData, TOffset>>(acceptingPartition);
				partitions.Add(set);
				waiting.Add(set);
			}

			while (waiting.Count > 0)
			{
				HashSet<State<TData, TOffset>> a = waiting.First();
				waiting.RemoveAt(0);
				foreach (IGrouping<MinimizeStateInfo, State<TData, TOffset>> x in _states.SelectMany(state => state.Arcs, (state, arc) => new MinimizeStateInfo(state, arc))
					.Where(msi => a.Contains(msi.Arc.Target)).GroupBy(msi => msi, msi => msi.State))
				{
					for (int i = partitions.Count - 1; i >= 0; i--)
					{
						HashSet<State<TData, TOffset>> y = partitions[i];
						var subset1 = new HashSet<State<TData, TOffset>>(x.Intersect(y));
						if (subset1.Count == 0)
							continue;

						var subset2 = new HashSet<State<TData, TOffset>>(y.Except(x));
						partitions[i] = subset1;
						if (subset2.Count > 0)
							partitions.Add(subset2);
						bool found = false;
						for (int j = 0; j < waiting.Count; j++)
						{
							if (waiting[j].SetEquals(y))
							{
								waiting[j] = subset1;
								if (subset2.Count > 0)
									waiting.Add(subset2);
								found = true;
								break;
							}
						}

						if (!found)
						{
							if (subset1.Count <= subset2.Count)
								waiting.Add(subset1);
							else if (subset2.Count > 0)
								waiting.Add(subset2);
						}
					}
				}
			}

			Dictionary<State<TData, TOffset>, State<TData, TOffset>> nondistinguisablePairs = partitions.Where(p => p.Count > 1)
				.SelectMany(p => p.Where(state => !state.Equals(p.First())), (p, state) => new {Key = state, Value = p.First()}).ToDictionary(pair => pair.Key, pair => pair.Value);
			if (nondistinguisablePairs.Count > 0)
			{
		        var statesToRemove = new HashSet<State<TData, TOffset>>(_states.Where(s => !s.Equals(StartState)));
		        foreach (State<TData, TOffset> state in _states)
		        {
		            foreach (Arc<TData, TOffset> arc in state.Arcs)
		            {
		                State<TData, TOffset> curState = arc.Target;
		                State<TData, TOffset> s;
		                while (nondistinguisablePairs.TryGetValue(curState, out s))
		                    curState = s;
		                arc.Target = curState;
		                statesToRemove.Remove(curState);
		            }
		        }
		        _states.RemoveAll(statesToRemove.Contains);
			}
		}

		private class MinimizeStateInfo : IEquatable<MinimizeStateInfo>
		{
			private readonly State<TData, TOffset> _state;
			private readonly Arc<TData, TOffset> _arc;

			public MinimizeStateInfo(State<TData, TOffset> state, Arc<TData, TOffset> arc = null)
			{
				_state = state;
				_arc = arc;
			}

			public State<TData, TOffset> State
			{
				get { return _state; }
			}

			public Arc<TData, TOffset> Arc
			{
				get { return _arc; }
			}

			public bool Equals(MinimizeStateInfo other)
			{
				if (other == null)
					return false;

				if (_state.IsAccepting != other._state.IsAccepting)
					return false;

				if (_state.IsAccepting && (!_state.Finishers.SequenceEqual(other._state.Finishers) || !_state.AcceptInfos.SequenceEqual(other._state.AcceptInfos)))
					return false;

				if (_arc == null)
					return other._arc == null;
				if (other._arc == null)
					return _arc == null;
				return _arc.Commands.SequenceEqual(other._arc.Commands) && _arc.Input.Equals(other._arc.Input);
			}

			public override bool Equals(object obj)
			{
				return obj != null && Equals(obj as MinimizeStateInfo);
			}

			public override int GetHashCode()
			{
				int code = 23;
				code = code * 31 + _state.IsAccepting.GetHashCode();
				if (_state.IsAccepting)
				{
					code = code * 31 + _state.Finishers.GetSequenceHashCode();
					code = code * 31 + _state.AcceptInfos.GetSequenceHashCode();
				}
				if (_arc != null)
				{
					code = code * 31 + _arc.Commands.GetSequenceHashCode();
					code = code * 31 + _arc.Input.GetHashCode();
				}
				return code;
			}
		}

		public Fst<TData, TOffset> Intersect(Fst<TData, TOffset> other)
		{
			var newFst = new Fst<TData, TOffset>(_dir, _filter);
			newFst.StartState = StartState.IsAccepting && other.StartState.IsAccepting ? newFst.CreateAcceptingState() : newFst.CreateState();

			var queue = new Queue<Tuple<State<TData, TOffset>, State<TData, TOffset>>>();
			var newStates = new Dictionary<Tuple<State<TData, TOffset>, State<TData, TOffset>>, State<TData, TOffset>>();
			Tuple<State<TData, TOffset>, State<TData, TOffset>> pair = Tuple.Create(StartState, other.StartState);
			queue.Enqueue(pair);
			newStates[pair] = newFst.StartState;
			while (queue.Count > 0)
			{
				Tuple<State<TData, TOffset>, State<TData, TOffset>> p = queue.Dequeue();
				State<TData, TOffset> s = newStates[p];

				var newArcs = new List<Tuple<State<TData, TOffset>, State<TData, TOffset>, FeatureStruct, int, ArcPriorityType>>();
				foreach (Arc<TData, TOffset> arc1 in p.Item1.Arcs)
				{
					if (arc1.Input.IsEpsilon)
					{
						newArcs.Add(Tuple.Create(arc1.Target, p.Item2, (FeatureStruct) null, CopyTag(newFst, arc1.Tag), arc1.PriorityType));
					}
					else
					{
						foreach (Arc<TData, TOffset> arc2 in p.Item2.Arcs.Where(a => !a.Input.IsEpsilon))
						{
							FeatureStruct fs;
							if (arc1.Input.FeatureStruct.Unify(arc2.Input.FeatureStruct, out fs))
							{
								fs.Freeze();
								newArcs.Add(Tuple.Create(arc1.Target, arc2.Target, fs, -1, ArcPriorityType.Medium));
							}
						}
					}
				}

				foreach (Arc<TData, TOffset> arc2 in p.Item2.Arcs.Where(a => a.Input.IsEpsilon))
					newArcs.Add(Tuple.Create(p.Item1, arc2.Target, (FeatureStruct) null, CopyTag(newFst, arc2.Tag), arc2.PriorityType));

				foreach (Tuple<State<TData, TOffset>, State<TData, TOffset>, FeatureStruct, int, ArcPriorityType> newArc in newArcs)
				{
					Tuple<State<TData, TOffset>, State<TData, TOffset>> key = Tuple.Create(newArc.Item1, newArc.Item2);
					State<TData, TOffset> r;
					if (!newStates.TryGetValue(key, out r))
					{
						if (newArc.Item1.IsAccepting && newArc.Item2.IsAccepting)
							r = newFst.CreateAcceptingState(newArc.Item1.AcceptInfos.Concat(newArc.Item2.AcceptInfos));
						else
							r = newFst.CreateState();
						queue.Enqueue(key);
						newStates[key] = r;
					}

					if (newArc.Item3 == null)
					{
						if (newArc.Item4 != -1)
							s.Arcs.Add(r, newArc.Item4);
						else
							s.Arcs.Add(r, newArc.Item5);
					}
					else
					{
						s.Arcs.Add(newArc.Item3, r);
					}
				}
			}
			return newFst;
		}

		private int CopyTag(Fst<TData, TOffset> fst, int tag)
		{
			if (tag == -1)
				return -1;

			bool isStart = tag % 2 == 0;
			int startTag = isStart ? tag : tag - 1;
			string groupName = _groups.Single(kvp => kvp.Value == startTag).Key;
			return fst.GetTag(groupName, isStart);
		}

		public Fst<TData, TOffset> Compose(Fst<TData, TOffset> other)
		{
			var newFst = new Fst<TData, TOffset>(_operations, _dir, _filter);
			newFst.StartState = StartState.IsAccepting && other.StartState.IsAccepting ? newFst.CreateAcceptingState() : newFst.CreateState();

			var queue = new Queue<Tuple<State<TData, TOffset>, State<TData, TOffset>>>();
			var newStates = new Dictionary<Tuple<State<TData, TOffset>, State<TData, TOffset>>, State<TData, TOffset>>();
			Tuple<State<TData, TOffset>, State<TData, TOffset>> pair = Tuple.Create(StartState, other.StartState);
			queue.Enqueue(pair);
			newStates[pair] = newFst.StartState;
			while (queue.Count > 0)
			{
				Tuple<State<TData, TOffset>, State<TData, TOffset>> p = queue.Dequeue();
				State<TData, TOffset> s = newStates[p];

				var newArcs = new List<Tuple<State<TData, TOffset>, State<TData, TOffset>, Input, Output<TData, TOffset>, int, ArcPriorityType>>();
				foreach (Arc<TData, TOffset> arc1 in p.Item1.Arcs)
				{
					if (arc1.Outputs.Count == 0 || arc1.Outputs[0] is RemoveOutput<TData, TOffset>)
					{
						newArcs.Add(Tuple.Create(arc1.Target, p.Item2, arc1.Input, arc1.Outputs.Count == 0 ? null : arc1.Outputs[0], CopyTag(newFst, arc1.Tag), arc1.PriorityType));
					}
					else
					{
						FeatureStruct compareFs;
						if (arc1.Outputs[0] is PriorityUnionOutput<TData, TOffset>)
						{
							compareFs = arc1.Input.FeatureStruct.DeepClone();
							compareFs.PriorityUnion(arc1.Outputs[0].FeatureStruct);
						}
						else
						{
							compareFs = arc1.Outputs[0].FeatureStruct;
						}

						foreach (Arc<TData, TOffset> arc2 in p.Item2.Arcs.Where(a => !a.Input.IsEpsilon))
						{
							if (arc2.Input.FeatureStruct.IsUnifiable(compareFs))
							{
								Output<TData, TOffset> output = null;
								if (arc2.Outputs.Count > 0)
								{
									if (arc2.Outputs[0] is PriorityUnionOutput<TData, TOffset>)
									{
										FeatureStruct fs = arc1.Outputs[0].FeatureStruct.DeepClone();
										fs.PriorityUnion(arc2.Outputs[0].FeatureStruct);
										if (arc1.Outputs[0] is PriorityUnionOutput<TData, TOffset>)
											output = new PriorityUnionOutput<TData, TOffset>(fs);
										else if (arc1.Outputs[0] is InsertOutput<TData, TOffset>)
											output = new InsertOutput<TData, TOffset>(fs);
										else
											output = new ReplaceOutput<TData, TOffset>(fs);
									}
									else
									{
										output = arc2.Outputs[0];
									}
								}

								newArcs.Add(Tuple.Create(arc1.Target, arc2.Target, arc1.Input, output, -1, ArcPriorityType.Medium));
							}
						}
					}
				}

				foreach (Arc<TData, TOffset> arc2 in p.Item2.Arcs.Where(a => a.Input.IsEpsilon))
					newArcs.Add(Tuple.Create(p.Item1, arc2.Target, arc2.Input, arc2.Outputs.Count == 0 ? null : arc2.Outputs[0], CopyTag(newFst, arc2.Tag), arc2.PriorityType));

				foreach (Tuple<State<TData, TOffset>, State<TData, TOffset>, Input, Output<TData, TOffset>, int, ArcPriorityType> newArc in newArcs)
				{
					Tuple<State<TData, TOffset>, State<TData, TOffset>> key = Tuple.Create(newArc.Item1, newArc.Item2);
					State<TData, TOffset> r;
					if (!newStates.TryGetValue(key, out r))
					{
						if (newArc.Item1.IsAccepting && newArc.Item2.IsAccepting)
							r = newFst.CreateAcceptingState(newArc.Item1.AcceptInfos.Concat(newArc.Item2.AcceptInfos));
						else
							r = newFst.CreateState();
						queue.Enqueue(key);
						newStates[key] = r;
					}

					if (newArc.Item3.EnqueueCount == 0)
					{
						if (newArc.Item5 != -1)
							s.Arcs.Add(r, newArc.Item5);
						else
							s.Arcs.Add(r, newArc.Item6);
					}
					else
					{
						s.Arcs.Add(newArc.Item3, newArc.Item4 == null ? Enumerable.Empty<Output<TData, TOffset>>() : newArc.Item4.ToEnumerable(), r);
					}
				}
			}
			return newFst;
		}

		public void ToGraphViz(TextWriter writer)
		{
			writer.WriteLine("digraph G {");

			var stack = new Stack<State<TData, TOffset>>();
			var processed = new HashSet<State<TData, TOffset>>();
			stack.Push(StartState);
			while (stack.Count != 0)
			{
				State<TData, TOffset> state = stack.Pop();
				processed.Add(state);

				writer.Write("  {0} [shape=\"{1}\", color=\"{2}\"", state.Index, state.Equals(StartState) ? "diamond" : "circle",
					state.Equals(StartState) ? "green" : state.IsAccepting ? "red" : "black");
				if (state.IsAccepting)
					writer.Write(", peripheries=\"2\"");
				writer.WriteLine("];");

				foreach (Arc<TData, TOffset> arc in state.Arcs)
				{
					writer.WriteLine("  {0} -> {1} [label=\"{2}\"];", state.Index, arc.Target.Index,
						arc.ToString().Replace("\"", "\\\""));
					if (!processed.Contains(arc.Target) && !stack.Contains(arc.Target))
						stack.Push(arc.Target);
				}
			}

			writer.WriteLine("}");
		}
	}
}
