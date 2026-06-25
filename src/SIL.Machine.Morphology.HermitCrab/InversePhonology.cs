using System.Collections.Generic;
using System.Linq;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// An inverse-phonology transducer (surface → underlying) for Lever 2 lazy composition
    /// (LEVER_2.md). States are ints; an arc carries a <b>surface input</b> feature structure
    /// (<c>null</c> = ε-input, i.e. a deletion <i>restoration</i> that consumes no surface) and an
    /// <b>underlying output</b> feature structure. <see cref="FstTemplateAnalyzer.AnalyzeComposed"/> walks
    /// this against the morphotactic acceptor as a product automaton: the underlying output must unify a
    /// lexicon arc, so a restoration only survives where the lexicon actually has that underlying segment
    /// — the constraint the runtime inverse lacked.
    ///
    /// This is the consuming end of Lever 2 (proven); building a <i>general</i> <see cref="InversePhonology"/>
    /// from a grammar's phonological rules (substitution + deletion + cascades) is Blocker 2's remaining
    /// compiler work.
    /// </summary>
    public sealed class InversePhonology
    {
        public readonly struct Arc
        {
            public Arc(FeatureStruct surfaceInput, FeatureStruct underlyingOutput, int target)
            {
                SurfaceInput = surfaceInput;
                UnderlyingOutput = underlyingOutput;
                Target = target;
            }

            /// <summary>The surface segment consumed, or <c>null</c> for an ε-input (restoration) arc.</summary>
            public FeatureStruct SurfaceInput { get; }

            /// <summary>The underlying segment emitted (matched against a lexicon arc).</summary>
            public FeatureStruct UnderlyingOutput { get; }

            public int Target { get; }

            public bool IsEpsilonInput => SurfaceInput == null;
        }

        private readonly Dictionary<int, List<Arc>> _arcs = new Dictionary<int, List<Arc>>();
        private readonly HashSet<int> _accepting = new HashSet<int>();

        public int StartState { get; set; }

        public void AddArc(int from, FeatureStruct surfaceInput, FeatureStruct underlyingOutput, int to)
        {
            if (!_arcs.TryGetValue(from, out List<Arc> list))
            {
                _arcs[from] = list = new List<Arc>();
            }
            list.Add(new Arc(surfaceInput, underlyingOutput, to));
        }

        public void SetAccepting(int state) => _accepting.Add(state);

        public bool IsAccepting(int state) => _accepting.Contains(state);

        public IReadOnlyList<Arc> ArcsFrom(int state) =>
            _arcs.TryGetValue(state, out List<Arc> list) ? list : (IReadOnlyList<Arc>)System.Array.Empty<Arc>();
    }
}
