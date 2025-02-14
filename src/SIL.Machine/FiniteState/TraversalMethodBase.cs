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
        private readonly IDictionary<int, FeatureStruct> _frozenAnnotationFS;
        private readonly Queue<TInst> _cachedInstances;
        private readonly IDictionary<TInst, IList<CommandUpdate>> _commandUpdates;
        private readonly IDictionary<TInst, IList<TraverseOutput>> _outputs;
        private readonly TInst _finalInst;

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
            _frozenAnnotationFS = new Dictionary<int, FeatureStruct>();
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
            _commandUpdates = new Dictionary<TInst, IList<CommandUpdate>>();
            _outputs = new Dictionary<TInst, IList<TraverseOutput>>();
            _finalInst = CreateInstance();
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

        protected class CommandUpdate
        {
            public TInst Source;
            public Arc<TData, TOffset> Arc;
            public IEnumerable<TagMapCommand> Cmds;
            public Register<TOffset> Start;
            public Register<TOffset> End;

            public CommandUpdate(
                TInst source,
                Arc<TData, TOffset> arc,
                IEnumerable<TagMapCommand> cmds,
                Register<TOffset> start,
                Register<TOffset> end)
            {
                Source = source;
                Arc = arc;
                Cmds = cmds;
                Start = start;
                End = end;
            }
        }

        protected void RecordCommands(
            TInst source,
            Arc<TData, TOffset> arc,
            IEnumerable<TagMapCommand> cmds,
            Register<TOffset> start,
            Register<TOffset> end,
            TInst target
        )
        {
            if (source == target)
                return;
            var commandUpdate = new CommandUpdate(source, arc, cmds, start, end);
            if (!_commandUpdates.ContainsKey(target))
                _commandUpdates[target] = new List<CommandUpdate>();
            _commandUpdates[target].Add(commandUpdate);
        }

        protected void MergeCommands(TInst newInst, TInst oldInst)
        {
            _commandUpdates[oldInst].AddRange(_commandUpdates[newInst]);
            if (_commandUpdates.Keys.Contains(_finalInst))
            {
                // Avoid duplicates.
                foreach (CommandUpdate update in _commandUpdates[_finalInst])
                {
                    if (update.Source == newInst)
                    {
                        _commandUpdates[_finalInst].Remove(update);
                        break;
                    }
                }
            }
            _commandUpdates.Remove(newInst);
        }

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

        private void RecordAccepting(
            TInst inst,
            Arc<TData, TOffset> arc
        )
        {
          if (arc.Target.IsAccepting && (!_endAnchor || inst.AnnotationIndex == _annotations.Count))
          {
             RecordCommands(inst, arc, arc.Target.Finishers, new Register<TOffset>(), new Register<TOffset>(), _finalInst);
          }
        }

        public void GetFstResults(ICollection<FstResult<TData, TOffset>> curResults, bool isTransducer = false)
        {
            if (!_commandUpdates.ContainsKey(_finalInst))
                return;
            var keys = new HashSet<Tuple<State<TData, TOffset>, int, Register<TOffset>[,]>>(
                AnonymousEqualityComparer.Create<Tuple<State<TData, TOffset>, int, Register<TOffset>[,]>>(
                    KeyEquals,
                    KeyGetHashCode
            ));
            foreach (CommandUpdate update in _commandUpdates[_finalInst])
            {
                foreach (TraverseOutput output in GetOutputs(update.Source, isTransducer, keys))
                {
                    CheckAccepting(
                         update.Source.AnnotationIndex,
                         output.Registers,
                         output.Output,
                         update.Source.VariableBindings,
                         update.Arc,
                         curResults,
                         output.Priorities);
                }
            }
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
            if (curResults == null)
                return;
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
                        if (resOutput is ICloneable<TData> cloneable)
                            resOutput = cloneable.Clone();

                        var candidate = new FstResult<TData, TOffset>(
                            _fst.RegistersEqualityComparer,
                            acceptInfo.ID,
                            matchRegisters,
                            resOutput,
                            varBindings?.Clone(),
                            acceptInfo.Priority,
                            arc.Target.IsLazy,
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
                            arc.Target.IsLazy,
                            ann,
                            priorities?.ToArray(),
                            curResults.Count
                        )
                    );
                }
            }
        }

        protected class TraverseOutput
        {
            public IList<int> Priorities;
            public Register<TOffset>[,] Registers;
            public TData Output;
            public IDictionary<Annotation<TOffset>, Annotation<TOffset>> Mappings;
            public Queue<Annotation<TOffset>> Queue;

            public TraverseOutput(Register<TOffset>[,] registers, TData output, Dictionary<Annotation<TOffset>, Annotation<TOffset>> mappings)
            {
                Priorities = new List<int>();
                Registers = registers;
                Output = output;
                Mappings = mappings;
                Queue = new Queue<Annotation<TOffset>>();
            }

            public TraverseOutput(TraverseOutput other, Boolean isTransducer)
            {
                Priorities = new List<int>(other.Priorities);
                Registers = (Register<TOffset>[,])other.Registers.Clone();
                Output = isTransducer ? ((ICloneable<TData>)other.Output).Clone(): other.Output;
                Mappings = other.Mappings;
                Queue = new Queue<Annotation<TOffset>>(other.Queue);
            }
        }

        private IList<TraverseOutput> GetOutputs(TInst inst, Boolean isTransducer, HashSet<Tuple<State<TData, TOffset>, int, Register<TOffset>[,]>> keys, int depth = 0)
        {
            if (inst != null && _outputs.ContainsKey(inst))
                return _outputs[inst];
            IList<TraverseOutput> outputs = new List<TraverseOutput>();
            IList<CommandUpdate> updates = GetCommandUpdates(inst);
            if (updates.Count == 0)
            {
                if (inst == _finalInst)
                    return outputs;
                // We are at the beginning.
                var registers = inst != null ? inst.Registers : new Register<TOffset>[Fst.RegisterCount, 2];
                TData dataOutput = isTransducer ? ((ICloneable<TData>)Data).Clone() : Data;
                var mappings = new Dictionary<Annotation<TOffset>, Annotation<TOffset>>();
                mappings.AddRange(Data.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
                        .Zip(
                            dataOutput.Annotations.SelectMany(a => a.GetNodesBreadthFirst()),
                            (a1, a2) => new KeyValuePair<Annotation<TOffset>, Annotation<TOffset>>(a1, a2)
                        ));
                var output = new TraverseOutput(registers, dataOutput, mappings);
                outputs.Add(output);
                return outputs;
            }

            foreach (CommandUpdate update in updates)
            {
                IList<TraverseOutput> sourceOutputs = GetOutputs(update.Source, isTransducer, keys, depth + 1);
                foreach (TraverseOutput output in sourceOutputs)
                {
                    var newOutput = new TraverseOutput(output, isTransducer);
                    if (update.Cmds != null)
                        ExecuteCommands(newOutput.Registers, update.Cmds, update.Start, update.End);
                    if (update.Arc != null && inst != _finalInst)
                    {
                        newOutput.Priorities.Add(update.Arc.Priority);
                    }
                    if (update.Arc != null && inst != _finalInst && isTransducer)
                    {
                        for (int j = 0; j < update.Arc.Input.EnqueueCount; j++)
                            newOutput.Queue.Enqueue(Annotations[update.Source.AnnotationIndex]);

                        Annotation<TOffset> prevNewAnn = null;
                        foreach (Output<TData, TOffset> outputAction in update.Arc.Outputs)
                        {
                            Annotation<TOffset> outputAnn;
                            if (outputAction.UsePrevNewAnnotation && prevNewAnn != null)
                            {
                                outputAnn = prevNewAnn;
                            }
                            else
                            {
                                Annotation<TOffset> inputAnn = newOutput.Queue.Dequeue();
                                outputAnn = output.Mappings[inputAnn];
                                outputAnn = outputAnn.Clone();
                            }
                            prevNewAnn = outputAction.UpdateOutput(newOutput.Output, outputAnn, Fst.Operations);
                        }
                    }
                    var key = Tuple.Create(
                        inst.State,
                        inst.AnnotationIndex,
                        newOutput.Registers
                    );
                    if (keys.Contains(key))
                        continue;
                    keys.Add(key);
                    outputs.Add(newOutput);
                }
            }
            if (inst != null)
                _outputs[inst] = outputs;
            return outputs;
        }

        private bool KeyEquals(
            Tuple<State<TData, TOffset>, int, Register<TOffset>[,]> x,
            Tuple<State<TData, TOffset>, int, Register<TOffset>[,]> y
        )
        {
            return x.Item1 != null ? y.Item1 != null && x.Item1.Equals(y.Item1) : y.Item1 != null
                && x.Item2.Equals(y.Item2)
                && Fst.RegistersEqualityComparer.Equals(x.Item3, y.Item3);
        }

        private int KeyGetHashCode(Tuple<State<TData, TOffset>, int, Register<TOffset>[,]> m)
        {
            int code = 23;
            code = code * 31 + (m.Item1 != null ? m.Item1.GetHashCode() : 0);
            code = code * 31 + m.Item2.GetHashCode();
            code = code * 31 + Fst.RegistersEqualityComparer.GetHashCode(m.Item3);
            return code;
        }

        private IList<CommandUpdate> GetCommandUpdates(TInst inst)
        {
            if (inst == null)
                return new List<CommandUpdate>();
            if (!_commandUpdates.ContainsKey(inst))
                _commandUpdates[inst] = new List<CommandUpdate>();
            return _commandUpdates[inst];
        }

        protected IEnumerable<TInst> Initialize(
            ref int annIndex,
            Register<TOffset>[,] registers,
            IList<TagMapCommand> cmds,
            ISet<int> initAnns,
            Boolean first = true
        )
        {
            if (first)
            {
                _commandUpdates.Clear();
                _outputs.Clear();
            }
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
                                Initialize(ref nextIndex, (Register<TOffset>[,])registers.Clone(), cmds, initAnns, false)
                            );
                        }
                    }
                }
            }

            var startInst = CreateInstance();
            for (int i = 0; i < registers.Length/2; i++)
            {
                for (int j = 0; j < 2; j++)
                    startInst.Registers[i, j] = registers[i, j];
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
                    RecordCommands(startInst, null, cmds, new Register<TOffset>(offset, true), new Register<TOffset>(), inst);
                }
            }

            return insts;
        }

        protected IEnumerable<TInst> Advance(
            TInst inst,
            VariableBindings varBindings,
            Arc<TData, TOffset> arc,
            ICollection<FstResult<TData, TOffset>> curResults,
            bool optional = false,
            HashSet<FeatureStruct> optionalFeatureStructs = null
        )
        {
            TInst source = inst;
            if (curResults == null)
                inst = CopyInstance(inst);
            else
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
                        TInst ti = CopyInstance(source);
                        ti.AnnotationIndex = i;
                        if (curResults == null)
                            RecordCommands(source, arc, null, new Register<TOffset>(), new Register<TOffset>(), ti);
                        HashSet<FeatureStruct> nextOptionalFeatureStructs = null;
                        FeatureStruct annFS = null;
                        nextOptionalFeatureStructs = new HashSet<FeatureStruct>(FreezableEqualityComparer<FeatureStruct>.Default);
                        if (optionalFeatureStructs != null)
                            nextOptionalFeatureStructs.AddRange(optionalFeatureStructs);
                        if (!_frozenAnnotationFS.ContainsKey(i))
                        {
                            _frozenAnnotationFS[i] = _annotations[i].FeatureStruct.Clone();
                            _frozenAnnotationFS[i].Freeze();
                        }
                        annFS = _frozenAnnotationFS[i];
                        nextOptionalFeatureStructs.Add(annFS);
                        foreach (TInst ni in Advance(ti, varBindings, arc, curResults, true, nextOptionalFeatureStructs))
                        {
                            yield return ni;
                            cloneOutputs = true;
                        }
                        // If we skipped this optional annotation before, we cannot take it now.
                        // This avoids spurious ambiguities.
                        if (optionalFeatureStructs != null && optionalFeatureStructs.Contains(annFS))
                            continue;
                    }
                    anns.Add(i);
                }

                if (curResults == null)
                {
                    RecordCommands(
                        source,
                        arc,
                        arc.Commands,
                        new Register<TOffset>(nextOffset, nextStart),
                        new Register<TOffset>(end, false),
                        inst
                    );
                }
                else
                {
                    ExecuteCommands(
                        inst.Registers,
                        arc.Commands,
                        new Register<TOffset>(nextOffset, nextStart),
                        new Register<TOffset>(end, false)
                    );
                }

                if (!optional || _endAnchor)
                {
                    inst.AnnotationIndex = nextIndex;
                    if (curResults == null)
                    {
                        RecordAccepting(inst, arc);
                    }
                    else
                    {
                        CheckAccepting(
                            nextIndex,
                            inst.Registers,
                            inst.Output,
                            varBindings,
                            arc,
                            curResults,
                            inst.Priorities
                        );
                    }
                }

                inst.State = arc.Target;

                bool first = true;
                foreach (int curIndex in anns)
                {
                    TInst ni = first ? inst : CopyInstance(inst);
                    ni.AnnotationIndex = curIndex;
                    if (varBindings != null)
                        inst.VariableBindings = cloneOutputs ? varBindings.Clone() : varBindings;
                    if (curResults == null)
                        RecordCommands(inst, null, null, new Register<TOffset>(), new Register<TOffset>(), ni);
                    yield return ni;
                    cloneOutputs = true;
                    first = false;
                }
            }
            else
            {
                inst.State = arc.Target;
                inst.AnnotationIndex = nextIndex;
                inst.VariableBindings = varBindings;

                if (curResults == null)
                {
                    RecordCommands(
                        source,
                        arc,
                        arc.Commands,
                        new Register<TOffset>(nextOffset, nextStart),
                        new Register<TOffset>(end, false),
                        inst
                    );
                    RecordAccepting(inst, arc);
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
                }

                yield return inst;
            }
        }

        protected TInst EpsilonAdvance(
            TInst inst,
            Arc<TData, TOffset> arc,
            ICollection<FstResult<TData, TOffset>> curResults
        )
        {
            TInst source = inst;
            inst = CopyInstance(source);
            Annotation<TOffset> ann =
                inst.AnnotationIndex < _annotations.Count
                    ? _annotations[inst.AnnotationIndex]
                    : _data.Annotations.GetEnd(_fst.Direction);
            int prevIndex = GetPrevNonoverlappingAnnotationIndex(inst.AnnotationIndex);
            Annotation<TOffset> prevAnn = _annotations[prevIndex];
            if (curResults == null)
            {
                RecordCommands(
                    source,
                    arc,
                    arc.Commands,
                    new Register<TOffset>(ann.Range.GetStart(_fst.Direction), true),
                    new Register<TOffset>(prevAnn.Range.GetEnd(_fst.Direction), false),
                    inst
                );
                RecordAccepting(inst, arc);
            }
            else
            {
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
            }

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
            if (inst == null)
                return;
            // _cachedInstances.Enqueue(inst);
        }
    }
}
