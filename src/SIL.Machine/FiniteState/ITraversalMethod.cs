using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
    internal interface ITraversalMethod<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        IList<Annotation<TOffset>> Annotations { get; }
        void Reset(TData data, VariableBindings varBindings, bool startAnchor, bool endAnchor, bool useDefaults);
        List<FstResult<TData, TOffset>> Traverse(
            ref int annIndex,
            Register<TOffset>[,] initRegisters,
            IList<TagMapCommand> initCmds,
            ISet<int> initAnns
        );
    }
}
