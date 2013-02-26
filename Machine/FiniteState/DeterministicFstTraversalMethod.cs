using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	internal class DeterministicFstTraversalMethod<TData, TOffset> : FstTraversalMethod<TData, TOffset> where TData : IData<TOffset>, IDeepCloneable<TData>
	{
		public DeterministicFstTraversalMethod(IFstOperations<TData, TOffset> operations, Direction dir, Func<Annotation<TOffset>, bool> filter, State<TData, TOffset> startState, TData data, bool endAnchor,
			bool unification, bool useDefaults)
			: base(operations, dir, filter, startState, data, endAnchor, unification, useDefaults)
		{
		}

		public override IEnumerable<FstResult<TData, TOffset>> Traverse(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<Annotation<TOffset>> initAnns)
		{
			Stack<FstInstance> instStack = InitializeStack(ref ann, initRegisters, initCmds, initAnns);

			var curResults = new List<FstResult<TData, TOffset>>(); 
			while (instStack.Count != 0)
			{
				FstInstance inst = instStack.Pop();

				if (inst.Annotation != null)
				{
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
								output = inst.Output.DeepClone();

								Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
									.Zip(output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
								mappings = inst.Mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
								queue = new Queue<Annotation<TOffset>>(inst.Queue);
								if (varBindings == null)
									varBindings = inst.VariableBindings.DeepClone();
							}
							ExecuteOutputs(arc.Outputs, output, mappings, queue);
							instStack.Push(EpsilonAdvanceFst(inst.Annotation, registers, output, mappings, queue, varBindings, arc, curResults, inst.Depth));
							varBindings = null;
						}
						else
						{
							if (varBindings == null)
								varBindings = IsInstanceReuseable(inst) ? inst.VariableBindings : inst.VariableBindings.DeepClone();
							if (CheckInputMatch(arc, inst.Annotation, varBindings))
							{
								TData output = inst.Output;
								IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings = inst.Mappings;
								Queue<Annotation<TOffset>> queue = inst.Queue;
								if (!IsInstanceReuseable(inst))
								{
									output = inst.Output.DeepClone();

									Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
										.Zip(output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
									mappings = inst.Mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
									queue = new Queue<Annotation<TOffset>>(inst.Queue);
								}

								for (int i = 0; i < arc.Input.EnqueueCount; i++)
									queue.Enqueue(inst.Annotation);

								ExecuteOutputs(arc.Outputs, output, mappings, queue);

								foreach (FstInstance ni in AdvanceFst(inst.Annotation, inst.Registers, output, mappings, queue, varBindings, arc, curResults, inst.Depth))
									instStack.Push(ni);
								break;
							}
						}
					}
				}
			}

			return curResults;
		}

		private Stack<FstInstance> InitializeStack(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<Annotation<TOffset>> initAnns)
		{
			var instStack = new Stack<FstInstance>();
			foreach (FstInstance inst in Initialize(ref ann, registers, cmds, initAnns, 0,
				(state, startAnn, regs, vb, cd) =>
					{
						TData o = Data.DeepClone();
						Dictionary<Annotation<TOffset>, Annotation<TOffset>> m = Data.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
										   .Zip(o.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
						var q = new Queue<Annotation<TOffset>>();
						return new FstInstance(state, startAnn, regs, o, m, q, vb, cd);
					}))
				instStack.Push(inst);
			return instStack;
		}

		private bool IsInstanceReuseable(FstInstance inst)
		{
			return inst.State.Arcs.All(a => !a.Input.IsEpsilon);
		}

		private IEnumerable<FstInstance> AdvanceFst(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output,
			IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings, Queue<Annotation<TOffset>> queue, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int depth)
		{
			return Advance(ann, registers, output, varBindings, arc, curResults, depth, null,
				(state, nextAnn, regs, vb, cd, clone) =>
					{
						TData o = output;
						IDictionary<Annotation<TOffset>, Annotation<TOffset>> m = mappings;
						Queue<Annotation<TOffset>> q = queue;
						if (clone)
						{
							o = output.DeepClone();

							Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
								.Zip(o.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
							m = mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
							q = new Queue<Annotation<TOffset>>(queue);
						}
						return new FstInstance(state, nextAnn, regs, o, m, q, vb, cd);
					});
		}

		private FstInstance EpsilonAdvanceFst(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output, IDictionary<Annotation<TOffset>,
			Annotation<TOffset>> mappings, Queue<Annotation<TOffset>> queue, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int depth)
		{
			return EpsilonAdvance(ann, registers, output, varBindings, arc, curResults, depth, null,
				(state, a, regs, vb, cd) => new FstInstance(state, a, regs, output, mappings, queue, vb, cd));
		}

		private class FstInstance : Instance
		{
			private readonly TData _output;
			private readonly IDictionary<Annotation<TOffset>, Annotation<TOffset>> _mappings;
			private readonly Queue<Annotation<TOffset>> _queue;

			public FstInstance(State<TData, TOffset> state, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output,
				IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings, Queue<Annotation<TOffset>> queue, VariableBindings varBindings, int depth)
				: base(state, ann, registers, varBindings, depth)
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
