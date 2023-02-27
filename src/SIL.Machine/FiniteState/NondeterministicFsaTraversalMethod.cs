using System;
using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.FiniteState
{
    internal class NondeterministicFsaTraversalMethod<TData, TOffset>
        : TraversalMethodBase<TData, TOffset, NondeterministicFsaTraversalInstance<TData, TOffset>>
        where TData : IAnnotatedData<TOffset>
    {
        public NondeterministicFsaTraversalMethod(
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
            IList<TagMapCommand> initCmds,
            ISet<int> initAnns
        )
        {
            Stack<NondeterministicFsaTraversalInstance<TData, TOffset>> instStack = InitializeStack(
                ref annIndex,
                initRegisters,
                initCmds,
                initAnns
            );

            var curResults = new List<FstResult<TData, TOffset>>();
            var traversed = new HashSet<Tuple<State<TData, TOffset>, int, Register<TOffset>[,]>>(
                AnonymousEqualityComparer.Create<Tuple<State<TData, TOffset>, int, Register<TOffset>[,]>>(
                    KeyEquals,
                    KeyGetHashCode
                )
            );
            while (instStack.Count != 0)
            {
                NondeterministicFsaTraversalInstance<TData, TOffset> inst = instStack.Pop();

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
                            NondeterministicFsaTraversalInstance<TData, TOffset> ti;
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

                            ti.Visited.Add(arc.Target);
                            NondeterministicFsaTraversalInstance<TData, TOffset> newInst = EpsilonAdvance(
                                ti,
                                arc,
                                curResults
                            );
                            Tuple<State<TData, TOffset>, int, Register<TOffset>[,]> key = Tuple.Create(
                                newInst.State,
                                newInst.AnnotationIndex,
                                newInst.Registers
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
                            NondeterministicFsaTraversalInstance<TData, TOffset> ti = isInstReusable
                                ? inst
                                : CopyInstance(inst);

                            foreach (
                                NondeterministicFsaTraversalInstance<TData, TOffset> newInst in Advance(
                                    ti,
                                    varBindings,
                                    arc,
                                    curResults
                                )
                            )
                            {
                                newInst.Visited.Clear();
                                Tuple<State<TData, TOffset>, int, Register<TOffset>[,]> key = Tuple.Create(
                                    newInst.State,
                                    newInst.AnnotationIndex,
                                    newInst.Registers
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

        protected override NondeterministicFsaTraversalInstance<TData, TOffset> CreateInstance()
        {
            return new NondeterministicFsaTraversalInstance<TData, TOffset>(Fst.RegisterCount);
        }

        private bool KeyEquals(
            Tuple<State<TData, TOffset>, int, Register<TOffset>[,]> x,
            Tuple<State<TData, TOffset>, int, Register<TOffset>[,]> y
        )
        {
            return x.Item1.Equals(y.Item1)
                && x.Item2.Equals(y.Item2)
                && Fst.RegistersEqualityComparer.Equals(x.Item3, y.Item3);
        }

        private int KeyGetHashCode(Tuple<State<TData, TOffset>, int, Register<TOffset>[,]> m)
        {
            int code = 23;
            code = code * 31 + m.Item1.GetHashCode();
            code = code * 31 + m.Item2.GetHashCode();
            code = code * 31 + Fst.RegistersEqualityComparer.GetHashCode(m.Item3);
            return code;
        }

        private Stack<NondeterministicFsaTraversalInstance<TData, TOffset>> InitializeStack(
            ref int annIndex,
            Register<TOffset>[,] registers,
            IList<TagMapCommand> cmds,
            ISet<int> initAnns
        )
        {
            var instStack = new Stack<NondeterministicFsaTraversalInstance<TData, TOffset>>();
            foreach (
                NondeterministicFsaTraversalInstance<TData, TOffset> inst in Initialize(
                    ref annIndex,
                    registers,
                    cmds,
                    initAnns
                )
            )
                instStack.Push(inst);
            return instStack;
        }
    }
}
