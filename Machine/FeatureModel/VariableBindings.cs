using System.Collections;
using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.FeatureModel
{
	public class VariableBindings : IDictionary<string, SimpleFeatureValue>, ICloneable<VariableBindings>
	{
		private readonly Dictionary<string, SimpleFeatureValue> _varBindings;

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
			_varBindings.Add(variableName, value);
		}

		public void Replace(VariableBindings varBindings)
		{
			foreach (KeyValuePair<string, SimpleFeatureValue> varBinding in varBindings)
				_varBindings[varBinding.Key] = varBinding.Value;
		}

		public bool Remove(string variableName)
		{
			return _varBindings.Remove(variableName);
		}

		public bool TryGetValue(string variableName, out SimpleFeatureValue value)
		{
			return _varBindings.TryGetValue(variableName, out value);
		}

		public SimpleFeatureValue this[string variableName]
		{
			get { return _varBindings[variableName]; }
			set { _varBindings[variableName] = value; }
		}

		ICollection<string> IDictionary<string, SimpleFeatureValue>.Keys
		{
			get { return _varBindings.Keys; }
		}

		public ICollection<SimpleFeatureValue> Values
		{
			get { return _varBindings.Values; }
		}

		IEnumerator<KeyValuePair<string, SimpleFeatureValue>> IEnumerable<KeyValuePair<string, SimpleFeatureValue>>.GetEnumerator()
		{
			return _varBindings.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<string, FeatureValue>>) this).GetEnumerator();
		}

		void ICollection<KeyValuePair<string, SimpleFeatureValue>>.Add(KeyValuePair<string, SimpleFeatureValue> item)
		{
			((ICollection<KeyValuePair<string, SimpleFeatureValue>>) _varBindings).Add(item);
		}

		public void Clear()
		{
			_varBindings.Clear();
		}

		bool ICollection<KeyValuePair<string, SimpleFeatureValue>>.Contains(KeyValuePair<string, SimpleFeatureValue> item)
		{
			return ((ICollection<KeyValuePair<string, SimpleFeatureValue>>) _varBindings).Contains(item);
		}

		void ICollection<KeyValuePair<string, SimpleFeatureValue>>.CopyTo(KeyValuePair<string, SimpleFeatureValue>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<string, SimpleFeatureValue>>) _varBindings).CopyTo(array, arrayIndex);
		}

		bool ICollection<KeyValuePair<string, SimpleFeatureValue>>.Remove(KeyValuePair<string, SimpleFeatureValue> item)
		{
			return ((ICollection<KeyValuePair<string, SimpleFeatureValue>>) _varBindings).Remove(item);
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
