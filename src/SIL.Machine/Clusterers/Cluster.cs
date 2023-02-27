using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Clusterers
{
    public class Cluster<T>
    {
        private readonly ReadOnlySet<T> _dataObjects;
        private readonly bool _noise;

        public Cluster(params T[] dataObjects)
            : this(dataObjects, false) { }

        public Cluster(IEnumerable<T> dataObjects)
            : this(dataObjects, false) { }

        public Cluster(IEnumerable<T> dataObjects, bool noise)
        {
            _dataObjects = new ReadOnlySet<T>(new HashSet<T>(dataObjects));
            _noise = noise;
        }

        public string Description { get; set; }

        public IReadOnlySet<T> DataObjects
        {
            get { return _dataObjects; }
        }

        public bool Noise
        {
            get { return _noise; }
        }

        public override string ToString()
        {
            return Description;
        }
    }
}
