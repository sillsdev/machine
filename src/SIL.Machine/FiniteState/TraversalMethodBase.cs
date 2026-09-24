using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.FiniteState
{
    internal abstract class TraversalMethodBase<TData, TOffset, TInst> : ITraversalMethod<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
        where TInst : TraversalInstance<TData, TOffset>
    {
        private readonly Fst<TData, TOffset> _fst;
        private readonly TData _data;
        private readonly VariableBindings _varBindings;
        private readonly bool _startAnchor;
        private readonly bool _endAnchor;
        private readonly bool _useDefaults;
        private readonly List<Annotation<TOffset>> _annotations;
        private readonly Queue<TInst> _cachedInstances;

        protected TraversalMethodBase(
            Fst<TData, TOffset> fst,
            TData data,
            VariableBindings varBindings,
            bool startAnchor,
            bool endAnchor,
            bool useDefaults
        )
        {
            _fst = fst;
            _data = data;
            _varBindings = varBindings;
            _startAnchor = startAnchor;
            _endAnchor = endAnchor;
            _useDefaults = useDefaults;
            _annotations = new List<Annotation<TOffset>>();
            // insertion sort
            foreach (Annotation<TOffset> topAnn in _data.Annotations.GetNodes(_fst.Direction))
            {
                foreach (Annotation<TOffset> ann in topAnn.GetNodesDepthFirst(_fst.Direction))
                {
                    if (!_fst.Filter(ann))
                        continue;

                    int i = _annotations.Count - 1;
                    while (i >= 0 && CompareAnnotations(_annotations[i], ann) > 0)
                    {
                        if (i + 1 == _annotations.Count)
                            _annotations.Add(_annotations[i]);
                        else
                            _annotations[i + 1] = _annotations[i];
                        i--;
                    }
                    if (i + 1 == _annotations.Count)
                        _annotations.Add(ann);
                    else
                        _annotations[i + 1] = ann;
                }
            }
            _cachedInstances = new Queue<TInst>();
        }

        private int CompareAnnotations(Annotation<TOffset> x, Annotation<TOffset> y)
        {
            int res = x.Range.CompareTo(y.Range);
            if (res != 0)
                return _fst.Direction == Direction.LeftToRight ? res : -res;

            return x.Depth.CompareTo(y.Depth);
        }

        protected Fst<TData, TOffset> Fst
        {
            get { return _fst; }
        }

        protected TData Data
        {
            get { return _data; }
        }

        public IList<Annotation<TOffset>> Annotations
        {
            get { return _annotations; }
        }

        public abstract IEnumerable<FstResult<TData, TOffset>> Traverse(
            ref int annIndex,
            Register<TOffset>[,] initRegisters,
            IList<TagMapCommand> initCmds,
            ISet<int> initAnns,
            bool allMatches
        );

        protected static void ExecuteCommands(
            Register<TOffset>[,] registers,
            IEnumerable<TagMapCommand> cmds,
            Register<TOffset> start,
            Register<TOffset> end
        )
        {
            foreach (TagMapCommand cmd in cmds)
            {
                if (cmd.Src == TagMapCommand.CurrentPosition)
                {
                    registers[cmd.Dest, 0] = start;
                    registers[cmd.Dest, 1] = end;
                }
                else
                {
                    registers[cmd.Dest, 0] = registers[cmd.Src, 0];
                    registers[cmd.Dest, 1] = registers[cmd.Src, 1];
                }
            }
        }

        protected bool CheckInputMatch(Arc<TData, TOffset> arc, int annIndex, VariableBindings varBindings)
        {
            return annIndex < _annotations.Count
                && arc.Input.Matches(
                    _annotations[annIndex].FeatureStruct,
                    _fst.UseUnification,
                    _useDefaults,
                    varBindings
                );
        }

        private void CheckAccepting(
            int annIndex,
            Register<TOffset>[,] registers,
            TData output,
            VariableBindings varBindings,
            State<TData, TOffset> state,
            ICollection<FstResult<TData, TOffset>> curResults,
            IList<int> priorities
        )
        {
            if (state.IsAccepting && (!_endAnchor || annIndex == _annotations.Count))
            {
                Annotation<TOffset> ann =
                    annIndex < _annotations.Count ? _annotations[annIndex] : _data.Annotations.GetEnd(_fst.Direction);
                var matchRegisters = (Register<TOffset>[,])registers.Clone();
                ExecuteCommands(matchRegisters, state.Finishers, new Register<TOffset>(), new Register<TOffset>());
                if (state.AcceptInfos.Count > 0)
                {
                    foreach (AcceptInfo<TData, TOffset> acceptInfo in state.AcceptInfos)
                    {
                        TData resOutput = output;
                        if (resOutput is ICloneable<TData> cloneable)
                            resOutput = cloneable.Clone();

                        var candidate = new FstResult<TData, TOffset>(
                            _fst.RegistersEqualityComparer,
                            acceptInfo.ID,
                            matchRegisters,
                            resOutput,
                            varBindings?.Clone(),
                            acceptInfo.Priority,
                            state.IsLazy,
                            ann,
                            priorities?.ToArray(),
                            curResults.Count
                        );
                        if (acceptInfo.Acceptable == null || acceptInfo.Acceptable(_data, candidate))
                            curResults.Add(candidate);
                    }
                }
                else
                {
                    TData resOutput = output;
                    if (resOutput is ICloneable<TData> cloneable)
                        resOutput = cloneable.Clone();
                    curResults.Add(
                        new FstResult<TData, TOffset>(
                            _fst.RegistersEqualityComparer,
                            null,
                            matchRegisters,
                            resOutput,
                            varBindings?.Clone(),
                            -1,
                            state.IsLazy,
                            ann,
                            priorities?.ToArray(),
                            curResults.Count
                        )
                    );
                }
            }
        }

        protected IEnumerable<TInst> Initialize(
            ref int annIndex,
            Register<TOffset>[,] registers,
            IList<TagMapCommand> cmds,
            ISet<int> initAnns
        )
        {
            var insts = new List<TInst>();
            TOffset offset = _annotations[annIndex].Range.GetStart(_fst.Direction);

            if (_startAnchor)
            {
                for (
                    int i = annIndex;
                    i < _annotations.Count && _annotations[i].Range.GetStart(_fst.Direction).Equals(offset);
                    i++
                )
                {
                    if (_annotations[i].Optional)
                    {
                        int nextIndex = GetNextNonoverlappingAnnotationIndex(i);
                        if (nextIndex != _annotations.Count)
                        {
                            insts.AddRange(
                                Initialize(ref nextIndex, (Register<TOffset>[,])registers.Clone(), cmds, initAnns)
                            );
                        }
                    }
                }
            }

            ExecuteCommands(registers, cmds, new Register<TOffset>(offset, true), new Register<TOffset>());

            for (
                ;
                annIndex < _annotations.Count && _annotations[annIndex].Range.GetStart(_fst.Direction).Equals(offset);
                annIndex++
            )
            {
                if (!initAnns.Contains(annIndex))
                {
                    TInst inst = GetCachedInstance();
                    inst.State = _fst.StartState;
                    inst.AnnotationIndex = annIndex;
                    Array.Copy(registers, inst.Registers, registers.Length);
                    if (!_fst.IgnoreVariables)
                        inst.VariableBindings = _varBindings != null ? _varBindings.Clone() : new VariableBindings();
                    insts.Add(inst);
                    initAnns.Add(annIndex);
                }
            }

            return insts;
        }

        protected IEnumerable<TInst> Advance(
            TInst inst,
            VariableBindings varBindings,
            Arc<TData, TOffset> arc,
            ICollection<FstResult<TData, TOffset>> curResults,
            bool optional = false
        )
        {
            inst.Priorities?.Add(arc.Priority);
            int nextIndex = GetNextNonoverlappingAnnotationIndex(inst.AnnotationIndex);
            TOffset nextOffset;
            bool nextStart;
            if (nextIndex < _annotations.Count)
            {
                nextOffset = _annotations[nextIndex].Range.GetStart(_fst.Direction);
                nextStart = true;
            }
            else
            {
                nextOffset = _data.Annotations.GetLast(_fst.Direction, _fst.Filter).Range.GetEnd(_fst.Direction);
                nextStart = false;
            }
            TOffset end = _annotations[inst.AnnotationIndex].Range.GetEnd(_fst.Direction);

            if (nextIndex < _annotations.Count)
            {
                var anns = new List<int>();
                bool cloneOutputs = false;
                for (
                    int i = nextIndex;
                    i < _annotations.Count && _annotations[i].Range.GetStart(_fst.Direction).Equals(nextOffset);
                    i++
                )
                {
                    if (_annotations[i].Optional)
                    {
                        TInst ti = CopyInstance(inst);
                        ti.AnnotationIndex = i;
                        foreach (TInst ni in Advance(ti, varBindings, arc, curResults, true))
                        {
                            yield return ni;
                            cloneOutputs = true;
                        }
                    }
                    anns.Add(i);
                }

                ExecuteCommands(
                    inst.Registers,
                    arc.Commands,
                    new Register<TOffset>(nextOffset, nextStart),
                    new Register<TOffset>(end, false)
                );
                if (!optional || _endAnchor)
                {
                    CheckAccepting(
                        nextIndex,
                        inst.Registers,
                        inst.Output,
                        varBindings,
                        arc.Target,
                        curResults,
                        inst.Priorities
                    );
                }

                inst.State = arc.Target;

                bool first = true;
                foreach (int curIndex in anns)
                {
                    TInst ni = first ? inst : CopyInstance(inst);
                    ni.AnnotationIndex = curIndex;
                    if (varBindings != null)
                        inst.VariableBindings = cloneOutputs ? varBindings.Clone() : varBindings;
                    yield return ni;
                    cloneOutputs = true;
                    first = false;
                }
            }
            else
            {
                ExecuteCommands(
                    inst.Registers,
                    arc.Commands,
                    new Register<TOffset>(nextOffset, nextStart),
                    new Register<TOffset>(end, false)
                );
                CheckAccepting(
                    nextIndex,
                    inst.Registers,
                    inst.Output,
                    varBindings,
                    arc.Target,
                    curResults,
                    inst.Priorities
                );

                inst.State = arc.Target;
                inst.AnnotationIndex = nextIndex;
                inst.VariableBindings = varBindings;
                yield return inst;
            }
        }

        protected TInst EpsilonAdvance(
            TInst inst,
            Arc<TData, TOffset> arc,
            ICollection<FstResult<TData, TOffset>> curResults
        )
        {
            Annotation<TOffset> ann =
                inst.AnnotationIndex < _annotations.Count
                    ? _annotations[inst.AnnotationIndex]
                    : _data.Annotations.GetEnd(_fst.Direction);
            int prevIndex = GetPrevNonoverlappingAnnotationIndex(inst.AnnotationIndex);
            Annotation<TOffset> prevAnn = _annotations[prevIndex];
            ExecuteCommands(
                inst.Registers,
                arc.Commands,
                new Register<TOffset>(ann.Range.GetStart(_fst.Direction), true),
                new Register<TOffset>(prevAnn.Range.GetEnd(_fst.Direction), false)
            );
            CheckAccepting(
                inst.AnnotationIndex,
                inst.Registers,
                inst.Output,
                inst.VariableBindings,
                arc.Target,
                curResults,
                inst.Priorities
            );

            inst.State = arc.Target;
            return inst;
        }

        protected void CheckAcceptingStartState(
            ISet<int> anns,
            Register<TOffset>[,] registers,
            ICollection<FstResult<TData, TOffset>> curResults
        )
        {
            if (!_fst.StartState.IsAccepting)
                return;

            foreach (int annIndex in anns)
            {
                TInst inst = GetCachedInstance();
                inst.State = _fst.StartState;
                inst.AnnotationIndex = annIndex;
                Array.Copy(registers, inst.Registers, registers.Length);
                if (!_fst.IgnoreVariables)
                    inst.VariableBindings = _varBindings != null ? _varBindings.Clone() : new VariableBindings();

                CheckAccepting(
                    inst.AnnotationIndex,
                    inst.Registers,
                    inst.Output,
                    inst.VariableBindings,
                    inst.State,
                    curResults,
                    inst.Priorities
                );
            }
        }

        private int GetNextNonoverlappingAnnotationIndex(int start)
        {
            Annotation<TOffset> cur = _annotations[start];
            for (int i = start + 1; i < _annotations.Count; i++)
            {
                if (!cur.Range.Overlaps(_annotations[i].Range))
                    return i;
            }
            return _annotations.Count;
        }

        private int GetPrevNonoverlappingAnnotationIndex(int start)
        {
            Annotation<TOffset> cur =
                start < _annotations.Count ? _annotations[start] : _data.Annotations.GetEnd(_fst.Direction);
            for (int i = start - 1; i >= 0; i--)
            {
                if (!cur.Range.Overlaps(_annotations[i].Range))
                    return i;
            }
            return -1;
        }

        private TInst GetCachedInstance()
        {
            if (_cachedInstances.Count == 0)
                return CreateInstance();

            TInst inst = _cachedInstances.Dequeue();
            inst.Clear();
            return inst;
        }

        protected TInst CopyInstance(TInst inst)
        {
            TInst ni = GetCachedInstance();
            inst.CopyTo(ni);
            return ni;
        }

        protected TInst CopyInstanceAndBindings(TInst inst)
        {
            TInst ni = CopyInstance(inst);
            if (inst.VariableBindings != null)
            {
                ni.VariableBindings = inst.VariableBindings.Clone();
            }
            return ni;
        }

        protected abstract TInst CreateInstance();

        protected void ReleaseInstance(TInst inst)
        {
            _cachedInstances.Enqueue(inst);
        }

        protected class LatticeNode
        {
            public State<TData, TOffset> State { get; set; }
            public int AnnotationIndex { get; set; }
            public VariableBindings VariableBindings { get; set; }
        }

        protected class LatticeArc
        {
            public TInst Instance { get; set; }
            public Arc<TData, TOffset> Arc { get; set; }
            public IList<TInst> Instances { get; set; }
        }

        private readonly LatticeNode _finalState = new LatticeNode();

        /// <summary>
        /// Creates a lattice.
        /// A lattice is a graph that represents the space of traversals as a packed forest.
        /// The nodes are [State, AnnotationIndex] pairs.
        /// Each node has a list of incoming arcs that are [Instance, Arc] pairs.
        /// The Instance encodes the previous node.
        /// </summary>
        protected IDictionary<LatticeNode, IList<LatticeArc>> CreateFstLattice()
        {
            return new Dictionary<LatticeNode, IList<LatticeArc>>(
                AnonymousEqualityComparer.Create<LatticeNode>(LatticeNodeKeyEquals, LatticeNodeKeyGetHashCode)
            );
        }

        /// <summary>
        /// Check whether instance is already recorded in lattice.
        /// If not, adds instance to lattice.
        /// Also adds [origInstance, arc] to instance's incoming arcs.
        /// </summary>
        protected bool RecordedInstance(
            IDictionary<LatticeNode, IList<LatticeArc>> lattice,
            TInst instance,
            TInst origInstance,
            Arc<TData, TOffset> arc
        )
        {
            var nodeKey = new LatticeNode()
            {
                State = instance.State,
                AnnotationIndex = instance.AnnotationIndex,
                VariableBindings = instance.VariableBindings?.Clone(),
            };
            bool recorded = lattice.TryGetValue(nodeKey, out IList<LatticeArc> incoming);
            if (!recorded)
            {
                // Add nodeKey to lattice.
                incoming = new List<LatticeArc>();
                lattice[nodeKey] = incoming;
            }
            // Add [origInstance, arc] to incoming.
            incoming.Add(new LatticeArc() { Instance = origInstance, Arc = arc });
            return recorded;
        }

        protected void RecordFinalArc(
            IDictionary<LatticeNode, IList<LatticeArc>> lattice,
            TInst origInstance,
            Arc<TData, TOffset> arc
        )
        {
            bool recorded = lattice.TryGetValue(_finalState, out IList<LatticeArc> incoming);
            if (!recorded)
            {
                // Add _finalState to lattice.
                incoming = new List<LatticeArc>();
                lattice[_finalState] = incoming;
            }
            // Add [origInstance, arc] to incoming.
            incoming.Add(new LatticeArc() { Instance = origInstance, Arc = arc });
        }

        /// <summary>
        /// Extract the results encoded in lattice under the final state.
        /// </summary>
        protected List<FstResult<TData, TOffset>> ExtractResults(
            IDictionary<LatticeNode, IList<LatticeArc>> lattice,
            bool allMatches
        )
        {
            List<FstResult<TData, TOffset>> newResults = new List<FstResult<TData, TOffset>>();
            IList<LatticeArc> incoming;
            if (!lattice.TryGetValue(_finalState, out incoming))
                return newResults;
            foreach (LatticeArc latticeArc in incoming)
            {
                foreach (TInst instance in ExpandArcInstances(latticeArc, lattice, allMatches))
                {
                    AdvanceInstance(instance, latticeArc.Arc, null, newResults, null, 0);
                }
            }
            return newResults;
        }

        private IList<TInst> ExpandArcInstances(
            LatticeArc latticeArc,
            IDictionary<LatticeNode, IList<LatticeArc>> lattice,
            bool allMatches
        )
        {
            if (latticeArc.Instances == null)
            {
                latticeArc.Instances = ExpandInstances(latticeArc.Instance, lattice, allMatches);
            }
            IList<TInst> instances = new List<TInst>();
            foreach (TInst instance in latticeArc.Instances)
            {
                instances.Add(CopyInstanceAndBindings(instance));
            }
            return instances;
        }

        private IList<TInst> ExpandInstances(
            TInst instance,
            IDictionary<LatticeNode, IList<LatticeArc>> lattice,
            bool allMatches
        )
        {
            IList<TInst> instances = new List<TInst>();
            IList<FstResult<TData, TOffset>> curResults = new List<FstResult<TData, TOffset>>();
            LatticeNode nodeKey = new LatticeNode()
            {
                State = instance.State,
                AnnotationIndex = instance.AnnotationIndex,
                VariableBindings = instance.VariableBindings?.Clone(),
            };
            bool recorded = lattice.TryGetValue(nodeKey, out IList<LatticeArc> incoming);
            if (!recorded)
            {
                // The starting instance.
                instances.Add(CopyInstanceAndBindings(instance));
                return instances;
            }
            foreach (LatticeArc latticeArc in incoming)
            {
                foreach (TInst source in ExpandArcInstances(latticeArc, lattice, allMatches))
                {
                    AdvanceInstance(
                        source,
                        latticeArc.Arc,
                        instances,
                        curResults,
                        instance.State,
                        instance.AnnotationIndex
                    );
                }
            }
            if (!allMatches && instances.Count > 1)
            {
                instances.Sort(InstanceCompare);
                TInst first = instances.First();
                instances.Clear();
                instances.Add(first);
            }
            return instances;
        }

        private void AdvanceInstance(
            TInst instance,
            Arc<TData, TOffset> arc,
            IList<TInst> instances,
            IList<FstResult<TData, TOffset>> curResults,
            State<TData, TOffset> state,
            int annotationIndex
        )
        {
            if (arc.Input.IsEpsilon)
            {
                TInst ni = EpsilonAdvance(instance, arc, curResults);
                if (instances != null && ni.State == state && ni.AnnotationIndex == annotationIndex)
                {
                    instances.Add(ni);
                }
            }
            else if (CheckInputMatch(arc, instance.AnnotationIndex, instance.VariableBindings))
            {
                foreach (TInst ni in Advance(instance, instance.VariableBindings, arc, curResults))
                {
                    if (instances != null && ni.State == state && ni.AnnotationIndex == annotationIndex)
                        instances.Add(ni);
                }
            }
        }

        private int InstanceCompare(TInst x, TInst y)
        {
            int compare = 0;
            if (x.Priorities != null)
            {
                foreach (Tuple<int, int> priorityPair in x.Priorities.Zip(y.Priorities))
                {
                    compare = priorityPair.Item1.CompareTo(priorityPair.Item2);
                    if (compare != 0)
                        break;
                }
            }
            return compare;
        }

        private bool LatticeNodeKeyEquals(LatticeNode x, LatticeNode y)
        {
            if (x.State == null || y.State == null)
                return x.State == y.State;
            if (x.VariableBindings == null)
                return x.State.Equals(y.State) && x.AnnotationIndex.Equals(y.AnnotationIndex);
            return x.State.Equals(y.State)
                && x.AnnotationIndex.Equals(y.AnnotationIndex)
                && x.VariableBindings.Equals(y.VariableBindings);
        }

        private int LatticeNodeKeyGetHashCode(LatticeNode m)
        {
            int code = 23;
            code = code * 31 + (m.State != null ? m.State.GetHashCode() : 0);
            code = code * 31 + m.AnnotationIndex.GetHashCode();
            code = code * 31 + (m.VariableBindings != null ? m.VariableBindings.GetHashCode() : 0);
            return code;
        }
    }
}
