using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Fsa
{
	public class FsaMatch<TOffset> : IComparable<FsaMatch<TOffset>>, IComparable
	{
		private readonly NullableValue<TOffset>[,] _registers;
		private readonly VariableBindings _varBindings;
		private readonly string _id;
		private readonly int _priority;

		private readonly int _matchNum;
		private readonly bool _isLazy;

		internal FsaMatch(string id, NullableValue<TOffset>[,] registers, VariableBindings varBindings, int priority, int matchNum, bool isLazy)
		{
			_id = id;
			_registers = registers;
			_varBindings = varBindings;
			_priority = priority;
			_matchNum = matchNum;
			_isLazy = isLazy;
		}

		public string ID
		{
			get { return _id; }
		}

		public NullableValue<TOffset>[,] Registers
		{
			get { return _registers; }
		}

		public VariableBindings VariableBindings
		{
			get { return _varBindings; }
		}

		public int CompareTo(FsaMatch<TOffset> other)
		{
			if (other == null)
				return 1;

			int res = _priority.CompareTo(other._priority);
			if (res != 0)
				return res;

			//if (_isLazy && other._isLazy)
			//    return _matchNum.CompareTo(other._matchNum);

			//if (_isLazy && !other._isLazy)
			//    return _matchNum.CompareTo(other._matchNum);

			//if (!_isLazy && other._isLazy)
			//    return _matchNum.CompareTo(other._matchNum);

			return -_matchNum.CompareTo(other._matchNum);
		}

		int IComparable.CompareTo(object obj)
		{
			var other = obj as FsaMatch<TOffset>;
			if (obj == null)
				throw new ArgumentException("The specified object is not the proper type.");
			return CompareTo(other);
		}
	}
}
