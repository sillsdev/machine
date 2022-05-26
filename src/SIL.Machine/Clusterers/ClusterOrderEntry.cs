namespace SIL.Machine.Clusterers
{
    public class ClusterOrderEntry<T>
    {
        private readonly T _dataObject;
        private readonly double _reachability;
        private readonly double _coreDistance;

        public ClusterOrderEntry(T dataObject, double reachability, double coreDistance)
        {
            _dataObject = dataObject;
            _reachability = reachability;
            _coreDistance = coreDistance;
        }

        public T DataObject
        {
            get { return _dataObject; }
        }

        public double Reachability
        {
            get { return _reachability; }
        }

        public double CoreDistance
        {
            get { return _coreDistance; }
        }
    }
}
