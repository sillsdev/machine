using System.Collections.Generic;

namespace SIL.Machine.Clusterers
{
    public interface IFlatClusterer<T>
    {
        IEnumerable<Cluster<T>> GenerateClusters(IEnumerable<T> dataObjects);
    }
}
