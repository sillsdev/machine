using System;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	internal abstract class TraversalMethod<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		private readonly IEqualityComparer<NullableValue<TOffset>[,]> _registersEqualityComparer;
		private readonly Direction _dir;
		private readonly State<TData, TOffset> _startState;
		private readonly TData _data;
		private readonly bool _endAnchor;
		private readonly bool _unification;
		private readonly bool _useDefaults;
		private readonly Func<Annotation<TOffset>, bool> _filter;

		protected TraversalMethod(IEqualityComparer<NullableValue<TOffset>[,]> registersEqualityComparer, Direction dir, Func<Annotation<TOffset>, bool> filter, State<TData, TOffset> startState,
			TData data, bool endAnchor, bool unification, bool useDefaults)
		{
			_registersEqualityComparer = registersEqualityComparer;
			_dir = dir;
			_filter = filter;
			_startState = startState;
			_data = data;
			_endAnchor = endAnchor;
			_unification = unification;
			_useDefaults = useDefaults;
		}

		protected TData Data
		{
			get { return _data; }
		}

		protected IEqualityComparer<NullableValue<TOffset>[,]> RegistersEqualityComparer
		{
			get { return _registersEqualityComparer; }
		}

		public abstract IEnumerable<FstResult<TData, TOffset>> Traverse(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<Annotation<TOffset>> initAnns);

		protected static void ExecuteCommands(NullableValue<TOffset>[,] registers, IEnumerable<TagMapCommand> cmds,
			NullableValue<TOffset> start, NullableValue<TOffset> end)
		{
			foreach (TagMapCommand cmd in cmds)
			{
				if (cmd.Src == TagMapCommand.CurrentPosition)
				{
					registers[cmd.Dest, 0] = start;
					registers[cmd.Dest, 1] = end;
				}
				else
				{
					registers[cmd.Dest, 0] = registers[cmd.Src, 0];
					registers[cmd.Dest, 1] = registers[cmd.Src, 1];
				}
			}
		}

		protected bool CheckInputMatch(Arc<TData, TOffset> arc, Annotation<TOffset> ann, VariableBindings varBindings)
		{
			return ann != _data.Annotations.GetEnd(_dir) && arc.Input.Matches(ann.FeatureStruct, _unification, _useDefaults, varBindings);
		}

		private void CheckAccepting(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output,
			VariableBindings varBindings, Arc<TData, TOffset> arc, ICollection<FstResult<TData, TOffset>> curResults, int[] priorities)
		{
			if (arc.Target.IsAccepting && (!_endAnchor || ann == _data.Annotations.GetEnd(_dir)))
			{
				var matchRegisters = (NullableValue<TOffset>[,]) registers.Clone();
				ExecuteCommands(matchRegisters, arc.Target.Finishers, new NullableValue<TOffset>(), new NullableValue<TOffset>());
				if (arc.Target.AcceptInfos.Count > 0)
				{
					foreach (AcceptInfo<TData, TOffset> acceptInfo in arc.Target.AcceptInfos)
					{
						TData resOutput = output;
						var cloneable = resOutput as IDeepCloneable<TData>;
						if (cloneable != null)
							resOutput = cloneable.DeepClone();

						var candidate = new FstResult<TData, TOffset>(_registersEqualityComparer, acceptInfo.ID, matchRegisters, resOutput, varBindings.DeepClone(),
							acceptInfo.Priority, arc.Target.IsLazy, ann, priorities, curResults.Count);
						if (acceptInfo.Acceptable == null || acceptInfo.Acceptable(_data, candidate))
							curResults.Add(candidate);
					}
				}
				else
				{
					TData resOutput = output;
					var cloneable = resOutput as IDeepCloneable<TData>;
					if (cloneable != null)
						resOutput = cloneable.DeepClone();
					curResults.Add(new FstResult<TData, TOffset>(_registersEqualityComparer, null, matchRegisters, resOutput, varBindings.DeepClone(), -1, arc.Target.IsLazy, ann,
						priorities, curResults.Count));
				}
			}
		}

		protected IEnumerable<TInst> Initialize<TInst>(ref Annotation<TOffset> ann, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<Annotation<TOffset>> initAnns, Func<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,], VariableBindings, TInst> instFactory)
		{
			var insts = new List<TInst>();
			TOffset offset = ann.Span.GetStart(_dir);

			var newRegisters = (NullableValue<TOffset>[,]) registers.Clone();
			ExecuteCommands(newRegisters, cmds, new NullableValue<TOffset>(ann.Span.GetStart(_dir)), new NullableValue<TOffset>());

			for (Annotation<TOffset> a = ann; a != _data.Annotations.GetEnd(_dir) && a.Span.GetStart(_dir).Equals(offset); a = a.GetNextDepthFirst(_dir, _filter))
			{
				if (a.Optional)
				{
					Annotation<TOffset> nextAnn = a.GetNextDepthFirst(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
					if (nextAnn != null)
						insts.AddRange(Initialize(ref nextAnn, registers, cmds, initAnns, instFactory));
				}
			}

			bool cloneRegisters = false;
			for (; ann != _data.Annotations.GetEnd(_dir) && ann.Span.GetStart(_dir).Equals(offset); ann = ann.GetNextDepthFirst(_dir, _filter))
			{
				if (!initAnns.Contains(ann))
				{
					insts.Add(instFactory(_startState, ann, cloneRegisters ? (NullableValue<TOffset>[,]) newRegisters.Clone() : newRegisters, new VariableBindings()));
					initAnns.Add(ann);
					cloneRegisters = true;
				}
			}

			return insts;
		}

		protected IEnumerable<TInst> Advance<TInst>(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output, VariableBindings varBindings,
			Arc<TData, TOffset> arc, ICollection<FstResult<TData, TOffset>> curResults, int[] priorities,
			Func<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,], VariableBindings, bool, TInst> instFactory)
		{
			Annotation<TOffset> nextAnn = ann.GetNextDepthFirst(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
			TOffset nextOffset = nextAnn == _data.Annotations.GetEnd(_dir) ? _data.Annotations.GetLast(_dir, _filter).Span.GetEnd(_dir) : nextAnn.Span.GetStart(_dir);
			TOffset end = ann.Span.GetEnd(_dir);
			var newRegisters = (NullableValue<TOffset>[,]) registers.Clone();
			ExecuteCommands(newRegisters, arc.Commands, new NullableValue<TOffset>(nextOffset), new NullableValue<TOffset>(end));

			CheckAccepting(nextAnn, newRegisters, output, varBindings, arc, curResults, priorities);

			if (nextAnn != _data.Annotations.GetEnd(_dir))
			{
				var anns = new List<Annotation<TOffset>>();
				bool cloneOutputs = false;
				for (Annotation<TOffset> curAnn = nextAnn; curAnn != _data.Annotations.GetEnd(_dir) && curAnn.Span.GetStart(_dir).Equals(nextOffset); curAnn = curAnn.GetNextDepthFirst(_dir, _filter))
				{
					if (curAnn.Optional)
					{
						foreach (TInst ni in Advance(curAnn, registers, output, varBindings, arc, curResults, priorities, instFactory))
						{
							yield return ni;
							cloneOutputs = true;
						}
					}
					anns.Add(curAnn);
				}

				bool cloneRegisters = false;
				foreach (Annotation<TOffset> curAnn in anns)
				{
					yield return instFactory(arc.Target, curAnn, cloneRegisters ? (NullableValue<TOffset>[,]) newRegisters.Clone() : newRegisters,
						cloneOutputs ? varBindings.DeepClone() : varBindings, cloneOutputs);
					cloneOutputs = true;
					cloneRegisters = true;
				}
			}
			else
			{
				yield return instFactory(arc.Target, nextAnn, newRegisters, varBindings, false);
			}
		}

		protected TInst EpsilonAdvance<TInst>(Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, TData output, VariableBindings varBindings, Arc<TData, TOffset> arc,
			ICollection<FstResult<TData, TOffset>> curResults, int[] priorities,
			Func<State<TData, TOffset>, Annotation<TOffset>, NullableValue<TOffset>[,], VariableBindings, TInst> instFactory)
		{
			Annotation<TOffset> prevAnn = ann.GetPrevDepthFirst(_dir, (cur, prev) => !cur.Span.Overlaps(prev.Span) && _filter(prev));
			ExecuteCommands(registers, arc.Commands, new NullableValue<TOffset>(ann.Span.GetStart(_dir)), new NullableValue<TOffset>(prevAnn.Span.GetEnd(_dir)));
			CheckAccepting(ann, registers, output, varBindings, arc, curResults, priorities);
			return instFactory(arc.Target, ann, registers, varBindings);
		}

		protected static int[] UpdatePriorities(int[] priorities, int priority)
		{
			var p = new int[priorities.Length + 1];
			priorities.CopyTo(p, 0);
			p[p.Length - 1] = priority;
			return p;
		}

		protected class Instance
		{
			private readonly NullableValue<TOffset>[,] _registers;
			private readonly VariableBindings _varBindings;
			private readonly State<TData, TOffset> _state;
			private readonly Annotation<TOffset> _annotation;

			public Instance(State<TData, TOffset> state, Annotation<TOffset> ann, NullableValue<TOffset>[,] registers, VariableBindings varBindings)
			{
				_state = state;
				_annotation = ann;
				_registers = registers;
				_varBindings = varBindings;
			}

			public State<TData, TOffset> State
			{
				get { return _state; }
			}

			public Annotation<TOffset> Annotation
			{
				get { return _annotation; }
			}

			public NullableValue<TOffset>[,] Registers
			{
				get { return _registers; }
			}

			public VariableBindings VariableBindings
			{
				get { return _varBindings; }
			}
		}
	}
}
