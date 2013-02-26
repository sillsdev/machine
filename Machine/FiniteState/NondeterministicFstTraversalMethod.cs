using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	internal class NondeterministicFstTraversalMethod<TData, TOffset> : TraversalMethod<TData, TOffset> where TData : IData<TOffset>, IDeepCloneable<TData>
	{
		private readonly IFstOperations<TData, TOffset> _operations;

		public NondeterministicFstTraversalMethod(IFstOperations<TData, TOffset> operations, Direction dir, Func<Annotation<TOffset>, bool> filter, State<TData, TOffset> startState, TData data,
			bool endAnchor, bool unification, bool useDefaults)
			: base(dir, filter, startState, data, endAnchor, unification, useDefaults)
		{
			_operations = operations;
		}

		public override IEnumerable<FstResult<TData, TOffset>> Traverse(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<Annotation<TOffset>> initAnns)
		{
			ConcurrentQueue<FstInstance> from = InitializeQueue(ref ann, initRegisters, initCmds, initAnns);

			var curResults = new ConcurrentBag<FstResult<TData, TOffset>>();
			while (!from.IsEmpty)
			{
				var to = new ConcurrentQueue<FstInstance>();
				Parallel.ForEach(from, inst =>
					{
						if (inst.Annotation == null)
							return;

						var taskResults = new List<FstResult<TData, TOffset>>();
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

										output = inst.Output.DeepClone();

										Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
											.Zip(output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
										mappings = inst.Mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
										if (varBindings == null)
											varBindings = inst.VariableBindings.DeepClone();
										visited = new HashSet<State<TData, TOffset>>(inst.Visited);
									}

									if (arc.Outputs.Count == 1)
									{
										Annotation<TOffset> outputAnn = mappings[inst.Annotation];
										arc.Outputs[0].UpdateOutput(output, outputAnn, _operations);
									}

									to.Enqueue(EpsilonAdvanceFst(inst.Annotation, registers, output, mappings, varBindings, visited, arc, taskResults,
										inst.Depth, inst.Priorities));
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
										output = inst.Output.DeepClone();

										Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = inst.Output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
											.Zip(output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
										mappings = inst.Mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
									}

									if (arc.Outputs.Count == 1)
									{
										Annotation<TOffset> outputAnn = mappings[inst.Annotation];
										arc.Outputs[0].UpdateOutput(output, outputAnn, _operations);
									}

									foreach (FstInstance newInst in AdvanceFst(inst.Annotation, inst.Registers, output, mappings, varBindings, arc,
										taskResults, inst.Depth, inst.Priorities))
									{
										to.Enqueue(newInst);
									}

									varBindings = null;
								}
							}
						}

						foreach (FstResult<TData, TOffset> res in taskResults)
							curResults.Add(res);
					});
				from = to;
			}

			return curResults;
		}

		private ConcurrentQueue<FstInstance> InitializeQueue(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<Annotation<TOffset>> initAnns)
		{
			var from = new ConcurrentQueue<FstInstance>();
			foreach (FstInstance inst in Initialize(ref ann, registers, cmds, initAnns, 0,
				(state, startAnn, regs, vb, cd) =>
					{
						TData o = Data.DeepClone();
						Dictionary<Annotation<TOffset>, Annotation<TOffset>> m = Data.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
										   .Zip(o.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
						return new FstInstance(state, startAnn, regs, o, m, vb, new HashSet<State<TData, TOffset>>(), cd, new int[0]);
					}))
			{
				from.Enqueue(inst);
			}
			return from;
		}

		private IEnumerable<FstInstance> AdvanceFst(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output,
			IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings, VariableBindings varBindings, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int depth, int[] priorities)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);
			return Advance(ann, registers, output, varBindings, arc, curResults, depth, priorities,
				(state, nextAnn, regs, vb, cd, clone) =>
					{
						TData o = output;
						IDictionary<Annotation<TOffset>, Annotation<TOffset>> m = mappings;
						if (clone)
						{
							o = output.DeepClone();

							Dictionary<Annotation<TOffset>, Annotation<TOffset>> outputMappings = output.Annotations.SelectMany(a => a.GetNodesBreadthFirst())
								.Zip(o.Annotations.SelectMany(a => a.GetNodesBreadthFirst())).ToDictionary(t => t.Item1, t => t.Item2);
							m = mappings.ToDictionary(kvp => kvp.Key, kvp => outputMappings[kvp.Value]);
						}
						return new FstInstance(state, nextAnn, regs, o, m, vb, new HashSet<State<TData, TOffset>>(), cd, priorities);
					});
		}

		private FstInstance EpsilonAdvanceFst(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output, IDictionary<Annotation<TOffset>,
			Annotation<TOffset>> mappings, VariableBindings varBindings, ISet<State<TData, TOffset>> visited, Arc<TData, TOffset> arc,
			List<FstResult<TData, TOffset>> curResults, int depth, int[] priorities)
		{
			priorities = UpdatePriorities(priorities, arc.Priority);
			visited.Add(arc.Target);
			return EpsilonAdvance(ann, registers, output, varBindings, arc, curResults, depth, priorities,
				(state, a, regs, vb, cd) => new FstInstance(state, a, regs, output, mappings, varBindings, visited, cd, priorities));
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

			public FstInstance(State<TData, TOffset> state, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output,
				IDictionary<Annotation<TOffset>, Annotation<TOffset>> mappings, VariableBindings varBindings,
				ISet<State<TData, TOffset>> visited, int depth, int[] priorities)
				: base(state, ann, registers, varBindings, depth)
			{
				_output = output;
				_mappings = mappings;
				_visited = visited;
				_priorities = priorities;
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
		}
	}
}
