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
    internal class NondeterministicFstTraversalMethod<TData, TOffset>
        : TraversalMethodBase<TData, TOffset, NondeterministicFstTraversalInstance<TData, TOffset>>
        where TData : IAnnotatedData<TOffset>
    {
        public NondeterministicFstTraversalMethod(Fst<TData, TOffset> fst)
            : base(fst) { }

        public override List<FstResult<TData, TOffset>> Traverse(
            ref int annIndex,
            Register<TOffset>[,] initRegisters,
            IList<TagMapCommand> initCmds,
            ISet<int> initAnns
        )
        {
            Stack<NondeterministicFstTraversalInstance<TData, TOffset>> instStack = InitializeStack(
                ref annIndex,
                initRegisters,
                initCmds,
                initAnns
            );

            var curResults = new List<FstResult<TData, TOffset>>();
            // Value-type dedup key (was Tuple<,,,>): stored inline in the HashSet, so a push no longer
            // allocates a heap Tuple. The per-push Outputs snapshot array remains (the key must capture
            // the outputs at push time, since the instance's Outputs list keeps growing afterward).
            var traversed = new HashSet<TraversalKey>(
                AnonymousEqualityComparer.Create<TraversalKey>(KeyEquals, KeyGetHashCode)
            );
            while (instStack.Count != 0)
            {
                NondeterministicFstTraversalInstance<TData, TOffset> inst = instStack.Pop();

                bool releaseInstance = true;
                VariableBindings varBindings = null;
                int i = 0;
                foreach (Arc<TData, TOffset> arc in inst.State.Arcs)
                {
                    bool isInstReusable = i == inst.State.Arcs.Count - 1;
                    if (arc.Input.IsEpsilon)
                    {
                        if (!inst.IsVisited(arc.Target))
                        {
                            NondeterministicFstTraversalInstance<TData, TOffset> ti;
                            if (isInstReusable)
                            {
                                ti = inst;
                            }
                            else
                            {
                                ti = CopyInstance(inst);
                                if (inst.VariableBindings != null && varBindings == null)
                                    varBindings = inst.VariableBindings.Clone();
                                ti.VariableBindings = varBindings;
                            }

                            if (arc.Outputs.Count == 1)
                            {
                                Annotation<TOffset> outputAnn = ti.Mappings[Annotations[inst.AnnotationIndex]];
                                arc.Outputs[0].UpdateOutput(ti.Output, outputAnn, Fst.Operations);
                                ti.Outputs.Add(arc.Outputs[0]);
                            }

                            ti.MarkVisited(arc.Target);
                            NondeterministicFstTraversalInstance<TData, TOffset> newInst = EpsilonAdvance(
                                inst,
                                arc,
                                curResults
                            );
                            var key = new TraversalKey(
                                newInst.State,
                                newInst.AnnotationIndex,
                                newInst.Registers,
                                newInst.Outputs.ToArray()
                            );
                            // Add returns false if already present; this single hash/lookup replaces
                            // the Contains-then-Add pair (the structural key hash over registers +
                            // outputs is expensive and this is the innermost traversal loop).
                            if (traversed.Add(key))
                                instStack.Push(newInst);
                            if (isInstReusable)
                                releaseInstance = false;
                            varBindings = null;
                        }
                    }
                    else
                    {
                        if (inst.VariableBindings != null && varBindings == null)
                            varBindings = isInstReusable ? inst.VariableBindings : inst.VariableBindings.Clone();
                        if (CheckInputMatch(arc, inst.AnnotationIndex, varBindings))
                        {
                            NondeterministicFstTraversalInstance<TData, TOffset> ti = isInstReusable
                                ? inst
                                : CopyInstance(inst);

                            if (arc.Outputs.Count == 1)
                            {
                                Annotation<TOffset> outputAnn = ti.Mappings[Annotations[inst.AnnotationIndex]];
                                arc.Outputs[0].UpdateOutput(ti.Output, outputAnn, Fst.Operations);
                                ti.Outputs.Add(arc.Outputs[0]);
                            }

                            foreach (
                                NondeterministicFstTraversalInstance<TData, TOffset> newInst in Advance(
                                    ti,
                                    varBindings,
                                    arc,
                                    curResults
                                )
                            )
                            {
                                newInst.ClearVisited();
                                var key = new TraversalKey(
                                    newInst.State,
                                    newInst.AnnotationIndex,
                                    newInst.Registers,
                                    newInst.Outputs.ToArray()
                                );
                                // Single hash/lookup (Add returns false if present) — see note above.
                                if (traversed.Add(key))
                                    instStack.Push(newInst);
                            }
                            if (isInstReusable)
                                releaseInstance = false;
                            varBindings = null;
                        }
                    }
                    i++;
                }

                if (releaseInstance)
                    ReleaseInstance(inst);
            }

            CheckAcceptingStartState(initAnns, initRegisters, curResults);

            return curResults;
        }

        protected override NondeterministicFstTraversalInstance<TData, TOffset> CreateInstance()
        {
            return new NondeterministicFstTraversalInstance<TData, TOffset>(Fst.RegisterCount);
        }

        // Value-type dedup key (was Tuple<State,int,Register[,],Output[]>): stored inline in the
        // `traversed` HashSet so a push no longer allocates a heap Tuple. Holds the instance's live
        // Registers by reference and a snapshot of its Outputs, exactly as the Tuple did.
        private readonly struct TraversalKey
        {
            public readonly State<TData, TOffset> State;
            public readonly int AnnotationIndex;
            public readonly Register<TOffset>[,] Registers;
            public readonly Output<TData, TOffset>[] Outputs;

            public TraversalKey(
                State<TData, TOffset> state,
                int annotationIndex,
                Register<TOffset>[,] registers,
                Output<TData, TOffset>[] outputs
            )
            {
                State = state;
                AnnotationIndex = annotationIndex;
                Registers = registers;
                Outputs = outputs;
            }
        }

        private bool KeyEquals(TraversalKey x, TraversalKey y)
        {
            return x.State.Equals(y.State)
                && x.AnnotationIndex.Equals(y.AnnotationIndex)
                && Fst.RegistersEqualityComparer.Equals(x.Registers, y.Registers)
                && x.Outputs.SequenceEqual(y.Outputs);
        }

        private int KeyGetHashCode(TraversalKey m)
        {
            int code = 23;
            code = code * 31 + m.State.GetHashCode();
            code = code * 31 + m.AnnotationIndex.GetHashCode();
            code = code * 31 + Fst.RegistersEqualityComparer.GetHashCode(m.Registers);
            code = code * 31 + m.Outputs.GetSequenceHashCode();
            return code;
        }

        private Stack<NondeterministicFstTraversalInstance<TData, TOffset>> InitializeStack(
            ref int annIndex,
            Register<TOffset>[,] registers,
            IList<TagMapCommand> cmds,
            ISet<int> initAnns
        )
        {
            var instStack = new Stack<NondeterministicFstTraversalInstance<TData, TOffset>>();
            List<NondeterministicFstTraversalInstance<TData, TOffset>> insts = InitializeBuffer;
            insts.Clear();
            Initialize(ref annIndex, registers, cmds, initAnns, insts);
            foreach (NondeterministicFstTraversalInstance<TData, TOffset> inst in insts)
            {
                inst.Output = ((ICloneable<TData>)Data).Clone();
                // Pair each source annotation with its clone via a lockstep preorder walk of the two
                // isomorphic forests — same result as zipping the two BFS node sequences (dict order
                // is irrelevant) but without the per-call Queue + SelectMany/Zip iterators + KVPs.
                DataStructuresExtensions.PairedPreorderTraverse(
                    Data.Annotations,
                    inst.Output.Annotations,
                    inst.Mappings,
                    (mappings, a1, a2) => mappings[a1] = a2,
                    Direction.LeftToRight
                );
                instStack.Push(inst);
            }
            return instStack;
        }
    }
}
