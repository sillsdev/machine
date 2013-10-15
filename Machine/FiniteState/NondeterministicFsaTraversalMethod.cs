using System;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	internal class NondeterministicFsaTraversalMethod<TData, TOffset> : TraversalMethod<TData, TOffset> where TData : IData<TOffset>
	{
		public NondeterministicFsaTraversalMethod(IEqualityComparer<NullableValue<TOffset>[,]> registersEqualityComparer, Direction dir, Func<Annotation<TOffset>, bool> filter, State<TData, TOffset> startState,
			TData data, bool endAnchor, bool unification, bool useDefaults)
			: base(registersEqualityComparer, dir, filter, startState, data, endAnchor, unification, useDefaults)
		{
		}

		public override IEnumerable<FstResult<TData, TOffset>> Traverse(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<Annotation<TOffset>> initAnns)
		{
			Stack<FsaInstance> instStack = InitializeStack(ref ann, initRegisters, initCmds, initAnns);

			var curResults = new List<FstResult<TData, TOffset>>();
			var traversed = new HashSet<Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,]>>(
				AnonymousEqualityComparer.Create<Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,]>>(KeyEquals, KeyGetHashCode));
			while (instStack.Count != 0)
			{
				FsaInstance inst = instStack.Pop();

				if (inst.Annotation != null)
				{
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
								FsaInstance newInst = EpsilonAdvanceFsa(inst.Annotation, registers, varBindings, visited, arc, curResults, inst.Priorities);
								Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,]> key = Tuple.Create(newInst.State, newInst.Annotation, newInst.Registers);
								if (!traversed.Contains(key))
								{
									instStack.Push(newInst);
									traversed.Add(key);
								}
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
									curResults, inst.Priorities))
								{
									Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,]> key = Tuple.Create(newInst.State, newInst.Annotation, newInst.Registers);
									if (!traversed.Contains(key))
									{
										instStack.Push(newInst);
										traversed.Add(key);
									}
								}
								varBindings = null;
							}
						}
					}
				}
			}

			return curResults;
		}

		private bool KeyEquals(Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,]> x, Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,]> y)
		{
			return x.Item1.Equals(y.Item1) && x.Item2.Equals(y.Item2) && RegistersEqualityComparer.Equals(x.Item3, y.Item3);
		}

		private int KeyGetHashCode(Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,]> m)
		{
			int code = 23;
			code = code * 31 + m.Item1.GetHashCode();
			code = code * 31 + m.Item2.GetHashCode();
			code = code * 31 + RegistersEqualityComparer.GetHashCode(m.Item3);
			return code;
		}

		private Stack<FsaInstance> InitializeStack(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<Annotation<TOffset>> initAnns)
		{
			var instStack = new Stack<FsaInstance>();
			foreach (FsaInstance inst in Initialize(ref ann, registers, cmds, initAnns, (state, startAnn, regs, vb) => new FsaInstance(state, startAnn, regs, vb, new HashSet<State<TData, TOffset>>(), new int[0])))
				instStack.Push(inst);
			return instStack;
		}

		private IEnumerable<FsaInstance> AdvanceFsa(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int[] priorities)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);
			return Advance(ann, registers, default(TData), varBindings, arc, curResults, priorities,
				(state, nextAnn, regs, vb, clone) => new FsaInstance(state, nextAnn, regs, vb, new HashSet<State<TData, TOffset>>(), priorities));
		}

		private FsaInstance EpsilonAdvanceFsa(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, VariableBindings varBindings, ISet<State<TData, TOffset>> visited,
			Arc<TData, TOffset> arc, List<FstResult<TData, TOffset>> curResults, int[] priorities)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);
			visited.Add(arc.Target);
			return EpsilonAdvance(ann, registers, Data, varBindings, arc, curResults, priorities,
				(state, a, regs, vb) => new FsaInstance(state, a, regs, vb, visited, priorities));
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
				ISet<State<TData, TOffset>> visited, int[] priorities)
				: base(state, ann, registers, varBindings)
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
