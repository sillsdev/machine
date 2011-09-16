using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.APRE.FeatureModel.Fluent;

namespace SIL.APRE.FeatureModel
{
	public class FeatureStruct : FeatureValue, IEquatable<FeatureStruct>
	{
		public static IDisjunctiveFeatureStructSyntax With(FeatureSystem featSys)
		{
			return new FeatureStructBuilder(featSys);
		}

		private readonly IDBearerDictionary<Feature, FeatureValue> _definite;
		private readonly HashSet<Disjunction> _indefinite;

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureStruct"/> class.
		/// </summary>
		public FeatureStruct()
		{
			_definite = new IDBearerDictionary<Feature, FeatureValue>();
			_indefinite = new HashSet<Disjunction>();
		}

		public FeatureStruct(FeatureStruct other)
			: this(other, new Dictionary<FeatureValue, FeatureValue>(new IdentityEqualityComparer<FeatureValue>()))
		{
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="other">The fs.</param>
		/// <param name="copies"></param>
		private FeatureStruct(FeatureStruct other, IDictionary<FeatureValue, FeatureValue> copies)
			: this()
		{
			copies[this] = other;
			foreach (KeyValuePair<Feature, FeatureValue> featVal in other._definite)
				_definite[featVal.Key] = Dereference(featVal.Value).Clone(copies);

			CopyDisjunctions(other, this, copies);
		}

		/// <summary>
		/// Gets the features.
		/// </summary>
		/// <value>The features.</value>
		public IEnumerable<Feature> Features
		{
			get { return _definite.Keys; }
		}

		public IEnumerable<Disjunction> Disjunctions
		{
			get { return _indefinite; }
		}

		/// <summary>
		/// Gets the number of features.
		/// </summary>
		/// <value>The number of features.</value>
		public int NumValues
		{
			get { return _definite.Count; }
		}

		public int NumDisjunctions
		{
			get { return _indefinite.Count; }
		}

		/// <summary>
		/// Adds the specified feature-value pair.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <param name="value">The value.</param>
		public void AddValue(Feature feature, FeatureValue value)
		{
			_definite[feature] = value;
		}

		public void AddValue(IEnumerable<Feature> path, FeatureValue value)
		{
		    Feature lastFeature;
			FeatureStruct lastFS;
			if (FollowPath(path, out lastFeature, out lastFS))
				lastFS._definite[lastFeature] = value;

			throw new ArgumentException("The feature path is invalid.", "path");
		}

		public void RemoveValue(Feature feature)
		{
			_definite.Remove(feature);
		}

		public void RemoveValue(IEnumerable<Feature> path)
		{
		    Feature lastFeature;
			FeatureStruct lastFS;
			if (FollowPath(path, out lastFeature, out lastFS))
				lastFS._definite.Remove(lastFeature);

			throw new ArgumentException("The feature path is invalid.", "path");
		}

		public void Replace(FeatureStruct other)
		{
			foreach (KeyValuePair<Feature, FeatureValue> featVal in other._definite)
			{
				FeatureValue otherValue = Dereference(featVal.Value);
				FeatureValue thisValue;
				if (_definite.TryGetValue(featVal.Key, out thisValue))
				{
					thisValue = Dereference(thisValue);
					var thisFS = thisValue as FeatureStruct;
					if (thisFS != null)
						thisFS.Replace((FeatureStruct) otherValue);
					else
						_definite[featVal.Key] = otherValue.Clone();
				}
				else
				{
					_definite[featVal.Key] = otherValue.Clone();
				}
			}

			// TODO: what do we do about disjunctions?
		}

		public void Merge(FeatureStruct other)
		{
			Merge(other, new VariableBindings());
		}

		public void Merge(FeatureStruct other, VariableBindings varBindings)
		{
			foreach (KeyValuePair<Feature, FeatureValue> featVal in other._definite)
			{
				FeatureValue otherValue = Dereference(featVal.Value);
				FeatureValue thisValue;
				if (_definite.TryGetValue(featVal.Key, out thisValue))
				{
					thisValue = Dereference(thisValue);
					thisValue.Merge(otherValue, varBindings);
				}
				else
				{
					_definite[featVal.Key] = otherValue.Clone();
				}
			}

			// TODO: what do we do about disjunctions?
		}

		internal override void Merge(FeatureValue other, VariableBindings varBindings)
		{
			FeatureStruct otherFS;
			if (Dereference(other, out otherFS))
				Merge(otherFS, varBindings);
		}

		public void Subtract(FeatureStruct other)
		{
			Subtract(other, new VariableBindings());
		}

		public void Subtract(FeatureStruct other, VariableBindings varBindings)
		{
			foreach (KeyValuePair<Feature, FeatureValue> featVal in other._definite)
			{
				FeatureValue otherValue = Dereference(featVal.Value);
				FeatureValue thisValue;
				if (_definite.TryGetValue(featVal.Key, out thisValue))
				{
					thisValue = Dereference(thisValue);
					if (!thisValue.Subtract(otherValue, varBindings))
						_definite.Remove(featVal.Key);
				}
			}

			// TODO: what do we do about disjunctions?
		}

		internal override bool Subtract(FeatureValue other, VariableBindings varBindings)
		{
			FeatureStruct otherFS;
			if (Dereference(other, out otherFS))
				Subtract(otherFS, varBindings);
			return _definite.Count > 0;
		}

		public void AddDisjunction(Disjunction disjunction)
		{
			_indefinite.Add(disjunction);
		}

		public void Clear()
		{
			ClearValues();
			ClearDisjunctions();
		}

		public void ClearValues()
		{
			_definite.Clear();
		}

		public void ClearDisjunctions()
		{
			_indefinite.Clear();
		}

		/// <summary>
		/// Gets the values for the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>All values.</returns>
		public FeatureValue GetValue(Feature feature)
		{
			FeatureValue value;
			if (TryGetValue(feature, out value))
				return value;

			throw new ArgumentException("The specified value could not be found.", "feature");
		}

		public T GetValue<T>(Feature feature) where T : FeatureValue
		{
			T value;
			if (TryGetValue(feature, out value))
				return value;

			throw new ArgumentException("The specified value could not be found.", "feature");
		}

		public FeatureValue GetValue(string featureID)
		{
			FeatureValue value;
			if (TryGetValue(featureID, out value))
				return value;

			throw new ArgumentException("The specified value could not be found.", "featureID");
		}

		public T GetValue<T>(string featureID) where T : FeatureValue
		{
			T value;
			if (TryGetValue(featureID, out value))
				return value;

			throw new ArgumentException("The specified value could not be found.", "featureID");
		}

		public FeatureValue GetValue(IEnumerable<Feature> path)
		{
			FeatureValue value;
			if (TryGetValue(path, out value))
				return value;

			throw new ArgumentException("The specified path is not valid.", "path");
		}

		public T GetValue<T>(IEnumerable<Feature> path) where T : FeatureValue
		{
			T value;
			if (TryGetValue(path, out value))
				return value;

			throw new ArgumentException("The specified path is not valid.", "path");
		}

		public FeatureValue GetValue(IEnumerable<string> path)
		{
			FeatureValue value;
			if (TryGetValue(path, out value))
				return value;

			throw new ArgumentException("The specified path is not valid.", "path");
		}

		public T GetValue<T>(IEnumerable<string> path) where T : FeatureValue
		{
			T value;
			if (TryGetValue(path, out value))
				return value;

			throw new ArgumentException("The specified path is not valid.", "path");
		}

		public bool TryGetValue<T>(Feature feature, out T value) where T : FeatureValue
		{
			FeatureValue val;
			if (_definite.TryGetValue(feature, out val))
				return Dereference(val, out value);
			value = null;
			return false;
		}

		public bool TryGetValue<T>(string featureID, out T value) where T : FeatureValue
		{
			FeatureValue val;
			if (_definite.TryGetValue(featureID, out val))
				return Dereference(val, out value);
			value = null;
			return false;
		}

		public bool TryGetValue<T>(IEnumerable<Feature> path, out T value) where T : FeatureValue
		{
		    Feature lastFeature;
			FeatureStruct lastFS;
			if (FollowPath(path, out lastFeature, out lastFS))
			{
				FeatureValue val;
				if (lastFS._definite.TryGetValue(lastFeature, out val))
					return Dereference(val, out value);
			}
			value = null;
			return false;
		}

		public bool TryGetValue<T>(IEnumerable<string> path, out T value) where T : FeatureValue
		{
		    string lastID;
			FeatureStruct lastFS;
			if (FollowPath(path, out lastID, out lastFS))
			{
				FeatureValue val;
				if (lastFS._definite.TryGetValue(lastID, out val))
					return Dereference(val, out value);
			}
			value = null;
			return false;
		}

		public bool ContainsFeature(Feature feature)
		{
			return _definite.ContainsKey(feature);
		}

		public bool ContainsFeature(string featureID)
		{
			return _definite.ContainsKey(featureID);
		}

		public bool ContainsFeature(IEnumerable<Feature> path)
		{
			Feature lastFeature;
			FeatureStruct lastFS;
			if (FollowPath(path, out lastFeature, out lastFS))
				return lastFS._definite.ContainsKey(lastFeature);
			return false;
		}

		public bool ContainsFeature(IEnumerable<string> path)
		{
			string lastID;
			FeatureStruct lastFS;
			if (FollowPath(path, out lastID, out lastFS))
				return lastFS._definite.ContainsKey(lastID);
			return false;
		}

		private bool FollowPath(IEnumerable<string> path, out string lastID, out FeatureStruct lastFS)
		{
			lastFS = this;
			lastID = null;
			foreach (string id in path)
			{
				if (lastID != null)
				{
					FeatureValue curValue;
					if (!lastFS._definite.TryGetValue(lastID, out curValue) || !Dereference(curValue, out lastFS))
					{
					    lastID = null;
                        lastFS = null;
						return false;
					}
				}
				lastID = id;
			}

			return true;
		}

        private bool FollowPath(IEnumerable<Feature> path, out Feature lastFeature, out FeatureStruct lastFS)
        {
            lastFS = this;
            lastFeature = null;
            foreach (Feature feature in path)
            {
                if (lastFeature != null)
                {
                    FeatureValue curValue;
                    if (!lastFS._definite.TryGetValue(lastFeature, out curValue) || !Dereference(curValue, out lastFS))
                    {
                        lastFeature = null;
                        lastFS = null;
                        return false;
                    }
                }
                lastFeature = feature;
            }

            return true;
        }

		public void ReplaceVariables(VariableBindings varBindings)
		{
			var replacements = new Dictionary<Feature, FeatureValue>();
			foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite)
			{
				FeatureValue value = Dereference(featVal.Value);
				var sfv = value as SimpleFeatureValue;
				if (sfv != null && sfv.IsVariable)
				{
					FeatureValue binding;
					if (varBindings.TryGetValue(sfv.VariableName, out binding))
					{
						if (sfv.Agree)
							binding = binding.Clone();
						else
							binding.Negation(out binding);
						replacements[featVal.Key] = binding;
					}
				}
				else
				{
					var fs = value as FeatureStruct;
					if (fs != null)
						fs.ReplaceVariables(varBindings);
				}
			}

			foreach (KeyValuePair<Feature, FeatureValue> replacement in replacements)
				_definite[replacement.Key] = replacement.Value;

			// TODO: what do we do about disjunctions?
		}

