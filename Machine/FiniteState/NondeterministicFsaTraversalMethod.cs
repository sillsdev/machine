using System;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	internal class NondeterministicFsaTraversalMethod<TData, TOffset> : TraversalMethod<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		public NondeterministicFsaTraversalMethod(IEqualityComparer<NullableValue<TOffset>[,]> registersEqualityComparer, Direction dir,
			Func<Annotation<TOffset>, bool> filter, State<TData, TOffset> startState, TData data, bool endAnchor, bool unification, bool useDefaults)
			: base(registersEqualityComparer, dir, filter, startState, data, endAnchor, unification, useDefaults)
		{
		}

		public override IEnumerable<FstResult<TData, TOffset>> Traverse(ref int annIndex, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<int> initAnns)
		{
			Stack<FsaInstance> instStack = InitializeStack(ref annIndex, initRegisters, initCmds, initAnns);

			var curResults = new List<FstResult<TData, TOffset>>();
			var traversed = new HashSet<Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,]>>(
				AnonymousEqualityComparer.Create<Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,]>>(KeyEquals, KeyGetHashCode));
			while (instStack.Count != 0)
			{
				FsaInstance inst = instStack.Pop();

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
							FsaInstance newInst = EpsilonAdvanceFsa(inst.AnnotationIndex, registers, varBindings, visited, arc, curResults, inst.Priorities);
							Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,]> key = Tuple.Create(newInst.State, newInst.AnnotationIndex, newInst.Registers);
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
						if (CheckInputMatch(arc, inst.AnnotationIndex, varBindings))
						{
							foreach (FsaInstance newInst in AdvanceFsa(inst.AnnotationIndex, inst.Registers, varBindings, arc,
								curResults, inst.Priorities))
							{
								Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,]> key = Tuple.Create(newInst.State, newInst.AnnotationIndex, newInst.Registers);
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

			return curResults;
		}

		private bool KeyEquals(Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,]> x, Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,]> y)
		{
			return x.Item1.Equals(y.Item1) && x.Item2.Equals(y.Item2) && RegistersEqualityComparer.Equals(x.Item3, y.Item3);
		}

		private int KeyGetHashCode(Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,]> m)
		{
			int code = 23;
			code = code * 31 + m.Item1.GetHashCode();
			code = code * 31 + m.Item2.GetHashCode();
			code = code * 31 + RegistersEqualityComparer.GetHashCode(m.Item3);
			return code;
		}

		private Stack<FsaInstance> InitializeStack(ref int annIndex, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<int> initAnns)
		{
			var instStack = new Stack<FsaInstance>();
			foreach (FsaInstance inst in Initialize(ref annIndex, registers, cmds, initAnns, (state, startIndex, regs, vb) => new FsaInstance(state, startIndex, regs, vb, new HashSet<State<TData, TOffset>>(), new int[0])))
				instStack.Push(inst);
			return instStack;
		}

		private IEnumerable<FsaInstance> AdvanceFsa(int annIndex, NullableValue<TOffset>[,] registers, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int[] priorities)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);
			return Advance(annIndex, registers, default(TData), varBindings, arc, curResults, priorities,
				(state, nextIndex, regs, vb, clone) => new FsaInstance(state, nextIndex, regs, vb, new HashSet<State<TData, TOffset>>(), priorities));
		}

		private FsaInstance EpsilonAdvanceFsa(int annIndex, NullableValue<TOffset>[,] registers, VariableBindings varBindings, ISet<State<TData, TOffset>> visited,
			Arc<TData, TOffset> arc, List<FstResult<TData, TOffset>> curResults, int[] priorities)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);
			visited.Add(arc.Target);
			return EpsilonAdvance(annIndex, registers, Data, varBindings, arc, curResults, priorities,
				(state, i, regs, vb) => new FsaInstance(state, i, regs, vb, visited, priorities));
		}

		private bool IsInstanceReuseable(FsaInstance inst)
		{
			return inst.State.Arcs.Count <= 1;
		}

		private class FsaInstance : Instance
		{
			private readonly ISet<State<TData, TOffset>> _visited;
			private readonly int[] _priorities; 

			public FsaInstance(State<TData, TOffset> state, int annotationIndex, NullableValue<TOffset>[,] registers, VariableBindings varBindings,
				ISet<State<TData, TOffset>> visited, int[] priorities)
				: base(state, annotationIndex, registers, varBindings)
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
