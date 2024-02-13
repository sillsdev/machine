using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;

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
            var outputMappings = Output
                .Annotations.SelectMany(a => a.GetNodesBreadthFirst())
                .Zip(Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst()))
                .ToDictionary(t => t.Item1, t => t.Item2);
            otherDfst.Mappings.AddRange(
                _mappings.Select(kvp => new KeyValuePair<Annotation<TOffset>, Annotation<TOffset>>(
                    kvp.Key,
                    outputMappings[kvp.Value]
                ))
            );
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
