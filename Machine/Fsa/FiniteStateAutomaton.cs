using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Fsa
{
	public class FiniteStateAutomaton<TData, TOffset> where TData : IData<TOffset>
	{
		private State<TData, TOffset> _startState;
		private readonly List<State<TData, TOffset>> _states;
		private int _nextTag;
		private readonly Dictionary<string, int> _groups;
		private readonly List<TagMapCommand> _initializers;
		private int _registerCount;
		private readonly Func<Annotation<TOffset>, bool> _filter;
		private readonly Direction _dir;
		private bool _deterministic;
		private bool _tryAllConditions;

		public FiniteStateAutomaton(Direction dir)
			: this(dir, ann => true)
		{
		}
		
		public FiniteStateAutomaton(Direction dir, Func<Annotation<TOffset>, bool> filter)
		{
			_initializers = new List<TagMapCommand>();
			_states = new List<State<TData, TOffset>>();
			_groups = new Dictionary<string, int>();
			_dir = dir;
			_startState = CreateState();
			_filter = filter;
			_tryAllConditions = true;
		}

		public IEnumerable<string> GroupNames
		{
			get { return _groups.Keys; }
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

		public bool Deterministic
		{
			get { return _deterministic; }
		}

		private State<TData, TOffset> CreateAcceptingState(IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers, bool isLazy)
		{
			var state = new State<TData, TOffset>(_states.Count, acceptInfos, finishers, isLazy);
			_states.Add(state);
			return state;
		}

		private State<TData, TOffset> CreateAcceptingState(IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos)
		{
			var state = new State<TData, TOffset>(_states.Count, acceptInfos);
			_states.Add(state);
			return state;
		}

		public State<TData, TOffset> CreateAcceptingState(string id, Func<TData, FsaMatch<TOffset>, bool> acceptable, int priority)
		{
			var state = new State<TData, TOffset>(_states.Count, new AcceptInfo<TData, TOffset>(id, acceptable, priority).ToEnumerable());
			_states.Add(state);
			return state;
		}

		public State<TData, TOffset> CreateAcceptingState()
		{
			var state = new State<TData, TOffset>(_states.Count, true);
			_states.Add(state);
			return state;
		}

		public State<TData, TOffset> CreateState()
		{
			var state = new State<TData, TOffset>(_states.Count, false);
			_states.Add(state);
			return state;
		}

		public State<TData, TOffset> CreateTag(State<TData, TOffset> source, State<TData, TOffset> target, string groupName, bool isStart)
		{
			int tag;
			if (isStart)
			{
				if (!_groups.TryGetValue(groupName, out tag))
				{
					tag = _nextTag;
					_nextTag += 2;
					_groups.Add(groupName, tag);
				}
			}
			else
			{
				tag = _groups[groupName] + 1;
			}

			_registerCount++;
			return source.Arcs.Add(target, tag);
		}

		public State<TData, TOffset> StartState
		{
			get
			{
				return _startState;
			}
		}

		public Direction Direction
		{
			get { return _dir; }
		}

		public IEnumerable<State<TData, TOffset>> States
		{
			get { return _states; }
		}

		private class FsaInstance
		{
			private readonly State<TData, TOffset> _state;
			private readonly Annotation<TOffset> _ann;
			private readonly NullableValue<TOffset>[,] _registers;
			private readonly VariableBindings _varBindings;
			private readonly HashSet<State<TData, TOffset>> _visited;

			public FsaInstance(State<TData, TOffset> state, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers)
				: this(state, ann, registers, null)
			{
			}

			public FsaInstance(State<TData, TOffset> state, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
				VariableBindings varBindings)
				: this(state, ann, registers, varBindings, Enumerable.Empty<State<TData, TOffset>>())
			{
			}

			public FsaInstance(State<TData, TOffset> state, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
				VariableBindings varBindings, IEnumerable<State<TData, TOffset>> visited)
			{
				_state = state;
				_ann = ann;
				_registers = registers;
				_varBindings = varBindings == null ? new VariableBindings() : varBindings.DeepClone();
				_visited = new HashSet<State<TData, TOffset>>(visited);
			}

			public State<TData, TOffset> State
			{
				get { return _state; }
			}

			public Annotation<TOffset> Annotation
			{
				get { return _ann; }
			}

			public NullableValue<TOffset>[,] Registers
			{
				get { return _registers; }
			}

			public VariableBindings VariableBindings
			{
				get { return _varBindings; }
			}

			public ISet<State<TData, TOffset>> Visited
			{
				get { return _visited; }
			}
		}

		public bool IsMatch(TData data, Annotation<TOffset> start, bool startAnchor, bool endAnchor, bool allMatches, bool useDefaults, out IEnumerable<FsaMatch<TOffset>> matches)
		{
			var instStack = new Stack<FsaInstance>();

			List<FsaMatch<TOffset>> matchList = null;

			Annotation<TOffset> ann = start;

			var initAnns = new HashSet<Annotation<TOffset>>();
			while (ann != data.Annotations.GetEnd(_dir))
			{
				var registers = new NullableValue<TOffset>[_registerCount, 2];

				var cmds = new List<TagMapCommand>();
				foreach (TagMapCommand cmd in _initializers)
				{
					if (cmd.Dest == 0)
						registers[cmd.Dest, 0].Value = ann.Span.GetStart(_dir);
					else
						cmds.Add(cmd);
				}

				ann = InitializeStack(data, ann, registers, cmds, instStack, initAnns);

				var curMatches = new List<FsaMatch<TOffset>>(); 
				while (instStack.Count != 0)
				{
					FsaInstance inst = instStack.Pop();

					if (inst.Annotation != null)
					{
						foreach (Arc<TData, TOffset> arc in inst.State.Arcs)
						{
							if (arc.Condition == null)
							{
								Debug.Assert(!_deterministic);
								if (!inst.Visited.Contains(arc.Target))
									instStack.Push(EpsilonAdvanceFsa(data, endAnchor, inst, arc, curMatches));
							}
							else if (inst.Annotation != data.Annotations.GetEnd(_dir) && inst.Annotation.FeatureStruct.IsUnifiable(arc.Condition, useDefaults, inst.VariableBindings))
							{
								foreach (FsaInstance ni in AdvanceFsa(data, endAnchor, inst.Annotation, inst, arc, curMatches))
									instStack.Push(ni);
								if (!_tryAllConditions)
									break;
							}
						}

						if (!_deterministic && !allMatches && curMatches.Count > 0)
							break;
					}
				}

				if (curMatches.Count > 0)
				{
					if (matchList == null)
						matchList = new List<FsaMatch<TOffset>>();
					if (_deterministic)
						curMatches.Sort(MatchCompare);
					matchList.AddRange(curMatches);
					if (!allMatches)
						break;
				}

				if (startAnchor)
					break;
			}

			if (matchList == null)
			{
				matches = null;
				return false;
			}

			matches = matchList;
			return true;
		}

		private static int MatchCompare(FsaMatch<TOffset> x, FsaMatch<TOffset> y)
		{
			int compare = x.Priority.CompareTo(y.Priority);
			if (compare != 0)
				return compare;

			if (x.IsLazy != y.IsLazy)
				return x.IsLazy ? -1 : 1;

			compare = x.Index.CompareTo(y.Index);
			return x.IsLazy ? compare : -compare;
		}

		private Annotation<TOffset> InitializeStack(TData data, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
			List<TagMapCommand> cmds, Stack<FsaInstance> instStack, HashSet<Annotation<TOffset>> initAnns)
		{
			TOffset offset = ann.Span.GetStart(_dir);

			var newRegisters = (NullableValue<TOffset>[,]) registers.Clone();
			ExecuteCommands(newRegisters, cmds, new NullableValue<TOffset>(ann.Span.GetStart(_dir)), new NullableValue<TOffset>());

			for (Annotation<TOffset> a = ann; a != data.Annotations.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(offset); a = a.GetNextDepthFirst(_dir, _filter))
			{
				if (a.Optional)
				{
					Annotation<TOffset> nextAnn = a.GetNextDepthFirst(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
					if (nextAnn != null)
						InitializeStack(data, nextAnn, registers, cmds, instStack, initAnns);
				}
			}

			for (; ann != data.Annotations.GetEnd(_dir) && ann.Span.GetStart(_dir).Equals(offset); ann = ann.GetNextDepthFirst(_dir, _filter))
			{
				if (!initAnns.Contains(ann))
				{
					instStack.Push(new FsaInstance(_startState, ann, (NullableValue<TOffset>[,]) newRegisters.Clone()));
					initAnns.Add(ann);
				}
			}

			return ann;
		}

		private IEnumerable<FsaInstance> AdvanceFsa(TData data, bool endAnchor, Annotation<TOffset> ann, FsaInstance inst,
			Arc<TData, TOffset> arc, List<FsaMatch<TOffset>> curMatches)
		{
			Annotation<TOffset> nextAnn = ann.GetNextDepthFirst(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
			TOffset nextOffset = nextAnn == data.Annotations.GetEnd(_dir) ? data.Annotations.GetLast(_dir, _filter).Span.GetEnd(_dir) : nextAnn.Span.GetStart(_dir);
			TOffset end = ann.Span.GetEnd(_dir);
			var newRegisters = (NullableValue<TOffset>[,]) inst.Registers.Clone();
			ExecuteCommands(newRegisters, arc.Commands, new NullableValue<TOffset>(nextOffset), new NullableValue<TOffset>(end));

			CheckAccepting(data, nextAnn, endAnchor, newRegisters, inst.VariableBindings, arc, curMatches);

			if (nextAnn != data.Annotations.GetEnd(_dir))
			{
				for (Annotation<TOffset> a = nextAnn; a != data.Annotations.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNextDepthFirst(_dir, _filter))
				{
					if (a.Optional)
					{
						foreach (FsaInstance ni in AdvanceFsa(data, endAnchor, a, inst, arc, curMatches))
							yield return ni;
					}
				}

				for (Annotation<TOffset> a = nextAnn; a != data.Annotations.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNextDepthFirst(_dir, _filter))
				{
					yield return new FsaInstance(arc.Target, a, (NullableValue<TOffset>[,]) newRegisters.Clone(), inst.VariableBindings);
				}
			}
			else if (!_deterministic)
			{
				yield return new FsaInstance(arc.Target, nextAnn, newRegisters, inst.VariableBindings);
			}
		}

		private FsaInstance EpsilonAdvanceFsa(TData data, bool endAnchor, FsaInstance inst, Arc<TData, TOffset> arc, List<FsaMatch<TOffset>> curMatches)
		{
			Annotation<TOffset> prevAnn = inst.Annotation.GetPrevDepthFirst(_dir, (cur, prev) => !cur.Span.Overlaps(prev.Span) && _filter(prev));
			var newRegisters = (NullableValue<TOffset>[,]) inst.Registers.Clone();
			ExecuteCommands(newRegisters, arc.Commands, new NullableValue<TOffset>(inst.Annotation.Span.GetStart(_dir)), new NullableValue<TOffset>(prevAnn.Span.GetEnd(_dir)));
			CheckAccepting(data, inst.Annotation, endAnchor, newRegisters, inst.VariableBindings, arc, curMatches);
			return new FsaInstance(arc.Target, inst.Annotation, newRegisters, inst.VariableBindings, inst.Visited.Concat(arc.Target));
		}

		private void CheckAccepting(TData data, Annotation<TOffset> ann, bool endAnchor, NullableValue<TOffset>[,] registers,
			VariableBindings varBindings, Arc<TData, TOffset> arc, List<FsaMatch<TOffset>> curMatches)
		{
			if (arc.Target.IsAccepting && (!endAnchor || ann == data.Annotations.GetEnd(_dir)))
			{
				var matchRegisters = (NullableValue<TOffset>[,]) registers.Clone();
				ExecuteCommands(matchRegisters, arc.Target.Finishers, new NullableValue<TOffset>(), new NullableValue<TOffset>());
				foreach (AcceptInfo<TData, TOffset> acceptInfo in arc.Target.AcceptInfos)
				{
					var candidate = new FsaMatch<TOffset>(acceptInfo.ID, matchRegisters, varBindings, acceptInfo.Priority, arc.Target.IsLazy, ann, curMatches.Count);
					if (acceptInfo.Acceptable(data, candidate))
						curMatches.Add(candidate);
				}
			}
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

		private class NfaStateInfo : IEquatable<NfaStateInfo>, IComparable<NfaStateInfo>, IComparable
		{
			private readonly State<TData, TOffset> _nfsState;
			private readonly Dictionary<int, int> _tags;
			private readonly int _lastPriority;
			private readonly int _maxPriority;

			public NfaStateInfo(State<TData, TOffset> nfaState, int maxPriority = 0, int lastPriority = 0, IDictionary<int, int> tags = null)
			{
				_nfsState = nfaState;
				_maxPriority = maxPriority;
				_lastPriority = lastPriority;
				_tags = tags == null ? new Dictionary<int, int>() : new Dictionary<int, int>(tags);
			}

			public State<TData, TOffset> NfaState
			{
				get
				{
					return _nfsState;
				}
			}

			public int MaxPriority
			{
				get { return _maxPriority; }
			}

			public int LastPriority
			{
				get { return _lastPriority; }
			}

			public IDictionary<int, int> Tags
			{
				get
				{
					return _tags;
				}
			}

			public override int GetHashCode()
			{
				int tagCode = _tags.Keys.Aggregate(0, (current, tag) => current ^ tag);
				return _nfsState.GetHashCode() ^ tagCode;
			}

			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
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

				return _nfsState.Equals(other._nfsState);
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

			int IComparable.CompareTo(object obj)
			{
				var other = obj as NfaStateInfo;
				if (other == null)
					throw new ArgumentException("The specified object is of the wrong type.");
				return CompareTo(other);
			}

			public override string ToString()
			{
				return string.Format("State {0} ({1}, {2})", _nfsState.Index, _maxPriority, _lastPriority);
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

			public bool IsEmpty
			{
				get { return _nfaStates.Count == 0; }
			}

			public State<TData, TOffset> DfaState { get; set; }


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

		private void MarkArcPriorities()
		{
			var visited = new HashSet<State<TData, TOffset>>();
			var todo = new Stack<Arc<TData, TOffset>>(_startState.Arcs.Reverse());
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

		public void Determinize(bool quasideterministic)
		{
			MarkArcPriorities();

			_registerCount = 0;
			var registerIndices = new Dictionary<Tuple<int, int>, int>();

			var startState = new NfaStateInfo(_startState);
			var subsetStart = new SubsetState(startState.ToEnumerable());
			subsetStart = EpsilonClosure(subsetStart, subsetStart);

			_states.Clear();
			_startState = CreateState();
			subsetStart.DfaState = _startState;

			var cmdTags = new Dictionary<int, int>();
			foreach (NfaStateInfo state in subsetStart.NfaStates)
			{
				foreach (KeyValuePair<int, int> kvp in state.Tags)
					cmdTags[kvp.Key] = kvp.Value;
			}
			_initializers.AddRange(from kvp in cmdTags
								   select new TagMapCommand(GetRegisterIndex(registerIndices, kvp.Key, kvp.Value), TagMapCommand.CurrentPosition));

			var subsetStates = new Dictionary<SubsetState, SubsetState> { {subsetStart, subsetStart} };
			var unmarkedSubsetStates = new Queue<SubsetState>();
			unmarkedSubsetStates.Enqueue(subsetStart);

			while (unmarkedSubsetStates.Count != 0)
			{
				SubsetState curSubsetState = unmarkedSubsetStates.Dequeue();

				ILookup<FeatureStruct, NfaStateInfo> conditions = curSubsetState.NfaStates
					.SelectMany(state => state.NfaState.Arcs, (state, arc) => new { State = state, Arc = arc} )
					.Where(stateArc => stateArc.Arc.Condition != null)
					.ToLookup(stateArc => stateArc.Arc.Condition, stateArc => new NfaStateInfo(stateArc.Arc.Target,
						Math.Max(stateArc.Arc.Priority, stateArc.State.MaxPriority), stateArc.Arc.Priority, stateArc.State.Tags), FreezableEqualityComparer<FeatureStruct>.Instance);

				if (quasideterministic)
				{
					foreach (IGrouping<FeatureStruct, NfaStateInfo> cond in conditions)
						CreateDeterministicState(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, new SubsetState(cond), cond.Key);
				}
				else
				{
					var preprocessedConditions = new List<Tuple<FeatureStruct, IEnumerable<NfaStateInfo>>> { Tuple.Create((FeatureStruct) null, Enumerable.Empty<NfaStateInfo>()) };
					foreach (IGrouping<FeatureStruct, NfaStateInfo> cond in conditions)
					{
						FeatureStruct negation;
						if (!cond.Key.Negation(out negation))
							negation = null;

						var temp = new List<Tuple<FeatureStruct, IEnumerable<NfaStateInfo>>>();
						foreach (Tuple<FeatureStruct, IEnumerable<NfaStateInfo>> preprocessedCond in preprocessedConditions)
						{
							FeatureStruct newCond;
							if (preprocessedCond.Item1 == null)
								newCond = cond.Key;
							else if (!preprocessedCond.Item1.Unify(cond.Key, false, new VariableBindings(), false, out newCond))
								newCond = null;
							if (newCond != null)
								temp.Add(Tuple.Create(newCond, preprocessedCond.Item2.Concat(cond)));

							if (negation != null)
							{
								if (preprocessedCond.Item1 == null)
									newCond = negation;
								else if (!preprocessedCond.Item1.Unify(negation, false, new VariableBindings(), false, out newCond))
									newCond = null;
								if (newCond != null)
									temp.Add(Tuple.Create(newCond, preprocessedCond.Item2));
							}
						}
						preprocessedConditions = temp;
					}

					foreach (Tuple<FeatureStruct, IEnumerable<NfaStateInfo>> preprocessedCond in preprocessedConditions)
					{
						var reach = new SubsetState(preprocessedCond.Item2);
						FeatureStruct condition;
						if (!reach.IsEmpty && preprocessedCond.Item1.CheckDisjunctiveConsistency(false, new VariableBindings(), out condition))
							CreateDeterministicState(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, reach, condition);
					}
				}
			}

			var regNums = new Dictionary<int, int>();
			for (_registerCount = 0; _registerCount < _nextTag; _registerCount++)
				regNums[_registerCount] = _registerCount;

			foreach (State<TData, TOffset> state in _states)
			{
				RenumberCommands(regNums, state.Finishers);
				foreach (Arc<TData, TOffset> arc in state.Arcs)
					RenumberCommands(regNums, arc.Commands);
			}

			_deterministic = true;
			_tryAllConditions = quasideterministic;
		}

		private void RenumberCommands(Dictionary<int, int> regNums, List<TagMapCommand> cmds)
		{
			foreach (TagMapCommand cmd in cmds)
			{
				if (cmd.Src != TagMapCommand.CurrentPosition && !regNums.ContainsKey(cmd.Src))
					regNums[cmd.Src] = _registerCount++;
				if (!regNums.ContainsKey(cmd.Dest))
					regNums[cmd.Dest] = _registerCount++;
				if (cmd.Src != TagMapCommand.CurrentPosition)
					cmd.Src = regNums[cmd.Src];
				cmd.Dest = regNums[cmd.Dest];
			}
			cmds.Sort();
		}

		private void CreateDeterministicState(Dictionary<SubsetState, SubsetState> subsetStates, Queue<SubsetState> unmarkedSubsetStates,
			Dictionary<Tuple<int, int>, int> registerIndices, SubsetState curSubsetState, SubsetState reach, FeatureStruct condition)
		{
			SubsetState target = EpsilonClosure(reach, curSubsetState);
			// this makes the FSA not complete
			if (!target.IsEmpty)
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
					NfaStateInfo[] acceptingStates = (from state in target.NfaStates
													  where state.NfaState.IsAccepting
													  orderby state descending
													  select state).ToArray();
					if (acceptingStates.Length > 0)
					{
						IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos = acceptingStates.SelectMany(state => state.NfaState.AcceptInfos);

						var finishers = new List<TagMapCommand>();
						var finishedTags = new HashSet<int>();
						foreach (NfaStateInfo acceptingState in acceptingStates)
						{
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
						target.DfaState = CreateAcceptingState(acceptInfos, finishers, IsLazyAcceptingState(target));
					}
					else
					{
						target.DfaState = CreateState();
					}
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

				curSubsetState.DfaState.Arcs.Add(condition, target.DfaState, cmds);
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
					if (highestPriArc.Condition != null)
						break;
					curState = highestPriArc.Target;
				}

				if (curState.IsAccepting)
				{
					if ((from s in state.NfaStates
						 from tran in s.NfaState.Arcs
						 where tran.Condition != null
						 select tran.Condition).Any())
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

		private static SubsetState EpsilonClosure(SubsetState from, SubsetState prev)
		{
			int firstFreeIndex = -1;
			foreach (NfaStateInfo state in prev.NfaStates)
			{
				foreach (KeyValuePair<int, int> tag in state.Tags)
					firstFreeIndex = Math.Max(firstFreeIndex, tag.Value);
			}
			firstFreeIndex++;

			var stack = new Stack<NfaStateInfo>();
			var closure = new Dictionary<int, NfaStateInfo>();
			foreach (NfaStateInfo state in from.NfaStates)
			{
				stack.Push(state);
				closure[state.NfaState.Index] = state;
			}

			while (stack.Count != 0)
			{
				NfaStateInfo topState = stack.Pop();

				foreach (Arc<TData, TOffset> arc in topState.NfaState.Arcs)
				{
					if (arc.Condition == null)
					{
						int newMaxPriority = Math.Max(arc.Priority, topState.MaxPriority);
						NfaStateInfo temp;
						if (closure.TryGetValue(arc.Target.Index, out temp))
						{
							if (temp.MaxPriority < newMaxPriority)
								continue;
							if (temp.MaxPriority == newMaxPriority && temp.LastPriority <= arc.Priority)
								continue;
						}

						var newState = new NfaStateInfo(arc.Target, newMaxPriority, arc.Priority, topState.Tags);

						if (arc.Tag != -1)
							newState.Tags[arc.Tag] = firstFreeIndex;

						closure[arc.Target.Index] = newState;
						stack.Push(newState);
					}
				}
			}

			return new SubsetState(closure.Values);
		}

		private int GetRegisterIndex(Dictionary<Tuple<int, int>, int> registerIndices, int tag, int index)
		{
			if (index == 0)
				return tag;

			Tuple<int, int> key =  Tuple.Create(tag, index);
			int registerIndex;
			if (registerIndices.TryGetValue(key, out registerIndex))
				return registerIndex;

			registerIndex = _nextTag + registerIndices.Count;
			registerIndices[key] = registerIndex;
			return registerIndex;
		}

		public FiniteStateAutomaton<TData, TOffset> Intersect(FiniteStateAutomaton<TData, TOffset> fsa)
		{
			var newFsa = new FiniteStateAutomaton<TData, TOffset>(_dir);

			var queue = new Queue<Tuple<State<TData, TOffset>, State<TData, TOffset>>>();
			var newStates = new Dictionary<Tuple<State<TData, TOffset>, State<TData, TOffset>>, State<TData, TOffset>>();
			Tuple<State<TData, TOffset>, State<TData, TOffset>> pair = Tuple.Create(StartState, fsa.StartState);
			queue.Enqueue(pair);
			newStates[pair] = newFsa.StartState;
			while (queue.Count > 0)
			{
				Tuple<State<TData, TOffset>, State<TData, TOffset>> p = queue.Dequeue();
				State<TData, TOffset> s = newStates[p];

				var newArcs = (from t1 in p.Item1.Arcs
							   where t1.Condition == null
							   select new { q = Tuple.Create(t1.Target, p.Item2), cond = (FeatureStruct) null }
							  ).Union(
							  (from t2 in p.Item2.Arcs
							   where t2.Condition == null
							   select new { q = Tuple.Create(p.Item1, t2.Target), cond = (FeatureStruct) null }
							  ).Union(
							   from t1 in p.Item1.Arcs
							   where t1.Condition != null
							   from t2 in p.Item2.Arcs
							   let newCond = t2.Condition != null ? Unify(t1.Condition, t2.Condition) : null
							   where newCond != null
							   select new { q = Tuple.Create(t1.Target, t2.Target), cond = newCond }
							  ));

				foreach (var newArc in newArcs)
				{
					State<TData, TOffset> r;
					if (!newStates.TryGetValue(newArc.q, out r))
					{
						if (newArc.q.Item1.IsAccepting && newArc.q.Item2.IsAccepting)
							r = newFsa.CreateAcceptingState(newArc.q.Item1.AcceptInfos.Concat(newArc.q.Item2.AcceptInfos));
						else
							r = newFsa.CreateState();
						queue.Enqueue(newArc.q);
						newStates[newArc.q] = r;
					}
					s.Arcs.Add(newArc.cond, r);
				}
			}
			return newFsa;
		}

		private static FeatureStruct Unify(FeatureStruct fs1, FeatureStruct fs2)
		{
			FeatureStruct result;
			if (fs1.Unify(fs2, out result))
				return result;
			return null;
		}

		public void ToGraphViz(TextWriter writer)
		{
			writer.WriteLine("digraph G {");

			var stack = new Stack<State<TData, TOffset>>();
			var processed = new HashSet<State<TData, TOffset>>();
			stack.Push(_startState);
			while (stack.Count != 0)
			{
				State<TData, TOffset> state = stack.Pop();
				processed.Add(state);

				writer.Write("  {0} [shape=\"{1}\", color=\"{2}\"", state.Index, state == _startState ? "diamond" : "circle",
					state == _startState ? "green" : state.IsAccepting ? "red" : "black");
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
