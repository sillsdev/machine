using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	internal class DeterministicFstTraversalMethod<TData, TOffset> : TraversalMethod<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		private readonly IFstOperations<TData, TOffset> _operations;

		public DeterministicFstTraversalMethod(IEqualityComparer<NullableValue<TOffset>[,]> registersEqualityComparer, IFstOperations<TData, TOffset> operations, Direction dir, Func<Annotation<TOffset>, bool> filter,
			State<TData, TOffset> startState, TData data, bool endAnchor, bool unification, bool useDefaults)
			: base(registersEqualityComparer, dir, filter, startState, data, endAnchor, unification, useDefaults)
		{
			_operations = operations;
		}

		public override IEnumerable<FstResult<TData, TOffset>> Traverse(ref int annIndex, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<int> initAnns)
		{
			Stack<FstInstance> instStack = InitializeStack(ref annIndex, initRegisters, initCmds, initAnns);

			var curResults = new List<FstResult<TData, TOffset>>(); 
			while (instStack.Count != 0)
			{
				FstInstance inst = instStack.Pop();

				VariableBindings varBindings = null;
				foreach (Arc<TData, TOffset> arc in inst.State.Arcs)
				{
					if (arc.Input.IsEpsilon)
					{
						TData output = inst.Output;
						IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings = inst.Mappings;
						Queue<Annotation<TOffset>> queue = inst.Queue;
						NullableValue<TOffset>[,] registers = inst.Registers;
						if (IsInstanceReuseable(inst))
						{
							if (varBindings == null)
								varBindings = inst.VariableBindings;
						}
						else
						{
							registers = (NullableValue<TOffset>[,]) inst.Registers.Clone();
							output = ((IDeepCloneable<TData>) inst.Output).DeepClone();

							Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
								.Zip(output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
							mappings = inst.Mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
							queue = new Queue<Annotation<TOffset>>(inst.Queue);
							if (varBindings == null)
								varBindings = inst.VariableBindings.DeepClone();
						}
						ExecuteOutputs(arc.Outputs, output, mappings, queue);
						instStack.Push(EpsilonAdvanceFst(inst.AnnotationIndex, registers, output, mappings, queue, varBindings, arc, curResults));
						varBindings = null;
					}
					else
					{
						if (varBindings == null)
							varBindings = IsInstanceReuseable(inst) ? inst.VariableBindings : inst.VariableBindings.DeepClone();
						if (CheckInputMatch(arc, inst.AnnotationIndex, varBindings))
						{
							TData output = inst.Output;
							IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings = inst.Mappings;
							Queue<Annotation<TOffset>> queue = inst.Queue;
							if (!IsInstanceReuseable(inst))
							{
								output = ((IDeepCloneable<TData>) inst.Output).DeepClone();

								Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
									.Zip(output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
								mappings = inst.Mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
								queue = new Queue<Annotation<TOffset>>(inst.Queue);
							}

							for (int i = 0; i < arc.Input.EnqueueCount; i++)
								queue.Enqueue(Annotations[inst.AnnotationIndex]);

							ExecuteOutputs(arc.Outputs, output, mappings, queue);

							foreach (FstInstance ni in AdvanceFst(inst.AnnotationIndex, inst.Registers, output, mappings, queue, varBindings, arc, curResults))
								instStack.Push(ni);
							break;
						}
					}
				}
			}

			return curResults;
		}

		private void ExecuteOutputs(IEnumerable<Output<TData, TOffset>> outputs, TData output, IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings,
			Queue<Annotation<TOffset>> queue)
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
				prevNewAnn = outputAction.UpdateOutput(output, outputAnn, _operations);
			}
		}

		private Stack<FstInstance> InitializeStack(ref int annIndex, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<int> initAnns)
		{
			var instStack = new Stack<FstInstance>();
			foreach (FstInstance inst in Initialize(ref annIndex, registers, cmds, initAnns,
				(state, startIndex, regs, vb) =>
					{
						TData o = ((IDeepCloneable<TData>) Data).DeepClone();
						Dictionary<Annotation<TOffset>, Annotation<TOffset>> m = Data.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
							.Zip(o.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
						var q = new Queue<Annotation<TOffset>>();
						return new FstInstance(state, startIndex, regs, o, m, q, vb);
					}))
				instStack.Push(inst);
			return instStack;
		}

		private bool IsInstanceReuseable(FstInstance inst)
		{
			return inst.State.Arcs.All(a => !a.Input.IsEpsilon);
		}

		private IEnumerable<FstInstance> AdvanceFst(int index, NullableValue<TOffset>[,] registers, TData output,
			IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings, Queue<Annotation<TOffset>> queue, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults)
		{
			return Advance(index, registers, output, varBindings, arc, curResults, null,
				(state, nextIndex, regs, vb, clone) =>
					{
						TData o = output;
						IDictionary<Annotation<TOffset>, Annotation<TOffset>> m = mappings;
						Queue<Annotation<TOffset>> q = queue;
						if (clone)
						{
							o = ((IDeepCloneable<TData>) output).DeepClone();

							Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
								.Zip(o.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
							m = mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
							q = new Queue<Annotation<TOffset>>(queue);
						}
						return new FstInstance(state, nextIndex, regs, o, m, q, vb);
					});
		}

		private FstInstance EpsilonAdvanceFst(int annIndex, NullableValue<TOffset>[,] registers, TData output, IDictionary<Annotation<TOffset>,
			Annotation<TOffset>> mappings, Queue<Annotation<TOffset>> queue, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults)
		{
			return EpsilonAdvance(annIndex, registers, output, varBindings, arc, curResults, null,
				(state, i, regs, vb) => new FstInstance(state, i, regs, output, mappings, queue, vb));
		}

		private class FstInstance : Instance
		{
			private readonly TData _output;
			private readonly IDictionary<Annotation<TOffset>, Annotation<TOffset>> _mappings;
			private readonly Queue<Annotation<TOffset>> _queue;

			public FstInstance(State<TData, TOffset> state, int annotationIndex, NullableValue<TOffset>[,] registers, TData output,
				IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings, Queue<Annotation<TOffset>> queue, VariableBindings varBindings)
				: base(state, annotationIndex, registers, varBindings)
			{
				_output = output;
				_mappings = mappings;
				_queue = queue;
			}

			public TData Output
			{
				get { return _output; }
			}

			public IDictionary<Annotation<TOffset>, Annotation<TOffset>> Mappings
			{
				get { return _mappings; }
			}

			public Queue<Annotation<TOffset>> Queue
			{
				get { return _queue; }
			}
		}
	}
}
