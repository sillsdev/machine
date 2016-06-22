using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	internal abstract class TraversalMethodBase<TData, TOffset, TInst> : ITraversalMethod<TData, TOffset>
		where TData : IAnnotatedData<TOffset> where TInst : TraversalInstance<TData, TOffset>
	{
		private readonly IEqualityComparer<NullableValue<TOffset>[,]> _registersEqualityComparer;
		private readonly int _registerCount;
		private readonly Direction _dir;
		private readonly State<TData, TOffset> _startState;
		private readonly TData _data;
		private readonly bool _endAnchor;
		private readonly bool _unification;
		private readonly bool _useDefaults;
		private readonly bool _ignoreVariables;
		private readonly Func<Annotation<TOffset>, bool> _filter;
		private readonly List<Annotation<TOffset>> _annotations;
		private readonly Queue<TInst> _cachedInstances;

		protected TraversalMethodBase(IEqualityComparer<NullableValue<TOffset>[,]> registersEqualityComparer, int registerCount, Direction dir, Func<Annotation<TOffset>, bool> filter,
			State<TData, TOffset> startState, TData data, bool endAnchor, bool unification, bool useDefaults, bool ignoreVariables)
		{
			_registersEqualityComparer = registersEqualityComparer;
			_registerCount = registerCount;
			_dir = dir;
			_filter = filter;
			_startState = startState;
			_data = data;
			_endAnchor = endAnchor;
			_unification = unification;
			_useDefaults = useDefaults;
			_ignoreVariables = ignoreVariables;
			_annotations = new List<Annotation<TOffset>>();
			// insertion sort
			foreach (Annotation<TOffset> ann in _data.Annotations.GetNodes(_dir).SelectMany(a => a.GetNodesDepthFirst(_dir)).Where(a => _filter(a)))
			{
				int i = _annotations.Count - 1;
				while (i >= 0 && CompareAnnotations(_annotations[i], ann) > 0)
				{
					if (i + 1 == _annotations.Count)
						_annotations.Add(_annotations[i]);
					else
						_annotations[i + 1] = _annotations[i];
					i--;
				}
				if (i + 1 == _annotations.Count)
					_annotations.Add(ann);
				else
					_annotations[i + 1] = ann;
			}
			_cachedInstances = new Queue<TInst>();
		}

		private int CompareAnnotations(Annotation<TOffset> x, Annotation<TOffset> y)
		{
			int res = x.Span.CompareTo(y.Span);
			if (res != 0)
				return _dir == Direction.LeftToRight ? res : -res;

			return x.Depth.CompareTo(y.Depth);
		}

		protected TData Data
		{
			get { return _data; }
		}

		protected IEqualityComparer<NullableValue<TOffset>[,]> RegistersEqualityComparer
		{
			get { return _registersEqualityComparer; }
		}

		public IList<Annotation<TOffset>> Annotations
		{
			get { return _annotations; }
		}

		public abstract IEnumerable<FstResult<TData, TOffset>> Traverse(ref int annIndex, NullableValue<TOffset>[,] initRegisters, IList<TagMapCommand> initCmds, ISet<int> initAnns);

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

		protected bool CheckInputMatch(Arc<TData, TOffset> arc, int annIndex, VariableBindings varBindings)
		{
			return annIndex < _annotations.Count && arc.Input.Matches(_annotations[annIndex].FeatureStruct, _unification, _useDefaults, varBindings);
		}

		private void CheckAccepting(int annIndex, NullableValue<TOffset>[,] registers, TData output,
			VariableBindings varBindings, Arc<TData, TOffset> arc, ICollection<FstResult<TData, TOffset>> curResults, IList<int> priorities)
		{
			if (arc.Target.IsAccepting && (!_endAnchor || annIndex == _annotations.Count))
			{
				Annotation<TOffset> ann = annIndex < _annotations.Count ? _annotations[annIndex] : _data.Annotations.GetEnd(_dir);
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

						var candidate = new FstResult<TData, TOffset>(_registersEqualityComparer, acceptInfo.ID, matchRegisters, resOutput,
							varBindings == null ? null : varBindings.DeepClone(), acceptInfo.Priority, arc.Target.IsLazy, ann,
							priorities == null ? null : priorities.ToArray(), curResults.Count);
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
					curResults.Add(new FstResult<TData, TOffset>(_registersEqualityComparer, null, matchRegisters, resOutput,
						varBindings == null ? null : varBindings.DeepClone(), -1, arc.Target.IsLazy, ann,
						priorities == null ? null : priorities.ToArray(), curResults.Count));
				}
			}
		}

		protected IEnumerable<TInst> Initialize(ref int annIndex, NullableValue<TOffset>[,] registers,
			IList<TagMapCommand> cmds, ISet<int> initAnns)
		{
			var insts = new List<TInst>();
			TOffset offset = _annotations[annIndex].Span.GetStart(_dir);

			for (int i = annIndex; i < _annotations.Count && _annotations[i].Span.GetStart(_dir).Equals(offset); i++)
			{
				if (_annotations[i].Optional)
				{
					int nextIndex = GetNextNonoverlappingAnnotationIndex(i);
					if (nextIndex != -1)
						insts.AddRange(Initialize(ref nextIndex, (NullableValue<TOffset>[,]) registers.Clone(), cmds, initAnns));
				}
			}

			ExecuteCommands(registers, cmds, new NullableValue<TOffset>(offset), new NullableValue<TOffset>());

			for (; annIndex < _annotations.Count && _annotations[annIndex].Span.GetStart(_dir).Equals(offset); annIndex++)
			{
				if (!initAnns.Contains(annIndex))
				{
					TInst inst = GetCachedInstance();
					inst.State = _startState;
					inst.AnnotationIndex = annIndex;
					Array.Copy(registers, inst.Registers, registers.Length);
					insts.Add(inst);
					initAnns.Add(annIndex);
				}
			}

			return insts;
		}

		protected IEnumerable<TInst> Advance(TInst inst, VariableBindings varBindings, Arc<TData, TOffset> arc, ICollection<FstResult<TData, TOffset>> curResults)
		{
			if (inst.Priorities != null)
				inst.Priorities.Add(arc.Priority);
			int nextIndex = GetNextNonoverlappingAnnotationIndex(inst.AnnotationIndex);
			TOffset nextOffset = nextIndex < _annotations.Count ? _annotations[nextIndex].Span.GetStart(_dir) : _data.Annotations.GetLast(_dir, _filter).Span.GetEnd(_dir);
			TOffset end = _annotations[inst.AnnotationIndex].Span.GetEnd(_dir);

			if (nextIndex < _annotations.Count)
			{
				var anns = new List<int>();
				bool cloneOutputs = false;
				for (int i = nextIndex; i < _annotations.Count && _annotations[i].Span.GetStart(_dir).Equals(nextOffset); i++)
				{
					if (_annotations[i].Optional)
					{
						TInst ti = CopyInstance(inst);
						ti.AnnotationIndex = i;
						foreach (TInst ni in Advance(ti, varBindings, arc, curResults))
						{
							yield return ni;
							cloneOutputs = true;
						}
					}
					anns.Add(i);
				}

				ExecuteCommands(inst.Registers, arc.Commands, new NullableValue<TOffset>(nextOffset), new NullableValue<TOffset>(end));
				CheckAccepting(nextIndex, inst.Registers, inst.Output, varBindings, arc, curResults, inst.Priorities);

				inst.State = arc.Target;

				bool first = true;
				foreach (int curIndex in anns)
				{
					TInst ni = first ? inst : CopyInstance(inst);
					ni.AnnotationIndex = curIndex;
					if (varBindings != null)
						inst.VariableBindings = cloneOutputs ? varBindings.DeepClone() : varBindings;
					yield return ni;
					cloneOutputs = true;
					first = false;
				}
			}
			else
			{
				ExecuteCommands(inst.Registers, arc.Commands, new NullableValue<TOffset>(nextOffset), new NullableValue<TOffset>(end));
				CheckAccepting(nextIndex, inst.Registers, inst.Output, varBindings, arc, curResults, inst.Priorities);

				inst.State = arc.Target;
				inst.AnnotationIndex = nextIndex;
				inst.VariableBindings = varBindings;
				yield return inst;
			}
		}

		protected TInst EpsilonAdvance(TInst inst, Arc<TData, TOffset> arc, ICollection<FstResult<TData, TOffset>> curResults)
		{
			Annotation<TOffset> ann = inst.AnnotationIndex < _annotations.Count ? _annotations[inst.AnnotationIndex] : _data.Annotations.GetEnd(_dir);
			int prevIndex = GetPrevNonoverlappingAnnotationIndex(inst.AnnotationIndex);
			Annotation<TOffset> prevAnn = _annotations[prevIndex];
			ExecuteCommands(inst.Registers, arc.Commands, new NullableValue<TOffset>(ann.Span.GetStart(_dir)), new NullableValue<TOffset>(prevAnn.Span.GetEnd(_dir)));
			CheckAccepting(inst.AnnotationIndex, inst.Registers, inst.Output, inst.VariableBindings, arc, curResults, inst.Priorities);

			inst.State = arc.Target;
			return inst;
		}

		private int GetNextNonoverlappingAnnotationIndex(int start)
		{
			Annotation<TOffset> cur = _annotations[start];
			for (int i = start + 1; i < _annotations.Count; i++)
			{
				if (!cur.Span.Overlaps(_annotations[i].Span))
					return i;
			}
			return _annotations.Count;
		}

		private int GetPrevNonoverlappingAnnotationIndex(int start)
		{
			Annotation<TOffset> cur = start < _annotations.Count ? _annotations[start] : _data.Annotations.GetEnd(_dir);
			for (int i = start - 1; i >= 0; i--)
			{
				if (!cur.Span.Overlaps(_annotations[i].Span))
					return i;
			}
			return -1;
		}

		private TInst GetCachedInstance()
		{
			if (_cachedInstances.Count == 0)
				return CreateInstance(_registerCount, _ignoreVariables);

			TInst inst = _cachedInstances.Dequeue();
			inst.Clear();
			return inst;
		}

		protected TInst CopyInstance(TInst inst)
		{
			TInst ni = GetCachedInstance();
			inst.CopyTo(ni);
			return ni;
		}

		protected abstract TInst CreateInstance(int registerCount, bool ignoreVariables);

		protected void ReleaseInstance(TInst inst)
		{
			_cachedInstances.Enqueue(inst);
		}
	}
}
