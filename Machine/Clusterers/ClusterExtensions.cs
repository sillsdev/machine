using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using QuickGraph;
using SIL.Collections;

namespace SIL.Machine.Clusterers
{
	public static class ClusterExtensions
	{
		public static IEnumerable<T> GetAllDataObjects<T>(this IBidirectionalGraph<Cluster<T>, ClusterEdge<T>> tree, Cluster<T> cluster)
		{
			if (tree.IsOutEdgesEmpty(cluster))
				return cluster.DataObjects;
			return tree.OutEdges(cluster).Aggregate((IEnumerable<T>) cluster.DataObjects, (res, edge) => res.Concat(tree.GetAllDataObjects(edge.Target)));
		}

		private static void GetMidpoint<T>(IUndirectedGraph<Cluster<T>, ClusterEdge<T>> tree, out ClusterEdge<T> midpointEdge, out double pointOnEdge, out Cluster<T> firstCluster)
		{
			Cluster<T> cluster1;
			IEnumerable<ClusterEdge<T>> path;
			GetLongestPath(tree, null, tree.Vertices.First(), 0, Enumerable.Empty<ClusterEdge<T>>(), out cluster1, out path);
			Cluster<T> cluster2;
			double deepestLen = GetLongestPath(tree, null, cluster1, 0, Enumerable.Empty<ClusterEdge<T>>(), out cluster2, out path);
			double midpoint = deepestLen / 2;

			firstCluster = cluster1;
			double totalLen = 0;
			midpointEdge = null;
			foreach (ClusterEdge<T> edge in path)
			{
				totalLen += edge.Length;
				if (totalLen >= midpoint)
				{
					midpointEdge = edge;
					break;
				}
				firstCluster = edge.GetOtherVertex(firstCluster);
			}
			Debug.Assert(midpointEdge != null);

			double diff = totalLen - midpoint;
			pointOnEdge = midpointEdge.Length - diff;
		}

		public static Cluster<T> GetCenter<T>(this IUndirectedGraph<Cluster<T>, ClusterEdge<T>> tree)
		{
			ClusterEdge<T> midpointEdge;
			double pointOnEdge;
			Cluster<T> firstCluster;
			GetMidpoint(tree, out midpointEdge, out pointOnEdge, out firstCluster);
			return pointOnEdge < midpointEdge.Length - pointOnEdge ? firstCluster : midpointEdge.GetOtherVertex(firstCluster);
		}

		public static IBidirectionalGraph<Cluster<T>, ClusterEdge<T>> ToRootedTree<T>(this IUndirectedGraph<Cluster<T>, ClusterEdge<T>> tree)
		{
			ClusterEdge<T> midpointEdge;
			double pointOnEdge;
			Cluster<T> firstCluster;
			GetMidpoint(tree, out midpointEdge, out pointOnEdge, out firstCluster);

			var rootedTree = new BidirectionalGraph<Cluster<T>, ClusterEdge<T>>(false);
			if (pointOnEdge < double.Epsilon)
			{
				rootedTree.AddVertex(firstCluster);
				GenerateRootedTree(tree, null, firstCluster, rootedTree);
			}
			else
			{
				var root = new Cluster<T>();
				rootedTree.AddVertex(root);
				Cluster<T> otherCluster = midpointEdge.GetOtherVertex(firstCluster);
				rootedTree.AddVertex(otherCluster);
				rootedTree.AddEdge(new ClusterEdge<T>(root, otherCluster, midpointEdge.Length - pointOnEdge));
				GenerateRootedTree(tree, firstCluster, otherCluster, rootedTree);
				rootedTree.AddVertex(firstCluster);
				rootedTree.AddEdge(new ClusterEdge<T>(root, firstCluster, pointOnEdge));
				GenerateRootedTree(tree, otherCluster, firstCluster, rootedTree);
			}
			return rootedTree;
		}

		private static void GenerateRootedTree<T>(IUndirectedGraph<Cluster<T>, ClusterEdge<T>> unrootedTree, Cluster<T> parent, Cluster<T> node, BidirectionalGraph<Cluster<T>, ClusterEdge<T>> rootedTree)
		{
			foreach (ClusterEdge<T> edge in unrootedTree.AdjacentEdges(node).Where(e => e.GetOtherVertex(node) != parent))
			{
				Cluster<T> otherCluster = edge.GetOtherVertex(node);
				rootedTree.AddVertex(otherCluster);
				rootedTree.AddEdge(new ClusterEdge<T>(node, otherCluster, edge.Length));
				GenerateRootedTree(unrootedTree, node, otherCluster, rootedTree);
			}
		}

		private static double GetLongestPath<T>(IUndirectedGraph<Cluster<T>, ClusterEdge<T>> tree, Cluster<T> parent, Cluster<T> node, double len, IEnumerable<ClusterEdge<T>> path,
			out Cluster<T> deepestNode, out IEnumerable<ClusterEdge<T>> deepestPath)
		{
			deepestNode = node;
			deepestPath = path;
			double maxDepth = 0;
			foreach (ClusterEdge<T> childEdge in tree.AdjacentEdges(node).Where(e => e.GetOtherVertex(node) != parent))
			{
				Cluster<T> cdn;
				IEnumerable<ClusterEdge<T>> cdp;
				double depth = GetLongestPath(tree, node, childEdge.GetOtherVertex(node), childEdge.Length, path.Concat(childEdge), out cdn, out cdp);
				if (depth >= maxDepth)
				{
					deepestNode = cdn;
					maxDepth = depth;
					deepestPath = cdp;
				}
			}
			return maxDepth + len;
		}
	}
}
