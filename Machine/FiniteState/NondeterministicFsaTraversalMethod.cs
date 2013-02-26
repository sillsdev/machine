using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	internal class NondeterministicFsaTraversalMethod<TData, TOffset> : TraversalMethod<TData, TOffset> where TData : IData<TOffset>, IDeepCloneable<TData>
	{
		private readonly RegistersEqualityComparer<TOffset> _registersComparer;

		public NondeterministicFsaTraversalMethod(RegistersEqualityComparer<TOffset> registersComparer, Direction dir, Func<Annotation<TOffset>, bool> filter, State<TData, TOffset> startState, TData data,
			bool endAnchor, bool unification, bool useDefaults)
			: base(dir, filter, startState, data, endAnchor, unification, useDefaults)
		{
			_registersComparer = registersComparer;
		}

		public override IEnumerable<FstResult<TData, TOffset>> Traverse(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<Annotation<TOffset>> initAnns)
		{
			ConcurrentQueue<FsaInstance> from = InitializeQueue(ref ann, initRegisters, initCmds, initAnns);

			var curResults = new ConcurrentBag<FstResult<TData, TOffset>>();
			var traversed = new HashSet<Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,]>>(
				AnonymousEqualityComparer.Create<Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,]>>(KeyEquals, KeyGetHashCode));
			while (!from.IsEmpty)
			{
				var to = new ConcurrentQueue<FsaInstance>();
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

										if (varBindings == null)
											varBindings = inst.VariableBindings.DeepClone();
										visited = new HashSet<State<TData, TOffset>>(inst.Visited);
									}
									to.Enqueue(EpsilonAdvanceFsa(inst.Annotation, registers, varBindings, visited, arc, taskResults,
										inst.Depth, inst.Priorities));
									varBindings = null;
								}
							}
							else
							{
								if (varBindings == null)
									varBindings = IsInstanceReuseable(inst) ? inst.VariableBindings : inst.VariableBindings.DeepClone();
								if (CheckInputMatch(arc, inst.Annotation, varBindings))
								{
									foreach (FsaInstance newInst in AdvanceFsa(inst.Annotation, inst.Registers, varBindings, arc,
										taskResults, inst.Depth, inst.Priorities))
									{
										to.Enqueue(newInst);
									}
									varBindings = null;
								}
							}
						}

						foreach (FstResult<TData, TOffset> res in taskResults)
							curResults.Add(res);
					});
				from = new ConcurrentQueue<FsaInstance>();
				foreach (FsaInstance inst in to)
				{
					Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,]> key = Tuple.Create(inst.State, inst.Annotation, inst.Registers);
					if (!traversed.Contains(key))
					{
						from.Enqueue(inst);
						traversed.Add(key);
					}
				}
			}

			return curResults;
		}

		private bool KeyEquals(Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,]> x, Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,]> y)
		{
			return x.Item1.Equals(y.Item1) && x.Item2.Equals(y.Item2) && _registersComparer.Equals(x.Item3, y.Item3);
		}

		private int KeyGetHashCode(Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,]> m)
		{
			int code = 23;
			code = code * 31 + m.Item1.GetHashCode();
			code = code * 31 + m.Item2.GetHashCode();
			code = code * 31 + _registersComparer.GetHashCode(m.Item3);
			return code;
		}

		private ConcurrentQueue<FsaInstance> InitializeQueue(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<Annotation<TOffset>> initAnns)
		{
			var from = new ConcurrentQueue<FsaInstance>();
			foreach (FsaInstance inst in Initialize(ref ann, registers, cmds, initAnns, 0,
				(state, startAnn, regs, vb, cd) => new FsaInstance(state, startAnn, regs, vb, new HashSet<State<TData, TOffset>>(), cd, new int[0])))
			{
				from.Enqueue(inst);
			}
			return from;
		}

		private IEnumerable<FsaInstance> AdvanceFsa(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int depth, int[] priorities)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);
			return Advance(ann, registers, Data, varBindings, arc, curResults, depth, priorities,
				(state, nextAnn, regs, vb, cd, clone) => new FsaInstance(state, nextAnn, regs, vb, new HashSet<State<TData, TOffset>>(), cd, priorities));
		}

		private FsaInstance EpsilonAdvanceFsa(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, VariableBindings varBindings, ISet<State<TData, TOffset>> visited,
			Arc<TData, TOffset> arc, List<FstResult<TData, TOffset>> curResults, int depth, int[] priorities)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);
			visited.Add(arc.Target);
			return EpsilonAdvance(ann, registers, Data, varBindings, arc, curResults, depth, priorities,
				(state, a, regs, vb, cd) => new FsaInstance(state, a, regs, vb, visited, cd, priorities));
		}

		private bool IsInstanceReuseable(FsaInstance inst)
		{
			return inst.State.Arcs.Count <= 1;
		}

		private class FsaInstance : Instance
		{
			private readonly ISet<State<TData, TOffset>> _visited;
			private readonly int[] _priorities; 

			public FsaInstance(State<TData, TOffset> state, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, VariableBindings varBindings,
				ISet<State<TData, TOffset>> visited, int depth, int[] priorities)
				: base(state, ann, registers, varBindings, depth)
			{
				_visited = visited;
				_priorities = priorities;
			}

			public ISet<State<TData, TOffset>> Visited
			{
				get { return _visited; }
			}

			public int[] Priorities
			{
				get { return _priorities; }
			}
		}
	}
}
