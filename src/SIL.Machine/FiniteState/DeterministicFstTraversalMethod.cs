using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.FiniteState
{
    internal class DeterministicFstTraversalMethod<TData, TOffset>
        : TraversalMethodBase<TData, TOffset, DeterministicFstTraversalInstance<TData, TOffset>>
        where TData : IAnnotatedData<TOffset>
    {
        public DeterministicFstTraversalMethod(
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
            Stack<DeterministicFstTraversalInstance<TData, TOffset>> instStack = InitializeStack(
                ref annIndex,
                initRegisters,
                initCmds,
                initAnns
            );

            var curResults = new List<FstResult<TData, TOffset>>();
            while (instStack.Count != 0)
            {
                DeterministicFstTraversalInstance<TData, TOffset> inst = instStack.Pop();

                bool releaseInstance = true;
                VariableBindings varBindings = null;
                int i = 0;
                foreach (Arc<TData, TOffset> arc in inst.State.Arcs)
                {
                    bool isInstReusable = i == inst.State.Arcs.Count - 1;
                    if (arc.Input.IsEpsilon)
                    {
                        DeterministicFstTraversalInstance<TData, TOffset> ti;
                        if (isInstReusable)
                        {
                            ti = inst;
                        }
                        else
                        {
                            ti = CopyInstance(inst);
                            if (inst.VariableBindings != null)
                                ti.VariableBindings = inst.VariableBindings.Clone();
                        }

                        ExecuteOutputs(arc.Outputs, ti.Output, ti.Mappings, ti.Queue);
                        instStack.Push(EpsilonAdvance(ti, arc, curResults));
                        if (isInstReusable)
                            releaseInstance = false;
                        varBindings = null;
                    }
                    else
                    {
                        if (inst.VariableBindings != null && varBindings == null)
                            varBindings = isInstReusable ? inst.VariableBindings : inst.VariableBindings.Clone();
                        if (CheckInputMatch(arc, inst.AnnotationIndex, varBindings))
                        {
                            for (int j = 0; j < arc.Input.EnqueueCount; j++)
                                inst.Queue.Enqueue(Annotations[inst.AnnotationIndex]);

                            ExecuteOutputs(arc.Outputs, inst.Output, inst.Mappings, inst.Queue);

                            foreach (
                                DeterministicFstTraversalInstance<TData, TOffset> ni in Advance(
                                    inst,
                                    varBindings,
                                    arc,
                                    curResults
                                )
                            )
                            {
                                instStack.Push(ni);
                            }

                            releaseInstance = false;
                            break;
                        }
                    }
                    i++;
                }

                if (releaseInstance)
                    ReleaseInstance(inst);
            }

            return curResults;
        }

        protected override DeterministicFstTraversalInstance<TData, TOffset> CreateInstance()
        {
            return new DeterministicFstTraversalInstance<TData, TOffset>(Fst.RegisterCount);
        }

        private void ExecuteOutputs(
            IEnumerable<Output<TData, TOffset>> outputs,
            TData output,
            IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings,
            Queue<Annotation<TOffset>> queue
        )
        {
            Annotation<TOffset> prevNewAnn = null;
            foreach (Output<TData, TOffset> outputAction in outputs)
            {
                Annotation<TOffset> outputAnn;
                if (outputAction.UsePrevNewAnnotation && prevNewAnn != null)
                {
                    outputAnn = prevNewAnn;
                }
                else
                {
                    Annotation<TOffset> inputAnn = queue.Dequeue();
                    outputAnn = mappings[inputAnn];
                }
                prevNewAnn = outputAction.UpdateOutput(output, outputAnn, Fst.Operations);
            }
        }

        private Stack<DeterministicFstTraversalInstance<TData, TOffset>> InitializeStack(
            ref int annIndex,
            Register<TOffset>[,] registers,
            IList<TagMapCommand> cmds,
            ISet<int> initAnns
        )
        {
            var instStack = new Stack<DeterministicFstTraversalInstance<TData, TOffset>>();
            foreach (
                DeterministicFstTraversalInstance<TData, TOffset> inst in Initialize(
                    ref annIndex,
                    registers,
                    cmds,
                    initAnns
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
