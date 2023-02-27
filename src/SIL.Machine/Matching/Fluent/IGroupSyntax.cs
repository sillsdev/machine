using System;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Matching.Fluent
{
    public interface IGroupSyntax<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        IQuantifierGroupSyntax<TData, TOffset> Group(
            string name,
            Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build
        );
        IQuantifierGroupSyntax<TData, TOffset> Group(
            Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build
        );

        IQuantifierGroupSyntax<TData, TOffset> Annotation(FeatureStruct fs);

        Group<TData, TOffset> Value { get; }
    }
}
