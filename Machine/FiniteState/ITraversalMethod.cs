using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Annotations;

namespace SIL.Machine.FiniteState
{
	internal interface ITraversalMethod<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		IList<Annotation<TOffset>> Annotations { get; }
		IEnumerable<FstResult<TData, TOffset>> Traverse(ref int annIndex, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<int> initAnns);
	}
}
