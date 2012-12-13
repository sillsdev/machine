using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Fsa
{
	public class FiniteStateTransducer<TData, TOffset> : FiniteStateAutomaton<TData, TOffset, FstResult<TData, TOffset>> where TData : IData<TOffset>, IDeepCloneable<TData>
	{
		private readonly Direction _dir;
		private readonly Func<Annotation<TOffset>, bool> _filter;
		private bool _tryAllConditions;

		public FiniteStateTransducer()
			: this(Direction.LeftToRight)
		{
		}

		public FiniteStateTransducer(Direction dir)
			: this(dir, ann => true)
		{
		}

		public FiniteStateTransducer(Func<Annotation<TOffset>, bool> filter)
			: this(Direction.LeftToRight, filter)
		{
		}
		
		public FiniteStateTransducer(Direction dir, Func<Annotation<TOffset>, bool> filter)
		{
			_dir = dir;
			_filter = filter;
			_tryAllConditions = true;
		}

		private class FstInstance
		{
			private readonly TData _output;
			private readonly IDictionary<Annotation<TOffset>, Annotation<TOffset>> _mappings; 
			private readonly Queue<Annotation<TOffset>> _queue;
			private readonly VariableBindings _varBindings;
			private readonly ISet<State<TData, TOffset, FstResult<TData, TOffset>>> _visited;

			public FstInstance(State<TData, TOffset, FstResult<TData, TOffset>> state, Annotation<TOffset> ann, TData output, IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings,
				Queue<Annotation<TOffset>> queue, VariableBindings varBindings, ISet<State<TData, TOffset, FstResult<TData, TOffset>>> visited)
			{
				State = state;
				Annotation = ann;
				_output = output;
				_mappings = mappings;
				_queue = queue;
				_varBindings = varBindings;
				_visited = visited;
			}

			public State<TData, TOffset, FstResult<TData, TOffset>> State { get; set; }

			public Annotation<TOffset> Annotation { get; set; }

			public TData Output
			{
				get { return _output; }
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

			public ISet<State<TData, TOffset, FstResult<TData, TOffset>>> Visited
			{
				get { return _visited; }
			}
		}

		public bool Transduce(TData data, Annotation<TOffset> start, bool startAnchor, bool endAnchor, bool allMatches, bool useDefaults, out IEnumerable<FstResult<TData, TOffset>> results)
		{
			var instStack = new Stack<FstInstance>();

			List<FstResult<TData, TOffset>> resultList = null;

			Annotation<TOffset> ann = start;

			var initAnns = new HashSet<Annotation<TOffset>>();
			while (ann != data.Annotations.GetEnd(_dir))
			{
				ann = InitializeStack(data, ann, instStack, initAnns);

				var curResults = new List<FstResult<TData, TOffset>>(); 
				while (instStack.Count != 0)
				{
					FstInstance inst = instStack.Pop();

					if (inst.Annotation != null)
					{
						VariableBindings varBindings = null;
						foreach (Arc<TData, TOffset, FstResult<TData, TOffset>> arc in inst.State.Arcs)
						{
							if (arc.Input == null)
							{
								if (!inst.Visited.Contains(arc.Target))
								{
									FstInstance curInst = _tryAllConditions && inst.State.Arcs.Count > 1
										? CreateInstance(inst.State, inst.Annotation, inst.Output, inst.Mappings, inst.Queue, varBindings, inst.Visited) : inst;
									ExecuteOutputs(arc.Outputs, curInst);
									instStack.Push(EpsilonAdvanceFst(data, endAnchor, curInst, arc, curResults));
								}
							}
							else
							{
								if (varBindings == null)
									varBindings = _tryAllConditions && inst.State.Arcs.Count > 1 ? inst.VariableBindings.DeepClone() : inst.VariableBindings;
								if (inst.Annotation != data.Annotations.GetEnd(_dir) && inst.Annotation.FeatureStruct.IsUnifiable(arc.Input.FeatureStruct, useDefaults, varBindings))
								{
									FstInstance curInst = _tryAllConditions && inst.State.Arcs.Count > 1
										? CreateInstance(inst.State, inst.Annotation, inst.Output, inst.Mappings, inst.Queue, varBindings, Enumerable.Empty<State<TData, TOffset, FstResult<TData, TOffset>>>())
										: inst;

									if (arc.Input.Identity)
										curInst.Queue.Enqueue(inst.Annotation);

									ExecuteOutputs(arc.Outputs, curInst);

									foreach (FstInstance ni in AdvanceFst(data, endAnchor, inst.Annotation, inst, arc, curResults))
										instStack.Push(ni);

									if (!_tryAllConditions)
										break;
									varBindings = null;
								}
							}
						}
					}
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

			results = resultList;
			return true;
		}

		private void ExecuteOutputs(IEnumerable<Predicate> outputs, FstInstance inst)
		{
			foreach (Predicate output in outputs)
			{
				if (output.Identity)
				{
					Annotation<TOffset> inputAnn = inst.Queue.Dequeue();
					Annotation<TOffset> outputAnn = inst.Mappings[inputAnn];
					outputAnn.FeatureStruct.PriorityUnion(output.FeatureStruct);
				}
				else
				{
					
				}
			}
		}

		private int ResultCompare(FstResult<TData, TOffset> x, FstResult<TData, TOffset> y)
		{
			int compare = x.Priority.CompareTo(y.Priority);
			if (compare != 0)
				return compare;

			compare = x.Index.CompareTo(y.Index);
			return Deterministic ? compare : -compare;
		}

		private Annotation<TOffset> InitializeStack(TData data, Annotation<TOffset> ann, Stack<FstInstance> instStack, HashSet<Annotation<TOffset>> initAnns)
		{
			TOffset offset = ann.Span.GetStart(_dir);

			for (Annotation<TOffset> a = ann; a != data.Annotations.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(offset); a = a.GetNextDepthFirst(_dir, _filter))
			{
				if (a.Optional)
				{
					Annotation<TOffset> nextAnn = a.GetNextDepthFirst(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
					if (nextAnn != null)
						InitializeStack(data, nextAnn, instStack, initAnns);
				}
			}

			for (; ann != data.Annotations.GetEnd(_dir) && ann.Span.GetStart(_dir).Equals(offset); ann = ann.GetNextDepthFirst(_dir, _filter))
			{
				if (!initAnns.Contains(ann))
				{
					TData newOutput = data.DeepClone();
					Dictionary<Annotation<TOffset>, Annotation<TOffset>> mappings = data.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
						.Zip(newOutput.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
					instStack.Push(new FstInstance(StartState, ann, newOutput, mappings, new Queue<Annotation<TOffset>>(), new VariableBindings(),
						new HashSet<State<TData, TOffset, FstResult<TData, TOffset>>>()));
					initAnns.Add(ann);
				}
			}

			return ann;
		}

		private IEnumerable<FstInstance> AdvanceFst(TData data, bool endAnchor, Annotation<TOffset> ann, FstInstance inst,
			Arc<TData, TOffset, FstResult<TData, TOffset>> arc, List<FstResult<TData, TOffset>> curResults)
		{
			Annotation<TOffset> nextAnn = ann.GetNextDepthFirst(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
			TOffset nextOffset = nextAnn == data.Annotations.GetEnd(_dir) ? data.Annotations.GetLast(_dir, _filter).Span.GetEnd(_dir) : nextAnn.Span.GetStart(_dir);

			CheckAccepting(data, nextAnn, endAnchor, inst.Output, inst.VariableBindings, arc, curResults);

			if (nextAnn != data.Annotations.GetEnd(_dir))
			{
				bool reuseInst = true;
				for (Annotation<TOffset> a = nextAnn; a != data.Annotations.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNextDepthFirst(_dir, _filter))
				{
					if (a.Optional)
					{
						foreach (FstInstance ni in AdvanceFst(data, endAnchor, a, inst, arc, curResults))
						{
							yield return ni;
							reuseInst = false;
						}
					}
				}

				for (Annotation<TOffset> a = nextAnn; a != data.Annotations.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(nextOffset); a = a.GetNextDepthFirst(_dir, _filter))
				{
					FstInstance newInst = GetInstance(arc.Target, a, inst, reuseInst);
					newInst.Visited.Clear();
					yield return newInst;
					reuseInst = false;
				}
			}
			else
			{
				FstInstance newInst = GetInstance(arc.Target, nextAnn, inst, true);
				newInst.Visited.Clear();
				yield return newInst;
			}
		}

		private FstInstance EpsilonAdvanceFst(TData data, bool endAnchor, FstInstance inst, Arc<TData, TOffset, FstResult<TData, TOffset>> arc, List<FstResult<TData, TOffset>> curResults)
		{
			CheckAccepting(data, inst.Annotation, endAnchor, inst.Output, inst.VariableBindings, arc, curResults);
			FstInstance newInst = GetInstance(arc.Target, inst.Annotation, inst, true);
			newInst.Visited.Add(arc.Target);
			return newInst;
		}

		private FstInstance GetInstance(State<TData, TOffset, FstResult<TData, TOffset>> state, Annotation<TOffset> ann, FstInstance inst, bool reuse)
		{
			if (reuse)
			{
				inst.State = state;
				inst.Annotation = ann;
				return inst;
			}
			return CreateInstance(state, ann, inst.Output, inst.Mappings, inst.Queue, inst.VariableBindings.DeepClone(),
				Enumerable.Empty<State<TData, TOffset, FstResult<TData, TOffset>>>());
		}

		private FstInstance CreateInstance(State<TData, TOffset, FstResult<TData, TOffset>> state, Annotation<TOffset> ann, TData output, IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings,
				Queue<Annotation<TOffset>> queue, VariableBindings varBindings, IEnumerable<State<TData, TOffset, FstResult<TData, TOffset>>> visited)
		{
			TData newOutput = output.DeepClone();

			Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
				.Zip(newOutput.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
			Dictionary<Annotation<TOffset>, Annotation<TOffset>> newMappings = mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);

			var newQueue = new Queue<Annotation<TOffset>>(queue);
			var newVisited = new HashSet<State<TData, TOffset, FstResult<TData, TOffset>>>(visited);
			return new FstInstance(state, ann, newOutput, newMappings, newQueue, varBindings, newVisited);
		}

		private void CheckAccepting(TData data, Annotation<TOffset> ann, bool endAnchor, TData output, VariableBindings varBindings, Arc<TData, TOffset, FstResult<TData, TOffset>> arc,
			List<FstResult<TData, TOffset>> curResults)
		{
			if (arc.Target.IsAccepting && (!endAnchor || ann == data.Annotations.GetEnd(_dir)))
			{
				if (arc.Target.AcceptInfos.Count > 0)
				{
					foreach (AcceptInfo<TData, TOffset, FstResult<TData, TOffset>> acceptInfo in arc.Target.AcceptInfos)
					{
						var candidate = new FstResult<TData, TOffset>(acceptInfo.ID, output, varBindings, acceptInfo.Priority, ann, curResults.Count);
						if (acceptInfo.Acceptable(data, candidate))
							curResults.Add(candidate);
					}
				}
				else
				{
					curResults.Add(new FstResult<TData, TOffset>(null, output, varBindings, -1, ann, curResults.Count));
				}
			}
		}

		public void Determinize()
		{
			Optimize(DeterministicGetArcs);
			Deterministic = true;
			_tryAllConditions = false;
		}

		private class NfaStateInfo : IEquatable<NfaStateInfo>
		{
			private readonly State<TData, TOffset, FstResult<TData, TOffset>> _nfaState;
			private readonly List<Predicate> _output;

			public NfaStateInfo(State<TData, TOffset, FstResult<TData, TOffset>> nfaState, IEnumerable<Predicate> output)
			{
				_nfaState = nfaState;
				_output = output.ToList();
			}

			public State<TData, TOffset, FstResult<TData, TOffset>> NfaState
			{
				get
				{
					return _nfaState;
				}
			}

			public List<Predicate> Output
			{
				get { return _output; }
			}

			public override int GetHashCode()
			{
				return _nfaState.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as NfaStateInfo);
			}

			public bool Equals(NfaStateInfo other)
			{
				if (other == null)
					return false;

				return _nfaState.Equals(other._nfaState);
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

			public bool IsEmpty
			{
				get { return _nfaStates.Count == 0; }
			}

			public State<TData, TOffset, FstResult<TData, TOffset>> State { get; set; }

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

		private static IEnumerable<Tuple<SubsetState, Predicate, IEnumerable<Predicate>>> DeterministicGetArcs(SubsetState from)
		{
			ILookup<Predicate, NfaStateInfo> conditions = from.NfaStates
				.SelectMany(state => state.NfaState.Arcs, (state, arc) => new { State = state, Arc = arc} )
				.Where(stateArc => stateArc.Arc.Input != null)
				.ToLookup(stateArc => stateArc.Arc.Input, stateArc => new NfaStateInfo(stateArc.Arc.Target, stateArc.State.Output.Concat(stateArc.Arc.Outputs)));

			var preprocessedConditions = new List<Tuple<Predicate, IEnumerable<NfaStateInfo>>> { Tuple.Create((Predicate) null, Enumerable.Empty<NfaStateInfo>()) };
			foreach (IGrouping<Predicate, NfaStateInfo> cond in conditions)
			{
				FeatureStruct negation;
				if (!cond.Key.FeatureStruct.Negation(out negation))
					negation = null;

				var temp = new List<Tuple<Predicate, IEnumerable<NfaStateInfo>>>();
				foreach (Tuple<Predicate, IEnumerable<NfaStateInfo>> preprocessedCond in preprocessedConditions)
				{
					bool identity = cond.Key.Identity || (preprocessedCond.Item1 != null && preprocessedCond.Item1.Identity);

					FeatureStruct newCond;
					if (preprocessedCond.Item1 == null)
						newCond = cond.Key.FeatureStruct;
					else if (!preprocessedCond.Item1.FeatureStruct.Unify(cond.Key.FeatureStruct, false, new VariableBindings(), false, out newCond))
						newCond = null;
					if (newCond != null)
						temp.Add(Tuple.Create(new Predicate(newCond, identity), preprocessedCond.Item2.Concat(cond)));

					if (negation != null)
					{
						if (preprocessedCond.Item1 == null)
							newCond = negation;
						else if (!preprocessedCond.Item1.FeatureStruct.Unify(negation, false, new VariableBindings(), false, out newCond))
							newCond = null;
						if (newCond != null)
							temp.Add(Tuple.Create(new Predicate(newCond, identity), preprocessedCond.Item2));
					}
				}
				preprocessedConditions = temp;
			}

			foreach (Tuple<Predicate, IEnumerable<NfaStateInfo>> preprocessedCond in preprocessedConditions)
			{
				var commonPrefix = new List<Predicate>();
				bool first = true;
				var states = new List<NfaStateInfo>();
				foreach (IGrouping<NfaStateInfo, NfaStateInfo> group in preprocessedCond.Item2.GroupBy(s => s))
				{
					List<NfaStateInfo> nsis = group.ToList();

					var state = new NfaStateInfo(nsis[0].NfaState, nsis[0].Output);
					while (nsis.Count > 1)
					{
						NfaStateInfo otherState = nsis[nsis.Count - 1];
						for (int i = 0; i < state.Output.Count; i++)
						{
							if (!state.Output[i].Equals(otherState.Output[i]))
							{
								var set = new HashSet<FeatureStruct>(FreezableEqualityComparer<FeatureStruct>.Instance);
								if (state.Output[i].FeatureStruct.Features.Count == 0 && state.Output[i].FeatureStruct.Disjunctions.Count == 1)
									set.UnionWith(state.Output[i].FeatureStruct.Disjunctions.First());
								else
									set.Add(state.Output[i].FeatureStruct);
								if (otherState.Output[i].FeatureStruct.Features.Count == 0 && otherState.Output[i].FeatureStruct.Disjunctions.Count == 1)
									set.UnionWith(otherState.Output[i].FeatureStruct.Disjunctions.First());
								else
									set.Add(otherState.Output[i].FeatureStruct);
								var fs = new FeatureStruct();
								fs.AddDisjunction(set);
								state.Output[i] = new Predicate(fs, state.Output[i].Identity || otherState.Output[i].Identity);
							}
						}
						nsis.RemoveAt(nsis.Count - 1);
					}

					bool noIdentities = true;
					for (int i = 0; i < state.Output.Count; i++)
					{
						if (state.Output[i].Identity)
							noIdentities = false;
						if (state.Output[i].FeatureStruct != null && !state.Output[i].FeatureStruct.IsFrozen)
							state.Output[i].FeatureStruct.Freeze();
						if (first)
							commonPrefix.Add(state.Output[i]);
						else if (i < commonPrefix.Count && !commonPrefix[i].Equals(state.Output[i]))
							commonPrefix.RemoveRange(i, commonPrefix.Count - i);
					}
					if (preprocessedCond.Item1.Identity && noIdentities)
						state.Output.Add(new Predicate(null, true));
					states.Add(state);
					first = false;
				}

				FeatureStruct condition;
				if (states.Count > 0 && preprocessedCond.Item1.FeatureStruct.CheckDisjunctiveConsistency(false, new VariableBindings(), out condition))
				{
					foreach (NfaStateInfo state in states)
						state.Output.RemoveRange(0, commonPrefix.Count);

					condition.Freeze();
					yield return Tuple.Create(new SubsetState(states), new Predicate(condition, preprocessedCond.Item1.Identity), (IEnumerable<Predicate>) commonPrefix);
				}
			}
		}

		private void Optimize(Func<SubsetState, IEnumerable<Tuple<SubsetState, Predicate, IEnumerable<Predicate>>>> arcsSelector)
		{
			var startState = new NfaStateInfo(StartState, Enumerable.Empty<Predicate>());
			var subsetStart = new SubsetState(startState.ToEnumerable());
			subsetStart = EpsilonClosure(subsetStart);

			StatesList.Clear();
			CreateOptimizedState(subsetStart);
			StartState = subsetStart.State;

			var subsetStates = new Dictionary<SubsetState, SubsetState> { {subsetStart, subsetStart} };
			var unmarkedSubsetStates = new Queue<SubsetState>();
			unmarkedSubsetStates.Enqueue(subsetStart);

			while (unmarkedSubsetStates.Count != 0)
			{
				SubsetState curSubsetState = unmarkedSubsetStates.Dequeue();

				foreach (Tuple<SubsetState, Predicate, IEnumerable<Predicate>> t in arcsSelector(curSubsetState))
				{
					SubsetState target = EpsilonClosure(t.Item1);
					// this makes the FSA not complete
					if (!target.IsEmpty)
					{
						SubsetState subsetState;
						if (subsetStates.TryGetValue(target, out subsetState))
						{
							target = subsetState;
						}
						else
						{
							subsetStates.Add(target, target);
							unmarkedSubsetStates.Enqueue(target);
							CreateOptimizedState(target);
						}

						curSubsetState.State.Arcs.Add(t.Item2, t.Item3, target.State);
					}
				}

				Arc<TData, TOffset, FstResult<TData, TOffset>>[] arcs = curSubsetState.State.Arcs.ToArray();
				for (int i = 0; i < arcs.Length; i++)
				{
				    for (int j = i + 1; j < arcs.Length; j++)
				    {
				        FeatureStruct fs;
				        if (arcs[i].Target.Equals(arcs[j].Target) && arcs[i].Input.Identity == arcs[j].Input.Identity && arcs[i].Input.FeatureStruct.Unify(arcs[j].Input.FeatureStruct, out fs))
				        {
				            arcs[j].Input = new Predicate(fs, arcs[i].Input.Identity);
				            curSubsetState.State.Arcs.Remove(arcs[i]);
				            break;
				        }
				    }
				}
			}
		}

		private void CreateOptimizedState(SubsetState subsetState)
		{
			List<Predicate> remaining = null;
			bool accepting = false;
			foreach (NfaStateInfo s in subsetState.NfaStates)
			{
				if (s.NfaState.IsAccepting)
				{
					if (s.Output.Count == 0)
						accepting = true;
					else
						remaining = s.Output;
					break;
				}
			}
			subsetState.State = accepting ? CreateAcceptingState() : CreateState();
			if (remaining != null)
				subsetState.State.Arcs.Add(null, remaining, CreateAcceptingState());
		}

		private static SubsetState EpsilonClosure(SubsetState from)
		{
			var stack = new Stack<NfaStateInfo>();
			var closure = new HashSet<NfaStateInfo>();
			foreach (NfaStateInfo state in from.NfaStates)
			{
				stack.Push(state);
				closure.Add(state);
			}

			while (stack.Count != 0)
			{
				NfaStateInfo topState = stack.Pop();

				foreach (Arc<TData, TOffset, FstResult<TData, TOffset>> arc in topState.NfaState.Arcs)
				{
					if (arc.Input == null)
					{
						var newState = new NfaStateInfo(arc.Target, arc.Outputs);

						closure.Add(newState);
						stack.Push(newState);
					}
				}
			}

			return new SubsetState(closure);
		}

		public FiniteStateTransducer<TData, TOffset> Compose(FiniteStateTransducer<TData, TOffset> other)
		{
			var newFst = new FiniteStateTransducer<TData, TOffset>(_dir);
			newFst.StartState = StartState.IsAccepting && other.StartState.IsAccepting ? newFst.CreateAcceptingState() : newFst.CreateState();

			var queue = new Queue<Tuple<State<TData, TOffset, FstResult<TData, TOffset>>, State<TData, TOffset, FstResult<TData, TOffset>>>>();
			var newStates = new Dictionary<Tuple<State<TData, TOffset, FstResult<TData, TOffset>>, State<TData, TOffset, FstResult<TData, TOffset>>>, State<TData, TOffset, FstResult<TData, TOffset>>>();
			Tuple<State<TData, TOffset, FstResult<TData, TOffset>>, State<TData, TOffset, FstResult<TData, TOffset>>> pair = Tuple.Create(StartState, other.StartState);
			queue.Enqueue(pair);
			newStates[pair] = newFst.StartState;
			while (queue.Count > 0)
			{
				Tuple<State<TData, TOffset, FstResult<TData, TOffset>>, State<TData, TOffset, FstResult<TData, TOffset>>> p = queue.Dequeue();
				State<TData, TOffset, FstResult<TData, TOffset>> s = newStates[p];

				var newArcs = new List<Tuple<State<TData, TOffset, FstResult<TData, TOffset>>, State<TData, TOffset, FstResult<TData, TOffset>>, Predicate, Predicate>>();
				foreach (Arc<TData, TOffset, FstResult<TData, TOffset>> arc1 in p.Item1.Arcs)
				{
					if (arc1.Outputs.Count == 0)
					{
						newArcs.Add(Tuple.Create(arc1.Target, p.Item2, arc1.Input, (Predicate) null));
					}
					else
					{
						foreach (Arc<TData, TOffset, FstResult<TData, TOffset>> arc2 in p.Item2.Arcs.Where(a => a.Input != null))
						{
							FeatureStruct compareFs;
							if (arc1.Input != null && arc1.Input.Identity)
							{
								compareFs = arc1.Input.FeatureStruct.DeepClone();
								compareFs.PriorityUnion(arc1.Outputs[0].FeatureStruct);
							}
							else
							{
								compareFs = arc1.Outputs[0].FeatureStruct;
							}

							if (arc2.Input.FeatureStruct.IsUnifiable(compareFs))
							{
								bool identity = arc1.Input.Identity && arc2.Input.Identity;
								var input = new Predicate(arc1.Input.FeatureStruct, identity);
								Predicate output;
								if (arc2.Input.Identity)
								{
									FeatureStruct fs;
									if (arc1.Outputs[0].FeatureStruct.Unify(arc2.Outputs[0].FeatureStruct, out fs))
										output = new Predicate(fs, identity);
									else
										continue;
								}
								else
								{
									output = arc2.Outputs.Count > 0 ? arc2.Outputs[0] : null;
								}

								newArcs.Add(Tuple.Create(arc1.Target, arc2.Target, input, output));
							}
						}
					}
				}

				//var newArcs = (from t1 in p.Item1.Arcs
				//               where t1.Output.Count == 0
				//               select new { Target = Tuple.Create(t1.Target, p.Item2), t1.Input, Output = (Predicate) null }
				//              ).Union(
				//              (from t2 in p.Item2.Arcs
				//               where t2.Input == null
				//               select new { Target = Tuple.Create(p.Item1, t2.Target), Input = (Predicate) null, Output = t2.Output.Count > 0 ? t2.Output[0] : null }
				//              ).Union(
				//               from t1 in p.Item1.Arcs
				//               where t1.Output.Count > 0
				//               from t2 in p.Item2.Arcs
				//               where t2.Input != null && t2.Input.FeatureStruct.IsUnifiable(t1.Output[0].FeatureStruct)
				//               select new { Target = Tuple.Create(t1.Target, t2.Target), t1.Input, Output = t2.Output.Count > 0 ? t2.Output[0] : null }
				//              ));

				foreach (Tuple<State<TData, TOffset, FstResult<TData, TOffset>>, State<TData, TOffset, FstResult<TData, TOffset>>, Predicate, Predicate> newArc in newArcs)
				{
					Tuple<State<TData, TOffset, FstResult<TData, TOffset>>, State<TData, TOffset, FstResult<TData, TOffset>>> key = Tuple.Create(newArc.Item1, newArc.Item2);
					State<TData, TOffset, FstResult<TData, TOffset>> r;
					if (!newStates.TryGetValue(key, out r))
					{
						if (newArc.Item1.IsAccepting && newArc.Item2.IsAccepting)
							r = newFst.CreateAcceptingState(newArc.Item1.AcceptInfos.Concat(newArc.Item2.AcceptInfos));
						else
							r = newFst.CreateState();
						queue.Enqueue(key);
						newStates[key] = r;
					}
					s.Arcs.Add(newArc.Item3, newArc.Item4 == null ? Enumerable.Empty<Predicate>() : newArc.Item4.ToEnumerable(), r);
				}
			}
			return newFst;
		}
	}
}
