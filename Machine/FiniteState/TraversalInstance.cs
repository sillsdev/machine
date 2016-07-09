using System;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	internal abstract class TraversalInstance<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		private readonly Register<TOffset>[,] _registers;
		private readonly List<int> _priorities;

		protected TraversalInstance(int registerCount, bool ignoreVariables, bool deterministic)
		{
			_registers = new Register<TOffset>[registerCount, 2];
			if (!ignoreVariables)
				VariableBindings = new VariableBindings();
			if (!deterministic)
				_priorities = new List<int>();
		}

		public State<TData, TOffset> State { get; set; }
		public int AnnotationIndex { get; set; }
		public TData Output { get; set; }

		public IList<int> Priorities
		{
			get { return _priorities; }
		}

		public Register<TOffset>[,] Registers
		{
			get { return _registers; }
		}

		public VariableBindings VariableBindings { get; set; }

		public virtual void CopyTo(TraversalInstance<TData, TOffset> other)
		{
			other.State = State;
			other.AnnotationIndex = AnnotationIndex;
			var cloneable = Output as IDeepCloneable<TData>;
			other.Output = cloneable != null ? cloneable.DeepClone() : Output;
			if (_priorities != null)
				other.Priorities.AddRange(_priorities);
			Array.Copy(_registers, other.Registers, _registers.Length);
		}

		public virtual void Clear()
		{
			State = null;
			Output = default(TData);
			if (_priorities != null)
				_priorities.Clear();
			if (VariableBindings != null)
				VariableBindings.Clear();
		}
	}
}