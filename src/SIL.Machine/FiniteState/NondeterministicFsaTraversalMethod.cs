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
        public NondeterministicFsaTraversalMethod(Fst<TData, TOffset> fst)
            : base(fst) { }

        public override List<FstResult<TData, TOffset>> Traverse(
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
            // The dedup key is a value type (was Tuple<,,>): the HashSet stores it inline in its slot
            // array, so there is no per-push heap object — `traversed.Add` is the hottest allocation in
            // nondeterministic traversal. Byte-identical equality/hash (same fields, same comparers).
            var traversed = new HashSet<TraversalKey>(
                AnonymousEqualityComparer.Create<TraversalKey>(KeyEquals, KeyGetHashCode)
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
                        if (!inst.IsVisited(arc.Target))
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

                            ti.MarkVisited(arc.Target);
                            NondeterministicFsaTraversalInstance<TData, TOffset> newInst = EpsilonAdvance(
                                ti,
                                arc,
                                curResults
                            );
                            var key = new TraversalKey(newInst.State, newInst.AnnotationIndex, newInst.Registers);
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
                                newInst.ClearVisited();
                                var key = new TraversalKey(newInst.State, newInst.AnnotationIndex, newInst.Registers);
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

        protected override NondeterministicFsaTraversalInstance<TData, TOffset> CreateInstance()
        {
            return new NondeterministicFsaTraversalInstance<TData, TOffset>(Fst.RegisterCount);
        }

        // Value-type dedup key (was Tuple<State,int,Register[,]>): stored inline in the `traversed`
        // HashSet so a push no longer allocates a heap Tuple. Holds the instance's live Registers by
        // reference exactly as the Tuple did (same reference + hash-at-Add semantics).
        private readonly struct TraversalKey
        {
            public readonly State<TData, TOffset> State;
            public readonly int AnnotationIndex;
            public readonly Register<TOffset>[,] Registers;

            public TraversalKey(State<TData, TOffset> state, int annotationIndex, Register<TOffset>[,] registers)
            {
                State = state;
                AnnotationIndex = annotationIndex;
                Registers = registers;
            }
        }

        private bool KeyEquals(TraversalKey x, TraversalKey y)
        {
            return x.State.Equals(y.State)
                && x.AnnotationIndex.Equals(y.AnnotationIndex)
                && Fst.RegistersEqualityComparer.Equals(x.Registers, y.Registers);
        }

        private int KeyGetHashCode(TraversalKey m)
        {
            int code = 23;
            code = code * 31 + m.State.GetHashCode();
            code = code * 31 + m.AnnotationIndex.GetHashCode();
            code = code * 31 + Fst.RegistersEqualityComparer.GetHashCode(m.Registers);
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
            List<NondeterministicFsaTraversalInstance<TData, TOffset>> insts = InitializeBuffer;
            insts.Clear();
            Initialize(ref annIndex, registers, cmds, initAnns, insts);
            foreach (NondeterministicFsaTraversalInstance<TData, TOffset> inst in insts)
                instStack.Push(inst);

            return instStack;
        }
    }
}
