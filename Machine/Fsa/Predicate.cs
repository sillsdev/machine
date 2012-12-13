using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Fsa
{
	public class Predicate : IEquatable<Predicate>
	{
		private readonly FeatureStruct _fs;
		private readonly bool _identity;

		internal Predicate(FeatureStruct fs)
			: this(fs, false)
		{
		}

		internal Predicate(FeatureStruct fs, bool identity)
		{
			_fs = fs;
			_identity = identity;
		}

		public FeatureStruct FeatureStruct
		{
			get { return _fs; }
		}

		public bool Identity
		{
			get { return _identity; }
		}

		public override bool Equals(object obj)
		{
			return obj != null && Equals(obj as Predicate);
		}

		public bool Equals(Predicate other)
		{
			return other != null && _fs.ValueEquals(other._fs) && _identity == other._identity;
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + _identity.GetHashCode();
			code = code * 31 + _fs.GetFrozenHashCode();
			return code;
		}

		public override string ToString()
		{
			if (_identity)
				return string.Format("<{0}>", _fs == null ? "" : _fs.ToString());
			return _fs.ToString();
		}
	}
}
