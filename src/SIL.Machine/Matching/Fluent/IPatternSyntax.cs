using System;
using SIL.Machine.Annotations;

namespace SIL.Machine.Matching.Fluent
{
    public interface IPatternSyntax<TData, TOffset> : INodesPatternSyntax<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        INodesPatternSyntax<TData, TOffset> MatchAcceptableWhere(Func<Match<TData, TOffset>, bool> acceptable);
    }
}