		public bool IsUnifiable(FeatureValue other)
		{
			return IsUnifiable(other, false);
		}

		public bool IsUnifiable(FeatureValue other, bool useDefaults)
		{
			return IsUnifiable(other, useDefaults, null);
		}

		public bool IsUnifiable(FeatureValue other, VariableBindings varBindings)
		{
			return IsUnifiable(other, false, varBindings);
		}

		/// <summary>
		/// Determines whether the specified set of feature values is compatible with this
		/// set of feature values. It is much like <c>Matches</c> except that if a the
		/// specified set does not contain a feature in this set, it is still a match.
		/// It basically checks to make sure that there is no contradictory features.
		/// </summary>
		/// <param name="other">The feature value.</param>
		/// <param name="useDefaults"></param>
		/// <param name="varBindings"></param>
		/// <returns>
		/// 	<c>true</c> the sets are compatible, otherwise <c>false</c>.
		/// </returns>
		public bool IsUnifiable(FeatureValue other, bool useDefaults, VariableBindings varBindings)
		{
			FeatureStruct otherFS;
			if (!Dereference(other, out otherFS))
				return false;

			if (_indefinite.Count == 0 && otherFS._indefinite.Count == 0)
			{
				VariableBindings definiteVarBindings = varBindings == null ? new VariableBindings() : varBindings.Clone();
				if (IsDefiniteUnifiable(otherFS, useDefaults, definiteVarBindings))
				{
					if (varBindings != null)
						varBindings.Replace(definiteVarBindings);
					return true;
				}
				return false;
			}

			FeatureStruct output;
			return Unify(other, useDefaults, varBindings, out output);
		}

