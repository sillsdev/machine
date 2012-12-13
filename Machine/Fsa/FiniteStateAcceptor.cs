using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Fsa
{
	public class FiniteStateAcceptor<TData, TOffset> : FiniteStateAutomaton<TData, TOffset, FsaMatch<TOffset>> where TData : IData<TOffset>
	{
		private int _nextTag;
		private readonly Dictionary<string, int> _groups;
		private readonly List<TagMapCommand> _initializers;
		private int _registerCount;
		private readonly Func<Annotation<TOffset>, bool> _filter;
		private readonly Direction _dir;
		private bool _tryAllConditions;
		private readonly IEqualityComparer<FsaMatch<TOffset>> _fsaMatchComparer;

		public FiniteStateAcceptor()
			: this(Direction.LeftToRight)
		{
		}

		public FiniteStateAcceptor(Direction dir)
			: this(dir, ann => true)
		{
		}

		public FiniteStateAcceptor(Func<Annotation<TOffset>, bool> filter)
			: this(Direction.LeftToRight, filter)
		{
		}
		
		public FiniteStateAcceptor(Direction dir, Func<Annotation<TOffset>, bool> filter)
		{
			_initializers = new List<TagMapCommand>();
			_groups = new Dictionary<string, int>();
			_dir = dir;
			_filter = filter;
			_tryAllConditions = true;
			_fsaMatchComparer = AnonymousEqualityComparer.Create<FsaMatch<TOffset>>(FsaMatchEquals, FsaMatchGetHashCode);
		}

		public Direction Direction
		{
			get { return _dir; }
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

		public State<TData, TOffset, FsaMatch<TOffset>> CreateTag(State<TData, TOffset, FsaMatch<TOffset>> source, State<TData, TOffset, FsaMatch<TOffset>> target, string groupName, bool isStart)
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

		private State<TData, TOffset, FsaMatch<TOffset>> CreateAcceptingState(IEnumerable<AcceptInfo<TData, TOffset, FsaMatch<TOffset>>> acceptInfos, IEnumerable<TagMapCommand> finishers, bool isLazy)
		{
			var state = new State<TData, TOffset, FsaMatch<TOffset>>(StatesList.Count, acceptInfos, finishers, isLazy);
			StatesList.Add(state);
			return state;
		}

		private class FsaInstance
		{
			private readonly State<TData, TOffset, FsaMatch<TOffset>> _state;
			private readonly Annotation<TOffset> _ann;
			private readonly NullableValue<TOffset>[,] _registers;
			private readonly VariableBindings _varBindings;
			private readonly ISet<State<TData, TOffset, FsaMatch<TOffset>>> _visited;

			public FsaInstance(State<TData, TOffset, FsaMatch<TOffset>> state, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
				VariableBindings varBindings, ISet<State<TData, TOffset, FsaMatch<TOffset>>> visited)
			{
				_state = state;
				_ann = ann;
				_registers = registers;
				_varBindings = varBindings;
				_visited = visited;
			}

			public State<TData, TOffset, FsaMatch<TOffset>> State
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

			public ISet<State<TData, TOffset, FsaMatch<TOffset>>> Visited
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
						VariableBindings varBindings = null;
						foreach (Arc<TData, TOffset, FsaMatch<TOffset>> arc in inst.State.Arcs)
						{
							if (arc.Input == null)
							{
								if (!inst.Visited.Contains(arc.Target))
									instStack.Push(EpsilonAdvanceFsa(data, endAnchor, inst, arc, curMatches));
							}
							else
							{
								if (varBindings == null)
									varBindings = _tryAllConditions && inst.State.Arcs.Count > 1 ? inst.VariableBindings.DeepClone() : inst.VariableBindings;
								if (inst.Annotation != data.Annotations.GetEnd(_dir) && inst.Annotation.FeatureStruct.IsUnifiable(arc.Input.FeatureStruct, useDefaults, varBindings))
								{
									foreach (FsaInstance ni in AdvanceFsa(data, endAnchor, inst.Annotation, inst, varBindings, arc, curMatches))
										instStack.Push(ni);
									if (!_tryAllConditions)
										break;
									varBindings = null;
								}
							}
						}

						//if (!_deterministic && !allMatches && curMatches.Count > 0)
						//	break;
					}
				}

				if (curMatches.Count > 0)
				{
					if (matchList == null)
						matchList = new List<FsaMatch<TOffset>>();
					//if (_deterministic)
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

			matches = Deterministic ? matchList : matchList.Distinct(_fsaMatchComparer);
			return true;
		}

		private bool FsaMatchEquals(FsaMatch<TOffset> x, FsaMatch<TOffset> y)
		{
			if (x.ID != y.ID)
				return false;

			for (int i = 0; i < _registerCount; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					if (x.Registers[i, j].HasValue != y.Registers[i, j].HasValue)
						return false;

					if (x.Registers[i, j].HasValue && !EqualityComparer<TOffset>.Default.Equals(x.Registers[i, j].Value, x.Registers[i, j].Value))
						return false;
				}
			}

			return true;
		}

		private int FsaMatchGetHashCode(FsaMatch<TOffset> m)
		{
			int code = 23;
			code = code * 31 + (m.ID == null ? 0 : m.ID.GetHashCode());
			for (int i = 0; i < _registerCount; i++)
			{
				for (int j = 0; j < 2; j++)
					code = code * 31 + (m.Registers[i, j].HasValue && m.Registers[i, j].Value != null ? EqualityComparer<TOffset>.Default.GetHashCode(m.Registers[i, j].Value) : 0);
			}
			return code;
		}

		private int MatchCompare(FsaMatch<TOffset> x, FsaMatch<TOffset> y)
		{
			int compare = x.Priority.CompareTo(y.Priority);
			if (compare != 0)
				return compare;

			if (x.IsLazy != y.IsLazy)
				return x.IsLazy ? -1 : 1;

			compare = x.Index.CompareTo(y.Index);
			compare = x.IsLazy ? compare : -compare;
			return Deterministic ? compare : -compare;
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

			bool cloneRegisters = false;
			for (; ann != data.Annotations.GetEnd(_dir) && ann.Span.GetStart(_dir).Equals(offset); ann = ann.GetNextDepthFirst(_dir, _filter))
			{
				if (!initAnns.Contains(ann))
				{
					instStack.Push(new FsaInstance(StartState, ann, cloneRegisters ? (NullableValue<TOffset>[,]) newRegisters.Clone() : newRegisters, new VariableBindings(),
						new HashSet<State<TData, TOffset, FsaMatch<TOffset>>>()));
					initAnns.Add(ann);
					cloneRegisters = true;
				}
			}

			return ann;
		}

		private IEnumerable<FsaInstance> AdvanceFsa(TData data, bool endAnchor, Annotation<TOffset> ann, FsaInstance inst, VariableBindings varBindings,
			Arc<TData, TOffset, FsaMatch<TOffset>> arc, List<FsaMatch<TOffset>> curMatches)
		{
			Annotation<TOffset> nextAnn = ann.GetNextDepthFirst(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
			TOffset nextOffset = nextAnn == data.Annotations.GetEnd(_dir) ? data.Annotations.GetLast(_dir, _filter).Span.GetEnd(_dir) : nextAnn.Span.GetStart(_dir);
			TOffset end = ann.Span.GetEnd(_dir);
			var newRegisters = (NullableValue<TOffset>[,]) inst.Registers.Clone();
			ExecuteCommands(newRegisters, arc.Commands, new NullableValue<TOffset>(nextOffset), new NullableValue<TOffset>(end));

			CheckAccepting(data, nextAnn, endAnchor, newRegisters, varBindings, arc, curMatches);

			if (nextAnn != data.Annotations.GetEnd(_dir))
			{
				bool cloneVarBindings = false;
				for (Annotation<TOffset> a = nextAnn; a != data.Annotations.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNextDepthFirst(_dir, _filter))
				{
					if (a.Optional)
					{
						foreach (FsaInstance ni in AdvanceFsa(data, endAnchor, a, inst, varBindings, arc, curMatches))
						{
							yield return ni;
							cloneVarBindings = true;
						}
					}
				}

				bool cloneRegisters = false;
				for (Annotation<TOffset> a = nextAnn; a != data.Annotations.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNextDepthFirst(_dir, _filter))
				{
					yield return new FsaInstance(arc.Target, a, cloneRegisters ? (NullableValue<TOffset>[,]) newRegisters.Clone() : newRegisters,
						cloneVarBindings ? varBindings.DeepClone() : varBindings, new HashSet<State<TData, TOffset, FsaMatch<TOffset>>>());
					cloneVarBindings = true;
					cloneRegisters = true;
				}
			}
			else if (!Deterministic)
			{
				yield return new FsaInstance(arc.Target, nextAnn, newRegisters, varBindings, new HashSet<State<TData, TOffset, FsaMatch<TOffset>>>());
			}
		}

		private FsaInstance EpsilonAdvanceFsa(TData data, bool endAnchor, FsaInstance inst, Arc<TData, TOffset, FsaMatch<TOffset>> arc, List<FsaMatch<TOffset>> curMatches)
		{
			Annotation<TOffset> prevAnn = inst.Annotation.GetPrevDepthFirst(_dir, (cur, prev) => !cur.Span.Overlaps(prev.Span) && _filter(prev));
			var newRegisters = (NullableValue<TOffset>[,]) inst.Registers.Clone();
			ExecuteCommands(newRegisters, arc.Commands, new NullableValue<TOffset>(inst.Annotation.Span.GetStart(_dir)), new NullableValue<TOffset>(prevAnn.Span.GetEnd(_dir)));
			CheckAccepting(data, inst.Annotation, endAnchor, newRegisters, inst.VariableBindings, arc, curMatches);
			return new FsaInstance(arc.Target, inst.Annotation, newRegisters, inst.VariableBindings.DeepClone(), new HashSet<State<TData, TOffset, FsaMatch<TOffset>>>(inst.Visited) {arc.Target});
		}

		private void CheckAccepting(TData data, Annotation<TOffset> ann, bool endAnchor, NullableValue<TOffset>[,] registers,
			VariableBindings varBindings, Arc<TData, TOffset, FsaMatch<TOffset>> arc, List<FsaMatch<TOffset>> curMatches)
		{
			if (arc.Target.IsAccepting && (!endAnchor || ann == data.Annotations.GetEnd(_dir)))
			{
				var matchRegisters = (NullableValue<TOffset>[,]) registers.Clone();
				ExecuteCommands(matchRegisters, arc.Target.Finishers, new NullableValue<TOffset>(), new NullableValue<TOffset>());
				foreach (AcceptInfo<TData, TOffset, FsaMatch<TOffset>> acceptInfo in arc.Target.AcceptInfos)
				{
					var candidate = new FsaMatch<TOffset>(acceptInfo.ID, matchRegisters, varBindings.DeepClone(), acceptInfo.Priority, arc.Target.IsLazy, ann, curMatches.Count);
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

		public void Determinize()
		{
			Optimize(DeterministicGetArcs, true);
			Deterministic = true;
			_tryAllConditions = false;
		}

		public void Quasideterminize()
		{
			Optimize(QuasideterministicGetArcs, true);
			Deterministic = true;
			_tryAllConditions = true;
		}

		public void EpsilonRemoval()
		{
			Optimize(EpsilonRemovalGetArcs, false);
			Deterministic = false;
			_tryAllConditions = true;
		}

		private void MarkArcPriorities()
		{
			var visited = new HashSet<State<TData, TOffset, FsaMatch<TOffset>>>();
			var todo = new Stack<Arc<TData, TOffset, FsaMatch<TOffset>>>(StartState.Arcs.Reverse());
			int nextPriority = 0;
			while (todo.Count > 0)
			{
				Arc<TData, TOffset, FsaMatch<TOffset>> arc = todo.Pop();
				arc.Priority = nextPriority++;
				if (!visited.Contains(arc.Target))
				{
					visited.Add(arc.Target);
					foreach (Arc<TData, TOffset, FsaMatch<TOffset>> nextArc in arc.Target.Arcs)
						todo.Push(nextArc);
				}
			}
		}

		private class NfaStateInfo : IEquatable<NfaStateInfo>, IComparable<NfaStateInfo>
		{
			private readonly State<TData, TOffset, FsaMatch<TOffset>> _nfsState;
			private readonly Dictionary<int, int> _tags;
			private readonly int _lastPriority;
			private readonly int _maxPriority;

			public NfaStateInfo(State<TData, TOffset, FsaMatch<TOffset>> nfaState, int maxPriority = 0, int lastPriority = 0, IDictionary<int, int> tags = null)
			{
				_nfsState = nfaState;
				_maxPriority = maxPriority;
				_lastPriority = lastPriority;
				_tags = tags == null ? new Dictionary<int, int>() : new Dictionary<int, int>(tags);
			}

			public State<TData, TOffset, FsaMatch<TOffset>> NfaState
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

			public Dictionary<int, int> Tags
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

			public override string ToString()
			{
				return string.Format("State {0} ({1}, {2})", _nfsState.Index, _maxPriority, _lastPriority);
			}
		}

		private class SubsetState : IEquatable<SubsetState>
		{
			private readonly HashSet<NfaStateInfo> _nfaStates;

			public SubsetState(NfaStateInfo nfaState)
			{
				_nfaStates = new HashSet<NfaStateInfo> {nfaState};
			}

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

			public State<TData, TOffset, FsaMatch<TOffset>> State { get; set; }

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

		private static IEnumerable<Tuple<SubsetState, Predicate>> DeterministicGetArcs(SubsetState from)
		{
			ILookup<Predicate, NfaStateInfo> conditions = from.NfaStates
				.SelectMany(state => state.NfaState.Arcs, (state, arc) => new { State = state, Arc = arc} )
				.Where(stateArc => stateArc.Arc.Input != null)
				.ToLookup(stateArc => stateArc.Arc.Input, stateArc => new NfaStateInfo(stateArc.Arc.Target,
					Math.Max(stateArc.Arc.Priority, stateArc.State.MaxPriority), stateArc.Arc.Priority, stateArc.State.Tags));

			var preprocessedConditions = new List<Tuple<Predicate, IEnumerable<NfaStateInfo>>> { Tuple.Create((Predicate) null, Enumerable.Empty<NfaStateInfo>()) };
			foreach (IGrouping<Predicate, NfaStateInfo> cond in conditions)
			{
				FeatureStruct negation;
				if (!cond.Key.FeatureStruct.Negation(out negation))
					negation = null;

				var temp = new List<Tuple<Predicate, IEnumerable<NfaStateInfo>>>();
				foreach (Tuple<Predicate, IEnumerable<NfaStateInfo>> preprocessedCond in preprocessedConditions)
				{
					FeatureStruct newCond;
					if (preprocessedCond.Item1 == null)
						newCond = cond.Key.FeatureStruct;
					else if (!preprocessedCond.Item1.FeatureStruct.Unify(cond.Key.FeatureStruct, false, new VariableBindings(), false, out newCond))
						newCond = null;
					if (newCond != null)
						temp.Add(Tuple.Create(new Predicate(newCond), preprocessedCond.Item2.Concat(cond)));

					if (negation != null)
					{
						if (preprocessedCond.Item1 == null)
							newCond = negation;
						else if (!preprocessedCond.Item1.FeatureStruct.Unify(negation, false, new VariableBindings(), false, out newCond))
							newCond = null;
						if (newCond != null)
							temp.Add(Tuple.Create(new Predicate(newCond), preprocessedCond.Item2));
					}
				}
				preprocessedConditions = temp;
			}

			foreach (Tuple<Predicate, IEnumerable<NfaStateInfo>> preprocessedCond in preprocessedConditions)
			{
				var reach = new SubsetState(preprocessedCond.Item2);
				FeatureStruct condition;
				if (!reach.IsEmpty && preprocessedCond.Item1.FeatureStruct.CheckDisjunctiveConsistency(false, new VariableBindings(), out condition))
				{
					condition.Freeze();
					yield return Tuple.Create(reach, new Predicate(condition));
				}
			}
		}

		private static IEnumerable<Tuple<SubsetState, Predicate>> QuasideterministicGetArcs(SubsetState from)
		{
			ILookup<Predicate, NfaStateInfo> conditions = from.NfaStates
				.SelectMany(state => state.NfaState.Arcs, (state, arc) => new { State = state, Arc = arc} )
				.Where(stateArc => stateArc.Arc.Input != null)
				.ToLookup(stateArc => stateArc.Arc.Input, stateArc => new NfaStateInfo(stateArc.Arc.Target,
					Math.Max(stateArc.Arc.Priority, stateArc.State.MaxPriority), stateArc.Arc.Priority, stateArc.State.Tags));

			return conditions.Select(cond => Tuple.Create(new SubsetState(cond), cond.Key));
		}

		private static IEnumerable<Tuple<SubsetState, Predicate>> EpsilonRemovalGetArcs(SubsetState from)
		{
			foreach (NfaStateInfo q in from.NfaStates)
			{
				foreach (Arc<TData, TOffset, FsaMatch<TOffset>> arc in q.NfaState.Arcs.Where(a => a.Input != null))
				{
					var nsi = new NfaStateInfo(arc.Target);
					yield return Tuple.Create(new SubsetState(nsi), arc.Input);
				}
			}
		}

		private void Optimize(Func<SubsetState, IEnumerable<Tuple<SubsetState, Predicate>>> arcsSelector, bool deterministic)
		{
			MarkArcPriorities();

			_registerCount = 0;
			var registerIndices = new Dictionary<Tuple<int, int>, int>();

			var startState = new NfaStateInfo(StartState);
			var subsetStart = new SubsetState(startState.ToEnumerable());
			subsetStart = EpsilonClosure(subsetStart, subsetStart);

			StatesList.Clear();
			CreateOptimizedState(subsetStart, registerIndices);
			StartState = subsetStart.State;

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

				foreach (Tuple<SubsetState, Predicate> t in arcsSelector(curSubsetState))
					CreateOptimizedArc(subsetStates, unmarkedSubsetStates, registerIndices, curSubsetState, t.Item1, t.Item2, deterministic);

				Arc<TData, TOffset, FsaMatch<TOffset>>[] arcs = curSubsetState.State.Arcs.ToArray();
				for (int i = 0; i < arcs.Length; i++)
				{
				    for (int j = i + 1; j < arcs.Length; j++)
				    {
				        FeatureStruct fs;
				        if (arcs[i].Target.Equals(arcs[j].Target) && arcs[i].Input.FeatureStruct.Unify(arcs[j].Input.FeatureStruct, out fs))
				        {
				            arcs[j].Input = new Predicate(fs);
				            curSubsetState.State.Arcs.Remove(arcs[i]);
				            break;
				        }
				    }
				}
			}

			var regNums = new Dictionary<int, int>();
			for (_registerCount = 0; _registerCount < _nextTag; _registerCount++)
				regNums[_registerCount] = _registerCount;

			foreach (State<TData, TOffset, FsaMatch<TOffset>> state in StatesList)
			{
				RenumberCommands(regNums, state.Finishers);
				foreach (Arc<TData, TOffset, FsaMatch<TOffset>> arc in state.Arcs)
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

		private void CreateOptimizedArc(Dictionary<SubsetState, SubsetState> subsetStates, Queue<SubsetState> unmarkedSubsetStates,
			Dictionary<Tuple<int, int>, int> registerIndices, SubsetState curSubsetState, SubsetState reach, Predicate input, bool deterministic)
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
				if (!deterministic)
				{
					var tags = new HashSet<int>();
					foreach (NfaStateInfo srcState in curSubsetState.NfaStates)
					{
						foreach (KeyValuePair<int, int> tag in srcState.Tags)
						{
							if (tag.Value > 0 && !tags.Contains(tag.Key))
							{
								tags.Add(tag.Key);
								int src = GetRegisterIndex(registerIndices, tag.Key, tag.Value);
								int dest = GetRegisterIndex(registerIndices, tag.Key, 0);
								cmds.Add(new TagMapCommand(dest, src));
							}
						}
					}
				}
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
					CreateOptimizedState(target, registerIndices);
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

				curSubsetState.State.Arcs.Add(input, target.State, cmds);
			}
		}

		private void CreateOptimizedState(SubsetState subsetState, Dictionary<Tuple<int, int>, int> registerIndices)
		{
			NfaStateInfo[] acceptingStates = (from state in subsetState.NfaStates
											  where state.NfaState.IsAccepting
											  orderby state descending
											  select state).ToArray();
			if (acceptingStates.Length > 0)
			{
				IEnumerable<AcceptInfo<TData, TOffset, FsaMatch<TOffset>>> acceptInfos = acceptingStates.SelectMany(state => state.NfaState.AcceptInfos);

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
				subsetState.State = CreateAcceptingState(acceptInfos, finishers, IsLazyAcceptingState(subsetState));
			}
			else
			{
				subsetState.State = CreateState();
			}
		}

		private bool IsLazyAcceptingState(SubsetState state)
		{
			//Arc<TData, TOffset, FsaMatch<TOffset>> arc = state.NfaStates.SelectMany(s => s.NfaState.Arcs).MinBy(a => a.Priority);
			//State<TData, TOffset, FsaMatch<TOffset>> curState = arc.Target;
			//while (!curState.IsAccepting)
			//{
			//    Arc<TData, TOffset, FsaMatch<TOffset>> highestPriArc = curState.Arcs.MinBy(a => a.Priority);
			//    if (highestPriArc.Condition != null)
			//        break;
			//    curState = highestPriArc.Target;
			//}
			//return curState.IsAccepting;

			//foreach (Arc<TData, TOffset, FsaMatch<TOffset>> arc in state.NfaStates.Min().NfaState.Arcs)
			//{
				//State<TData, TOffset, FsaMatch<TOffset>> curState = arc.Target;
				State<TData, TOffset, FsaMatch<TOffset>> curState = state.NfaStates.Min().NfaState;
				while (!curState.IsAccepting)
				{
					Arc<TData, TOffset, FsaMatch<TOffset>> highestPriArc = curState.Arcs.MinBy(a => a.Priority);
					if (highestPriArc.Input != null)
						break;
					curState = highestPriArc.Target;
				}

				if (curState.IsAccepting)
				{
					if ((from s in state.NfaStates
						 from tran in s.NfaState.Arcs
						 where tran.Input != null
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

				foreach (Arc<TData, TOffset, FsaMatch<TOffset>> arc in topState.NfaState.Arcs)
				{
					if (arc.Input == null)
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

		public FiniteStateAcceptor<TData, TOffset> Intersect(FiniteStateAcceptor<TData, TOffset> other)
		{
			var newFsa = new FiniteStateAcceptor<TData, TOffset>(_dir, ann => _filter(ann) && other._filter(ann));

			var queue = new Queue<Tuple<State<TData, TOffset, FsaMatch<TOffset>>, State<TData, TOffset, FsaMatch<TOffset>>>>();
			var newStates = new Dictionary<Tuple<State<TData, TOffset, FsaMatch<TOffset>>, State<TData, TOffset, FsaMatch<TOffset>>>, State<TData, TOffset, FsaMatch<TOffset>>>();
			Tuple<State<TData, TOffset, FsaMatch<TOffset>>, State<TData, TOffset, FsaMatch<TOffset>>> pair = Tuple.Create(StartState, other.StartState);
			queue.Enqueue(pair);
			newStates[pair] = newFsa.StartState;
			while (queue.Count > 0)
			{
				Tuple<State<TData, TOffset, FsaMatch<TOffset>>, State<TData, TOffset, FsaMatch<TOffset>>> p = queue.Dequeue();
				State<TData, TOffset, FsaMatch<TOffset>> s = newStates[p];

				var newArcs = (from t1 in p.Item1.Arcs
							   where t1.Input == null
							   select new { q = Tuple.Create(t1.Target, p.Item2), cond = (FeatureStruct) null }
							  ).Union(
							  (from t2 in p.Item2.Arcs
							   where t2.Input == null
							   select new { q = Tuple.Create(p.Item1, t2.Target), cond = (FeatureStruct) null }
							  ).Union(
							   from t1 in p.Item1.Arcs
							   where t1.Input != null
							   from t2 in p.Item2.Arcs
							   let newCond = t2.Input != null ? Unify(t1.Input.FeatureStruct, t2.Input.FeatureStruct) : null
							   where newCond != null
							   select new { q = Tuple.Create(t1.Target, t2.Target), cond = newCond }
							  ));

				foreach (var newArc in newArcs)
				{
					State<TData, TOffset, FsaMatch<TOffset>> r;
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

		public void Minimize()
		{
		    if (!Deterministic)
		        throw new InvalidOperationException("The FSA must be deterministic to be minimized.");

			var acceptingStates = new HashSet<State<TData, TOffset, FsaMatch<TOffset>>>();
			var nonacceptingStates = new HashSet<State<TData, TOffset, FsaMatch<TOffset>>>();
			foreach (State<TData, TOffset, FsaMatch<TOffset>> state in StatesList)
			{
				if (state.IsAccepting)
					acceptingStates.Add(state);
				else
					nonacceptingStates.Add(state);
			}

			var partitions = new List<HashSet<State<TData, TOffset, FsaMatch<TOffset>>>> {nonacceptingStates};
			var waiting = new List<HashSet<State<TData, TOffset, FsaMatch<TOffset>>>>();
			foreach (IGrouping<MinimizeStateInfo, State<TData, TOffset, FsaMatch<TOffset>>> acceptingPartition in acceptingStates.GroupBy(s => new MinimizeStateInfo(s)))
			{
				var set = new HashSet<State<TData, TOffset, FsaMatch<TOffset>>>(acceptingPartition);
				partitions.Add(set);
				waiting.Add(set);
			}

			while (waiting.Count > 0)
			{
				HashSet<State<TData, TOffset, FsaMatch<TOffset>>> a = waiting.First();
				waiting.RemoveAt(0);
				foreach (IGrouping<MinimizeStateInfo, State<TData, TOffset, FsaMatch<TOffset>>> x in StatesList.SelectMany(state => state.Arcs, (state, arc) => new MinimizeStateInfo(state, arc))
					.Where(msi => a.Contains(msi.Arc.Target)).GroupBy(msi => msi, msi => msi.State))
				{
					for (int i = partitions.Count - 1; i >= 0; i--)
					{
						HashSet<State<TData, TOffset, FsaMatch<TOffset>>> y = partitions[i];
						var subset1 = new HashSet<State<TData, TOffset, FsaMatch<TOffset>>>(x.Intersect(y));
						if (subset1.Count == 0)
							continue;

						var subset2 = new HashSet<State<TData, TOffset, FsaMatch<TOffset>>>(y.Except(x));
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

			Dictionary<State<TData, TOffset, FsaMatch<TOffset>>, State<TData, TOffset, FsaMatch<TOffset>>> nondistinguisablePairs = partitions.Where(p => p.Count > 1)
				.SelectMany(p => p.Where(state => !state.Equals(p.First())), (p, state) => new {Key = state, Value = p.First()}).ToDictionary(pair => pair.Key, pair => pair.Value);
			if (nondistinguisablePairs.Count > 0)
			{
		        var statesToRemove = new HashSet<State<TData, TOffset, FsaMatch<TOffset>>>(StatesList.Where(s => !s.Equals(StartState)));
		        foreach (State<TData, TOffset, FsaMatch<TOffset>> state in StatesList)
		        {
		            foreach (Arc<TData, TOffset, FsaMatch<TOffset>> arc in state.Arcs)
		            {
		                State<TData, TOffset, FsaMatch<TOffset>> curState = arc.Target;
		                State<TData, TOffset, FsaMatch<TOffset>> s;
		                while (nondistinguisablePairs.TryGetValue(curState, out s))
		                    curState = s;
		                arc.Target = curState;
		                statesToRemove.Remove(curState);
		            }
		        }
		        StatesList.RemoveAll(statesToRemove.Contains);
			}
		}

		private class MinimizeStateInfo : IEquatable<MinimizeStateInfo>
		{
			private readonly State<TData, TOffset, FsaMatch<TOffset>> _state;
			private readonly Arc<TData, TOffset, FsaMatch<TOffset>> _arc;

			public MinimizeStateInfo(State<TData, TOffset, FsaMatch<TOffset>> state, Arc<TData, TOffset, FsaMatch<TOffset>> arc = null)
			{
				_state = state;
				_arc = arc;
			}

			public State<TData, TOffset, FsaMatch<TOffset>> State
			{
				get { return _state; }
			}

			public Arc<TData, TOffset, FsaMatch<TOffset>> Arc
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
	}
}
