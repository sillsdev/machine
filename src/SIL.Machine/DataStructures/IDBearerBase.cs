namespace SIL.Machine.DataStructures
{
    public abstract class IDBearerBase : IIDBearer
    {
        private readonly string _id;

        protected IDBearerBase(string id)
        {
            _id = id;
            Description = id;
        }

        public string ID
        {
            get { return _id; }
        }

        public string Description { get; set; }

        public override string ToString()
        {
            return Description;
        }

        // Equals() is intentionally left as the default (reference equality) — this override only
        // makes GetHashCode() cheap. Without it, every derived type (Feature and its subclasses,
        // symbols, etc.) falls back to the CLR's identity hash, which a CPU profile showed
        // contributing real self-time when these objects are used as Dictionary/HashSet keys (e.g.
        // FeatureStruct._definite's Dictionary<Feature,FeatureValue>, rebuilt on every unify output).
        // _id is immutable and set once at construction, so hashing on it changes nothing about
        // which objects compare equal: two objects Equal by the untouched reference-equality Equals
        // are the same instance, hence share the same _id, hence the same hash — the
        // Equals/GetHashCode contract holds trivially.
        public override int GetHashCode()
        {
            return _id == null ? 0 : _id.GetHashCode();
        }
    }
}
