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

		private State<TOffset> CreateAcceptingState(IEnumerable<AcceptInfo<TOffset>> acceptInfos, IEnumerable<TagMapCommand> finishers)
		{
			var state = new State<TOffset>(_states.Count, acceptInfos, finishers);
			_states.Add(state);
			return state;
		}

		public State<TOffset> CreateAcceptingState(string id, Func<IBidirList<Annotation<TOffset>>, FsaMatch<TOffset>, bool> acceptable)
		{
			var state = new State<TOffset>(_states.Count, id, acceptable);
			_states.Add(state);
			return state;
		}

		public State<TOffset> CreateAcceptingState()
		{
			var state = new State<TOffset>(_states.Count, true);
			_states.Add(state);
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
			private readonly NullableValue<TOffset>[,] _registers;
			private readonly VariableBindings _varBindings;

			public FsaInstance(State<TOffset> state, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
				VariableBindings varBindings)
			{
				_state = state;
				_ann = ann;
				_registers = registers;
				_varBindings = varBindings;
			}

			public State<TOffset> State
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
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, bool allMatches, out IEnumerable<FsaMatch<TOffset>> matches)
		{
			var instStack = new Stack<FsaInstance>();
			var matchStack = new Stack<FsaMatch<TOffset>>();

			var matchList = new List<FsaMatch<TOffset>>();
			matches = matchList;

			Annotation<TOffset> ann = annList.GetFirst(_dir, _filter);

			var registers = new NullableValue<TOffset>[_registerCount,2];

			var cmds = new List<TagMapCommand>();
			foreach (TagMapCommand cmd in _initializers)
			{
				if (cmd.Dest == 0)
					registers[cmd.Dest, 0].Value = ann.Span.GetStart(_dir);
				else
					cmds.Add(cmd);
			}

			InitializeStack(ann, registers, cmds, instStack);

			while (instStack.Count != 0)
			{
				FsaInstance inst = instStack.Pop();

				foreach (Arc<TOffset> arc in inst.State.OutgoingArcs)
				{
					if (arc.Condition.IsMatch(inst.Annotation, inst.VariableBindings))
					{
						AdvanceFsa(annList, inst.Annotation, inst.Annotation.Span.GetEnd(_dir), inst.Registers, inst.VariableBindings, arc,
							instStack, matchStack);
					}
				}
			}

			while (matchStack.Count != 0)
			{
				matchList.Add(matchStack.Pop());
				if (!allMatches)
					return true;
			}

			return matchList.Count > 0;
		}

		private void InitializeStack(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, List<TagMapCommand> cmds, Stack<FsaInstance> instStack)
		{
			TOffset offset = ann.Span.GetStart(_dir);

			ExecuteCommands(registers, cmds, new NullableValue<TOffset>(ann.Span.GetStart(_dir)), new NullableValue<TOffset>(),
				ann.Span.GetEnd(_dir));

			for (Annotation<TOffset> a = ann; a != null && a.Span.GetStart(_dir).Equals(offset); a = a.GetNext(_dir, _filter))
			{
				if (a.IsOptional)
				{
					Annotation<TOffset> nextAnn = a.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
					if (nextAnn != null)
						InitializeStack(nextAnn, registers, cmds, instStack);
				}
			}

			for (; ann != null && ann.Span.GetStart(_dir).Equals(offset); ann = ann.GetNext(_dir, _filter))
			{
				instStack.Push(new FsaInstance(_startState, ann, (NullableValue<TOffset>[,]) registers.Clone(),
					new VariableBindings()));
			}
		}

		private void AdvanceFsa(IBidirList<Annotation<TOffset>> annList, Annotation<TOffset> ann, TOffset end,
			NullableValue<TOffset>[,] registers, VariableBindings varBindings, Arc<TOffset> arc, Stack<FsaInstance> instStack,
			Stack<FsaMatch<TOffset>> matchStack)
		{
			Annotation<TOffset> nextAnn = ann.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
			TOffset nextOffset = nextAnn == null ? annList.GetLast(_dir, _filter).Span.GetEnd(_dir) : nextAnn.Span.GetStart(_dir);
			var newRegisters = (NullableValue<TOffset>[,]) registers.Clone();
			ExecuteCommands(newRegisters, arc.Commands, new NullableValue<TOffset>(nextOffset), new NullableValue<TOffset>(end),
				ann.Span.GetEnd(_dir));
			if (arc.Target.IsAccepting)
			{
				var matchRegisters = (NullableValue<TOffset>[,]) newRegisters.Clone();
				ExecuteCommands(matchRegisters, arc.Target.Finishers, new NullableValue<TOffset>(), new NullableValue<TOffset>(),
					ann.Span.GetEnd(_dir));
				foreach (AcceptInfo<TOffset> acceptInfo in arc.Target.AcceptInfos.Reverse())
				{
					var match = new FsaMatch<TOffset>(acceptInfo.ID, matchRegisters, varBindings);
					if (acceptInfo.Acceptable == null || acceptInfo.Acceptable(annList, match))
						matchStack.Push(match);
				}
			}
			if (nextAnn != null)
			{
				for (Annotation<TOffset> a = nextAnn; a != null && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNext(_dir, _filter))
				{
					if (a.IsOptional)
						AdvanceFsa(annList, a, end, registers, varBindings, arc, instStack, matchStack);
				}

				for (Annotation<TOffset> a = nextAnn; a != null && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNext(_dir, _filter))
				{
					instStack.Push(new FsaInstance(arc.Target, a, (NullableValue<TOffset>[,]) newRegisters.Clone(),
						varBindings.Clone()));
				}
			}
		}

		private static void ExecuteCommands(NullableValue<TOffset>[,] registers, IEnumerable<TagMapCommand> cmds,
			NullableValue<TOffset> start, NullableValue<TOffset> end, TOffset curEnd)
		{
			foreach (TagMapCommand cmd in cmds)
			{
				if (cmd.Src == TagMapCommand.CurrentPosition)
				{
					registers[cmd.Dest, 0] = start;
					if (cmd.Dest == 1)
						registers[1, 1].Value = curEnd;
					else
						registers[cmd.Dest, 1] = end;
				}
				else
				{
					registers[cmd.Dest, 0] = registers[cmd.Src, 0];
					registers[cmd.Dest, 1] = registers[cmd.Src, 1];
				}
			}
		}

		private class NfaStateInfo : IEquatable<NfaStateInfo>
		{
			private readonly State<TOffset> _nfsState;
			private readonly Dictionary<int, int> _tags;
			private int _priority;

			public NfaStateInfo(State<TOffset> nfaState)
				: this(nfaState, null)
			{
			}

			public NfaStateInfo(State<TOffset> nfaState, IDictionary<int, int> tags)
				: this(nfaState, -1, tags)
			{
			}

			public NfaStateInfo(State<TOffset> nfaState, int priority, IDictionary<int, int> tags)
			{
				_nfsState = nfaState;
				_priority = priority;
				_tags = tags == null ? new Dictionary<int, int>() : new Dictionary<int, int>(tags);
			}

			public State<TOffset> NfaState
			{
				get
				{
					return _nfsState;
				}
			}

			public int Priority
			{
				get
				{
					return _priority;
				}

				set
				{
					_priority = value;
				}
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

			public override string ToString()
			{
				return string.Format("State {0} ({1})", _nfsState.Index, _priority);
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

		public void MarkPriorities()
		{
			var arcs = new Dictionary<PriorityType, List<Arc<TOffset>>>();

			foreach (State<TOffset> state in _states)
			{
				foreach (Arc<TOffset> arc in state.OutgoingArcs)
				{
					List<Arc<TOffset>> priorityArcs;
					if (!arcs.TryGetValue(arc.PriorityType, out priorityArcs))
					{
						priorityArcs = new List<Arc<TOffset>>();
						arcs[arc.PriorityType] = priorityArcs;
					}
					priorityArcs.Add(arc);
				}
			}

			int nextPriority = 0;
			foreach (PriorityType priorityType in Enum.GetValues(typeof(PriorityType)))
			{
				List<Arc<TOffset>> priorityArcs;
				if (arcs.TryGetValue(priorityType, out priorityArcs))
				{
					foreach (Arc<TOffset> arc in priorityArcs)
						arc.Priority = nextPriority++;
				}
			}
		}

		public void Determinize()
		{
			var registerIndices = new Dictionary<int, int>();

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

				ArcCondition<TOffset>[] conditions = (from state in curSubsetState.NfaStates
													  from tran in state.NfaState.OutgoingArcs
													  where tran.Condition != null
													  select tran.Condition).Distinct().ToArray();
				if (conditions.Length > 0)
				{
					ComputeArcs(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, conditions, 0,
						new ArcCondition<TOffset>[0], new ArcCondition<TOffset>[0]);
				}
			}
			_registerCount = _nextTag + registerIndices.Count;
		}

		private void ComputeArcs(Dictionary<SubsetState, SubsetState> subsetStates, Queue<SubsetState> unmarkedSubsetStates,
			Dictionary<int, int> registerIndices, SubsetState curSubsetState, ArcCondition<TOffset>[] conditions, int conditionIndex,
			ArcCondition<TOffset>[] subset1, ArcCondition<TOffset>[] subset2)
		{
			if (conditionIndex == conditions.Length)
			{
				ArcCondition<TOffset> condition;
				if (CreateDisjointCondition(subset1, subset2, out condition))
				{
					var cmdTags = new Dictionary<int, int>();
					SubsetState target = EpsilonClosure(new SubsetState(from state in curSubsetState.NfaStates
																		from tran in state.NfaState.OutgoingArcs
																		where tran.Condition != null && subset1.Contains(tran.Condition)
																		select new NfaStateInfo(tran.Target, state.Tags)), curSubsetState);
					// this makes the FSA not complete
					if (!target.IsEmpty)
					{
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

						var cmds = (from kvp in cmdTags
						            select new TagMapCommand(GetRegisterIndex(registerIndices, kvp.Key, kvp.Value),
						                                     TagMapCommand.CurrentPosition)).ToList();

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
															  orderby state.Priority
															  select state).ToArray();
							if (acceptingStates.Length > 0)
							{
								IEnumerable<AcceptInfo<TOffset>> acceptInfos = acceptingStates.SelectMany(state => state.NfaState.AcceptInfos);
								NfaStateInfo minState = acceptingStates.First();
								IEnumerable<TagMapCommand> finishers = from kvp in minState.Tags
								                                       let dest = GetRegisterIndex(registerIndices, kvp.Key, 0)
								                                       let src = GetRegisterIndex(registerIndices, kvp.Key, kvp.Value)
								                                       where dest != src
								                                       select new TagMapCommand(dest, src);
								target.DfaState = CreateAcceptingState(acceptInfos, finishers);
							}
							else
							{
								target.DfaState = CreateState();
							}
						}

						curSubsetState.DfaState.AddArc(condition, target.DfaState, cmds);
					}
				}
			}
			else
			{
				ArcCondition<TOffset> condition = conditions[conditionIndex];
				ComputeArcs(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, conditions, conditionIndex + 1,
					subset1.Concat(condition).ToArray(), subset2);
				ComputeArcs(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, conditions, conditionIndex + 1,
					subset1, subset2.Concat(condition).ToArray());
			}
		}

		private bool CreateDisjointCondition(IEnumerable<ArcCondition<TOffset>> conditions, IEnumerable<ArcCondition<TOffset>> negConditions, out ArcCondition<TOffset> result)
		{
			FeatureStruct fs = null;
			foreach (ArcCondition<TOffset> curCond in conditions)
			{
				if (fs == null)
				{
					fs = curCond.FeatureStruct;
				}
				else
				{
					if (!fs.Unify(curCond.FeatureStruct, false, new VariableBindings(), false, out fs))
					{
						result = null;
						return false;
					}
				}
			}

			foreach (ArcCondition<TOffset> curCond in negConditions)
			{
				FeatureStruct negation;
				if (!curCond.FeatureStruct.Negation(out negation))
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

			result = new ArcCondition<TOffset>(fs);
			return true;
		}

		private void ReorderTagIndices(SubsetState from, SubsetState to, Dictionary<int, int> registerIndices,
			List<TagMapCommand> cmds)
		{
			var newCmds = new SortedDictionary<int, TagMapCommand>();
			foreach (NfaStateInfo fromState in from.NfaStates)
			{
				foreach (KeyValuePair<int, int> kvp in fromState.Tags)
				{
					foreach (NfaStateInfo toState in to.NfaStates)
					{
						if (toState.NfaState.Equals(fromState.NfaState) && toState.Tags[kvp.Key] != kvp.Value)
						{
							int dest = GetRegisterIndex(registerIndices, kvp.Key, toState.Tags[kvp.Key]);
							newCmds[dest] = new TagMapCommand(dest, GetRegisterIndex(registerIndices, kvp.Key, kvp.Value));
						}
					}
				}
			}
			cmds.AddRange(newCmds.Values);
		}

		private static SubsetState EpsilonClosure(SubsetState s, SubsetState prev)
		{
			var stack = new Stack<NfaStateInfo>();
			var closure = new Dictionary<int, NfaStateInfo>();
			foreach (NfaStateInfo state in s.NfaStates)
			{
				state.Priority = 0;
				stack.Push(state);
				closure[state.NfaState.Index] = state;
			}

			while (stack.Count != 0)
			{
				NfaStateInfo top = stack.Pop();

				foreach (Arc<TOffset> arc in top.NfaState.OutgoingArcs)
				{
					if (arc.Condition == null)
					{
						int newPriority = Math.Max(arc.Priority, top.Priority);
						NfaStateInfo tempSe;
						if (closure.TryGetValue(arc.Target.Index, out tempSe))
						{
							if (tempSe.Priority < newPriority)
								continue;
						}

						var newSe = new NfaStateInfo(arc.Target, newPriority, top.Tags);

						if (arc.Tag != -1)
						{
							var indices = new List<int>();
							foreach (NfaStateInfo state in prev.NfaStates)
							{
								int index;
								if (state.Tags.TryGetValue(arc.Tag, out index))
									indices.Add(index);
							}

							int minIndex = 0;
							if (indices.Count > 0)
							{
								indices.Sort();
								for (int i = 0; i <= indices[indices.Count - 1] + 1; i++)
								{
									if (indices.BinarySearch(i) < 0)
									{
										minIndex = i;
										break;
									}
								}
							}

							newSe.Tags[arc.Tag] = minIndex;
						}

						closure[arc.Target.Index] = newSe;
						stack.Push(newSe);
					}
				}
			}

			return new SubsetState(closure.Values);
		}

		private int GetRegisterIndex(Dictionary<int, int> registerIndices, int tag, int index)
		{
			if (index == 0)
				return tag;

			int key = tag ^ index;
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
