using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class Fst<TData, TOffset> : IFreezable where TData : IAnnotatedData<TOffset>
	{
		private int _nextTag;
		private readonly Dictionary<string, int> _groups;
		private readonly List<TagMapCommand> _initializers;
		private int _registerCount;
		private Direction _dir;
		private Func<Annotation<TOffset>, bool> _filter;
		private readonly List<State<TData, TOffset>> _states;
		private readonly ReadOnlyCollection<State<TData, TOffset>> _readonlyStates;
		private readonly IFstOperations<TData, TOffset> _operations;
		private readonly RegistersEqualityComparer<TOffset> _registersEqualityComparer;
		private readonly IEqualityComparer<TOffset> _offsetEqualityComparer; 
		private bool _unification;
		private State<TData, TOffset> _startState;
		private bool _ignoreVariables;

		public Fst()
			: this(null, EqualityComparer<TOffset>.Default)
		{
		}

		public Fst(IEqualityComparer<TOffset> equalityComparer)
			: this(null, equalityComparer)
		{
		}

		public Fst(IFstOperations<TData, TOffset> operations)
			: this(operations, EqualityComparer<TOffset>.Default)
		{
		}

		public Fst(IFstOperations<TData, TOffset> operations, IEqualityComparer<TOffset> offsetEqualityComparer)
		{
			_states = new List<State<TData, TOffset>>();
			_readonlyStates = _states.ToReadOnlyCollection();
			_operations = operations;
			_initializers = new List<TagMapCommand>();
			_groups = new Dictionary<string, int>();
			_filter = ann => true;
			_registersEqualityComparer = new RegistersEqualityComparer<TOffset>(offsetEqualityComparer);
			_offsetEqualityComparer = offsetEqualityComparer;
		}

		public bool IsDeterministic { get; private set; }

		public State<TData, TOffset> CreateAcceptingState()
		{
			CheckFrozen();

			var state = new State<TData, TOffset>(_operations == null, _states.Count, true);
			_states.Add(state);
			return state;
		}

		protected State<TData, TOffset> CreateAcceptingState(IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos)
		{
			CheckFrozen();

			var state = new State<TData, TOffset>(_operations == null, _states.Count, acceptInfos);
			_states.Add(state);
			return state;
		}

		public State<TData, TOffset> CreateAcceptingState(string id, Func<TData, FstResult<TData, TOffset>,  bool> acceptable, int priority)
		{
			CheckFrozen();

			var state = new State<TData, TOffset>(_operations == null, _states.Count, new AcceptInfo<TData, TOffset>(id, acceptable, priority).ToEnumerable());
			_states.Add(state);
			return state;
		}

		public State<TData, TOffset> CreateState()
		{
			CheckFrozen();

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
			CheckFrozen();

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

		public State<TData, TOffset> StartState
		{
			get { return _startState; }
			set
			{
				CheckFrozen();

				_startState = value;
			}
		}

		public Direction Direction
		{
			get { return _dir; }
			set
			{
				CheckFrozen();

				_dir = value;
			}
		}

		public Func<Annotation<TOffset>, bool> Filter
		{
			get { return _filter; }
			set
			{
				CheckFrozen();

				_filter = value;
			}
		}

		public bool UseUnification
		{
			get { return _unification; }
			set
			{
				CheckFrozen();

				_unification = value;
			}
		}

		public bool IgnoreVariables
		{
			get { return _ignoreVariables; }
			set
			{
				CheckFrozen();

				_ignoreVariables = value;
			}
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
			CheckFrozen();

			_states.Clear();
			StartState = null;
			IsDeterministic = false;
		}

		public bool Transduce(TData data, Annotation<TOffset> start, bool startAnchor, bool endAnchor, bool useDefaults, out IEnumerable<FstResult<TData, TOffset>> results)
		{
			if (_operations != null && !(data is IDeepCloneable<TData>))
				throw new ArgumentException("The input data must be cloneable.", "data");
			return Transduce(data, start, startAnchor, endAnchor, true, useDefaults, out results);
		}

		public bool Transduce(TData data, Annotation<TOffset> start, bool startAnchor, bool endAnchor, bool useDefaults, out FstResult<TData, TOffset> result)
		{
			if (_operations != null && !(data is IDeepCloneable<TData>))
				throw new ArgumentException("The input data must be cloneable.", "data");
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
			ITraversalMethod<TData, TOffset> traversalMethod;
			if (_operations != null)
			{
				if (IsDeterministic)
				{
					traversalMethod = new DeterministicFstTraversalMethod<TData, TOffset>(_registersEqualityComparer, _registerCount, _operations, _dir, _filter, StartState, data,
						endAnchor, _unification, useDefaults, _ignoreVariables);
				}
				else
				{
					traversalMethod = new NondeterministicFstTraversalMethod<TData, TOffset>(_registersEqualityComparer, _registerCount, _operations, _dir, _filter, StartState, data,
						endAnchor, _unification, useDefaults, _ignoreVariables);
				}
			}
			else
			{
				if (IsDeterministic)
				{
					traversalMethod = new DeterministicFsaTraversalMethod<TData, TOffset>(_registersEqualityComparer, _registerCount, _dir, _filter, StartState, data, endAnchor,
						_unification, useDefaults, _ignoreVariables);
				}
				else
				{
					traversalMethod = new NondeterministicFsaTraversalMethod<TData, TOffset>(_registersEqualityComparer, _registerCount, _dir, _filter, StartState, data, endAnchor,
						_unification, useDefaults, _ignoreVariables);
				}
			}
			List<FstResult<TData, TOffset>> resultList = null;

			int annIndex = traversalMethod.Annotations.IndexOf(start);

			var initAnns = new HashSet<int>();
			while (annIndex < traversalMethod.Annotations.Count)
			{
				var initRegisters = new NullableValue<TOffset>[_registerCount, 2];

				var cmds = new List<TagMapCommand>();
				foreach (TagMapCommand cmd in _initializers)
				{
					if (cmd.Dest == 0)
						initRegisters[cmd.Dest, 0].Value = traversalMethod.Annotations[annIndex].Span.GetStart(_dir);
					else
						cmds.Add(cmd);
				}

				List<FstResult<TData, TOffset>> curResults = traversalMethod.Traverse(ref annIndex, initRegisters, cmds, initAnns).ToList();
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

			results = allMatches ? resultList.Distinct() : resultList;
			return true;
		}

		private int ResultCompare(FstResult<TData, TOffset> x, FstResult<TData, TOffset> y)
		{
			int compare = x.Priority.CompareTo(y.Priority);
			if (compare != 0)
				return compare;

			compare = -x.NextAnnotation.CompareTo(y.NextAnnotation);
			if (_dir == Direction.RightToLeft)
				compare = -compare;
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
			if (compare != 0)
				return compare;
			return x.Order.CompareTo(y.Order);
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
				int enqueueCount = input.EnqueueCount + (targetStates.SelectMany(s => s.NfaState.Arcs).Any(a => a.Input.IsEpsilon && a.Input.EnqueueCount > 0) ? 1 : 0);
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

			var newFst = new Fst<TData, TOffset>(_operations, _offsetEqualityComparer) {IsDeterministic = deterministic, _nextTag = _nextTag, Direction = _dir, Filter = _filter, UseUnification = _unification};
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
					CreateOptimizedArc(newFst, subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, t.Item1, t.Item2, t.Item3, t.Item4);
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
			Dictionary<Tuple<int, int>, int> registerIndices, SubsetState curSubsetState, SubsetState target, Input input, IEnumerable<Output<TData, TOffset>> outputs, int priority)
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

				// for non-deterministic FSTs, do not add finishers for any start tags that occur after this accepting state.
				// this can result in spurious group captures
				HashSet<int> startTags = null;
				if (!newFst.IsDeterministic)
					startTags = new HashSet<int>(subsetState.NfaStates.SelectMany(s => s.NfaState.Arcs).Where(a => a.Tag % 2 == 0).Select(a => a.Tag));

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
						if (tag.Value > 0 && !finishedTags.Contains(tag.Key)
							&& (startTags == null || !startTags.Contains(GetRegisterIndex(registerIndices, tag.Key, 0))))
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
			}
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
			var fst = new Fst<TData, TOffset>(_operations, _offsetEqualityComparer) {Direction = _dir, Filter = _filter, UseUnification = _unification};
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
				.Where(input => input != null).Select(input => input.FeatureStruct).Distinct(FreezableEqualityComparer<FeatureStruct>.Default);
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
			var fst = new Fst<TData, TOffset>(_offsetEqualityComparer) {_nextTag = _nextTag, _registerCount = _registerCount, Direction = _dir, Filter = _filter, UseUnification = _unification};
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
			var fst = new Fst<TData, TOffset>(_offsetEqualityComparer) {_nextTag = _nextTag, _registerCount = _registerCount, Direction = _dir, Filter = _filter, UseUnification = _unification};
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
			var newFst = new Fst<TData, TOffset>(_offsetEqualityComparer) {Direction = _dir, Filter = _filter, UseUnification = _unification};
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
			var newFst = new Fst<TData, TOffset>(_operations, _offsetEqualityComparer) {Direction = _dir, Filter = _filter, UseUnification = _unification};
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
							if (_unification ? arc2.Input.FeatureStruct.IsUnifiable(compareFs) : arc2.Input.FeatureStruct.Subsumes(compareFs))
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

		private void CheckFrozen()
		{
			if (IsFrozen)
				throw new InvalidOperationException("The FST is immutable.");
		}

		public bool IsFrozen { get; private set; }

		public void Freeze()
		{
			if (IsFrozen)
				return;

			IsFrozen = true;
			foreach (State<TData, TOffset> state in _states)
				state.Freeze();
		}

		public int GetFrozenHashCode()
		{
			return GetHashCode();
		}
	}
}
