using System;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	internal class DeterministicFsaTraversalMethod<TData, TOffset> : TraversalMethod<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		public DeterministicFsaTraversalMethod(IEqualityComparer<NullableValue<TOffset>[,]> registersEqualityComparer, Direction dir, Func<Annotation<TOffset>, bool> filter,
			State<TData, TOffset> startState, TData data, bool endAnchor, bool unification, bool useDefaults, bool ignoreVariables)
			: base(registersEqualityComparer, dir, filter, startState, data, endAnchor, unification, useDefaults, ignoreVariables)
		{
		}

		public override IEnumerable<FstResult<TData, TOffset>> Traverse(ref int annIndex, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<int> initAnns)
		{
			Stack<Instance> instStack = InitializeStack(ref annIndex, initRegisters, initCmds, initAnns);

			var curResults = new List<FstResult<TData, TOffset>>(); 
			while (instStack.Count != 0)
			{
				Instance inst = instStack.Pop();

				foreach (Arc<TData, TOffset> arc in inst.State.Arcs)
				{
					if (CheckInputMatch(arc, inst.AnnotationIndex, inst.VariableBindings))
					{
						foreach (Instance ni in AdvanceFsa(inst.AnnotationIndex, inst.Registers, inst.VariableBindings, arc, curResults))
							instStack.Push(ni);
						break;
					}
				}
			}

			return curResults;
		}

		private static Instance CreateInstance()
		{
			return new Instance();
		}

		private Stack<Instance> InitializeStack(ref int annIndex, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<int> initAnns)
		{
			var instStack = new Stack<Instance>();
			foreach (Instance inst in Initialize(ref annIndex, registers, cmds, initAnns, CreateInstance))
				instStack.Push(inst);
			return instStack;
		}

		private IEnumerable<Instance> AdvanceFsa(int annIndex, NullableValue<TOffset>[,] registers, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults)
		{
			return Advance(annIndex, registers, default(TData), varBindings, arc, curResults, null, CreateInstance);
		}
	}
}
