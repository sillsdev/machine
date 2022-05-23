using System.Collections;
using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal class Properties : IDictionary<string, object>
    {
        private readonly Dictionary<string, object> _props;

        public Properties()
        {
            _props = new Dictionary<string, object>();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _props.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            ((ICollection<KeyValuePair<string, object>>)_props).Add(item);
        }

        public void Clear()
        {
            _props.Clear();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)_props).Contains(item);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>)_props).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)_props).Remove(item);
        }

        public int Count
        {
            get { return _props.Count; }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return false; }
        }

        public bool ContainsKey(string key)
        {
            return _props.ContainsKey(key);
        }

        public void Add(string key, object value)
        {
            _props.Add(key, value);
        }

        public bool Remove(string key)
        {
            return _props.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return _props.TryGetValue(key, out value);
        }

        public object this[string key]
        {
            get
            {
                object value;
                if (_props.TryGetValue(key, out value))
                    return value;
                return null;
            }
            set { _props[key] = value; }
        }

        public ICollection<string> Keys
        {
            get { return _props.Keys; }
        }

        public ICollection<object> Values
        {
            get { return _props.Values; }
        }
    }
}
