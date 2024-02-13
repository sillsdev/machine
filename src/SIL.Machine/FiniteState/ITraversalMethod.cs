using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.FiniteState
{
    internal interface ITraversalMethod<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        IList<Annotation<TOffset>> Annotations { get; }
        IEnumerable<FstResult<TData, TOffset>> Traverse(
            ref int annIndex,
            Register<TOffset>[,] initRegisters,
            IList<TagMapCommand> initCommands,
            ISet<int> initAnnotations
        );
    }
}
