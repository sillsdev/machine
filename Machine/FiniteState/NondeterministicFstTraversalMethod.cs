using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	internal class NondeterministicFstTraversalMethod<TData, TOffset> : TraversalMethod<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly IFstOperations<TData, TOffset> _operations;

		public NondeterministicFstTraversalMethod(IEqualityComparer<NullableValue<TOffset>[,]> registersEqualityComparer, IFstOperations<TData, TOffset> operations, Direction dir,
			Func<Annotation<TOffset>, bool> filter, State<TData, TOffset> startState, TData data, bool endAnchor, bool unification, bool useDefaults)
			: base(registersEqualityComparer, dir, filter, startState, data, endAnchor, unification, useDefaults)
		{
			_operations = operations;
		}

		public override IEnumerable<FstResult<TData, TOffset>> Traverse(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<Annotation<TOffset>> initAnns)
		{
			Stack<FstInstance> instStack = InitializeStack(ref ann, initRegisters, initCmds, initAnns);

			var curResults = new List<FstResult<TData, TOffset>>();
			var traversed = new HashSet<Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,], Output<TData, TOffset>[]>>(
				AnonymousEqualityComparer.Create<Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,], Output<TData, TOffset>[]>>(KeyEquals, KeyGetHashCode));
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
							if (!inst.Visited.Contains(arc.Target))
							{
								TData output = inst.Output;
								IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings = inst.Mappings;
								NullableValue<TOffset>[,] registers = inst.Registers;
								ISet<State<TData, TOffset>> visited = inst.Visited;
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
									if (varBindings == null)
										varBindings = inst.VariableBindings.DeepClone();
									visited = new HashSet<State<TData, TOffset>>(inst.Visited);
								}

								Output<TData, TOffset>[] outputs = inst.Outputs;
								if (arc.Outputs.Count == 1)
								{
									Annotation<TOffset> outputAnn = mappings[inst.Annotation];
									arc.Outputs[0].UpdateOutput(output, outputAnn, _operations);
									outputs = UpdateOutputs(outputs, arc.Outputs[0]);
								}
								FstInstance newInst = EpsilonAdvanceFst(inst.Annotation, registers, output, mappings, varBindings, visited, arc, curResults, inst.Priorities, outputs);
								Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,], Output<TData, TOffset>[]> key = Tuple.Create(newInst.State, newInst.Annotation, newInst.Registers, outputs);
								if (!traversed.Contains(key))
								{
									instStack.Push(newInst);
									traversed.Add(key);
								}
								varBindings = null;
							}
						}
						else
						{
							if (varBindings == null)
								varBindings = IsInstanceReuseable(inst) ? inst.VariableBindings : inst.VariableBindings.DeepClone();
							if (CheckInputMatch(arc, inst.Annotation, varBindings))
							{
								TData output = inst.Output;
								IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings = inst.Mappings;
								if (!IsInstanceReuseable(inst))
								{
									output = ((IDeepCloneable<TData>) inst.Output).DeepClone();

									Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
										.Zip(output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
									mappings = inst.Mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
								}

								Output<TData, TOffset>[] outputs = inst.Outputs;
								if (arc.Outputs.Count == 1)
								{
									Annotation<TOffset> outputAnn = mappings[inst.Annotation];
									arc.Outputs[0].UpdateOutput(output, outputAnn, _operations);
									outputs = UpdateOutputs(outputs, arc.Outputs[0]);
								}

								foreach (FstInstance newInst in AdvanceFst(inst.Annotation, inst.Registers, output, mappings, varBindings, arc,
									curResults, inst.Priorities, outputs))
								{
									Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,], Output<TData, TOffset>[]> key = Tuple.Create(newInst.State, newInst.Annotation, newInst.Registers, outputs);
									if (!traversed.Contains(key))
									{
										instStack.Push(newInst);
										traversed.Add(key);
									}
								}

								varBindings = null;
							}
						}
					}
				}
			}

			return curResults;
		}

		private static Output<TData, TOffset>[] UpdateOutputs(Output<TData, TOffset>[] outputs, Output<TData, TOffset> output)
		{
			var o = new Output<TData, TOffset>[outputs.Length + 1];
			outputs.CopyTo(o, 0);
			o[o.Length - 1] = output;
			return o;
		}

		private bool KeyEquals(Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,], Output<TData, TOffset>[]> x,
			Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,], Output<TData, TOffset>[]> y)
		{
			return x.Item1.Equals(y.Item1) && x.Item2.Equals(y.Item2) && RegistersEqualityComparer.Equals(x.Item3, y.Item3) && x.Item4.SequenceEqual(y.Item4);
		}

		private int KeyGetHashCode(Tuple<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,], Output<TData, TOffset>[]> m)
		{
			int code = 23;
			code = code * 31 + m.Item1.GetHashCode();
			code = code * 31 + m.Item2.GetHashCode();
			code = code * 31 + RegistersEqualityComparer.GetHashCode(m.Item3);
			code = code * 31 + m.Item4.GetSequenceHashCode();
			return code;
		}

		private Stack<FstInstance> InitializeStack(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<Annotation<TOffset>> initAnns)
		{
			var instStack = new Stack<FstInstance>();
			foreach (FstInstance inst in Initialize(ref ann, registers, cmds, initAnns, (state, startAnn, regs, vb) =>
				{
					TData o = ((IDeepCloneable<TData>) Data).DeepClone();
					Dictionary<Annotation<TOffset>, Annotation<TOffset>> m = Data.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
						.Zip(o.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
					return new FstInstance(state, startAnn, regs, o, m, vb, new HashSet<State<TData, TOffset>>(), new int[0], new Output<TData, TOffset>[0]);
				}))
			{
				instStack.Push(inst);
			}
			return instStack;
		}

		private IEnumerable<FstInstance> AdvanceFst(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output,
			IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int[] priorities, Output<TData, TOffset>[] outputs)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);
			return Advance(ann, registers, output, varBindings, arc, curResults, priorities,
				(state, nextAnn, regs, vb, clone) =>
					{
						TData o = output;
						IDictionary<Annotation<TOffset>, Annotation<TOffset>> m = mappings;
						if (clone)
						{
							o = ((IDeepCloneable<TData>) output).DeepClone();

							Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
								.Zip(o.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
							m = mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
						}
						return new FstInstance(state, nextAnn, regs, o, m, vb, new HashSet<State<TData, TOffset>>(), priorities, outputs);
					});
		}

		private FstInstance EpsilonAdvanceFst(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output, IDictionary<Annotation<TOffset>,
			Annotation<TOffset>> mappings, VariableBindings varBindings, ISet<State<TData, TOffset>> visited, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int[] priorities, Output<TData, TOffset>[] outputs)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);
			visited.Add(arc.Target);
			return EpsilonAdvance(ann, registers, output, varBindings, arc, curResults, priorities,
				(state, a, regs, vb) => new FstInstance(state, a, regs, output, mappings, varBindings, visited, priorities, outputs));
		}

		private bool IsInstanceReuseable(FstInstance inst)
		{
			return inst.State.Arcs.Count <= 1;
		}

		private class FstInstance : Instance
		{
			private readonly TData _output;
			private readonly IDictionary<Annotation<TOffset>, Annotation<TOffset>> _mappings;
			private readonly ISet<State<TData, TOffset>> _visited;
			private readonly int[] _priorities;
			private readonly Output<TData, TOffset>[] _outputs; 

			public FstInstance(State<TData, TOffset> state, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output,
				IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings, VariableBindings varBindings,
				ISet<State<TData, TOffset>> visited, int[] priorities, Output<TData, TOffset>[] outputs)
				: base(state, ann, registers, varBindings)
			{
				_output = output;
				_mappings = mappings;
				_visited = visited;
				_priorities = priorities;
				_outputs = outputs;
			}

			public TData Output
			{
				get { return _output; }
			}

			public IDictionary<Annotation<TOffset>, Annotation<TOffset>> Mappings
			{
				get { return _mappings; }
			}

			public ISet<State<TData, TOffset>> Visited
			{
				get { return _visited; }
			}

			public int[] Priorities
			{
				get { return _priorities; }
			}

			public Output<TData, TOffset>[] Outputs
			{
				get { return _outputs; }
			}
		}
	}
}
