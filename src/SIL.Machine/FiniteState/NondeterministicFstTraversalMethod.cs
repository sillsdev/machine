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
        public NondeterministicFstTraversalMethod(
            Fst<TData, TOffset> fst,
            TData data,
            VariableBindings varBindings,
            bool startAnchor,
            bool endAnchor,
            bool useDefaults
        )
            : base(fst, data, varBindings, startAnchor, endAnchor, useDefaults) { }

        public override IEnumerable<FstResult<TData, TOffset>> Traverse(
            ref int annIndex,
            Register<TOffset>[,] initRegisters,
            IList<TagMapCommand> initCommands,
            ISet<int> initAnnotations
        )
        {
            Stack<NondeterministicFstTraversalInstance<TData, TOffset>> instStack = InitializeStack(
                ref annIndex,
                initRegisters,
                initCommands,
                initAnnotations
            );

            var curResults = new List<FstResult<TData, TOffset>>();
            var traversed = new HashSet<
                Tuple<State<TData, TOffset>, int, Register<TOffset>[,], Output<TData, TOffset>[]>
            >(
                AnonymousEqualityComparer.Create<
                    Tuple<State<TData, TOffset>, int, Register<TOffset>[,], Output<TData, TOffset>[]>
                >(KeyEquals, KeyGetHashCode)
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
                        if (!inst.Visited.Contains(arc.Target))
                        {
                            NondeterministicFstTraversalInstance<TData, TOffset> ti;
                            if (isInstReusable)
                                ti = inst;
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

                            ti.Visited.Add(arc.Target);
                            NondeterministicFstTraversalInstance<TData, TOffset> newInst = EpsilonAdvance(
                                inst,
                                arc,
                                curResults
                            );
                            var key = Tuple.Create(
                                newInst.State,
                                newInst.AnnotationIndex,
                                newInst.Registers,
                                newInst.Outputs.ToArray()
                            );
                            if (!traversed.Contains(key))
                            {
                                instStack.Push(newInst);
                                traversed.Add(key);
                            }
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
                                newInst.Visited.Clear();
                                var key = Tuple.Create(
                                    newInst.State,
                                    newInst.AnnotationIndex,
                                    newInst.Registers,
                                    newInst.Outputs.ToArray()
                                );
                                if (!traversed.Contains(key))
                                {
                                    instStack.Push(newInst);
                                    traversed.Add(key);
                                }
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

            return curResults;
        }

        protected override NondeterministicFstTraversalInstance<TData, TOffset> CreateInstance()
        {
            return new NondeterministicFstTraversalInstance<TData, TOffset>(Fst.RegisterCount);
        }

        private bool KeyEquals(
            Tuple<State<TData, TOffset>, int, Register<TOffset>[,], Output<TData, TOffset>[]> x,
            Tuple<State<TData, TOffset>, int, Register<TOffset>[,], Output<TData, TOffset>[]> y
        )
        {
            return x.Item1.Equals(y.Item1)
                && x.Item2.Equals(y.Item2)
                && Fst.RegistersEqualityComparer.Equals(x.Item3, y.Item3)
                && x.Item4.SequenceEqual(y.Item4);
        }

        private int KeyGetHashCode(Tuple<State<TData, TOffset>, int, Register<TOffset>[,], Output<TData, TOffset>[]> m)
        {
            int code = 23;
            code = code * 31 + m.Item1.GetHashCode();
            code = code * 31 + m.Item2.GetHashCode();
            code = code * 31 + Fst.RegistersEqualityComparer.GetHashCode(m.Item3);
            code = code * 31 + m.Item4.GetSequenceHashCode();
            return code;
        }

        private Stack<NondeterministicFstTraversalInstance<TData, TOffset>> InitializeStack(
            ref int annIndex,
            Register<TOffset>[,] registers,
            IList<TagMapCommand> commands,
            ISet<int> initAnnotations
        )
        {
            var instStack = new Stack<NondeterministicFstTraversalInstance<TData, TOffset>>();
            foreach (
                NondeterministicFstTraversalInstance<TData, TOffset> inst in Initialize(
                    ref annIndex,
                    registers,
                    commands,
                    initAnnotations
                )
            )
            {
                inst.Output = ((ICloneable<TData>)Data).Clone();
                inst.Mappings.AddRange(
                    Data.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
                        .Zip(
                            inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst()),
                            (a1, a2) => new KeyValuePair<Annotation<TOffset>, Annotation<TOffset>>(a1, a2)
                        )
                );
                instStack.Push(inst);
            }
            return instStack;
        }
    }
}
