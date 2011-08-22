using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE.FeatureModel
{
	public class FeatureStructure : FeatureValue
	{
		public static DisjunctiveFeatureStructureBuilder Build(FeatureSystem featSys)
		{
			return new DisjunctiveFeatureStructureBuilder(featSys);
		}

		private readonly SortedDictionary<Feature, FeatureValue> _definite;
		private readonly HashSet<HashSet<FeatureStructure>> _indefinite;

		private readonly Dictionary<string, FeatureValue> _varBindings; 

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureStructure"/> class.
		/// </summary>
		public FeatureStructure()
		{
			_definite = new SortedDictionary<Feature, FeatureValue>();
			_indefinite = new HashSet<HashSet<FeatureStructure>>();
			_varBindings = new Dictionary<string, FeatureValue>();
		}

		public FeatureStructure(FeatureStructure fs)
			: this(fs, new Dictionary<FeatureValue, FeatureValue>(new IdentityEqualityComparer<FeatureValue>()))
		{
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="fs">The fs.</param>
		/// <param name="copies"></param>
		private FeatureStructure(FeatureStructure fs, IDictionary<FeatureValue, FeatureValue> copies)
			: this()
		{
			copies[this] = fs;
			foreach (KeyValuePair<Feature, FeatureValue> featVal in fs._definite)
				_definite[featVal.Key] = featVal.Value.Clone(copies);

			CopyDisjunctions(fs, this, copies);
		}

		/// <summary>
		/// Gets the features.
		/// </summary>
		/// <value>The features.</value>
		public IEnumerable<Feature> Features
		{
			get
			{
				if (Forward != null)
					return ((FeatureStructure) Forward).Features;

				return _definite.Keys;
			}
		}

		public IEnumerable<IEnumerable<FeatureStructure>> Disjunctions
		{
			get
			{
				if (Forward != null)
					return ((FeatureStructure) Forward).Disjunctions;

				return _indefinite.Cast<IEnumerable<FeatureStructure>>();
			}
		}

		/// <summary>
		/// Gets the number of features.
		/// </summary>
		/// <value>The number of features.</value>
		public int NumValues
		{
			get
			{
				if (Forward != null)
					return ((FeatureStructure) Forward).NumValues;

				return _definite.Count;
			}
		}

		public int NumDisjunctions
		{
			get
			{
				if (Forward != null)
					return ((FeatureStructure) Forward).NumDisjunctions;

				return _indefinite.Count;
			}
		}

		public override FeatureValueType Type
		{
			get
			{
				return FeatureValueType.Complex;
			}
		}

		/// <summary>
		/// Adds the specified feature-value pair.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <param name="value">The value.</param>
		public void AddValue(Feature feature, FeatureValue value)
		{
			if (Forward != null)
			{
				((FeatureStructure) Forward).AddValue(feature, value);
				return;
			}

			_definite[feature] = value;
		}

		public void AddValue(IEnumerable<Feature> path, FeatureValue value)
		{
			if (Forward != null)
			{
				((FeatureStructure) Forward).AddValue(path, value);
				return;
			}

			Feature f = path.First();
			IEnumerable<Feature> remaining = path.Skip(1);
			if (remaining.Any())
			{
				FeatureValue curValue;
				if (_definite.TryGetValue(f, out curValue))
				{
					var fs = curValue as FeatureStructure;
					if (fs != null)
						fs.AddValue(remaining, value);
					else
						throw new ArgumentException("The feature path is invalid.", "path");
				}
				else
				{
					throw new ArgumentException("The feature path is invalid.", "path");
				}
			}
			else
			{
				AddValue(f, value);
			}
		}

		public void AddValues(FeatureStructure fs)
		{
			foreach (KeyValuePair<Feature, FeatureValue> featVal in fs._definite)
			{
				FeatureValue curValue;
				if (_definite.TryGetValue(featVal.Key, out curValue))
				{
					var curFS = curValue as FeatureStructure;
					if (curFS != null)
						curFS.AddValues((FeatureStructure) featVal.Value);
					else
						_definite[featVal.Key] = featVal.Value.Clone();
				}
				else
				{
					_definite[featVal.Key] = featVal.Value.Clone();
				}
			}
		}

		public void AddDisjunction(IEnumerable<FeatureStructure> disjunction)
		{
			if (Forward != null)
			{
				((FeatureStructure) Forward).AddDisjunction(disjunction);
				return;
			}

			_indefinite.Add(new HashSet<FeatureStructure>(disjunction));
		}

		public bool TryGetVariableBinding(string name, out FeatureValue value)
		{
			if (Forward != null)
				return ((FeatureStructure) Forward).TryGetVariableBinding(name, out value);

			if (_varBindings.TryGetValue(name, out value))
				return true;

			value = null;
			return false;
		}

		public void AddVariableBinding(string name, FeatureValue value)
		{
			if (Forward != null)
			{
				((FeatureStructure) Forward).AddVariableBinding(name, value);
				return;
			}

			_varBindings[name] = value;
		}

		public void Clear()
		{
			if (Forward != null)
			{
				((FeatureStructure) Forward).Clear();
				return;
			}

			ClearValues();
			ClearDisjunctions();
		}

		public void ClearValues()
		{
			if (Forward != null)
			{
				((FeatureStructure) Forward).ClearValues();
				return;
			}

			_definite.Clear();
		}

		public void ClearDisjunctions()
		{
			if (Forward != null)
			{
				((FeatureStructure) Forward).ClearDisjunctions();
				return;
			}

			_indefinite.Clear();
		}

		/// <summary>
		/// Gets the values for the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>All values.</returns>
		public FeatureValue GetValue(Feature feature)
		{
			if (Forward != null)
				return ((FeatureStructure) Forward).GetValue(feature);

			try
			{
				return _definite[feature];
			}
			catch (KeyNotFoundException ex)
			{
				throw new ArgumentException("The specified value could not be found.", "feature", ex);
			}
		}

		public FeatureValue GetValue(IEnumerable<Feature> path)
		{
			if (Forward != null)
				return ((FeatureStructure) Forward).GetValue(path);

			Feature f = path.First();
			IEnumerable<Feature> remaining = path.Skip(1);
			if (remaining.Any())
			{
				FeatureValue curValue;
				if (_definite.TryGetValue(f, out curValue))
				{
					var fs = curValue as FeatureStructure;
					if (fs != null)
						return fs.GetValue(remaining);
				}
				throw new ArgumentException("The specified path is not valid.", "path");
			}

			return GetValue(f);
		}

		public bool IsUnifiable(FeatureValue other, bool useDefaults, bool definite)
		{
			if (Forward != null)
				return ((FeatureStructure) Forward).IsUnifiable(other, useDefaults, definite);

			return IsUnifiable(other, useDefaults, definite, _varBindings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone()));
		}

		/// <summary>
		/// Determines whether the specified set of feature values is compatible with this
		/// set of feature values. It is much like <c>Matches</c> except that if a the
		/// specified set does not contain a feature in this set, it is still a match.
		/// It basically checks to make sure that there is no contradictory features.
		/// </summary>
		/// <param name="other">The feature value.</param>
		/// <param name="useDefaults"></param>
		/// <param name="definite"></param>
		/// <param name="varBindings"></param>
		/// <returns>
		/// 	<c>true</c> the sets are compatible, otherwise <c>false</c>.
		/// </returns>
		public bool IsUnifiable(FeatureValue other, bool useDefaults, bool definite, IDictionary<string, FeatureValue> varBindings)
		{
			if (Forward != null)
				return ((FeatureStructure) Forward).IsUnifiable(other, useDefaults, definite, varBindings);

			if (definite)
				return IsUnifiable(other, useDefaults, varBindings);

			FeatureStructure output;
			return Unify(other, false, false, varBindings, out output);
		}

		internal override bool IsUnifiable(FeatureValue other, bool useDefaults, IDictionary<string, FeatureValue> varBindings)
		{
			if (Forward != null)
				return Forward.IsUnifiable(other, useDefaults, varBindings);

			FeatureStructure fs;
			if (!GetValue(other, out fs))
				return false;

			foreach (KeyValuePair<Feature, FeatureValue> featVal in fs._definite)
			{
				FeatureValue curValue;
				if (_definite.TryGetValue(featVal.Key, out curValue))
				{
					if (!curValue.IsUnifiable(featVal.Value, useDefaults, varBindings))
						return false;
				}
				else if (useDefaults && featVal.Key.DefaultValue != null)
				{
					if (!featVal.Key.DefaultValue.IsUnifiable(featVal.Value, true, varBindings))
						return false;
				}
			}
			return true;
		}

		public bool Unify(FeatureValue other, bool useDefaults, bool definite, out FeatureStructure output)
		{
			if (Forward != null)
				return ((FeatureStructure) Forward).Unify(other, useDefaults, definite, out output);

			return Unify(other, useDefaults, definite, _varBindings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone()),
				out output);
		}

		public bool Unify(FeatureValue other, bool useDefaults, bool definite, IDictionary<string, FeatureValue> varBindings,
			out FeatureStructure output)
		{
			if (Forward != null)
				return ((FeatureStructure) Forward).Unify(other, useDefaults, definite, varBindings, out output);

			FeatureStructure fs;
			if (!GetValue(other, out fs))
			{
				output = null;
				return false;
			}

			var copies = new Dictionary<FeatureValue, FeatureValue>(new IdentityEqualityComparer<FeatureValue>());
			FeatureValue newFv;
			if (!UnifyCopy(fs, useDefaults, copies, varBindings, out newFv))
			{
				output = null;
				return false;
			}

			var newFs = (FeatureStructure) newFv;
			if (!definite && newFs.NumDisjunctions > 0)
			{
				// TODO: use variable bindings
				if (!CheckIndefinite(newFs, newFs, useDefaults, out newFs))
				{
					output = null;
					return false;
				}

				if (newFs.NumDisjunctions > 0)
				{
					for (int n = 1; n < newFs.NumDisjunctions; n++)
						NWiseConsistency(newFs, n, useDefaults, out newFs);
				}
			}

			foreach (KeyValuePair<string, FeatureValue> kvp in varBindings)
				newFs.AddVariableBinding(kvp.Key, kvp.Value);

			output = newFs;
			return true;
		}

		internal override bool DestructiveUnify(FeatureValue other, bool useDefaults, bool preserveInput,
			IDictionary<FeatureValue, FeatureValue> copies, IDictionary<string, FeatureValue> varBindings)
		{
			if (Forward != null)
				return Forward.DestructiveUnify(other, useDefaults, preserveInput, copies, varBindings);

			FeatureStructure fs;
			if (!GetValue(other, out fs))
				return false;

			if (preserveInput)
			{
				if (copies != null)
					copies[fs] = this;
			}
			else
			{
				fs.Forward = this;
			}

			foreach (KeyValuePair<Feature, FeatureValue> featVal in fs._definite)
			{
				FeatureValue curValue;
				if (_definite.TryGetValue(featVal.Key, out curValue))
				{
					if (!curValue.DestructiveUnify(featVal.Value, useDefaults, preserveInput, copies, varBindings))
						return false;
				}
				else if (useDefaults && featVal.Key.DefaultValue != null)
				{
					curValue = featVal.Key.DefaultValue.Clone();
					_definite[featVal.Key] = curValue;
					if (!curValue.DestructiveUnify(featVal.Value, true, preserveInput, copies, varBindings))
						return false;
				}
				else
				{
					FeatureValue value;
					if (preserveInput)
						value = copies != null ? featVal.Value.Clone(copies) : featVal.Value.Clone();
					else
						value = featVal.Value;
					_definite[featVal.Key] = value;
				}
			}

			return true;
		}

		protected override bool UnifyCopy(FeatureValue other, bool useDefaults, IDictionary<FeatureValue, FeatureValue> copies,
			IDictionary<string, FeatureValue> varBindings, out FeatureValue output)
		{
			FeatureStructure fs;
			if (!GetValue(other, out fs))
			{
				output = null;
				return false;
			}

			var copy = new FeatureStructure();
			copies[this] = copy;
			copies[other] = copy;
			foreach (KeyValuePair<Feature, FeatureValue> featVal in fs._definite)
			{
				FeatureValue curValue;
				if (_definite.TryGetValue(featVal.Key, out curValue))
				{
					FeatureValue newValue;
					if (!curValue.Unify(featVal.Value, useDefaults, copies, varBindings, out newValue))
					{
						output = null;
						return false;
					}
					copy.AddValue(featVal.Key, newValue);
				}
				else if (useDefaults && featVal.Key.DefaultValue != null)
				{
					curValue = featVal.Key.DefaultValue.Clone();
					FeatureValue newValue;
					if (!curValue.Unify(featVal.Value, true, copies, varBindings, out newValue))
					{
						output = null;
						return false;
					}
					copy.AddValue(featVal.Key, newValue);
				}
				else
				{
					copy.AddValue(featVal.Key, featVal.Value.Clone(copies));
				}
			}

			foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite)
			{
				if (!fs._definite.ContainsKey(featVal.Key))
					copy.AddValue(featVal.Key, featVal.Value.Clone(copies));
			}

			CopyDisjunctions(this, copy, copies);
			CopyDisjunctions(fs, copy, copies);

			output = copy;
			return true;
		}

		private static void CopyDisjunctions(FeatureStructure src, FeatureStructure dest, IDictionary<FeatureValue, FeatureValue> mapping)
		{
			foreach (IEnumerable<FeatureStructure> disjunction in src.Disjunctions)
			{
				var newDisjunction = new HashSet<FeatureStructure>();
				foreach (FeatureStructure disjunct in disjunction)
					newDisjunction.Add((FeatureStructure)disjunct.Clone(mapping));
				dest.AddDisjunction(newDisjunction);
			}
		}

		internal override FeatureValue Clone(IDictionary<FeatureValue, FeatureValue> copies)
		{
			if (Forward != null)
				return Forward.Clone(copies);

			FeatureValue clone;
			if (copies.TryGetValue(this, out clone))
				return clone;

			return new FeatureStructure(this, copies);
		}

		private bool CheckIndefinite(FeatureStructure fs, FeatureStructure cond, bool useDefaults, out FeatureStructure newFs)
		{
			var indefinite = new HashSet<IEnumerable<FeatureStructure>>(fs.Disjunctions);
			newFs = fs;
			bool uncheckedParts = true;
			while (uncheckedParts)
			{
				uncheckedParts = false;
				newFs.ClearDisjunctions();

				foreach (HashSet<FeatureStructure> disjunction in indefinite)
				{
					var newDisjunction = new HashSet<FeatureStructure>();
					foreach (FeatureStructure disjunct in disjunction)
					{
						if (cond.IsUnifiable(disjunct, useDefaults, true))
						{
							if (disjunct.NumDisjunctions > 0)
							{
								FeatureStructure newDisjunct;
								if (CheckIndefinite(disjunct, cond, useDefaults, out newDisjunct))
									newDisjunction.Add(newDisjunct);
							}
							else
							{
								newDisjunction.Add(disjunct);
							}
						}
					}

					if (newDisjunction.Count == 0)
					{
						newFs = null;
						return false;
					}
					else if (newDisjunction.Count == 1)
					{
						FeatureStructure disjunct = newDisjunction.First();
						newFs.Unify(disjunct, useDefaults, true, out newFs);
						uncheckedParts = true;
					}
					else
					{
						newFs.AddDisjunction(newDisjunction);
					}
				}
				cond = newFs;
				indefinite.Clear();
				indefinite.UnionWith(newFs.Disjunctions);
			}

			return true;
		}

		private bool NWiseConsistency(FeatureStructure fs, int n, bool useDefaults, out FeatureStructure newFs)
		{
			newFs = fs;
			if (fs.NumDisjunctions <= n)
				return true;

			var indefinite = new HashSet<IEnumerable<FeatureStructure>>(newFs.Disjunctions);
			newFs.ClearDisjunctions();

			while (indefinite.Any())
			{
				IEnumerable<FeatureStructure> disjunction = indefinite.First();
				indefinite.Remove(disjunction);
				var newDisjunction = new HashSet<FeatureStructure>();

				foreach (FeatureStructure disjunct in disjunction)
				{
					FeatureStructure hypFs;
					fs.Unify(disjunct, useDefaults, true, out hypFs);
					foreach (HashSet<FeatureStructure> disj in indefinite)
						hypFs.AddDisjunction(disj);

					FeatureStructure nFs;
					if (n == 1 ? CheckIndefinite(hypFs, hypFs, useDefaults, out nFs)
						: NWiseConsistency(hypFs, n - 1, useDefaults, out nFs))
					{
						newDisjunction.Add(nFs);
					}
				}

				if (newDisjunction.Count == 0)
				{
					newFs = null;
					return false;
				}
				else if (newDisjunction.Count == 1)
				{
					FeatureStructure nFs = newDisjunction.First();
					newFs = nFs;
					indefinite.Clear();
					indefinite.UnionWith(newFs.Disjunctions);
					newFs.ClearDisjunctions();
				}
				else
				{
					newFs.AddDisjunction(newDisjunction);
				}
			}

			return true;
		}

		internal override bool Negation(out FeatureValue output)
		{
			if (Forward != null)
				return Forward.Negation(out output);

			FeatureStructure fs;
			if (!Negation(out fs))
			{
				output = null;
				return false;
			}

			output = fs;
			return true;
		}

		public bool Negation(out FeatureStructure output)
		{
			if (Forward != null)
				return ((FeatureStructure) Forward).Negation(out output);

			output = null;
			foreach (HashSet<FeatureStructure> disjunction in _indefinite)
			{
				foreach (FeatureStructure disjunct in disjunction)
				{
					FeatureStructure negation;
					if (!disjunct.Negation(out negation))
					{
						output = null;
						return false;
					}

					if (output == null)
					{
						output = negation;
					}
					else
					{
						if (!output.Unify(negation, false, true, out output))
						{
							output = null;
							return false;
						}
					}
				}
			}

			if (output == null)
				output = new FeatureStructure();

			var newDisjunction = new HashSet<FeatureStructure>();
			foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite)
			{
				FeatureValue value;
				if (!kvp.Value.Negation(out value))
				{
					output = null;
					return false;
				}
				var fs = new FeatureStructure();
				fs.AddValue(kvp.Key, value);
				newDisjunction.Add(fs);
			}
			output.AddDisjunction(newDisjunction);
			return true;
		}

		public override FeatureValue Clone()
		{
			if (Forward != null)
				return Forward.Clone();

			return new FeatureStructure(this);
		}

		public override int GetHashCode()
		{
			if (Forward != null)
				return Forward.GetHashCode();

			return _definite.Aggregate(0, (current, kvp) => current ^ (kvp.Key.GetHashCode() ^ (kvp.Value != null ? kvp.Value.GetHashCode() : 0)));
		}

		public override bool Equals(object obj)
		{
			if (Forward != null)
				return Forward.Equals(obj);

			if (obj == null)
				return false;
			return Equals(obj as FeatureStructure);
		}

		public bool Equals(FeatureStructure other)
		{
			if (Forward != null)
				return ((FeatureStructure) Forward).Equals(other);

			if (other == null)
				return false;

			other = GetValue(other);

			if (_definite.Count != other._definite.Count)
				return false;

			foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite)
			{
				FeatureValue value;
				if (!other._definite.TryGetValue(kvp.Key, out value))
					return false;

				if (kvp.Value != null && !kvp.Value.Equals(value))
					return false;
			}

			return true;
		}

		public override string ToString()
		{
			if (Forward != null)
				return Forward.ToString();

			bool firstFeature = true;
			var sb = new StringBuilder();
			sb.Append("[");
			foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite)
			{
				if (!firstFeature)
					sb.Append(", ");
				sb.Append(kvp.Key.Description);
				if (kvp.Value != null)
				{
					sb.Append(":");
					sb.Append(kvp.Value.ToString());
				}
				firstFeature = false;
			}
			sb.Append("]");
			return sb.ToString();
		}
	}
}
