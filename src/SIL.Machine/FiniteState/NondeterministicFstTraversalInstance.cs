using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;

namespace SIL.Machine.FiniteState
{
    internal class NondeterministicFstTraversalInstance<TData, TOffset> : TraversalInstance<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        private readonly HashSet<State<TData, TOffset>> _visited;
        private readonly Dictionary<Annotation<TOffset>, Annotation<TOffset>> _mappings;
        private readonly List<Output<TData, TOffset>> _outputs;

        public NondeterministicFstTraversalInstance(int registerCount) : base(registerCount, false)
        {
            _visited = new HashSet<State<TData, TOffset>>();
            _mappings = new Dictionary<Annotation<TOffset>, Annotation<TOffset>>();
            _outputs = new List<Output<TData, TOffset>>();
        }

        public ISet<State<TData, TOffset>> Visited
        {
            get { return _visited; }
        }

        public IDictionary<Annotation<TOffset>, Annotation<TOffset>> Mappings
        {
            get { return _mappings; }
        }

        public IList<Output<TData, TOffset>> Outputs
        {
            get { return _outputs; }
        }

        public override void CopyTo(TraversalInstance<TData, TOffset> other)
        {
            base.CopyTo(other);

            var otherNfst = (NondeterministicFstTraversalInstance<TData, TOffset>)other;

            otherNfst._visited.UnionWith(_visited);
            Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = Output.Annotations
                .SelectMany(a => a.GetNodesBreadthFirst())
                .Zip(Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst()))
                .ToDictionary(t => t.Item1, t => t.Item2);
            otherNfst._mappings.AddRange(
                _mappings.Select(
                    kvp =>
                        new KeyValuePair<Annotation<TOffset>, Annotation<TOffset>>(kvp.Key, outputMappings[kvp.Value])
                )
            );
            otherNfst._outputs.AddRange(_outputs);
        }

        public override void Clear()
        {
            base.Clear();
            _visited.Clear();
            _mappings.Clear();
            _outputs.Clear();
        }
    }
}
