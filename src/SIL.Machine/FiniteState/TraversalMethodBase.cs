using System;
using System.Collections.Generic;
using System.Linq;
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
        private TData _data;
        private VariableBindings _varBindings;
        private bool _startAnchor;
        private bool _endAnchor;
        private bool _useDefaults;
        private readonly List<Annotation<TOffset>> _annotations;

        // Instance free-list, kept across Reset() calls so a traversal method pooled for the
        // duration of one word (see Fst.Transduce + Morpher per-word reset) reuses instances across
        // the thousands of Transduce calls that word triggers.
        private readonly Queue<TInst> _cachedInstances;

        // Cached delegate for the per-annotation insertion sort in Reset(). Allocated once here
        // rather than per Reset() call so the depth-first walk uses the allocation-free
        // PreorderTraverse(action) form instead of GetNodesDepthFirst(), whose yield state machine
        // was heap-allocated on every Transduce (Reset runs once per Transduce, thousands per word).
        private readonly Action<Annotation<TOffset>> _insertAnnotation;

        protected TraversalMethodBase(Fst<TData, TOffset> fst)
        {
            _fst = fst;
            _annotations = new List<Annotation<TOffset>>();
            _cachedInstances = new Queue<TInst>();
            _insertAnnotation = InsertAnnotation;
        }

        /// <summary>
        /// Re-targets this (pooled) traversal method at a new input without reallocating it or its
        /// instance free-list. Rebuilds the per-input annotation list; keeps <see cref="_cachedInstances"/>.
        /// </summary>
        public void Reset(TData data, VariableBindings varBindings, bool startAnchor, bool endAnchor, bool useDefaults)
        {
            _data = data;
            _varBindings = varBindings;
            _startAnchor = startAnchor;
            _endAnchor = endAnchor;
            _useDefaults = useDefaults;
            _annotations.Clear();
            // insertion sort (PreorderTraverse with a cached delegate — same depth-first order as
            // GetNodesDepthFirst but no per-call yield-iterator allocation; see _insertAnnotation).
            foreach (Annotation<TOffset> topAnn in _data.Annotations.GetNodes(_fst.Direction))
                topAnn.PreorderTraverse(_insertAnnotation, _fst.Direction);
        }

        private void InsertAnnotation(Annotation<TOffset> ann)
        {
            if (!_fst.Filter(ann))
                return;

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

        public abstract List<FstResult<TData, TOffset>> Traverse(
            ref int annIndex,
            Register<TOffset>[,] initRegisters,
            IList<TagMapCommand> initCmds,
            ISet<int> initAnns
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
                Register<TOffset>[,] matchRegisters;
                if (FstStatistics.Enabled && FstStatistics.AllocationProbe != null)
                {
                    long regBefore = FstStatistics.AllocationProbe();
                    matchRegisters = (Register<TOffset>[,])registers.Clone();
                    FstStatistics.AddRegisterCloneBytes(FstStatistics.AllocationProbe() - regBefore);
                }
                else
                {
                    matchRegisters = (Register<TOffset>[,])registers.Clone();
                }
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

        // De-iterator (RUSTIFY lever 1): fills the caller-provided buffer instead of allocating a fresh
        // List per call (plus a nested List per recursive optional-skip). The buffer is reused per
        // Transduce by the traversal method (see InitializeStack); recursion appends to the same buffer.
        protected void Initialize(
            ref int annIndex,
            Register<TOffset>[,] registers,
            IList<TagMapCommand> cmds,
            ISet<int> initAnns,
            List<TInst> output
        )
        {
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
                            Initialize(ref nextIndex, (Register<TOffset>[,])registers.Clone(), cmds, initAnns, output);
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
                    output.Add(inst);
                    initAnns.Add(annIndex);
                }
            }
        }

        // RUSTIFY lever 1 (de-iterator): Advance was a `yield`-based iterator, so every call (one per
        // matched arc, recursively for optional-skip forks — millions/word) allocated an iterator state
        // machine. It now fills a reusable per-method buffer instead. The traversal method is created
        // fresh per Transduce (dies in Gen0), so the buffer carries no cross-word retention (the Phase-1b
        // regression), and Advance is not re-entrant within one method (a re-entrant Transduce gets its
        // own method instance + buffer). Byte-identical: same results in the same order.
        // One reusable result buffer per traversal method (per-Transduce → no cross-word retention; can't
        // be a thread-static — CheckAccepting's Acceptable predicate can re-enter Transduce). Shared by
        // Initialize and Advance: Initialize fills it once at the start of Traverse and the caller fully
        // consumes it building the work stack before the main loop's first Advance reuses it, so they
        // never overlap.
        private readonly List<TInst> _buffer = new List<TInst>();

        protected List<TInst> InitializeBuffer => _buffer;

        protected List<TInst> Advance(
            TInst inst,
            VariableBindings varBindings,
            Arc<TData, TOffset> arc,
            ICollection<FstResult<TData, TOffset>> curResults
        )
        {
            _buffer.Clear();
            AdvanceInto(inst, varBindings, arc, curResults, false, _buffer);
            return _buffer;
        }

        private void AdvanceInto(
            TInst inst,
            VariableBindings varBindings,
            Arc<TData, TOffset> arc,
            ICollection<FstResult<TData, TOffset>> curResults,
            bool optional,
            List<TInst> output
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
                bool cloneOutputs = false;
                // The same-offset window is a contiguous index range [nextIndex, annsEnd); track its
                // end bound instead of materializing a List<int> per Advance call (hot path).
                int annsEnd = nextIndex;
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
                        int before = output.Count;
                        AdvanceInto(ti, varBindings, arc, curResults, true, output);
                        if (output.Count > before)
                            cloneOutputs = true;
                    }
                    annsEnd = i + 1;
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
                for (int curIndex = nextIndex; curIndex < annsEnd; curIndex++)
                {
                    TInst ni = first ? inst : CopyInstance(inst);
                    ni.AnnotationIndex = curIndex;
                    if (varBindings != null)
                        inst.VariableBindings = cloneOutputs ? varBindings.Clone() : varBindings;
                    output.Add(ni);
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
                output.Add(inst);
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

        protected abstract TInst CreateInstance();

        protected void ReleaseInstance(TInst inst)
        {
            _cachedInstances.Enqueue(inst);
        }
    }
}