		internal override bool IsDefiniteUnifiable(FeatureValue other, bool useDefaults, VariableBindings varBindings)
		{
			FeatureStruct otherFS;
			if (!Dereference(other, out otherFS))
				return false;

			foreach (KeyValuePair<Feature, FeatureValue> featVal in otherFS._definite)
			{
				FeatureValue otherValue = Dereference(featVal.Value);
				FeatureValue thisValue;
				if (_definite.TryGetValue(featVal.Key, out thisValue))
				{
					thisValue = Dereference(thisValue);
					if (!thisValue.IsDefiniteUnifiable(otherValue, useDefaults, varBindings))
						return false;
				}
				else if (useDefaults && featVal.Key.DefaultValue != null)
				{
					if (!featVal.Key.DefaultValue.IsDefiniteUnifiable(otherValue, true, varBindings))
						return false;
				}
			}
			return true;
		}

		public bool Unify(FeatureValue other, out FeatureStruct output)
		{
			return Unify(other, false, out output);
		}

		public bool Unify(FeatureValue other, bool useDefaults, out FeatureStruct output)
		{
			return Unify(other, useDefaults, null, out output);
		}

		public bool Unify(FeatureValue other, VariableBindings varBindings, out FeatureStruct output)
		{
			return Unify(other, false, varBindings, out output);
		}

