using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Fsa
{
	public class FiniteStateAutomaton<TOffset>
	{
		private State<TOffset> _startState;
		private readonly List<State<TOffset>> _acceptingStates;
		private readonly List<State<TOffset>> _states;
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
			_acceptingStates = new List<State<TOffset>>();
			_states = new List<State<TOffset>>();
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

		private State<TOffset> CreateAcceptingState(IEnumerable<AcceptInfo<TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers, bool isLazy)
		{
			var state = new State<TOffset>(_states.Count, acceptInfos, finishers, isLazy);
			_states.Add(state);
			_acceptingStates.Add(state);
			return state;
		}

		public State<TOffset> CreateAcceptingState(string id, Func<IBidirList<Annotation<TOffset>>, FsaMatch<TOffset>, bool> acceptable, int priority)
		{
			var state = new State<TOffset>(_states.Count, new AcceptInfo<TOffset>(id, acceptable, priority).ToEnumerable());
			_states.Add(state);
			_acceptingStates.Add(state);
			return state;
		}

		public State<TOffset> CreateAcceptingState()
		{
			var state = new State<TOffset>(_states.Count, true);
			_states.Add(state);
			_acceptingStates.Add(state);
			return state;
		}

		public State<TOffset> CreateState()
		{
			var state = new State<TOffset>(_states.Count, false);
			_states.Add(state);
			return state;
		}

		public State<TOffset> CreateTag(State<TOffset> source, State<TOffset> target, string groupName, bool isStart)
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

		public State<TOffset> StartState
		{
			get
			{
				return _startState;
			}
		}

		public IEnumerable<State<TOffset>> AcceptingStates
		{
			get { return _acceptingStates; }
		}

		public Direction Direction
		{
			get { return _dir; }
		}

		public IEnumerable<State<TOffset>> States
		{
			get { return _states; }
		}

		private class FsaInstance
		{
			private readonly State<TOffset> _state;
			private readonly Annotation<TOffset> _ann;
			private readonly Annotation<TOffset> _nextAnn;
			private readonly NullableValue<TOffset>[,] _registers;
			private readonly VariableBindings _varBindings;
			private readonly FsaMatch<TOffset> _match;

			public FsaInstance(State<TOffset> state, FsaMatch<TOffset> match)
				: this(state, null, null, null, null, match)
			{
			}

			public FsaInstance(State<TOffset> state, Annotation<TOffset> ann, Annotation<TOffset> nextAnn,
				NullableValue<TOffset>[,] registers, VariableBindings varBindings)
				: this(state, ann, nextAnn, registers, varBindings, null)
			{
				_nextAnn = nextAnn;
			}

			public FsaInstance(State<TOffset> state, Annotation<TOffset> ann, Annotation<TOffset> nextAnn,
				NullableValue<TOffset>[,] registers, VariableBindings varBindings, FsaMatch<TOffset> match)
			{
				_state = state;
				_ann = ann;
				_nextAnn = nextAnn;
				_registers = registers;
				_varBindings = varBindings;
				_match = match;
			}

			public State<TOffset> State
			{
				get { return _state; }
			}

			public Annotation<TOffset> Annotation
			{
				get { return _ann; }
			}

			public Annotation<TOffset> NextAnnotation
			{
				get { return _nextAnn; }
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

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, Annotation<TOffset> start, bool allMatches, out IEnumerable<FsaMatch<TOffset>> matches)
		{
			var instStack = new Stack<FsaInstance>();

			List<FsaMatch<TOffset>> matchList = null;

			Annotation<TOffset> ann = start;

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

				ann = InitializeStack(annList, ann, registers, cmds, instStack);

				var matchStack = new Stack<FsaMatch<TOffset>>();
				while (instStack.Count != 0)
				{
					FsaInstance inst = instStack.Pop();

					bool advance = false;
					if (inst.Annotation != null)
					{
						foreach (Arc<TOffset> arc in inst.State.OutgoingArcs)
						{
							if (inst.Annotation.FeatureStruct.IsUnifiable(arc.Condition, false, inst.VariableBindings))
							{
								AdvanceFsa(annList, inst.Annotation, inst.NextAnnotation, inst.Registers, inst.VariableBindings, inst.Match, arc, instStack);
								advance = true;
								break;
							}
						}
						inst.Annotation.FeatureStruct.RemoveValue(FsaFeatureSystem.Anchor);
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

		private Annotation<TOffset> InitializeStack(IBidirList<Annotation<TOffset>> annList, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
			List<TagMapCommand> cmds, Stack<FsaInstance> instStack)
		{
			TOffset offset = ann.Span.GetStart(_dir);

			var newRegisters = (NullableValue<TOffset>[,]) registers.Clone();
			ExecuteCommands(newRegisters, cmds, new NullableValue<TOffset>(ann.Span.GetStart(_dir)), new NullableValue<TOffset>());

			for (Annotation<TOffset> a = ann; a != annList.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(offset); a = a.GetNext(_dir, _filter))
			{
				if (a.Optional)
				{
					Annotation<TOffset> nextAnn = a.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
					if (nextAnn != null)
						InitializeStack(annList, nextAnn, registers, cmds, instStack);
				}
			}

			for (; ann != annList.GetEnd(_dir) && ann.Span.GetStart(_dir).Equals(offset); ann = ann.GetNext(_dir, _filter))
			{
				Annotation<TOffset> instAnn;
				Annotation<TOffset> nextAnn;
				if (IsStartOfInput(annList, ann))
				{
					var anchorFS = FeatureStruct.With(FsaFeatureSystem.Instance)
						.Symbol(_dir == Direction.LeftToRight ? FsaFeatureSystem.LeftSide : FsaFeatureSystem.RightSide).Value;
					instAnn = new Annotation<TOffset>("anchor", ann.Span, anchorFS);
					nextAnn = ann;
				}
				else
				{
					instAnn = ann;
					if (IsEndOfInput(annList, ann))
					{
						var anchorFS = FeatureStruct.With(FsaFeatureSystem.Instance)
							.Symbol(_dir == Direction.LeftToRight ? FsaFeatureSystem.RightSide : FsaFeatureSystem.LeftSide).Value;
						nextAnn = new Annotation<TOffset>("anchor", ann.Span, anchorFS);
					}
					else
					{
						nextAnn = ann.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
					}
				}
				instStack.Push(new FsaInstance(_startState, instAnn, nextAnn, (NullableValue<TOffset>[,]) newRegisters.Clone(),
					new VariableBindings()));
			}

			return ann;
		}

		private void AdvanceFsa(IBidirList<Annotation<TOffset>> annList, Annotation<TOffset> ann, Annotation<TOffset> nextAnn,
			NullableValue<TOffset>[,] registers, VariableBindings varBindings, FsaMatch<TOffset> match, Arc<TOffset> arc,
			Stack<FsaInstance> instStack)
		{
			TOffset nextOffset = nextAnn == annList.GetEnd(_dir) ? annList.GetLast(_dir, _filter).Span.GetEnd(_dir) : nextAnn.Span.GetStart(_dir);
			TOffset end = ann.Span.GetEnd(_dir);
			var newRegisters = (NullableValue<TOffset>[,]) registers.Clone();
			ExecuteCommands(newRegisters, arc.Commands, new NullableValue<TOffset>(nextOffset), new NullableValue<TOffset>(end));
			if (arc.Target.IsAccepting)
			{
				var matchRegisters = (NullableValue<TOffset>[,]) newRegisters.Clone();
				ExecuteCommands(matchRegisters, arc.Target.Finishers, new NullableValue<TOffset>(), new NullableValue<TOffset>());
				foreach (AcceptInfo<TOffset> acceptInfo in arc.Target.AcceptInfos)
				{
					if (match == null || acceptInfo.Priority < match.Priority || !match.IsLazy)
					{
						var candidate = new FsaMatch<TOffset>(acceptInfo.ID, matchRegisters, varBindings, acceptInfo.Priority, arc.Target.IsLazy);
						if (acceptInfo.Acceptable(annList, candidate))
							match = candidate;
					}
				}
			}

			if (nextAnn != null && nextAnn.List == null)
			{
				instStack.Push(new FsaInstance(arc.Target, nextAnn, annList.GetEnd(_dir), (NullableValue<TOffset>[,])newRegisters.Clone(),
					varBindings.Clone(), match));
			}
			else if (nextAnn != null && nextAnn != annList.GetEnd(_dir))
			{
				for (Annotation<TOffset> a = nextAnn; a != annList.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNext(_dir, _filter))
				{
					if (a.Optional)
					{
						Annotation<TOffset> nextNextAnn = a.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
						AdvanceFsa(annList, a, nextNextAnn, registers, varBindings, match, arc, instStack);
					}
				}

				for (Annotation<TOffset> a = nextAnn; a != annList.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNext(_dir, _filter))
				{
					Annotation<TOffset> nextNextAnn;
					if (IsEndOfInput(annList, a))
					{
						var anchorFS = FeatureStruct.With(FsaFeatureSystem.Instance)
							.Symbol(_dir == Direction.LeftToRight ? FsaFeatureSystem.RightSide : FsaFeatureSystem.LeftSide).Value;
						nextNextAnn = new Annotation<TOffset>("anchor", a.Span, anchorFS);
					}
					else
					{
						nextNextAnn = a.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
					}
					instStack.Push(new FsaInstance(arc.Target, a, nextNextAnn, (NullableValue<TOffset>[,]) newRegisters.Clone(),
						varBindings.Clone(), match));
				}
			}
			else if (match != null)
			{
				instStack.Push(new FsaInstance(arc.Target, match));
			}
		}

		private bool IsStartOfInput(IBidirList<Annotation<TOffset>> annList, Annotation<TOffset> ann)
		{
			Annotation<TOffset> prevAnn = ann.GetPrev(_dir, (cur, prev) => !cur.Span.Overlaps(prev.Span) && _filter(prev));
			while (prevAnn != annList.GetBegin(_dir))
			{
				if (prevAnn.Optional)
					prevAnn = prevAnn.GetNext(_dir, (cur, prev) => !cur.Span.Overlaps(prev.Span) && _filter(prev));
				else
					return false;
			}
			return true;
		}

		private bool IsEndOfInput(IBidirList<Annotation<TOffset>> annList, Annotation<TOffset> ann)
		{
			Annotation<TOffset> nextAnn = ann.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
			while (nextAnn != annList.GetEnd(_dir))
			{
				if (nextAnn.Optional)
					nextAnn = nextAnn.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
				else
					return false;
			}
			return true;
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
			private readonly State<TOffset> _nfsState;
			private readonly Dictionary<int, int> _tags;
			private readonly int _lastPriority;
			private readonly int _maxPriority;

			public NfaStateInfo(State<TOffset> nfaState)
				: this(nfaState, 0, 0, null)
			{
			}

			public NfaStateInfo(State<TOffset> nfaState, int maxPriority, int lastPriority, IDictionary<int, int> tags)
			{
				_nfsState = nfaState;
				_maxPriority = maxPriority;
				_lastPriority = lastPriority;
				_tags = tags == null ? new Dictionary<int, int>() : new Dictionary<int, int>(tags);
			}

			public State<TOffset> NfaState
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

			public State<TOffset> DfaState { get; set; }


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
			foreach (Arc<TOffset> arc in from state in _states
										 from arc in state.OutgoingArcs
										 group arc by arc.PriorityType into priorityGroup
										 orderby priorityGroup.Key
										 from arc in priorityGroup
										 select arc)
			{
				arc.Priority = nextPriority++;
			}
		}

		public void Determinize()
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

				FeatureStruct[] conditions = (from state in curSubsetState.NfaStates
										      from tran in state.NfaState.OutgoingArcs
											  where tran.Condition != null
											  select tran.Condition).Distinct().ToArray();
				if (conditions.Length > 0)
				{
					ComputeArcs(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, conditions, 0,
						new FeatureStruct[0], new FeatureStruct[0]);
				}
			}

			//_registerCount = _nextTag + registerIndices.Count;

			var regNums = new Dictionary<int, int>();
			for (_registerCount = 0; _registerCount < _nextTag; _registerCount++)
				regNums[_registerCount] = _registerCount;

			foreach (State<TOffset> state in _states)
			{
				RenumberCommands(regNums, state.Finishers);
				foreach (Arc<TOffset> arc in state.OutgoingArcs)
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

		private void ComputeArcs(Dictionary<SubsetState, SubsetState> subsetStates, Queue<SubsetState> unmarkedSubsetStates,
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
								IEnumerable<AcceptInfo<TOffset>> acceptInfos = acceptingStates.SelectMany(state => state.NfaState.AcceptInfos);

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
			}
			else
			{
				FeatureStruct condition = conditions[conditionIndex];
				ComputeArcs(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, conditions, conditionIndex + 1,
					subset1.Concat(condition).ToArray(), subset2);
				ComputeArcs(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, conditions, conditionIndex + 1,
					subset1, subset2.Concat(condition).ToArray());
			}
		}

		private bool IsLazyAcceptingState(SubsetState state)
		{
			foreach (Arc<TOffset> arc in state.NfaStates.Min().NfaState.OutgoingArcs)
			{
				State<TOffset> curState = arc.Target;
				while (!curState.IsAccepting)
				{
					Arc<TOffset> highestPriArc = curState.OutgoingArcs.MinBy(a => a.Priority);
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

				foreach (Arc<TOffset> arc in topState.NfaState.OutgoingArcs)
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

		public void ToGraphViz(TextWriter writer)
		{
			writer.WriteLine("digraph G {");

			var stack = new Stack<State<TOffset>>();
			var processed = new HashSet<State<TOffset>>();
			stack.Push(_startState);
			while (stack.Count != 0)
			{
				State<TOffset> state = stack.Pop();
				processed.Add(state);

				writer.Write("  {0} [shape=\"{1}\", color=\"{2}\"", state.Index, state == _startState ? "diamond" : "circle",
					state == _startState ? "green" : state.IsAccepting ? "red" : "black");
				if (state.IsAccepting)
					writer.Write(", peripheries=\"2\"");
				writer.WriteLine("];");

				foreach (Arc<TOffset> arc in state.OutgoingArcs)
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
