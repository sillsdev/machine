using SIL.Machine.Annotations;

namespace SIL.Machine.Matching.Fluent
{
    public interface IAlternationGroupSyntax<TData, TOffset> : IGroupSyntax<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        IGroupSyntax<TData, TOffset> Or { get; }
    }
}