		public bool Unify(FeatureValue other, bool useDefaults, VariableBindings varBindings, out FeatureStruct output)
		{
			return Unify(other, useDefaults, varBindings, true, out output);
		}

		public bool Unify(FeatureValue other, bool useDefaults, VariableBindings varBindings, bool checkDisjunctiveConsistency, out FeatureStruct output)
		{
			FeatureStruct otherFS;
			if (!Dereference(other, out otherFS))
			{
				output = null;
				return false;
			}

			VariableBindings tempVarBindings = varBindings == null ? new VariableBindings() : varBindings.Clone();
			FeatureValue newFV;
			if (!UnifyDefinite(otherFS, useDefaults, tempVarBindings, out newFV))
			{
				output = null;
				return false;
			}

			var newFS = (FeatureStruct) newFV;
			if (newFS._indefinite.Count > 0)
			{
				if (!CheckIndefinite(newFS, newFS, useDefaults, tempVarBindings, out newFS))
				{
					output = null;
					return false;
				}

				if (checkDisjunctiveConsistency)
					newFS.CheckDisjunctiveConsistency(useDefaults, tempVarBindings, out newFS);
			}

			if (varBindings != null)
				varBindings.Replace(tempVarBindings);
			output = newFS;
			return true;
		}

		public bool CheckDisjunctiveConsistency(bool useDefaults, VariableBindings varBindings, out FeatureStruct output)
		{
			output = this;
			if (_indefinite.Count > 0)
			{
				for (int n = 1; n < output._indefinite.Count; n++)
					NWiseConsistency(output, n, useDefaults, varBindings, out output);
			}
			return true;
		}

		internal override bool DestructiveUnify(FeatureValue other, bool useDefaults, bool preserveInput,
			IDictionary<FeatureValue, FeatureValue> copies, VariableBindings varBindings)
		{
			FeatureStruct otherFS;
			if (!Dereference(other, out otherFS))
				return false;

			if (preserveInput)
			{
				if (copies != null)
					copies[otherFS] = this;
			}
			else
			{
				otherFS.Forward = this;
			}

			foreach (KeyValuePair<Feature, FeatureValue> featVal in otherFS._definite)
			{
				FeatureValue otherValue = Dereference(featVal.Value);
				FeatureValue thisValue;
				if (_definite.TryGetValue(featVal.Key, out thisValue))
				{
					thisValue = Dereference(thisValue);
					if (!thisValue.DestructiveUnify(otherValue, useDefaults, preserveInput, copies, varBindings))
						return false;
				}
				else if (useDefaults && featVal.Key.DefaultValue != null)
				{
					thisValue = featVal.Key.DefaultValue.Clone();
					_definite[featVal.Key] = thisValue;
					if (!thisValue.DestructiveUnify(otherValue, true, preserveInput, copies, varBindings))
						return false;
				}
				else
				{
					FeatureValue value;
					if (preserveInput)
						value = copies != null ? otherValue.Clone(copies) : otherValue.Clone();
					else
						value = otherValue;
					_definite[featVal.Key] = value;
				}
			}

			return true;
		}

