using SIL.Machine.Annotations;

namespace SIL.Machine.Matching.Fluent
{
    public interface IAlternationPatternSyntax<TData, TOffset> : INodesPatternSyntax<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        IInitialNodesPatternSyntax<TData, TOffset> Or { get; }
    }
}
