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
            Arc<TData, TOffset> arc,
            ICollection<FstResult<TData, TOffset>> curResults,
            IList<int> priorities
        )
        {
            if (arc.Target.IsAccepting && (!_endAnchor || annIndex == _annotations.Count))
            {
                Annotation<TOffset> ann =
                    annIndex < _annotations.Count ? _annotations[annIndex] : _data.Annotations.GetEnd(_fst.Direction);
                var matchRegisters = (Register<TOffset>[,])registers.Clone();
                ExecuteCommands(matchRegisters, arc.Target.Finishers, new Register<TOffset>(), new Register<TOffset>());
                if (arc.Target.AcceptInfos.Count > 0)
                {
                    foreach (AcceptInfo<TData, TOffset> acceptInfo in arc.Target.AcceptInfos)
                    {
                        TData resOutput = output;
                        var cloneable = resOutput as ICloneable<TData>;
                        if (cloneable != null)
                            resOutput = cloneable.Clone();

                        var candidate = new FstResult<TData, TOffset>(
                            _fst.RegistersEqualityComparer,
                            acceptInfo.ID,
                            matchRegisters,
                            resOutput,
                            varBindings == null ? null : varBindings.Clone(),
                            acceptInfo.Priority,
                            arc.Target.IsLazy,
                            ann,
                            priorities == null ? null : priorities.ToArray(),
                            curResults.Count
                        );
                        if (acceptInfo.Acceptable == null || acceptInfo.Acceptable(_data, candidate))
                            curResults.Add(candidate);
                    }
                }
                else
                {
                    TData resOutput = output;
                    var cloneable = resOutput as ICloneable<TData>;
                    if (cloneable != null)
                        resOutput = cloneable.Clone();
                    curResults.Add(
                        new FstResult<TData, TOffset>(
                            _fst.RegistersEqualityComparer,
                            null,
                            matchRegisters,
                            resOutput,
                            varBindings == null ? null : varBindings.Clone(),
                            -1,
                            arc.Target.IsLazy,
                            ann,
                            priorities == null ? null : priorities.ToArray(),
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
                            insts.AddRange(
                                Initialize(ref nextIndex, (Register<TOffset>[,])registers.Clone(), cmds, initAnns)
                            );
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
            if (inst.Priorities != null)
                inst.Priorities.Add(arc.Priority);
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
                    CheckAccepting(
                        nextIndex,
                        inst.Registers,
                        inst.Output,
                        varBindings,
                        arc,
                        curResults,
                        inst.Priorities
                    );

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
                CheckAccepting(nextIndex, inst.Registers, inst.Output, varBindings, arc, curResults, inst.Priorities);

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
                arc,
                curResults,
                inst.Priorities
            );

            inst.State = arc.Target;
            return inst;
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
