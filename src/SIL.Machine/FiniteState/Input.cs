using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.FiniteState
{
    public class Input : IEquatable<Input>
    {
        private readonly FeatureStruct _fs;
        private readonly int _enqueueCount;
        private readonly HashSet<FeatureStruct> _negatedFSs;

        internal Input(int enqueueCount)
            : this(null, enqueueCount) { }

        internal Input(FeatureStruct fs, int enqueueCount)
            : this(fs, Enumerable.Empty<FeatureStruct>(), enqueueCount) { }

        internal Input(FeatureStruct fs, IEnumerable<FeatureStruct> negatedFSs, int enqueueCount)
        {
            _fs = fs;
            _negatedFSs = new HashSet<FeatureStruct>(negatedFSs, FreezableEqualityComparer<FeatureStruct>.Default);
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

        public bool Matches(FeatureStruct fs, bool unification, bool useDefaults, VariableBindings varBindings)
        {
            if (unification)
            {
                // Bit-packed fast path for the common phonological case (no defaults, no negation,
                // both operands simple symbolic structs). Identical result, no varBindings clone,
                // no dictionary walk. Falls back to the full engine otherwise.
                if (!useDefaults && _negatedFSs.Count == 0 && fs.TryFastUnifiable(_fs, out bool fastResult))
                    return fastResult;

                if (!fs.IsUnifiable(_fs, useDefaults, varBindings))
                    return false;
                return NoneUnifiable(fs, useDefaults);
            }

            return _fs.Subsumes(fs, useDefaults, varBindings) && NoneSubsumed(fs, useDefaults);
        }

        // Explicit loops instead of `_negatedFSs.All(nfs => ...)`: the lambda's closure (capturing fs
        // and useDefaults) and the boxed HashSet<T>.Enumerator (via the IEnumerable<T> extension-method
        // path) were allocated on every call, even for the common case where _negatedFSs is empty.
        // A plain `foreach` on the concrete HashSet<T> reference uses its unboxed struct enumerator.
        private bool NoneUnifiable(FeatureStruct fs, bool useDefaults)
        {
            if (_negatedFSs.Count == 0)
                return true;
            foreach (FeatureStruct nfs in _negatedFSs)
            {
                if (fs.IsUnifiable(nfs, useDefaults))
                    return false;
            }
            return true;
        }

        private bool NoneSubsumed(FeatureStruct fs, bool useDefaults)
        {
            if (_negatedFSs.Count == 0)
                return true;
            foreach (FeatureStruct nfs in _negatedFSs)
            {
                if (nfs.Subsumes(fs, useDefaults))
                    return false;
            }
            return true;
        }

        public bool IsSatisfiable
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
            return string.Format("{0}{1},{2}", _fs == null ? "ε" : _fs.ToString(), sb, _enqueueCount);
        }
    }
}
