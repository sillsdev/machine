using System;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class FstResult<TData, TOffset> : IEquatable<FstResult<TData, TOffset>>
	{
		private readonly IEqualityComparer<NullableValue<TOffset>[,]> _registersEqualityComparer;
		private readonly NullableValue<TOffset>[,] _registers;
		private readonly TData _output;
		private readonly VariableBindings _varBindings;
		private readonly string _id;
		private readonly int _priority;
		private readonly bool _isLazy;
		private readonly Annotation<TOffset> _nextAnn;
		private readonly int[] _priorities;

		internal FstResult(IEqualityComparer<NullableValue<TOffset>[,]> registersEqualityComparer, string id, NullableValue<TOffset>[,] registers, TData output, VariableBindings varBindings, int priority,
			bool isLazy, Annotation<TOffset> nextAnn, int[] priorities)
		{
			_registersEqualityComparer = registersEqualityComparer;
			_id = id;
			_registers = registers;
			_output = output;
			_varBindings = varBindings;
			_priority = priority;
			_isLazy = isLazy;
			_nextAnn = nextAnn;
			_priorities = priorities;
		}

		public string ID
		{
			get { return _id; }
		}

		public NullableValue<TOffset>[,] Registers
		{
			get { return _registers; }
		}

		public TData Output
		{
			get { return _output; }
		}

		public VariableBindings VariableBindings
		{
			get { return _varBindings; }
		}

		public int Priority
		{
			get { return _priority; }
		}

		public Annotation<TOffset> NextAnnotation
		{
			get { return _nextAnn; }
		}

		internal bool IsLazy
		{
			get { return _isLazy; }
		}

		internal int[] Priorities
		{
			get { return _priorities; }
		}

		public override bool Equals(object obj)
		{
			return obj != null && Equals(obj as FstResult<TData, TOffset>);
		}

		public bool Equals(FstResult<TData, TOffset> other)
		{
			if (other == null)
				return false;

			if (_id != other._id)
				return false;

			if (!_registersEqualityComparer.Equals(_registers, other._registers))
				return false;

			return EqualityComparer<TData>.Default.Equals(_output, other._output);
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + (_id == null ? 0 : _id.GetHashCode());
			code = code * 31 + _registersEqualityComparer.GetHashCode(_registers);
			code = code * 31 * EqualityComparer<TData>.Default.GetHashCode(_output);
			return code;
		}
	}
}
