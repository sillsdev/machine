using System;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	internal class DeterministicFsaTraversalMethod<TData, TOffset> : TraversalMethod<TData, TOffset> where TData : IData<TOffset>, IDeepCloneable<TData>
	{
		public DeterministicFsaTraversalMethod(Direction dir, Func<Annotation<TOffset>, bool> filter, State<TData, TOffset> startState, TData data, bool endAnchor, bool unification, bool useDefaults)
			: base(dir, filter, startState, data, endAnchor, unification, useDefaults)
		{
		}

		public override IEnumerable<FstResult<TData, TOffset>> Traverse(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<Annotation<TOffset>> initAnns)
		{
			Stack<Instance> instStack = InitializeStack(ref ann, initRegisters, initCmds, initAnns);

			var curResults = new List<FstResult<TData, TOffset>>(); 
			while (instStack.Count != 0)
			{
				Instance inst = instStack.Pop();

				if (inst.Annotation != null)
				{
					foreach (Arc<TData, TOffset> arc in inst.State.Arcs)
					{
						if (CheckInputMatch(arc, inst.Annotation, inst.VariableBindings))
						{
							foreach (Instance ni in AdvanceFsa(inst.Annotation, inst.Registers, inst.VariableBindings, arc, curResults, inst.Depth))
								instStack.Push(ni);
							break;
						}
					}
				}
			}

			return curResults;
		}

		private Stack<Instance> InitializeStack(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<Annotation<TOffset>> initAnns)
		{
			var instStack = new Stack<Instance>();
			foreach (Instance inst in Initialize(ref ann, registers, cmds, initAnns, 0, (state, startAnn, regs, vb, cd) => new Instance(state, startAnn, regs, vb, cd)))
				instStack.Push(inst);
			return instStack;
		}

		private IEnumerable<Instance> AdvanceFsa(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int depth)
		{
			return Advance(ann, registers, Data, varBindings, arc, curResults, depth, null,
				(state, nextAnn, regs, vb, cd, clone) => new Instance(state, nextAnn, regs, vb, cd));
		}
	}
}
