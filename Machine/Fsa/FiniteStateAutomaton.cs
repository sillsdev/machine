using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Fsa
{
	public class FiniteStateAutomaton<TData, TOffset> where TData : IData<TOffset>
	{
		private State<TData, TOffset> _startState;
		private readonly List<State<TData, TOffset>> _acceptingStates;
		private readonly List<State<TData, TOffset>> _states;
		private int _nextTag;
		private readonly Dictionary<string, int> _groups;
		private readonly List<TagMapCommand> _initializers;
		private int _registerCount;
		private readonly Func<Annotation<TOffset>, bool> _filter;
		private readonly Direction _dir;

		public FiniteStateAutomaton(Direction dir)
			: this(dir, ann => true)
		{
		}
		
		public FiniteStateAutomaton(Direction dir, Func<Annotation<TOffset>, bool> filter)
		{
			_initializers = new List<TagMapCommand>();
			_acceptingStates = new List<State<TData, TOffset>>();
			_states = new List<State<TData, TOffset>>();
			_groups = new Dictionary<string, int>();
			_dir = dir;
			_startState = CreateState();
			_filter = filter;
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

		private State<TData, TOffset> CreateAcceptingState(IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers, bool isLazy)
		{
			var state = new State<TData, TOffset>(_states.Count, acceptInfos, finishers, isLazy);
			_states.Add(state);
			_acceptingStates.Add(state);
			return state;
		}

		private State<TData, TOffset> CreateAcceptingState(IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos)
		{
			var state = new State<TData, TOffset>(_states.Count, acceptInfos);
			_states.Add(state);
			_acceptingStates.Add(state);
			return state;
		}

		public State<TData, TOffset> CreateAcceptingState(string id, Func<TData, FsaMatch<TOffset>, bool> acceptable, int priority)
		{
			var state = new State<TData, TOffset>(_states.Count, new AcceptInfo<TData, TOffset>(id, acceptable, priority).ToEnumerable());
			_states.Add(state);
			_acceptingStates.Add(state);
			return state;
		}

		public State<TData, TOffset> CreateAcceptingState()
		{
			var state = new State<TData, TOffset>(_states.Count, true);
			_states.Add(state);
			_acceptingStates.Add(state);
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

			return source.AddArc(target, tag);
		}

		public State<TData, TOffset> StartState
		{
			get
			{
				return _startState;
			}
		}

		public IEnumerable<State<TData, TOffset>> AcceptingStates
		{
			get { return _acceptingStates; }
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
			private readonly FsaMatch<TOffset> _match;

			public FsaInstance(State<TData, TOffset> state, FsaMatch<TOffset> match)
				: this(state, null, null, null, match)
			{
			}

			public FsaInstance(State<TData, TOffset> state, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
				VariableBindings varBindings) : this(state, ann, registers, varBindings, null)
			{
			}

			public FsaInstance(State<TData, TOffset> state, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
				VariableBindings varBindings, FsaMatch<TOffset> match)
			{
				_state = state;
				_ann = ann;
				_registers = registers;
				_varBindings = varBindings;
				_match = match;
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

			public FsaMatch<TOffset> Match
			{
				get { return _match; }
			}
		}

		public bool IsMatch(TData data, Annotation<TOffset> start, bool allMatches, bool useDefaults, out IEnumerable<FsaMatch<TOffset>> matches)
		{
			var instStack = new Stack<FsaInstance>();

			List<FsaMatch<TOffset>> matchList = null;

			Annotation<TOffset> ann = start;

			var initAnns = new HashSet<Annotation<TOffset>>();
			while (ann != null)
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

				var matchStack = new Stack<FsaMatch<TOffset>>();
				while (instStack.Count != 0)
				{
					FsaInstance inst = instStack.Pop();

					bool advance = false;
					if (inst.Annotation != null)
					{
						foreach (Arc<TData, TOffset> arc in inst.State.OutgoingArcs)
						{
							if (inst.Annotation.FeatureStruct.IsUnifiable(arc.Condition, useDefaults, inst.VariableBindings))
							{
								AdvanceFsa(data, inst.Annotation, inst.Registers, inst.VariableBindings, inst.Match, arc, instStack);
								advance = true;
							}
						}
					}
					if (!advance && inst.Match != null)
						matchStack.Push(inst.Match);
				}

				if (matchStack.Count > 0)
				{
					if (matchList == null)
						matchList = new List<FsaMatch<TOffset>>();
					matchList.AddRange(matchStack.Distinct());
					if (!allMatches)
						break;
				}
			}

			matches = matchList;
			return matches != null;
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
					instStack.Push(new FsaInstance(_startState, ann, (NullableValue<TOffset>[,]) newRegisters.Clone(),
						new VariableBindings()));
					initAnns.Add(ann);
				}
			}

			return ann;
		}

		private void AdvanceFsa(TData data, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
			VariableBindings varBindings, FsaMatch<TOffset> match, Arc<TData, TOffset> arc, Stack<FsaInstance> instStack)
		{
			Annotation<TOffset> nextAnn = ann.GetNextDepthFirst(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
			TOffset nextOffset = nextAnn == data.Annotations.GetEnd(_dir) ? data.Annotations.GetLast(_dir, _filter).Span.GetEnd(_dir) : nextAnn.Span.GetStart(_dir);
			TOffset end = ann.Span.GetEnd(_dir);
			var newRegisters = (NullableValue<TOffset>[,]) registers.Clone();
			ExecuteCommands(newRegisters, arc.Commands, new NullableValue<TOffset>(nextOffset), new NullableValue<TOffset>(end));
			if (arc.Target.IsAccepting)
			{
				var matchRegisters = (NullableValue<TOffset>[,]) newRegisters.Clone();
				ExecuteCommands(matchRegisters, arc.Target.Finishers, new NullableValue<TOffset>(), new NullableValue<TOffset>());
				foreach (AcceptInfo<TData, TOffset> acceptInfo in arc.Target.AcceptInfos)
				{
					if (match == null || acceptInfo.Priority < match.Priority || (acceptInfo.Priority != match.Priority && !match.IsLazy))
					{
						var candidate = new FsaMatch<TOffset>(acceptInfo.ID, matchRegisters, varBindings, acceptInfo.Priority, arc.Target.IsLazy, nextAnn);
						if (acceptInfo.Acceptable(data, candidate))
							match = candidate;
					}
				}
			}

			if (nextAnn != data.Annotations.GetEnd(_dir))
			{
				for (Annotation<TOffset> a = nextAnn; a != data.Annotations.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNextDepthFirst(_dir, _filter))
				{
					if (a.Optional)
						AdvanceFsa(data, a, registers, varBindings, match, arc, instStack);
				}

				for (Annotation<TOffset> a = nextAnn; a != data.Annotations.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNextDepthFirst(_dir, _filter))
				{
					instStack.Push(new FsaInstance(arc.Target, a, (NullableValue<TOffset>[,]) newRegisters.Clone(),
						varBindings.Clone(), match));
				}
			}
			else if (match != null)
			{
				instStack.Push(new FsaInstance(arc.Target, match));
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

		public void MarkArcPriorities()
		{
			// TODO: traverse through the FSA properly
			int nextPriority = 0;
			foreach (Arc<TData, TOffset> arc in from state in _states
												from arc in state.OutgoingArcs
												group arc by arc.PriorityType into priorityGroup
												orderby priorityGroup.Key
												from arc in priorityGroup
												select arc)
			{
				arc.Priority = nextPriority++;
			}
		}

		public void Determinize(bool quasideterministic)
		{
			var registerIndices = new Dictionary<Tuple<int, int>, int>();

			var startState = new NfaStateInfo(_startState);
			var subsetStart = new SubsetState(startState.ToEnumerable());
			subsetStart = EpsilonClosure(subsetStart, subsetStart);

			_states.Clear();
			_acceptingStates.Clear();
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

				IEnumerable<FeatureStruct> conditions = (from state in curSubsetState.NfaStates
				                                         from tran in state.NfaState.OutgoingArcs
				                                         where tran.Condition != null
				                                         select tran.Condition).Distinct();
				if (quasideterministic)
				{
					QuasideterministicComputeArcs(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, conditions);
				}
				else
				{
					FeatureStruct[] conditionsArray = conditions.ToArray();
					if (conditionsArray.Length > 0)
					{
						DeterministicComputeArcs(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, conditionsArray, 0,
							new FeatureStruct[0], new FeatureStruct[0]);
					}
				}
			}

			var regNums = new Dictionary<int, int>();
			for (_registerCount = 0; _registerCount < _nextTag; _registerCount++)
				regNums[_registerCount] = _registerCount;

			foreach (State<TData, TOffset> state in _states)
			{
				RenumberCommands(regNums, state.Finishers);
				foreach (Arc<TData, TOffset> arc in state.OutgoingArcs)
					RenumberCommands(regNums, arc.Commands);
			}
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

		private void QuasideterministicComputeArcs(Dictionary<SubsetState, SubsetState> subsetStates, Queue<SubsetState> unmarkedSubsetStates,
			Dictionary<Tuple<int, int>, int> registerIndices, SubsetState curSubsetState, IEnumerable<FeatureStruct> conditions)
		{
			foreach (FeatureStruct condition in conditions)
			{
				var reach = new SubsetState(from state in curSubsetState.NfaStates
											from tran in state.NfaState.OutgoingArcs
											where tran.Condition != null && tran.Condition.Equals(condition)
											select new NfaStateInfo(tran.Target, Math.Max(tran.Priority, state.MaxPriority), tran.Priority, state.Tags));
				CreateDeterministicState(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, reach, condition);
			}
		}

		private void DeterministicComputeArcs(Dictionary<SubsetState, SubsetState> subsetStates, Queue<SubsetState> unmarkedSubsetStates,
			Dictionary<Tuple<int, int>, int> registerIndices, SubsetState curSubsetState, FeatureStruct[] conditions, int conditionIndex,
			FeatureStruct[] subset1, FeatureStruct[] subset2)
		{
			if (conditionIndex == conditions.Length)
			{
				FeatureStruct condition;
				if (CreateDisjointCondition(subset1, subset2, out condition))
				{
					var reach = new SubsetState(from state in curSubsetState.NfaStates
											    from tran in state.NfaState.OutgoingArcs
												where tran.Condition != null && subset1.Contains(tran.Condition)
												select new NfaStateInfo(tran.Target, Math.Max(tran.Priority, state.MaxPriority), tran.Priority, state.Tags));
					CreateDeterministicState(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, reach, condition);
				}
			}
			else
			{
				FeatureStruct condition = conditions[conditionIndex];
				DeterministicComputeArcs(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, conditions, conditionIndex + 1,
					subset1.Concat(condition).ToArray(), subset2);
				DeterministicComputeArcs(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, conditions, conditionIndex + 1,
					subset1, subset2.Concat(condition).ToArray());
			}
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

				curSubsetState.DfaState.AddArc(condition, target.DfaState, cmds);
			}
		}

		private bool IsLazyAcceptingState(SubsetState state)
		{
			foreach (Arc<TData, TOffset> arc in state.NfaStates.Min().NfaState.OutgoingArcs)
			{
				State<TData, TOffset> curState = arc.Target;
				while (!curState.IsAccepting)
				{
					Arc<TData, TOffset> highestPriArc = curState.OutgoingArcs.MinBy(a => a.Priority);
					if (highestPriArc.Condition != null)
						break;
					curState = highestPriArc.Target;
				}

				if (curState.IsAccepting)
				{
					if ((from s in state.NfaStates
						 from tran in s.NfaState.OutgoingArcs
						 where tran.Condition != null
						 select tran.Condition).Any())
					{
						return true;
					}
					break;
				}
			}
			return false;
		}

		private bool CreateDisjointCondition(IEnumerable<FeatureStruct> conditions, IEnumerable<FeatureStruct> negConditions, out FeatureStruct result)
		{
			FeatureStruct fs = null;
			foreach (FeatureStruct curCond in conditions)
			{
				if (fs == null)
				{
					fs = curCond;
				}
				else
				{
					if (!fs.Unify(curCond, false, new VariableBindings(), false, out fs))
					{
						result = null;
						return false;
					}
				}
			}

			foreach (FeatureStruct curCond in negConditions)
			{
				FeatureStruct negation;
				if (!curCond.Negation(out negation))
				{
					result = null;
					return false;
				}

				if (fs == null)
				{
					fs = negation;
				}
				else
				{
					if (!fs.Unify(negation, false, new VariableBindings(), false, out fs))
					{
						result = null;
						return false;
					}
				}
			}

			if (fs == null)
			{
				fs = new FeatureStruct();
			}
			else if (!fs.CheckDisjunctiveConsistency(false, new VariableBindings(), out fs))
			{
				result = null;
				return false;
			}

			result = fs;
			return true;
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

				foreach (Arc<TData, TOffset> arc in topState.NfaState.OutgoingArcs)
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

				var newArcs = (from t1 in p.Item1.OutgoingArcs
							   where t1.Condition == null
							   select new { q = Tuple.Create(t1.Target, p.Item2), cond = (FeatureStruct) null }
							  ).Union(
							  (from t2 in p.Item2.OutgoingArcs
							   where t2.Condition == null
							   select new { q = Tuple.Create(p.Item1, t2.Target), cond = (FeatureStruct) null }
							  ).Union(
							   from t1 in p.Item1.OutgoingArcs
							   where t1.Condition != null
							   from t2 in p.Item2.OutgoingArcs
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
					s.AddArc(newArc.cond, r);
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

				foreach (Arc<TData, TOffset> arc in state.OutgoingArcs)
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
