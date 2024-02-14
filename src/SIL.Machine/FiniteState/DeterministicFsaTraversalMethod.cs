using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
    internal class DeterministicFsaTraversalMethod<TData, TOffset>
        : TraversalMethodBase<TData, TOffset, DeterministicFsaTraversalInstance<TData, TOffset>>
        where TData : IAnnotatedData<TOffset>
    {
        public DeterministicFsaTraversalMethod(
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
            Stack<DeterministicFsaTraversalInstance<TData, TOffset>> instStack = InitializeStack(
                ref annIndex,
                initRegisters,
                initCmds,
                initAnns
            );

            var curResults = new List<FstResult<TData, TOffset>>();
            while (instStack.Count != 0)
            {
                DeterministicFsaTraversalInstance<TData, TOffset> inst = instStack.Pop();

                bool releaseInstance = true;
                foreach (Arc<TData, TOffset> arc in inst.State.Arcs)
                {
                    if (CheckInputMatch(arc, inst.AnnotationIndex, inst.VariableBindings))
                    {
                        foreach (
                            DeterministicFsaTraversalInstance<TData, TOffset> ni in Advance(
                                inst,
                                inst.VariableBindings,
                                arc,
                                curResults
                            )
                        )
                            instStack.Push(ni);
                        releaseInstance = false;
                        break;
                    }
                }

                if (releaseInstance)
                    ReleaseInstance(inst);
            }

            return curResults;
        }

        protected override DeterministicFsaTraversalInstance<TData, TOffset> CreateInstance()
        {
            return new DeterministicFsaTraversalInstance<TData, TOffset>(Fst.RegisterCount);
        }

        private Stack<DeterministicFsaTraversalInstance<TData, TOffset>> InitializeStack(
            ref int annIndex,
            Register<TOffset>[,] registers,
            IList<TagMapCommand> cmds,
            ISet<int> initAnns
        )
        {
            var instStack = new Stack<DeterministicFsaTraversalInstance<TData, TOffset>>();
            foreach (
                DeterministicFsaTraversalInstance<TData, TOffset> inst in Initialize(
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
