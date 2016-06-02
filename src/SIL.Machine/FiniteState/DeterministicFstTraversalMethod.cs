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
			Stack<DeterministicFstInstance> instStack = InitializeStack(ref annIndex, initRegisters, initCmds, initAnns);

			var curResults = new List<FstResult<TData, TOffset>>(); 
			while (instStack.Count != 0)
			{
				DeterministicFstInstance inst = instStack.Pop();

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
							output = ((ICloneable<TData>) inst.Output).Clone();

							Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
								.Zip(output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
							mappings = inst.Mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
							queue = new Queue<Annotation<TOffset>>(inst.Queue);
							if (varBindings == null)
								varBindings = inst.VariableBindings.Clone();
						}
						ExecuteOutputs(arc.Outputs, output, mappings, queue);
						instStack.Push(EpsilonAdvanceFst(inst.AnnotationIndex, registers, output, mappings, queue, varBindings, arc, curResults));
						varBindings = null;
					}
					else
					{
						if (varBindings == null)
							varBindings = IsInstanceReuseable(inst) ? inst.VariableBindings : inst.VariableBindings.Clone();
						if (CheckInputMatch(arc, inst.AnnotationIndex, varBindings))
						{
							TData output = inst.Output;
							IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings = inst.Mappings;
							Queue<Annotation<TOffset>> queue = inst.Queue;
							if (!IsInstanceReuseable(inst))
							{
								output = ((ICloneable<TData>) inst.Output).Clone();

								Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
									.Zip(output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
								mappings = inst.Mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
								queue = new Queue<Annotation<TOffset>>(inst.Queue);
							}

							for (int i = 0; i < arc.Input.EnqueueCount; i++)
								queue.Enqueue(Annotations[inst.AnnotationIndex]);

							ExecuteOutputs(arc.Outputs, output, mappings, queue);

							foreach (DeterministicFstInstance ni in AdvanceFst(inst.AnnotationIndex, inst.Registers, output, mappings, queue, varBindings, arc, curResults))
								instStack.Push(ni);
							break;
						}
					}
				}
			}

			return curResults;
		}

		private static DeterministicFstInstance CreateInstance()
		{
			return new DeterministicFstInstance();
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

		private Stack<DeterministicFstInstance> InitializeStack(ref int annIndex, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<int> initAnns)
		{
			var instStack = new Stack<DeterministicFstInstance>();
			foreach (DeterministicFstInstance inst in Initialize(ref annIndex, registers, cmds, initAnns, CreateInstance))
			{
				inst.Output = ((ICloneable<TData>) Data).Clone();
				inst.Mappings = Data.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
					.Zip(inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
				inst.Queue = new Queue<Annotation<TOffset>>();
				instStack.Push(inst);
			}
			return instStack;
		}

		private bool IsInstanceReuseable(DeterministicFstInstance inst)
		{
			return inst.State.Arcs.All(a => !a.Input.IsEpsilon);
		}

		private IEnumerable<DeterministicFstInstance> AdvanceFst(int index, NullableValue<TOffset>[,] registers, TData output,
			IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings, Queue<Annotation<TOffset>> queue, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults)
		{
			bool clone = false;
			foreach (DeterministicFstInstance inst in Advance(index, registers, output, varBindings, arc, curResults, null, CreateInstance))
			{
				TData o = output;
				IDictionary<Annotation<TOffset>, Annotation<TOffset>> m = mappings;
				Queue<Annotation<TOffset>> q = queue;
				if (clone)
				{
					o = ((ICloneable<TData>) output).Clone();

					Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
						.Zip(o.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
					m = mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
					q = new Queue<Annotation<TOffset>>(queue);
				}
				inst.Output = o;
				inst.Mappings = m;
				inst.Queue = q;
				yield return inst;
				clone = true;
			}
		}

		private DeterministicFstInstance EpsilonAdvanceFst(int annIndex, NullableValue<TOffset>[,] registers, TData output, IDictionary<Annotation<TOffset>,
			Annotation<TOffset>> mappings, Queue<Annotation<TOffset>> queue, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults)
		{
			DeterministicFstInstance inst = EpsilonAdvance(annIndex, registers, output, varBindings, arc, curResults, null, CreateInstance);
			inst.Output = output;
			inst.Mappings = mappings;
			inst.Queue = queue;
			return inst;
		}

		private class DeterministicFstInstance : Instance
		{
			public TData Output { get; set; }
			public IDictionary<Annotation<TOffset>, Annotation<TOffset>> Mappings { get; set; }
			public Queue<Annotation<TOffset>> Queue { get; set; }
		}
	}
}
