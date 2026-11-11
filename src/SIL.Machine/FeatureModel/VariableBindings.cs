using System.Collections;
using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.FeatureModel
{
    public class VariableBindings : IDictionary<string, SimpleFeatureValue>, ICloneable<VariableBindings>
    {
        private readonly Dictionary<string, SimpleFeatureValue> _varBindings;
        private bool _inTransaction;
        private Dictionary<string, SimpleFeatureValue> _undo;

        public VariableBindings()
        {
            _varBindings = new Dictionary<string, SimpleFeatureValue>();
        }

        protected VariableBindings(VariableBindings varBindings)
            : this()
        {
            foreach (KeyValuePair<string, SimpleFeatureValue> kvp in varBindings._varBindings)
                _varBindings[kvp.Key] = kvp.Value.Clone();
        }

        // There are no nested transactions: only the outermost operation owns the rollback. An inner
        // Unify/Subsumes/IsUnifiable call on the same bindings gets false from BeginTransaction and its writes
        // are undone only if the outer operation fails. The bindings are not safe for concurrent use.
        /// <summary>
        /// Starts a transaction so a failed operation can be rolled back without cloning up front. The bindings
        /// are only snapshotted if something is written. Returns false if a transaction is already open.
        /// </summary>
        internal bool BeginTransaction()
        {
            if (_inTransaction)
                return false;
            _inTransaction = true;
            return true;
        }

        internal void EndTransaction(bool commit)
        {
            if (!commit && _undo != null)
            {
                _varBindings.Clear();
                foreach (KeyValuePair<string, SimpleFeatureValue> kvp in _undo)
                    _varBindings[kvp.Key] = kvp.Value;
            }
            _undo = null;
            _inTransaction = false;
        }

        private void BeforeWrite()
        {
            if (_inTransaction && _undo == null)
                _undo = new Dictionary<string, SimpleFeatureValue>(_varBindings);
        }

        public IEnumerable<string> VariableNames
        {
            get { return _varBindings.Keys; }
        }

        public bool ContainsVariable(string variableName)
        {
            return _varBindings.ContainsKey(variableName);
        }

        bool IDictionary<string, SimpleFeatureValue>.ContainsKey(string key)
        {
            return _varBindings.ContainsKey(key);
        }

        public void Add(string variableName, SimpleFeatureValue value)
        {
            BeforeWrite();
            _varBindings.Add(variableName, value);
        }

        public void Replace(VariableBindings varBindings)
        {
            BeforeWrite();
            foreach (KeyValuePair<string, SimpleFeatureValue> varBinding in varBindings)
                _varBindings[varBinding.Key] = varBinding.Value;
        }

        public bool Remove(string variableName)
        {
            BeforeWrite();
            return _varBindings.Remove(variableName);
        }

        public bool TryGetValue(string key, out SimpleFeatureValue value)
        {
            return _varBindings.TryGetValue(key, out value);
        }

        public bool TryGetValue<T>(string variableName, out T value)
            where T : SimpleFeatureValue
        {
            SimpleFeatureValue sfv;
            if (_varBindings.TryGetValue(variableName, out sfv))
            {
                value = (T)sfv;
                return true;
            }

            value = default;
            return false;
        }

        public SimpleFeatureValue this[string variableName]
        {
            get { return _varBindings[variableName]; }
            set
            {
                BeforeWrite();
                _varBindings[variableName] = value;
            }
        }

        ICollection<string> IDictionary<string, SimpleFeatureValue>.Keys
        {
            get { return _varBindings.Keys; }
        }

        public ICollection<SimpleFeatureValue> Values
        {
            get { return _varBindings.Values; }
        }

        IEnumerator<KeyValuePair<string, SimpleFeatureValue>> IEnumerable<
            KeyValuePair<string, SimpleFeatureValue>
        >.GetEnumerator()
        {
            return _varBindings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, FeatureValue>>)this).GetEnumerator();
        }

        void ICollection<KeyValuePair<string, SimpleFeatureValue>>.Add(KeyValuePair<string, SimpleFeatureValue> item)
        {
            BeforeWrite();
            ((ICollection<KeyValuePair<string, SimpleFeatureValue>>)_varBindings).Add(item);
        }

        public void Clear()
        {
            BeforeWrite();
            _varBindings.Clear();
        }

        bool ICollection<KeyValuePair<string, SimpleFeatureValue>>.Contains(
            KeyValuePair<string, SimpleFeatureValue> item
        )
        {
            return ((ICollection<KeyValuePair<string, SimpleFeatureValue>>)_varBindings).Contains(item);
        }

        void ICollection<KeyValuePair<string, SimpleFeatureValue>>.CopyTo(
            KeyValuePair<string, SimpleFeatureValue>[] array,
            int arrayIndex
        )
        {
            ((ICollection<KeyValuePair<string, SimpleFeatureValue>>)_varBindings).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, SimpleFeatureValue>>.Remove(KeyValuePair<string, SimpleFeatureValue> item)
        {
            BeforeWrite();
            return ((ICollection<KeyValuePair<string, SimpleFeatureValue>>)_varBindings).Remove(item);
        }

        public int Count
        {
            get { return _varBindings.Count; }
        }

        bool ICollection<KeyValuePair<string, SimpleFeatureValue>>.IsReadOnly
        {
            get { return false; }
        }

        public VariableBindings Clone()
        {
            return new VariableBindings(this);
        }
    }
}
