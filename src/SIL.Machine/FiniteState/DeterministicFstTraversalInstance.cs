using System.Collections.Generic;
using SIL.Extensions;
using SIL.Machine.Annotations;

namespace SIL.Machine.FiniteState
{
    internal class DeterministicFstTraversalInstance<TData, TOffset> : TraversalInstance<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        private readonly Dictionary<Annotation<TOffset>, Annotation<TOffset>> _mappings;
        private readonly Queue<Annotation<TOffset>> _queue;

        public DeterministicFstTraversalInstance(int registerCount)
            : base(registerCount, true)
        {
            _mappings = new Dictionary<Annotation<TOffset>, Annotation<TOffset>>();
            _queue = new Queue<Annotation<TOffset>>();
        }

        public IDictionary<Annotation<TOffset>, Annotation<TOffset>> Mappings
        {
            get { return _mappings; }
        }

        public Queue<Annotation<TOffset>> Queue
        {
            get { return _queue; }
        }

        public override void CopyTo(TraversalInstance<TData, TOffset> other)
        {
            base.CopyTo(other);

            var otherDfst = (DeterministicFstTraversalInstance<TData, TOffset>)other;
            // Identity map: the original zipped this.Output's node sequence with itself, so
            // outputMappings[v] == v and the block reduces to copying _mappings unchanged.
            // Avoids a Dictionary + two SelectMany(BFS) + Zip + Select per instance copy.
            // Byte-identical; otherDfst.Mappings is empty here (GetCachedInstance -> Clear()).
            otherDfst.Mappings.AddRange(_mappings);
            foreach (Annotation<TOffset> ann in _queue)
                otherDfst.Queue.Enqueue(ann);
        }

        public override void Clear()
        {
            base.Clear();
            _mappings.Clear();
            _queue.Clear();
        }
    }
}
