using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Rules
{
    public interface IRule<TData, TOffset> where TData : IAnnotatedData<TOffset>
    {
        IEnumerable<TData> Apply(TData input);
    }
}
