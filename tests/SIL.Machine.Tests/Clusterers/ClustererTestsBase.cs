using NUnit.Framework;
using QuickGraph;

namespace SIL.Machine.Clusterers;

[TestFixture]
public abstract class ClustererTestsBase
{
    protected void AssertTreeEqual<T>(
        IUndirectedGraph<Cluster<T>, ClusterEdge<T>> actual,
        IUndirectedGraph<Cluster<T>, ClusterEdge<T>> expected
    )
    {
        Assert.That(actual.Vertices, Is.EquivalentTo(expected.Vertices).Using(new ClusterEqualityComparer<T>()));
        Assert.That(
            actual.Edges,
            Is.EquivalentTo(expected.Edges).Using(new UndirectedClusterEdgeEqualityComparer<T>())
        );
    }

    protected void AssertTreeEqual<T>(
        IBidirectionalGraph<Cluster<T>, ClusterEdge<T>> actual,
        IBidirectionalGraph<Cluster<T>, ClusterEdge<T>> expected
    )
    {
        Assert.That(actual.Vertices, Is.EquivalentTo(expected.Vertices).Using(new ClusterEqualityComparer<T>()));
        Assert.That(actual.Edges, Is.EquivalentTo(expected.Edges).Using(new DirectedClusterEdgeEqualityComparer<T>()));
    }

    private class UndirectedClusterEdgeEqualityComparer<T> : IEqualityComparer<ClusterEdge<T>>
    {
        private readonly IEqualityComparer<Cluster<T>> _clusterComparer = new ClusterEqualityComparer<T>();

        public bool Equals(ClusterEdge<T>? x, ClusterEdge<T>? y)
        {
            if (x is null && y is null)
                return true;
            if (x is null || y is null)
                return false;

            return (
                    (_clusterComparer.Equals(x.Source, y.Source) && _clusterComparer.Equals(x.Target, y.Target))
                    || (_clusterComparer.Equals(x.Target, y.Source) && _clusterComparer.Equals(x.Source, y.Target))
                )
                && Math.Abs(x.Length - y.Length) < double.Epsilon;
        }

        public int GetHashCode(ClusterEdge<T> obj)
        {
            return _clusterComparer.GetHashCode(obj.Source)
                ^ _clusterComparer.GetHashCode(obj.Target)
                ^ obj.Length.GetHashCode();
        }
    }

    private class DirectedClusterEdgeEqualityComparer<T> : IEqualityComparer<ClusterEdge<T>>
    {
        private readonly IEqualityComparer<Cluster<T>> _clusterComparer = new ClusterEqualityComparer<T>();

        public bool Equals(ClusterEdge<T>? x, ClusterEdge<T>? y)
        {
            if (x is null && y is null)
                return true;
            if (x is null || y is null)
                return false;

            return _clusterComparer.Equals(x.Source, y.Source)
                && _clusterComparer.Equals(x.Target, y.Target)
                && Math.Abs(x.Length - y.Length) < double.Epsilon;
        }

        public int GetHashCode(ClusterEdge<T> obj)
        {
            int code = 23;
            code = code * 31 + _clusterComparer.GetHashCode(obj.Source);
            code = code * 31 + _clusterComparer.GetHashCode(obj.Target);
            code = code * 31 + obj.Length.GetHashCode();
            return code;
        }
    }

    protected class ClusterEqualityComparer<T> : IEqualityComparer<Cluster<T>>
    {
        public bool Equals(Cluster<T>? x, Cluster<T>? y)
        {
            if (x is null && y is null)
                return true;
            if (x is null || y is null)
                return false;

            return x.DataObjects.SetEquals(y.DataObjects) && x.Noise == y.Noise;
        }

        public int GetHashCode(Cluster<T> obj)
        {
            int code = 23;
            code = code * 31 + obj.DataObjects.Aggregate(0, (c, o) => c ^ o?.GetHashCode() ?? 0);
            code = code * 31 + obj.Noise.GetHashCode();
            return code;
        }
    }
}