		protected override bool NondestructiveUnify(FeatureValue other, bool useDefaults, IDictionary<FeatureValue, FeatureValue> copies,
			VariableBindings varBindings, out FeatureValue output)
		{
			FeatureStruct otherFS;
			if (!Dereference(other, out otherFS))
			{
				output = null;
				return false;
			}

			var copy = new FeatureStruct();
			copies[this] = copy;
			copies[other] = copy;
			foreach (KeyValuePair<Feature, FeatureValue> featVal in otherFS._definite)
			{
				FeatureValue otherValue = Dereference(featVal.Value);
				FeatureValue thisValue;
				if (_definite.TryGetValue(featVal.Key, out thisValue))
				{
					thisValue = Dereference(thisValue);
					FeatureValue newValue;
					if (!thisValue.UnifyDefinite(otherValue, useDefaults, copies, varBindings, out newValue))
					{
						output = null;
						return false;
					}
					copy.AddValue(featVal.Key, newValue);
				}
				else if (useDefaults && featVal.Key.DefaultValue != null)
				{
					thisValue = featVal.Key.DefaultValue.Clone();
					FeatureValue newValue;
					if (!thisValue.UnifyDefinite(otherValue, true, copies, varBindings, out newValue))
					{
						output = null;
						return false;
					}
					copy._definite[featVal.Key] = newValue;
				}
				else
				{
					copy._definite[featVal.Key] = otherValue.Clone(copies);
				}
			}

			foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite)
			{
				if (!otherFS._definite.ContainsKey(featVal.Key))
					copy._definite[featVal.Key] = Dereference(featVal.Value).Clone(copies);
			}

			CopyDisjunctions(this, copy, copies);
			CopyDisjunctions(otherFS, copy, copies);

			output = copy;
			return true;
		}

		private static void CopyDisjunctions(FeatureStruct src, FeatureStruct dest, IDictionary<FeatureValue, FeatureValue> copies)
		{
			foreach (Disjunction disjunction in src._indefinite)
				dest._indefinite.Add(disjunction.Clone(copies));
		}

		internal override FeatureValue Clone(IDictionary<FeatureValue, FeatureValue> copies)
		{
			FeatureValue clone;
			if (copies.TryGetValue(this, out clone))
				return clone;

			return new FeatureStruct(this, copies);
		}

