using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuickGraph;
using SIL.Machine.Clusterers;

namespace SIL.Machine.Tests.Clusterers
{
	public class NeighborJoiningClustererTests : ClustererTestsBase
	{
		[Test]
		public void Cluster()
		{
			var matrix = new double[,]
				{
					{0, 1, 2, 3, 3},
					{1, 0, 2, 3, 3},
					{2, 2, 0, 3, 3},
					{3, 3, 3, 0, 1},
					{3, 3, 3, 1, 0}
				};
			var nj = new NeighborJoiningClusterer<char>((o1, o2) => matrix[o1 - 'A', o2 - 'A']);
			IUndirectedGraph<Cluster<char>, ClusterEdge<char>> tree = nj.GenerateClusters(new[] {'A', 'B', 'C', 'D', 'E'});

			var vertices = new Dictionary<string, Cluster<char>> 
				{
					{"root", new Cluster<char> {Description = "root"}},
					{"A", new Cluster<char>('A') {Description = "A"}},
					{"B", new Cluster<char>('B') {Description = "B"}},
					{"C", new Cluster<char>('C') {Description = "C"}},
					{"D", new Cluster<char>('D') {Description = "D"}},
					{"E", new Cluster<char>('E') {Description = "E"}},
					{"DE", new Cluster<char> {Description = "DE"}},
					{"AB", new Cluster<char> {Description = "AB"}}
				};

			var edges = new[]
				{
					new ClusterEdge<char>(vertices["root"], vertices["C"], 1.0),
					new ClusterEdge<char>(vertices["root"], vertices["DE"], 1.5),
					new ClusterEdge<char>(vertices["root"], vertices["AB"], 0.5),
					new ClusterEdge<char>(vertices["DE"], vertices["D"], 0.5),
					new ClusterEdge<char>(vertices["DE"], vertices["E"], 0.5),
					new ClusterEdge<char>(vertices["AB"], vertices["A"], 0.5),
					new ClusterEdge<char>(vertices["AB"], vertices["B"], 0.5)
				};

			AssertTreeEqual(tree, edges.ToUndirectedGraph<Cluster<char>, ClusterEdge<char>>(false));
		}

		[Test]
		public void ClusterNoDataObjects()
		{
			var nj = new NeighborJoiningClusterer<char>((o1, o2) => 0);
			IUndirectedGraph<Cluster<char>, ClusterEdge<char>> tree = nj.GenerateClusters(Enumerable.Empty<char>());
			Assert.That(tree.IsEdgesEmpty);
		}

		[Test]
		public void ClusterOneDataObject()
		{
			var nj = new NeighborJoiningClusterer<char>((o1, o2) => 0);
			IUndirectedGraph<Cluster<char>, ClusterEdge<char>> tree = nj.GenerateClusters(new[] {'A'});
			Assert.That(tree.VertexCount, Is.EqualTo(1));
			Assert.That(tree.IsEdgesEmpty);
		}

		[Test]
		public void ClusterTwoDataObjects()
		{
			var nj = new NeighborJoiningClusterer<char>((o1, o2) => 1);
			IUndirectedGraph<Cluster<char>, ClusterEdge<char>> tree = nj.GenerateClusters(new[] {'A', 'B'});

			var edges = new[]
				{
					new ClusterEdge<char>(new Cluster<char>('A'), new Cluster<char>('B'), 1.0)
				};

			AssertTreeEqual(tree, edges.ToUndirectedGraph<Cluster<char>, ClusterEdge<char>>());
		}
	}
}
