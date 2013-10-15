using System.Collections.Generic;
using QuickGraph;

namespace SIL.Machine.Clusterers
{
	public interface IRootedHierarchicalClusterer<T>
	{
		IBidirectionalGraph<Cluster<T>, ClusterEdge<T>> GenerateClusters(IEnumerable<T> dataObjects);
	}
}
