using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.APRE.Fsa
{
	public class FiniteStateAutomaton<TOffset, TData>
	{
		private State<TOffset, TData> _startState;
		private readonly List<State<TOffset, TData>> _states;
		private int _nextTag;
		private readonly Dictionary<string, int> _groups;
		private readonly List<TagMapCommand> _initializers;
		private int _nextPriority;
		private int _registerCount;
		private readonly HashSet<string> _synthesisTypes;
		private readonly HashSet<string> _analysisTypes;
		private readonly Direction _dir;

		public FiniteStateAutomaton(Direction dir, IEnumerable<string> synthesisTypes, IEnumerable<string> analysisTypes)
		{
			_initializers = new List<TagMapCommand>();
			_states = new List<State<TOffset, TData>>();
			_groups = new Dictionary<string, int>();
			_dir = dir;
			_startState = CreateState();
			if (synthesisTypes != null)
				_synthesisTypes = new HashSet<string>(synthesisTypes);
			if (analysisTypes != null)
				_analysisTypes = new HashSet<string>(analysisTypes);
		}

		public IEnumerable<string> GroupNames
		{
			get { return _groups.Keys; }
		}

		public bool GetOffsets(string groupName, FsaMatch<TOffset, TData> match, out TOffset start, out TOffset end)
		{
			int tag = _groups[groupName];
			NullableValue<TOffset> startValue = match.Registers[tag, 0];
			NullableValue<TOffset> endValue = match.Registers[tag + 1, 1];
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

		private State<TOffset, TData> CreateState(IEnumerable<TagMapCommand> finishers)
		{
			var state = new State<TOffset, TData>(_states.Count, finishers);
			_states.Add(state);
			return state;
		}

		public State<TOffset, TData> CreateState(bool isAccepting)
		{
			var state = new State<TOffset, TData>(_states.Count, isAccepting);
			_states.Add(state);
			return state;
		}

		public State<TOffset, TData> CreateState()
		{
			return CreateState(false);
		}

		public State<TOffset, TData> CreateGroupTag(State<TOffset, TData> startState, string groupName, bool isStart)
		{
			State<TOffset, TData> tagState = CreateState();
			int tag;
			if (isStart)
			{
				tag = _nextTag;
				_nextTag += 2;
				_groups.Add(groupName, tag);
			}
			else
			{
				tag = _groups[groupName] + 1;
			}

			startState.AddTransition(new Transition<TOffset, TData>(tagState, tag, _nextPriority++));
			return tagState;
		}

		public State<TOffset, TData> StartState
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

		class FsaInstance
		{
			private readonly State<TOffset, TData> _state;
			private readonly TData _data;
			private readonly Annotation<TOffset> _ann;
			private readonly NullableValue<TOffset>[,] _registers;

			public FsaInstance(State<TOffset, TData> state, Annotation<TOffset> ann, TData data, NullableValue<TOffset>[,] registers)
			{
				_state = state;
				_ann = ann;
				_data = data;
				_registers = registers;
			}

			public State<TOffset, TData> State
			{
				get
				{
					return _state;
				}
			}

			public Annotation<TOffset> Annotation
			{
				get
				{
					return _ann;
				}
			}

			public TData Data
			{
				get
				{
					return _data;
				}
			}

			public NullableValue<TOffset>[,] Registers
			{
				get
				{
					return _registers;
				}
			}
		}

		private bool IsValidType(ModeType mode, Annotation<TOffset> ann)
		{
			HashSet<string> types = mode == ModeType.Analysis ? _analysisTypes : _synthesisTypes;
			if (types != null)
				return types.Contains(ann.Type);
			return true;
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, ModeType mode, TData data, bool allMatches,
			out IEnumerable<FsaMatch<TOffset, TData>> matches)
		{
			var instStack = new Stack<FsaInstance>();
			var matchStack = new Stack<FsaMatch<TOffset, TData>>();

			var matchList = new List<FsaMatch<TOffset, TData>>();
			matches = matchList;

			Annotation<TOffset> ann = annList.GetFirst(_dir, a => IsValidType(mode, a));
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

				ann = InitializeStack(ann, registers, mode, data, cmds, instStack);

				//int offset = annSet[index].GetStartOffset(dir);
				//ExecuteCommands(registers, cmds, offset, -1);
				//for (; index != -1 && annSet[index].GetStartOffset(dir) == offset; index = annSet.GetNextIndex(index, dir))
				//    instStack.Push(new FSAInstance(_startState, index, instantiatedVars, registers.Clone() as int[,]));

				while (instStack.Count != 0)
				{
					FsaInstance inst = instStack.Pop();

					foreach (Transition<TOffset, TData> transition in inst.State.Transitions)
					{
						TData curData = inst.Data;
						if (transition.Condition.IsMatch(inst.Annotation, mode, ref curData))
						{
							AdvanceFsa(annList, inst.Annotation, inst.Annotation.Span.GetEnd(_dir), inst.Registers,
								mode, transition, curData, instStack, matchStack);

							//int nextIndex = annSet.GetNextIndexNonOverlapping(inst.AnnotationIndex, dir);
							//int nextOffset = nextIndex == -1 ? annSet.GetLast(dir).GetEndOffset(dir) : annSet[nextIndex].GetStartOffset(dir);
							//int endOffset = annSet[inst.AnnotationIndex].GetEndOffset(dir);
							//registers = inst.Registers.Clone() as int[,];
							//ExecuteCommands(registers, transition.Commands, nextOffset, endOffset);
							//if (nextIndex != -1)
							//{
							//    for (; nextIndex != -1 && annSet[nextIndex].GetStartOffset(dir) == nextOffset;
							//        nextIndex = annSet.GetNextIndex(nextIndex, dir))
							//    {
							//        stack.Push(new FSAInstance(transition.Target, nextIndex, vars, registers.Clone() as int[,]));
							//    }
							//}

							//if (transition.Target.IsAccepting)
							//{
							//    ExecuteCommands(registers, transition.Target.Finishers, -1, annSet[inst.AnnotationIndex].GetEndOffset(dir));
							//    matchStack.Push(CreateMatch(registers, inst.VariableValues));
							//}
						}
					}
				}

				while (matchStack.Count != 0)
				{
					matchList.Add(matchStack.Pop());
					if (!allMatches)
						return true;
				}
			}

			return matchList.Count > 0;
		}

		private Annotation<TOffset> InitializeStack(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, ModeType mode,
			TData data, IEnumerable<TagMapCommand> cmds, Stack<FsaInstance> instStack)
		{
			TOffset offset = ann.Span.GetStart(_dir);

			ExecuteCommands(registers, cmds, new NullableValue<TOffset>(ann.Span.GetStart(_dir)), new NullableValue<TOffset>(),
				ann.Span.GetEnd(_dir));

			for (Annotation<TOffset> a = ann; a != null && a.Span.GetStart(_dir).Equals(offset); a = a.GetNext(_dir, next => IsValidType(mode, next)))
			{
				if (a.IsOptional)
				{
					Annotation<TOffset> nextAnn = a.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && IsValidType(mode, next));
					if (nextAnn != null)
						InitializeStack(nextAnn, registers, mode, data, cmds, instStack);
				}
			}

			for (; ann != null && ann.Span.GetStart(_dir).Equals(offset); ann = ann.GetNext(_dir, next => IsValidType(mode, next)))
				instStack.Push(new FsaInstance(_startState, ann, data, (NullableValue<TOffset>[,]) registers.Clone()));

			return ann;
		}

		private void AdvanceFsa(IBidirList<Annotation<TOffset>> annList, Annotation<TOffset> ann, TOffset end,
			NullableValue<TOffset>[,] registers, ModeType mode, Transition<TOffset, TData> transition, TData data,
			Stack<FsaInstance> instStack, Stack<FsaMatch<TOffset, TData>> matchStack)
		{
			Annotation<TOffset> nextAnn = ann.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && IsValidType(mode, next));
			TOffset nextOffset = nextAnn == null ? annList.GetLast(_dir, prev => IsValidType(mode, prev)).Span.GetEnd(_dir) : nextAnn.Span.GetStart(_dir);
			var newRegisters = (NullableValue<TOffset>[,]) registers.Clone();
			ExecuteCommands(newRegisters, transition.Commands, new NullableValue<TOffset>(nextOffset), new NullableValue<TOffset>(end),
				ann.Span.GetEnd(_dir));
			if (transition.Target.IsAccepting)
			{
				var matchRegisters = (NullableValue<TOffset>[,]) newRegisters.Clone();
				ExecuteCommands(matchRegisters, transition.Target.Finishers, new NullableValue<TOffset>(), new NullableValue<TOffset>(),
					ann.Span.GetEnd(_dir));
				matchStack.Push(new FsaMatch<TOffset, TData>(matchRegisters, data));
			}
			if (nextAnn != null)
			{
				for (Annotation<TOffset> a = nextAnn; a != null && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNext(_dir, next => IsValidType(mode, next)))
				{
					if (a.IsOptional)
						AdvanceFsa(annList, a, end, registers, mode, transition, data, instStack, matchStack);
				}

				for (Annotation<TOffset> a = nextAnn; a != null && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNext(_dir, next => IsValidType(mode, next)))
					instStack.Push(new FsaInstance(transition.Target, a, data, (NullableValue<TOffset>[,]) newRegisters.Clone()));
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

		//private Match<TOffset, TData> CreateMatch(NullableValue<TOffset>[,] registers, TData data)
		//{
		//    var groups = new Dictionary<int, Span<TOffset>>();
		//    int matchTag = _groups[0];
		//    TOffset matchStart = registers[matchTag, 0].Value;
		//    TOffset matchEnd = registers[matchTag + 1, 1].Value;
		//    var matchSpan = _spanFactory.Create(matchStart, matchEnd);

		//    foreach (KeyValuePair<int, int> kvp in _groups)
		//    {
		//        NullableValue<TOffset> start = registers[kvp.Value, 0];
		//        NullableValue<TOffset> end = registers[kvp.Value + 1, 1];
		//        Span<TOffset> span = !start.HasValue || !end.HasValue ? null : _spanFactory.Create(start.Value, end.Value);
		//        if (matchSpan.Contains(span))
		//            groups[kvp.Key] = span;
		//        //if (start != null && start >= matchStart && start <= matchEnd
		//        //    && end != null && end >= matchStart && end <= matchEnd)
		//        //{
		//        //    groups[kvp.Key] = new AtomSpan(start, end);
		//        //}
		//    }
		//    return new Match<TOffset, TData>(groups, data);
		//}

		private class StateElement : IEquatable<StateElement>
		{
			private readonly State<TOffset, TData> _nfsState;
			private readonly Dictionary<int, int> _tags;
			private int _priority;

			public StateElement(State<TOffset, TData> nfaState)
				: this(nfaState, null)
			{
			}

			public StateElement(State<TOffset, TData> nfaState, IDictionary<int, int> tags)
				: this(nfaState, -1, tags)
			{
			}

			public StateElement(State<TOffset, TData> nfaState, int priority, IDictionary<int, int> tags)
			{
				_nfsState = nfaState;
				_priority = priority;
				_tags = tags == null ? new Dictionary<int, int>() : new Dictionary<int, int>(tags);
			}

			public State<TOffset, TData> NfaState
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
				return Equals(obj as StateElement);
			}

			public bool Equals(StateElement other)
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

		private class SubsetState : HashSet<StateElement>
		{
			public SubsetState()
			{
			}

			public SubsetState(IEnumerable<StateElement> ses)
				: base(ses)
			{
			}

			public State<TOffset, TData> DfaState { get; set; }
		}

		public void Determinize()
		{
			var registerIndices = new Dictionary<int, int>();

			var subsetStart = new SubsetState();
			var se = new StateElement(_startState);
			subsetStart.Add(se);
			subsetStart = EpsilonClosure(subsetStart, subsetStart);

			_states.Clear();
			_startState = CreateState();
			subsetStart.DfaState = _startState;

			var cmdTags = new Dictionary<int, int>();
			foreach (StateElement state in subsetStart)
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

				IEnumerable<ITransitionCondition<TOffset, TData>> conditions = (from state in curSubsetState
																				from tran in state.NfaState.Transitions
																				where tran.Condition != null
																				select tran.Condition).Distinct();
				if (conditions.Any())
				{
					CreateTransitions(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, conditions,
						Enumerable.Empty<ITransitionCondition<TOffset, TData>>(), Enumerable.Empty<ITransitionCondition<TOffset, TData>>());
				}
			}
			_registerCount = _nextTag + registerIndices.Count;
		}

		private void CreateTransitions(Dictionary<SubsetState, SubsetState> subsetStates, Queue<SubsetState> unmarkedSubsetStates,
			Dictionary<int, int> registerIndices, SubsetState curSubsetState, IEnumerable<ITransitionCondition<TOffset, TData>> remaining,
			IEnumerable<ITransitionCondition<TOffset, TData>> subset1, IEnumerable<ITransitionCondition<TOffset, TData>> subset2)
		{
			if (!remaining.Any())
			{
				ITransitionCondition<TOffset, TData> condition = subset1.Aggregate((ITransitionCondition<TOffset, TData>)null,
					(newCondition, curCondition) => newCondition == null ? curCondition : newCondition.Conjunction(curCondition));
				condition = subset2.Aggregate(condition,
					(newCondition, curCondition) => newCondition == null ? curCondition.Negation() : newCondition.Conjunction(curCondition.Negation()));

				if (condition.IsSatisfiable)
				{
					var cmdTags = new Dictionary<int, int>();
					SubsetState u = EpsilonClosure((from state in curSubsetState
					                                from tran in state.NfaState.Transitions
					                                where tran.Condition != null && subset1.Contains(tran.Condition)
					                                select new StateElement(tran.Target, state.Tags)).Distinct(), curSubsetState);
					// this makes the FSA not complete
					if (u.Any())
					{
						foreach (StateElement uState in u)
						{
							foreach (KeyValuePair<int, int> kvp in uState.Tags)
							{
								bool found = false;
								foreach (StateElement curState in curSubsetState)
								{
									if (curState.Tags.Contains(kvp))
									{
										found = true;
										break;
									}
								}

								if (!found)
									cmdTags[kvp.Key] = kvp.Value;
							}
						}

						var cmds = (from kvp in cmdTags
						            select new TagMapCommand(GetRegisterIndex(registerIndices, kvp.Key, kvp.Value),
						                                     TagMapCommand.CurrentPosition)).ToList();

						SubsetState subsetState;
						if (subsetStates.TryGetValue(u, out subsetState))
						{
							ReorderTagIndices(u, subsetState, registerIndices, cmds);
							u = subsetState;
						}
						else
						{
							subsetStates.Add(u, u);
							unmarkedSubsetStates.Enqueue(u);
							StateElement minState = GetMinAcceptingGroup(u);
							if (minState != null)
							{
								u.DfaState = CreateState(from kvp in minState.Tags
								                         let dest = GetRegisterIndex(registerIndices, kvp.Key, 0)
								                         let src = GetRegisterIndex(registerIndices, kvp.Key, kvp.Value)
								                         where dest != src
								                         select new TagMapCommand(dest, src));
							}
							else
							{
								u.DfaState = CreateState();
							}
						}

						curSubsetState.DfaState.AddTransition(new Transition<TOffset, TData>(condition, u.DfaState, cmds));
					}
				}
			}
			else
			{
				ITransitionCondition<TOffset, TData> condition = remaining.First();
				CreateTransitions(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, remaining.Skip(1),
					subset1.Concat(condition), subset2);
				CreateTransitions(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, remaining.Skip(1),
					subset1, subset2.Concat(condition));
			}
		}

		private void ReorderTagIndices(IEnumerable<StateElement> from, IEnumerable<StateElement> to, Dictionary<int, int> registerIndices,
			List<TagMapCommand> cmds)
		{
			var newCmds = new SortedDictionary<int, TagMapCommand>();
			foreach (StateElement fromState in from)
			{
				foreach (KeyValuePair<int, int> kvp in fromState.Tags)
				{
					foreach (StateElement toState in to)
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

		private static StateElement GetMinAcceptingGroup(IEnumerable<StateElement> subsetState)
		{
			StateElement minState = null;
			foreach (StateElement state in subsetState)
			{
				if (state.NfaState.IsAccepting && (minState == null || minState.Priority > state.Priority))
					minState = state;
			}
			return minState;
		}

		private static SubsetState EpsilonClosure(IEnumerable<StateElement> s, IEnumerable<StateElement> prev)
		{
			var stack = new Stack<StateElement>();
			var closure = new Dictionary<int, StateElement>();
			foreach (StateElement state in s)
			{
				state.Priority = 0;
				stack.Push(state);
				closure[state.NfaState.Index] = state;
			}

			while (stack.Count != 0)
			{
				StateElement top = stack.Pop();

				foreach (Transition<TOffset, TData> transition in top.NfaState.Transitions)
				{
					if (transition.Condition == null)
					{
						int newPriority = Math.Max(transition.Priority, top.Priority);
						StateElement tempSe;
						if (closure.TryGetValue(transition.Target.Index, out tempSe))
						{
							if (tempSe.Priority < newPriority)
								continue;
						}

						var newSe = new StateElement(transition.Target, newPriority, top.Tags);

						//if (transition.Tag != -1)
						//{
						//    int maxIndex = -1;
						//    foreach (StateElement se in prev)
						//    {
						//        int index;
						//        if (se.Tags.TryGetValue(transition.Tag, out index))
						//            maxIndex = Math.Max(maxIndex, index);
						//    }

						//    newSe.Tags[transition.Tag] = maxIndex + 1;
						//}

						if (transition.Tag != -1)
						{
							var indices = new List<int>();
							foreach (StateElement se in prev)
							{
								int index;
								if (se.Tags.TryGetValue(transition.Tag, out index))
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

							newSe.Tags[transition.Tag] = minIndex;
						}

						closure[transition.Target.Index] = newSe;
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

		//SubsetState EpsilonClosure(SubsetState s, SubsetState prev)
		//{
		//    Stack<StateElement> stack = new Stack<StateElement>();
		//    SubsetState closure = new SubsetState();
		//    foreach (StateElement state in s)
		//    {
		//        StateElement se = new StateElement(state.NFAState, 0);
		//        stack.Push(se);
		//        closure.Add(state);
		//    }

		//    while (stack.Count != 0)
		//    {
		//        StateElement top = stack.Pop();

		//        foreach (Transition transition in top.NFAState.Transitions)
		//        {
		//            if (transition.Node == null)
		//            {
		//                if (transition.Tag != -1)
		//                {
		//                    int minIndex = -1;
		//                    foreach (StateElement se in s)
		//                    {
		//                        int index;
		//                        if (se.Tags.TryGetValue(transition.Tag, out index))
		//                        {
		//                            if (minIndex == -1 || index < minIndex)
		//                                minIndex = index;
		//                        }
		//                    }

		//                    if (minIndex == -1)
		//                        minIndex = 0;
		//                    else
		//                        minIndex--;
		//                    top.Tags[transition.Tag] = minIndex;
		//                }

		//                foreach (StateElement se in closure)
		//                {
		//                    if (se.NFAState.Index == transition.Target.Index && top.Priority < se.Priority)
		//                    {
		//                        closure.Remove(se);
		//                        break;
		//                    }
		//                }

		//                StateElement newSe = new StateElement(transition.Target, top.Priority, top.Tags);
		//                if (!closure.Contains(newSe))
		//                {
		//                    closure.Add(newSe);
		//                    stack.Push(newSe);
		//                }
		//            }
		//        }
		//    }

		//    return closure;
		//}

		public void ToGraphViz(TextWriter writer)
		{
			writer.WriteLine("digraph G {");

			var stack = new Stack<State<TOffset, TData>>();
			var processed = new HashSet<State<TOffset, TData>>();
			stack.Push(_startState);
			while (stack.Count != 0)
			{
				State<TOffset, TData> state = stack.Pop();
				processed.Add(state);

				writer.Write("  {0} [shape=\"{1}\", color=\"{2}\"", state.Index, state == _startState ? "diamond" : "circle",
					state == _startState ? "green" : state.IsAccepting ? "red" : "black");
				if (state.IsAccepting)
					writer.Write(", peripheries=\"2\"");
				writer.WriteLine("];");

				foreach (Transition<TOffset, TData> transition in state.Transitions)
				{
					writer.WriteLine("  {0} -> {1} [label=\"{2}\"];", state.Index, transition.Target.Index,
						transition);
					if (!processed.Contains(transition.Target) && !stack.Contains(transition.Target))
						stack.Push(transition.Target);
				}
			}

			writer.WriteLine("}");
		}

		public FiniteStateAutomaton<TOffset, TData> Intersect(FiniteStateAutomaton<TOffset, TData> fsa)
		{
			var newFsa = new FiniteStateAutomaton<TOffset, TData>(_dir, _synthesisTypes, _analysisTypes);

			var queue = new Queue<Tuple<State<TOffset, TData>, State<TOffset, TData>>>();
			var newStates = new Dictionary<Tuple<State<TOffset, TData>, State<TOffset, TData>>, State<TOffset, TData>>();
			Tuple<State<TOffset, TData>, State<TOffset, TData>> pair = Tuple.Create(StartState, fsa.StartState);
			queue.Enqueue(pair);
			newStates[pair] = newFsa.StartState;
			while (queue.Count > 0)
			{
				Tuple<State<TOffset, TData>, State<TOffset, TData>> p = queue.Dequeue();
				State<TOffset, TData> s = newStates[p];

				var newTrans = (from t1 in p.Item1.Transitions
							    where t1.Condition == null
							    select new { q = Tuple.Create(t1.Target, p.Item2), cond = (ITransitionCondition<TOffset, TData>) null }
							   ).Union(
							   (from t2 in p.Item2.Transitions
							    where t2.Condition == null
							    select new { q = Tuple.Create(p.Item1, t2.Target), cond = (ITransitionCondition<TOffset, TData>) null }
							   ).Union(
							    from t1 in p.Item1.Transitions
							    from t2 in p.Item2.Transitions
							    where t1.Condition != null && t2.Condition != null
						 	    select new { q = Tuple.Create(t1.Target, t2.Target), cond = t1.Condition.Conjunction(t2.Condition) }
							   ));

				foreach (var newTran in newTrans)
				{
					State<TOffset, TData> r;
					if (!newStates.TryGetValue(newTran.q, out r))
					{
						r = newFsa.CreateState(newTran.q.Item1.IsAccepting && newTran.q.Item2.IsAccepting);
						queue.Enqueue(newTran.q);
						newStates[newTran.q] = r;
					}
					s.AddTransition(new Transition<TOffset, TData>(newTran.cond, r));
				}
			}
			return newFsa;
		}
	}
}
