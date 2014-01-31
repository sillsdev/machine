using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuickGraph;
using SIL.Machine.Clusterers;

namespace SIL.Machine.Test.Clusterers
{
	public class UpgmaClustererTest : ClustererTestBase
	{
		[Test]
		public void Cluster()
		{
			var matrix = new double[,]
				{
					{0, 2, 4, 6, 6, 8},
					{2, 0, 4, 6, 6, 8},
					{4, 4, 0, 6, 6, 8},
					{6, 6, 6, 0, 4, 8},
					{6, 6, 6, 4, 0, 8},
					{8, 8, 8, 8, 8, 0}
				};
			var upgma = new UpgmaClusterer<char>((o1, o2) => matrix[o1 - 'A', o2 - 'A']);
			IBidirectionalGraph<Cluster<char>, ClusterEdge<char>> tree = upgma.GenerateClusters(new[] {'A', 'B', 'C', 'D', 'E', 'F'});

			var vertices = new Dictionary<string, Cluster<char>> 
				{
					{"root", new Cluster<char> {Description = "root"}},
					{"A", new Cluster<char>('A') {Description = "A"}},
					{"B", new Cluster<char>('B') {Description = "B"}},
					{"C", new Cluster<char>('C') {Description = "C"}},
					{"D", new Cluster<char>('D') {Description = "D"}},
					{"E", new Cluster<char>('E') {Description = "E"}},
					{"F", new Cluster<char>('F') {Description = "F"}},
					{"ABCDE", new Cluster<char> {Description = "ABCDE"}},
					{"ABC", new Cluster<char> {Description = "ABC"}},
					{"AB", new Cluster<char> {Description = "AB"}},
					{"DE", new Cluster<char> {Description = "DE"}}
				};

			var edges = new[]
				{
					new ClusterEdge<char>(vertices["root"], vertices["ABCDE"], 1),
					new ClusterEdge<char>(vertices["root"], vertices["F"], 4),
					new ClusterEdge<char>(vertices["ABCDE"], vertices["ABC"], 1),
					new ClusterEdge<char>(vertices["ABCDE"], vertices["DE"], 1),
					new ClusterEdge<char>(vertices["ABC"], vertices["AB"], 1),
					new ClusterEdge<char>(vertices["ABC"], vertices["C"], 2),
					new ClusterEdge<char>(vertices["AB"], vertices["A"], 1),
					new ClusterEdge<char>(vertices["AB"], vertices["B"], 1),
					new ClusterEdge<char>(vertices["DE"], vertices["D"], 2),
					new ClusterEdge<char>(vertices["DE"], vertices["E"], 2)
				};
			AssertTreeEqual(tree, edges.ToBidirectionalGraph<Cluster<char>, ClusterEdge<char>>());
		}

		[Test]
		public void ClusterNoDataObjects()
		{
			var upgma = new UpgmaClusterer<char>((o1, o2) => 0);
			IBidirectionalGraph<Cluster<char>, ClusterEdge<char>> tree = upgma.GenerateClusters(Enumerable.Empty<char>());
			Assert.That(tree.IsEdgesEmpty);
		}

		[Test]
		public void ClusterOneDataObject()
		{
			var upgma = new UpgmaClusterer<char>((o1, o2) => 0);
			IBidirectionalGraph<Cluster<char>, ClusterEdge<char>> tree = upgma.GenerateClusters(new[] {'A'});
			Assert.That(tree.VertexCount, Is.EqualTo(1));
			Assert.That(tree.IsEdgesEmpty);
		}

		[Test]
		public void ClusterTwoDataObjects()
		{
			var upgma = new UpgmaClusterer<char>((o1, o2) => 1);
			IBidirectionalGraph<Cluster<char>, ClusterEdge<char>> tree = upgma.GenerateClusters(new[] {'A', 'B'});

			var vertices = new Dictionary<string, Cluster<char>> 
				{
					{"root", new Cluster<char> {Description = "root"}},
					{"A", new Cluster<char>('A') {Description = "A"}},
					{"B", new Cluster<char>('B') {Description = "B"}}
				};

			var edges = new[]
				{
					new ClusterEdge<char>(vertices["root"], vertices["A"], 0.5),
					new ClusterEdge<char>(vertices["root"], vertices["B"], 0.5)
				};

			AssertTreeEqual(tree, edges.ToBidirectionalGraph<Cluster<char>, ClusterEdge<char>>());
		}
	}
}
