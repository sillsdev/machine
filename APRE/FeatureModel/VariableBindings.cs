using System.Collections;
using System.Collections.Generic;

namespace SIL.APRE.FeatureModel
{
	public class VariableBindings : IDictionary<string, FeatureValue>, ICloneable<VariableBindings>
	{
		private readonly Dictionary<string, FeatureValue> _varBindings;

		public VariableBindings()
		{
			_varBindings = new Dictionary<string, FeatureValue>();
		}

		public VariableBindings(VariableBindings varBindings)
			: this()
		{
			foreach (KeyValuePair<string, FeatureValue> kvp in varBindings._varBindings)
				_varBindings[kvp.Key] = kvp.Value.Clone();
		}

		public IEnumerable<string> VariableNames
		{
			get { return _varBindings.Keys; }
		}

		public bool ContainsVariable(string variableName)
		{
			return _varBindings.ContainsKey(variableName);
		}

		bool IDictionary<string, FeatureValue>.ContainsKey(string key)
		{
			return _varBindings.ContainsKey(key);
		}

		public void Add(string variableName, FeatureValue value)
		{
			_varBindings.Add(variableName, value);
		}

		public void Replace(VariableBindings varBindings)
		{
			foreach (KeyValuePair<string, FeatureValue> varBinding in varBindings)
				_varBindings[varBinding.Key] = varBinding.Value;
		}

		public bool Remove(string variableName)
		{
			return _varBindings.Remove(variableName);
		}

		public bool TryGetValue(string variableName, out FeatureValue value)
		{
			return _varBindings.TryGetValue(variableName, out value);
		}

		public FeatureValue this[string variableName]
		{
			get { return _varBindings[variableName]; }
			set { _varBindings[variableName] = value; }
		}

		ICollection<string> IDictionary<string, FeatureValue>.Keys
		{
			get { return _varBindings.Keys; }
		}

		ICollection<FeatureValue> IDictionary<string, FeatureValue>.Values
		{
			get { return _varBindings.Values; }
		}

		IEnumerator<KeyValuePair<string, FeatureValue>> IEnumerable<KeyValuePair<string, FeatureValue>>.GetEnumerator()
		{
			return _varBindings.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<string, FeatureValue>>) this).GetEnumerator();
		}

		void ICollection<KeyValuePair<string, FeatureValue>>.Add(KeyValuePair<string, FeatureValue> item)
		{
			((ICollection<KeyValuePair<string, FeatureValue>>) _varBindings).Add(item);
		}

		public void Clear()
		{
			_varBindings.Clear();
		}

		bool ICollection<KeyValuePair<string, FeatureValue>>.Contains(KeyValuePair<string, FeatureValue> item)
		{
			return ((ICollection<KeyValuePair<string, FeatureValue>>) _varBindings).Contains(item);
		}

		void ICollection<KeyValuePair<string, FeatureValue>>.CopyTo(KeyValuePair<string, FeatureValue>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<string, FeatureValue>>) _varBindings).CopyTo(array, arrayIndex);
		}

		bool ICollection<KeyValuePair<string, FeatureValue>>.Remove(KeyValuePair<string, FeatureValue> item)
		{
			return ((ICollection<KeyValuePair<string, FeatureValue>>) _varBindings).Remove(item);
		}

		public int Count
		{
			get { return _varBindings.Count; }
		}

		bool ICollection<KeyValuePair<string, FeatureValue>>.IsReadOnly
		{
			get { return false; }
		}

		public VariableBindings Clone()
		{
			return new VariableBindings(this);
		}
	}
}