		private bool CheckIndefinite(FeatureStruct fs, FeatureStruct cond, bool useDefaults, VariableBindings varBindings,
			out FeatureStruct newFS)
		{
			var indefinite = new List<Disjunction>(fs._indefinite);
			newFS = fs;
			bool uncheckedParts = true;
			while (uncheckedParts)
			{
				uncheckedParts = false;
				newFS._indefinite.Clear();

				foreach (Disjunction disjunction in indefinite)
				{
					var newDisjunction = new List<FeatureStruct>();
					foreach (FeatureStruct disjunct in disjunction)
					{
						if (cond.IsDefiniteUnifiable(disjunct, useDefaults, varBindings.Clone()))
						{
							if (disjunct._indefinite.Count > 0)
							{
								FeatureStruct newDisjunct;
								if (CheckIndefinite(disjunct, cond, useDefaults, varBindings.Clone(), out newDisjunct))
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
						newFS = null;
						return false;
					}
				    if (newDisjunction.Count == 1)
				    {
				        FeatureStruct disjunct = newDisjunction.First();
				        FeatureValue newFV;
				        newFS.UnifyDefinite(disjunct, useDefaults, varBindings, out newFV);
				        newFS = (FeatureStruct) newFV;
				        uncheckedParts = true;
				    }
				    else
				    {
				    	newFS._indefinite.Add(new Disjunction(newDisjunction));
				    }
				}
				cond = newFS;
				indefinite.Clear();
				indefinite.AddRange(newFS._indefinite);
			}

			return true;
		}

		private bool NWiseConsistency(FeatureStruct fs, int n, bool useDefaults, VariableBindings varBindings, out FeatureStruct newFS)
		{
			newFS = fs;
			if (fs._indefinite.Count <= n)
				return true;

			var indefinite = new List<Disjunction>(newFS._indefinite);
			newFS._indefinite.Clear();

			while (indefinite.Count > 0)
			{
				Disjunction disjunction = indefinite.First();
				indefinite.Remove(disjunction);
				
				var newDisjunction = new List<FeatureStruct>();
				FeatureStruct lastFS = null;
				VariableBindings lastVarBindings = null;
				foreach (FeatureStruct disjunct in disjunction)
				{
					VariableBindings tempVarBindings = varBindings.Clone();
					FeatureValue hypFV;
					fs.UnifyDefinite(disjunct, useDefaults, tempVarBindings, out hypFV);
					var hypFS = (FeatureStruct) hypFV;
					foreach (Disjunction disj in indefinite)
						hypFS._indefinite.Add(disj);

					FeatureStruct nFS;
					if (n == 1 ? CheckIndefinite(hypFS, hypFS, useDefaults, tempVarBindings, out nFS)
						: NWiseConsistency(hypFS, n - 1, useDefaults, tempVarBindings, out nFS))
					{
						newDisjunction.Add(disjunct);
						lastFS = nFS;
						lastVarBindings = tempVarBindings;
					}
				}

				if (lastFS == null)
				{
					newFS = null;
					return false;
				}
			    if (newDisjunction.Count == 1)
			    {
			    	newFS = lastFS;
			        varBindings.Replace(lastVarBindings);
			        indefinite.Clear();
			        indefinite.AddRange(newFS._indefinite);
			        newFS._indefinite.Clear();
			    }
			    else
			    {
			    	newFS._indefinite.Add(new Disjunction(newDisjunction));
			    }
			}

			return true;
		}

		public override bool Negation(out FeatureValue output)
		{
			FeatureStruct fs;
			if (!Negation(out fs))
			{
				output = null;
				return false;
			}

			output = fs;
			return true;
		}

		public bool Negation(out FeatureStruct output)
		{
			if (_definite.Count == 0 && _indefinite.Count == 0)
			{
				output = null;
				return false;
			}

			var newDisjunction = new List<FeatureStruct>();
			foreach (Disjunction disjunction in _indefinite)
			{
				FeatureStruct fs;
				if (!disjunction.Negation(out fs))
				{
					output = null;
					return false;
				}
				newDisjunction.Add(fs);
			}

			foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite)
			{
				FeatureValue value = Dereference(kvp.Value);
				FeatureValue negValue;
				if (!value.Negation(out negValue))
				{
					output = null;
					return false;
				}
				var fs = new FeatureStruct();
				fs.AddValue(kvp.Key, negValue);
				newDisjunction.Add(fs);
			}

			if (newDisjunction.Count == 0)
			{
				output = new FeatureStruct();
			}
			else if (newDisjunction.Count == 1)
			{
				output = newDisjunction.First();
			}
			else
			{
				output = new FeatureStruct();
				output.AddDisjunction(new Disjunction(newDisjunction));
			}

			return true;
		}

		public override FeatureValue Clone()
		{
			return new FeatureStruct(this);
		}

		public override int GetHashCode()
		{
			return _definite.Aggregate(0, (current, kvp) => current ^ (kvp.Key.GetHashCode() ^ (kvp.Value != null ? kvp.Value.GetHashCode() : 0)));
		}

		public override bool Equals(object obj)
		{
			var other = obj as FeatureStruct;
			return other != null && Equals(other);
		}

		public bool Equals(FeatureStruct other)
		{
			if (other == null)
				return false;

			other = Dereference(other);

			if (_definite.Count != other._definite.Count)
				return false;

			foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite)
			{
				FeatureValue thisValue = Dereference(kvp.Value);
				FeatureValue otherValue;
				if (!other._definite.TryGetValue(kvp.Key, out otherValue))
					return false;
				otherValue = Dereference(otherValue);
				if (!thisValue.Equals(otherValue))
					return false;
			}

			return true;
		}

		public override string ToString()
		{
			bool firstFeature = true;
			var sb = new StringBuilder();
			sb.Append("[");
			foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite)
			{
				FeatureValue value = Dereference(kvp.Value);
				if (!firstFeature)
					sb.Append(", ");
				sb.Append(kvp.Key.Description);
				sb.Append(":");
				sb.Append(value.ToString());
				firstFeature = false;
			}
			sb.Append("]");
			foreach (Disjunction disjunction in _indefinite)
			{
				sb.Append(" && (");
				sb.Append(disjunction);
				sb.Append(")");
			}
			return sb.ToString();
		}
	}
}
