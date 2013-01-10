using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class Input : IEquatable<Input>
	{
		private readonly FeatureStruct _fs;
		private readonly int _enqueueCount;

		internal Input(int enqueueCount)
			: this(null, enqueueCount)
		{
		}

		internal Input(FeatureStruct fs, int enqueueCount)
		{
			_fs = fs;
			_enqueueCount = enqueueCount;
		}

		public FeatureStruct FeatureStruct
		{
			get { return _fs; }
		}

		public int EnqueueCount
		{
			get { return _enqueueCount; }
		}

		public override bool Equals(object obj)
		{
			return obj != null && Equals(obj as Input);
		}

		public bool Equals(Input other)
		{
			if (other == null)
				return false;

			if (_enqueueCount != other._enqueueCount)
				return false;

			if (_fs == null)
				return other._fs == null;
			if (other._fs == null)
				return _fs == null;

			return _fs.ValueEquals(other._fs);
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + _enqueueCount.GetHashCode();
			if (_fs != null)
				code = code * 31 + _fs.GetFrozenHashCode();
			return code;
		}

		public override string ToString()
		{
			if (_enqueueCount == 0)
				return _fs == null ? "ε" : _fs.ToString();
			return string.Format("{0}/{1}", _fs == null ? "ε" : _fs.ToString(), _enqueueCount);
		}
	}
}
