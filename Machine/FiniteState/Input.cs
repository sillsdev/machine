using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class Input : IEquatable<Input>
	{
		private readonly FeatureStruct _fs;
		private readonly int _enqueueCount;
		private readonly HashSet<FeatureStruct> _negatedFSs; 

		internal Input(int enqueueCount)
			: this(null, enqueueCount)
		{
		}

		internal Input(FeatureStruct fs, int enqueueCount)
			: this(fs, Enumerable.Empty<FeatureStruct>(), enqueueCount)
		{
		}

		internal Input(FeatureStruct fs, IEnumerable<FeatureStruct> negatedFSs, int enqueueCount)
		{
			_fs = fs;
			_negatedFSs = new HashSet<FeatureStruct>(negatedFSs, FreezableEqualityComparer<FeatureStruct>.Instance);
			_enqueueCount = enqueueCount;
		}

		public bool IsEpsilon
		{
			get { return _fs == null; }
		}

		public FeatureStruct FeatureStruct
		{
			get { return _fs; }
		}

		public IEnumerable<FeatureStruct> NegatedFeatureStructs
		{
			get { return _negatedFSs; }
		}

		public int EnqueueCount
		{
			get { return _enqueueCount; }
		}

		public bool Matches(FeatureStruct fs, bool useDefaults, VariableBindings varBindings)
		{
			return fs.IsUnifiable(_fs, useDefaults, varBindings) && _negatedFSs.All(nfs => !fs.IsUnifiable(nfs, useDefaults));
		}

		public bool IsConsistent
		{
			get { return _negatedFSs.All(nfs => !nfs.Subsumes(_fs)); }
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

			if (!_fs.ValueEquals(other._fs))
				return false;

			return _negatedFSs.SetEquals(other._negatedFSs);
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + _enqueueCount.GetHashCode();
			if (_fs != null)
				code = code * 31 + _fs.GetFrozenHashCode();
			return _negatedFSs.Aggregate(code, (c, nfs) => c ^ nfs.GetFrozenHashCode());
		}

		public override string ToString()
		{
			if (_enqueueCount == 0)
				return _fs == null ? "ε" : _fs.ToString();
			var sb = new StringBuilder();
			foreach (FeatureStruct nfs in _negatedFSs)
				sb.AppendFormat(" && ~({0})", nfs);
			return string.Format("{0}{1}/{2}", _fs == null ? "ε" : _fs.ToString(), sb, _enqueueCount);
		}
	}
}
