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
			Stack<NondeterministicFsaInstance> instStack = InitializeStack(ref annIndex, initRegisters, initCmds, initAnns);

			var curResults = new List<FstResult<TData, TOffset>>();
			var traversed = new HashSet<Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,]>>(
				AnonymousEqualityComparer.Create<Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,]>>(KeyEquals, KeyGetHashCode));
			while (instStack.Count != 0)
			{
				NondeterministicFsaInstance inst = instStack.Pop();

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
							NondeterministicFsaInstance newInst = EpsilonAdvanceFsa(inst.AnnotationIndex, registers, varBindings, visited, arc, curResults, inst.Priorities);
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
							foreach (NondeterministicFsaInstance newInst in AdvanceFsa(inst.AnnotationIndex, inst.Registers, varBindings, arc,
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

		private static NondeterministicFsaInstance CreateInstance()
		{
			return new NondeterministicFsaInstance();
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

		private Stack<NondeterministicFsaInstance> InitializeStack(ref int annIndex, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<int> initAnns)
		{
			var instStack = new Stack<NondeterministicFsaInstance>();
			foreach (NondeterministicFsaInstance inst in Initialize(ref annIndex, registers, cmds, initAnns, CreateInstance))
			{
				inst.Visited = new HashSet<State<TData, TOffset>>();
				inst.Priorities = new int[0];
				instStack.Push(inst);
			}
			return instStack;
		}

		private IEnumerable<NondeterministicFsaInstance> AdvanceFsa(int annIndex, NullableValue<TOffset>[,] registers, VariableBindings varBindings,
			Arc<TData, TOffset> arc, List<FstResult<TData, TOffset>> curResults, int[] priorities)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);
			foreach (NondeterministicFsaInstance inst in Advance(annIndex, registers, default(TData), varBindings, arc, curResults, priorities, CreateInstance))
			{
				inst.Visited = new HashSet<State<TData, TOffset>>();
				inst.Priorities = priorities;
				yield return inst;
			}
		}

		private NondeterministicFsaInstance EpsilonAdvanceFsa(int annIndex, NullableValue<TOffset>[,] registers, VariableBindings varBindings,
			ISet<State<TData, TOffset>> visited, Arc<TData, TOffset> arc, List<FstResult<TData, TOffset>> curResults, int[] priorities)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);
			visited.Add(arc.Target);
			NondeterministicFsaInstance inst = EpsilonAdvance(annIndex, registers, Data, varBindings, arc, curResults, priorities, CreateInstance);
			inst.Visited = visited;
			inst.Priorities = priorities;
			return inst;
		}

		private bool IsInstanceReuseable(NondeterministicFsaInstance inst)
		{
			return inst.State.Arcs.Count <= 1;
		}

		private class NondeterministicFsaInstance : Instance
		{
			public ISet<State<TData, TOffset>> Visited { get; set; }
			public int[] Priorities { get; set; }
		}
	}
}
