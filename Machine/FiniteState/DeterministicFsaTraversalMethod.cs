using System;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Annotations;

namespace SIL.Machine.FiniteState
{
	internal class DeterministicFsaTraversalMethod<TData, TOffset> : TraversalMethodBase<TData, TOffset, DeterministicFsaTraversalInstance<TData, TOffset>> where TData : IAnnotatedData<TOffset>
	{
		public DeterministicFsaTraversalMethod(IEqualityComparer<NullableValue<TOffset>[,]> registersEqualityComparer, int registerCount, Direction dir, Func<Annotation<TOffset>, bool> filter,
			State<TData, TOffset> startState, TData data, bool endAnchor, bool unification, bool useDefaults, bool ignoreVariables)
			: base(registersEqualityComparer, registerCount, dir, filter, startState, data, endAnchor, unification, useDefaults, ignoreVariables)
		{
		}

		public override IEnumerable<FstResult<TData, TOffset>> Traverse(ref int annIndex, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<int> initAnns)
		{
			Stack<DeterministicFsaTraversalInstance<TData, TOffset>> instStack = InitializeStack(ref annIndex, initRegisters, initCmds, initAnns);

			var curResults = new List<FstResult<TData, TOffset>>(); 
			while (instStack.Count != 0)
			{
				DeterministicFsaTraversalInstance<TData, TOffset> inst = instStack.Pop();

				bool releaseInstance = true;
				foreach (Arc<TData, TOffset> arc in inst.State.Arcs)
				{
					if (CheckInputMatch(arc, inst.AnnotationIndex, inst.VariableBindings))
					{
						foreach (DeterministicFsaTraversalInstance<TData, TOffset> ni in Advance(inst, inst.VariableBindings, arc, curResults))
							instStack.Push(ni);
						releaseInstance = false;
						break;
					}
				}

				if (releaseInstance)
					ReleaseInstance(inst);
			}

			return curResults;
		}

		protected override DeterministicFsaTraversalInstance<TData, TOffset> CreateInstance(int registerCount, bool ignoreVariables)
		{
			return new DeterministicFsaTraversalInstance<TData, TOffset>(registerCount, ignoreVariables);
		}

		private Stack<DeterministicFsaTraversalInstance<TData, TOffset>> InitializeStack(ref int annIndex, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<int> initAnns)
		{
			var instStack = new Stack<DeterministicFsaTraversalInstance<TData, TOffset>>();
			foreach (DeterministicFsaTraversalInstance<TData, TOffset> inst in Initialize(ref annIndex, registers, cmds, initAnns))
				instStack.Push(inst);
			return instStack;
		}
	}
}
