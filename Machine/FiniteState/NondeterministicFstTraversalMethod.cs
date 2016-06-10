using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	internal class NondeterministicFstTraversalMethod<TData, TOffset> : TraversalMethod<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		private readonly IFstOperations<TData, TOffset> _operations;

		public NondeterministicFstTraversalMethod(IEqualityComparer<NullableValue<TOffset>[,]> registersEqualityComparer, IFstOperations<TData, TOffset> operations, Direction dir,
			Func<Annotation<TOffset>, bool> filter, State<TData, TOffset> startState, TData data, bool endAnchor, bool unification, bool useDefaults, bool ignoreVariables)
			: base(registersEqualityComparer, dir, filter, startState, data, endAnchor, unification, useDefaults, ignoreVariables)
		{
			_operations = operations;
		}

		public override IEnumerable<FstResult<TData, TOffset>> Traverse(ref int annIndex, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<int> initAnns)
		{
			Stack<NondeterministicFstInstance> instStack = InitializeStack(ref annIndex, initRegisters, initCmds, initAnns);

			var curResults = new List<FstResult<TData, TOffset>>();
			var traversed = new HashSet<Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,], Output<TData, TOffset>[]>>(
				AnonymousEqualityComparer.Create<Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,], Output<TData, TOffset>[]>>(KeyEquals, KeyGetHashCode));
			while (instStack.Count != 0)
			{
				NondeterministicFstInstance inst = instStack.Pop();

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
								if (!IgnoreVariables && varBindings == null)
									varBindings = inst.VariableBindings;
							}
							else
							{
								registers = (NullableValue<TOffset>[,]) inst.Registers.Clone();

								output = ((IDeepCloneable<TData>) inst.Output).DeepClone();

								Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
									.Zip(output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
								mappings = inst.Mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
								if (!IgnoreVariables && varBindings == null)
									varBindings = inst.VariableBindings.DeepClone();
								visited = new HashSet<State<TData, TOffset>>(inst.Visited);
							}

							Output<TData, TOffset>[] outputs = inst.Outputs;
							if (arc.Outputs.Count == 1)
							{
								Annotation<TOffset> outputAnn = mappings[Annotations[inst.AnnotationIndex]];
								arc.Outputs[0].UpdateOutput(output, outputAnn, _operations);
								outputs = UpdateOutputs(outputs, arc.Outputs[0]);
							}
							NondeterministicFstInstance newInst = EpsilonAdvanceFst(inst.AnnotationIndex, registers, output, mappings, varBindings, visited, arc,
								curResults, inst.Priorities, outputs);
							Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,], Output<TData, TOffset>[]> key = Tuple.Create(newInst.State, newInst.AnnotationIndex,
								newInst.Registers, outputs);
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
						if (!IgnoreVariables && varBindings == null)
							varBindings = IsInstanceReuseable(inst) ? inst.VariableBindings : inst.VariableBindings.DeepClone();
						if (CheckInputMatch(arc, inst.AnnotationIndex, varBindings))
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
								Annotation<TOffset> outputAnn = mappings[Annotations[inst.AnnotationIndex]];
								arc.Outputs[0].UpdateOutput(output, outputAnn, _operations);
								outputs = UpdateOutputs(outputs, arc.Outputs[0]);
							}

							foreach (NondeterministicFstInstance newInst in AdvanceFst(inst.AnnotationIndex, inst.Registers, output, mappings, varBindings, arc,
								curResults, inst.Priorities, outputs))
							{
								Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,], Output<TData, TOffset>[]> key = Tuple.Create(newInst.State, newInst.AnnotationIndex,
									newInst.Registers, outputs);
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

			return curResults;
		}

		private static NondeterministicFstInstance CreateInstance()
		{
			return new NondeterministicFstInstance();
		}

		private static Output<TData, TOffset>[] UpdateOutputs(Output<TData, TOffset>[] outputs, Output<TData, TOffset> output)
		{
			var o = new Output<TData, TOffset>[outputs.Length + 1];
			outputs.CopyTo(o, 0);
			o[o.Length - 1] = output;
			return o;
		}

		private bool KeyEquals(Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,], Output<TData, TOffset>[]> x,
			Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,], Output<TData, TOffset>[]> y)
		{
			return x.Item1.Equals(y.Item1) && x.Item2.Equals(y.Item2) && RegistersEqualityComparer.Equals(x.Item3, y.Item3) && x.Item4.SequenceEqual(y.Item4);
		}

		private int KeyGetHashCode(Tuple<State<TData, TOffset>, int, NullableValue<TOffset>[,], Output<TData, TOffset>[]> m)
		{
			int code = 23;
			code = code * 31 + m.Item1.GetHashCode();
			code = code * 31 + m.Item2.GetHashCode();
			code = code * 31 + RegistersEqualityComparer.GetHashCode(m.Item3);
			code = code * 31 + m.Item4.GetSequenceHashCode();
			return code;
		}

		private Stack<NondeterministicFstInstance> InitializeStack(ref int annIndex, NullableValue<TOffset>[,] registers, IList<TagMapCommand> cmds, ISet<int> initAnns)
		{
			var instStack = new Stack<NondeterministicFstInstance>();
			foreach (NondeterministicFstInstance inst in Initialize(ref annIndex, registers, cmds, initAnns, CreateInstance))
			{
				inst.Output = ((IDeepCloneable<TData>) Data).DeepClone();
				inst.Mappings = Data.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
					.Zip(inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
				inst.Visited = new HashSet<State<TData, TOffset>>();
				inst.Priorities = new int[0];
				inst.Outputs = new Output<TData, TOffset>[0];
				instStack.Push(inst);
			}
			return instStack;
		}

		private IEnumerable<NondeterministicFstInstance> AdvanceFst(int annIndex, NullableValue<TOffset>[,] registers, TData output,
			IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int[] priorities, Output<TData, TOffset>[] outputs)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);

			bool clone = false;
			foreach (NondeterministicFstInstance inst in Advance(annIndex, registers, output, varBindings, arc, curResults, priorities, CreateInstance))
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
				inst.Output = o;
				inst.Mappings = m;
				inst.Visited = new HashSet<State<TData, TOffset>>();
				inst.Priorities = priorities;
				inst.Outputs = outputs;
				yield return inst;
				clone = true;
			}
		}

		private NondeterministicFstInstance EpsilonAdvanceFst(int annIndex, NullableValue<TOffset>[,] registers, TData output, IDictionary<Annotation<TOffset>,
			Annotation<TOffset>> mappings, VariableBindings varBindings, ISet<State<TData, TOffset>> visited, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int[] priorities, Output<TData, TOffset>[] outputs)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);
			visited.Add(arc.Target);

			NondeterministicFstInstance inst = EpsilonAdvance(annIndex, registers, output, varBindings, arc, curResults, priorities, CreateInstance);
			inst.Output = output;
			inst.Mappings = mappings;
			inst.Visited = visited;
			inst.Priorities = priorities;
			inst.Outputs = outputs;
			return inst;
		}

		private bool IsInstanceReuseable(NondeterministicFstInstance inst)
		{
			return inst.State.Arcs.Count <= 1;
		}

		private class NondeterministicFstInstance : Instance
		{
			public ISet<State<TData, TOffset>> Visited { get; set; }
			public int[] Priorities { get; set; }
			public TData Output { get; set; }
			public IDictionary<Annotation<TOffset>, Annotation<TOffset>> Mappings { get; set; }
			public Output<TData, TOffset>[] Outputs { get; set; }
		}
	}
}
