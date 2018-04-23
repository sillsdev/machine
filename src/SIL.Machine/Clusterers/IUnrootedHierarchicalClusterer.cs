using System.Collections.Generic;
using QuickGraph;

namespace SIL.Machine.Clusterers
{
	public interface IUnrootedHierarchicalClusterer<T>
	{
		IUndirectedGraph<Cluster<T>, ClusterEdge<T>> GenerateClusters(IEnumerable<T> dataObjects);
	}
}
