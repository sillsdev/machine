using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Collections;
using SIL.Machine.FeatureModel.Fluent;

namespace SIL.Machine.FeatureModel
{
	public class FeatureStruct : FeatureValue, IDeepCloneable<FeatureStruct>, IFreezable<FeatureStruct>
	{
		public static IDisjunctiveFeatureStructSyntax New()
		{
			return new FeatureStructBuilder();
		}

		public static IDisjunctiveFeatureStructSyntax New(FeatureSystem featSys)
		{
			return new FeatureStructBuilder(featSys);
		}

		public static IDisjunctiveFeatureStructSyntax New(FeatureStruct fs)
		{
			return new FeatureStructBuilder(fs.DeepClone());
		}

		public static IDisjunctiveFeatureStructSyntax New(FeatureSystem featSys, FeatureStruct fs)
		{
			return new FeatureStructBuilder(featSys, fs.DeepClone());
		}

		public static IDisjunctiveFeatureStructSyntax NewMutable()
		{
			return new FeatureStructBuilder(true);
		}

		public static IDisjunctiveFeatureStructSyntax NewMutable(FeatureSystem featSys)
		{
			return new FeatureStructBuilder(featSys, true);
		}

		public static IDisjunctiveFeatureStructSyntax NewMutable(FeatureStruct fs)
		{
			return new FeatureStructBuilder(fs.DeepClone(), true);
		}

		public static IDisjunctiveFeatureStructSyntax NewMutable(FeatureSystem featSys, FeatureStruct fs)
		{
			return new FeatureStructBuilder(featSys, fs.DeepClone(), true);
		}

		private readonly IDBearerDictionary<Feature, FeatureValue> _definite;
		private readonly List<Disjunction> _indefinite;
		private int? _hashCode;

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureStruct"/> class.
		/// </summary>
		public FeatureStruct()
		{
			_definite = new IDBearerDictionary<Feature, FeatureValue>();
			_indefinite = new List<Disjunction>();
		}

		protected FeatureStruct(FeatureStruct other)
			: this(other, new Dictionary<FeatureValue, FeatureValue>())
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
			copies[other] = this;
			foreach (KeyValuePair<Feature, FeatureValue> featVal in other._definite)
				_definite[featVal.Key] = Dereference(featVal.Value).DeepCloneImpl(copies);

			CopyDisjunctions(other, this, copies);
		}

		/// <summary>
		/// Gets the features.
		/// </summary>
		/// <value>The features.</value>
		public IReadOnlyCollection<Feature> Features
		{
			get { return _definite.Keys.AsSimpleReadOnlyCollection(); }
		}

		public IReadOnlyCollection<IReadOnlyCollection<FeatureStruct>> Disjunctions
		{
			get { return _indefinite.AsSimpleReadOnlyCollection(); }
		}

		public bool HasVariables
		{
			get { return DetermineHasVariables(new HashSet<FeatureStruct>()); }
		}

		public bool IsEmpty
		{
			get { return _definite.Count == 0 && _indefinite.Count == 0; }
		}

		private bool DetermineHasVariables(ISet<FeatureStruct> visited)
		{
			if (visited.Contains(this))
				return false;

			visited.Add(this);

			foreach (FeatureValue value in _definite.Values)
			{
				var childFS = value as FeatureStruct;
				if (childFS != null)
				{
					if (childFS.DetermineHasVariables(visited))
						return true;
				}
				else if (((SimpleFeatureValue)value).IsVariable)
				{
					return true;
				}
			}

			return _indefinite.Any(disjunction => disjunction.HasVariables);
		}

		public void AddValue(SymbolicFeature feature, IEnumerable<FeatureSymbol> values)
		{
			FeatureSymbol[] vals = values.ToArray();
			AddValue(feature, vals.Length == 0 ? new SymbolicFeatureValue(feature) : new SymbolicFeatureValue(vals));
		}

		public void AddValue(SymbolicFeature feature, params FeatureSymbol[] values)
		{
			AddValue(feature, values.Length == 0 ? new SymbolicFeatureValue(feature) : new SymbolicFeatureValue(values));
		}

		public void AddValue(StringFeature feature, IEnumerable<string> values)
		{
			AddValue(feature, false, values);
		}

		public void AddValue(StringFeature feature, bool not, IEnumerable<string> values)
		{
			AddValue(feature, new StringFeatureValue(values, not));
		}

		public void AddValue(StringFeature feature, params string[] values)
		{
			AddValue(feature, false, values);
		}

		public void AddValue(StringFeature feature, bool not, params string[] values)
		{
			AddValue(feature, new StringFeatureValue(values, not));
		}

		/// <summary>
		/// Adds the specified feature-value pair.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <param name="value">The value.</param>
		public void AddValue(Feature feature, FeatureValue value)
		{
			CheckFrozen();
			_definite[feature] = value;
		}

		public void AddValue(IEnumerable<Feature> path, FeatureValue value)
		{
			CheckFrozen();
		    Feature lastFeature;
			FeatureStruct lastFS;
			if (FollowPath(path, out lastFeature, out lastFS))
				lastFS._definite[lastFeature] = value;

			throw new ArgumentException("The feature path is invalid.", "path");
		}

		public void RemoveValue(Feature feature)
		{
			CheckFrozen();
			_definite.Remove(feature);
		}

		public void RemoveValue(IEnumerable<Feature> path)
		{
			CheckFrozen();
		    Feature lastFeature;
			FeatureStruct lastFS;
			if (FollowPath(path, out lastFeature, out lastFS))
				lastFS._definite.Remove(lastFeature);

			throw new ArgumentException("The feature path is invalid.", "path");
		}

		public void ReplaceVariables(VariableBindings varBindings)
		{
			CheckFrozen();
			ReplaceVariables(varBindings, new HashSet<FeatureStruct>());
		}

		private void ReplaceVariables(VariableBindings varBindings, ISet<FeatureStruct> visited)
		{
			if (visited.Contains(this))
				return;

			visited.Add(this);

			var replacements = new Dictionary<Feature, FeatureValue>();
			foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite)
			{
				FeatureValue value = Dereference(featVal.Value);
				var sfv = value as SimpleFeatureValue;
				if (sfv != null)
				{
					if (sfv.IsVariable)
					{
						SimpleFeatureValue binding;
						if (varBindings.TryGetValue(sfv.VariableName, out binding))
							replacements[featVal.Key] = binding.GetVariableValue(sfv.Agree);
					}
				}
				else
				{
					var fs = (FeatureStruct) value;
					fs.ReplaceVariables(varBindings, visited);
				}
			}

			foreach (KeyValuePair<Feature, FeatureValue> replacement in replacements)
				_definite[replacement.Key] = replacement.Value;

			foreach (Disjunction disjunction in _indefinite)
				disjunction.ReplaceVariables(varBindings);
		}

		public void RemoveVariables()
		{
			CheckFrozen();
			RemoveVariables(new HashSet<FeatureStruct>());
		}

		private void RemoveVariables(ISet<FeatureStruct> visited)
		{
			if (visited.Contains(this))
				return;

			visited.Add(this);

			foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite.ToArray())
			{
				FeatureValue value = Dereference(featVal.Value);
				var sfv = value as SimpleFeatureValue;
				if (sfv != null)
				{
					if (sfv.IsVariable)
						_definite.Remove(featVal.Key);
				}
				else
				{
					var fs = (FeatureStruct) value;
					fs.RemoveVariables(visited);
					if (fs.IsEmpty)
						_definite.Remove(featVal.Key);
				}

			}

			for (int i = _indefinite.Count - 1; i >= 0; i--)
			{
				_indefinite[i].RemoveVariables();
				if (_indefinite[i].Count == 0)
				{
					_indefinite.RemoveAt(i);
				}
				else if (_indefinite[i].Count == 1)
				{
					UnionImpl(_indefinite[i].First(), new VariableBindings(), new Dictionary<FeatureStruct, ISet<FeatureStruct>>());
					_indefinite.RemoveAt(i);
				}
			}
		}

		public void PriorityUnion(FeatureStruct other)
		{
			PriorityUnion(other, new VariableBindings());
		}

		public void PriorityUnion(FeatureStruct other, VariableBindings varBindings)
		{
			CheckFrozen();
			// TODO: support disjunctive feature structures
			if (_indefinite.Count > 0 || other._indefinite.Count > 0)
				throw new InvalidOperationException("This operation is not supported on disjunctive feature structures.");
			PriorityUnion(other, varBindings, new Dictionary<FeatureValue, FeatureValue>());
		}

		private void PriorityUnion(FeatureStruct other, VariableBindings varBindings, IDictionary<FeatureValue, FeatureValue> copies)
		{
			other = Dereference(other);

			copies[other] = this; 

			foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite)
			{
				FeatureValue thisValue = Dereference(featVal.Value);
				FeatureValue otherValue;
				if (other._definite.TryGetValue(featVal.Key, out otherValue))
				{
					otherValue = Dereference(otherValue);
					var otherFS = otherValue as FeatureStruct;
					if (otherFS != null && !copies.ContainsKey(otherFS))
					{
						var thisFS = thisValue as FeatureStruct;
						if (thisFS != null)
							thisFS.PriorityUnion(otherFS, varBindings, copies);
					}
				}
			}

			foreach (KeyValuePair<Feature, FeatureValue> featVal in other._definite)
			{
				FeatureValue otherValue = Dereference(featVal.Value);
				FeatureValue thisValue;
				if (_definite.TryGetValue(featVal.Key, out thisValue))
				{
					otherValue = Dereference(otherValue);
					var otherFS = otherValue as FeatureStruct;
					if (otherFS != null)
					{
						if (thisValue is FeatureStruct)
						{
							FeatureValue reentrant;
							if (copies.TryGetValue(otherFS, out reentrant))
								_definite[featVal.Key] = reentrant;
						}
						else
						{
							_definite[featVal.Key] = otherFS.DeepCloneImpl(copies);
						}
					}
					else
					{
						var otherSfv = (SimpleFeatureValue) otherValue;
						SimpleFeatureValue binding;
						if (otherSfv.IsVariable && varBindings.TryGetValue(otherSfv.VariableName, out binding))
							_definite[featVal.Key] = binding.GetVariableValue(otherSfv.Agree);
						else
							_definite[featVal.Key] = otherSfv.DeepCloneImpl(copies);
					}
				}
				else
				{
					_definite[featVal.Key] = otherValue.DeepCloneImpl(copies);
				}
			}
		}

		public void Union(FeatureStruct other)
		{
			Union(other, new VariableBindings());
		}

		public void Union(FeatureStruct other, VariableBindings varBindings)
		{
			CheckFrozen();
			// TODO: support disjunctive feature structures
			if (_indefinite.Count > 0 || other._indefinite.Count > 0)
				throw new InvalidOperationException("This operation is not supported on disjunctive feature structures.");
			UnionImpl(other, varBindings, new Dictionary<FeatureStruct, ISet<FeatureStruct>>());
		}

		internal override bool UnionImpl(FeatureValue other, VariableBindings varBindings, IDictionary<FeatureStruct, ISet<FeatureStruct>> visited)
		{
			FeatureStruct otherFS;
			if (Dereference(other, out otherFS))
			{
				ISet<FeatureStruct> visitedOthers = visited.GetValue(this, () => new HashSet<FeatureStruct>());
				if (!visitedOthers.Contains(otherFS))
				{
					visitedOthers.Add(otherFS);

					foreach (KeyValuePair<Feature, FeatureValue> featVal in otherFS._definite)
					{
						FeatureValue otherValue = Dereference(featVal.Value);
						FeatureValue thisValue;
						if (_definite.TryGetValue(featVal.Key, out thisValue))
						{
							thisValue = Dereference(thisValue);
							if (!thisValue.UnionImpl(otherValue, varBindings, visited))
								_definite.Remove(featVal.Key);
						}
					}

					_definite.RemoveAll(kvp => !otherFS._definite.ContainsKey(kvp.Key));
				}
			}
			return _definite.Count > 0;
		}

		public void Add(FeatureStruct other)
		{
			Add(other, new VariableBindings());
		}

		public void Add(FeatureStruct other, VariableBindings varBindings)
		{
			CheckFrozen();
			// TODO: support disjunctive feature structures
			if (_indefinite.Count > 0 || other._indefinite.Count > 0)
				throw new InvalidOperationException("This operation is not supported on disjunctive feature structures.");
			AddImpl(other, varBindings, new Dictionary<FeatureStruct, ISet<FeatureStruct>>());
		}

		internal override bool AddImpl(FeatureValue other, VariableBindings varBindings, IDictionary<FeatureStruct, ISet<FeatureStruct>> visited)
		{
			FeatureStruct otherFS;
			if (Dereference(other, out otherFS))
			{
				ISet<FeatureStruct> visitedOthers = visited.GetValue(this, () => new HashSet<FeatureStruct>());
				if (!visitedOthers.Contains(otherFS))
				{
					visitedOthers.Add(otherFS);

					foreach (KeyValuePair<Feature, FeatureValue> featVal in otherFS._definite)
					{
						FeatureValue otherValue = Dereference(featVal.Value);
						FeatureValue thisValue;
						if (_definite.TryGetValue(featVal.Key, out thisValue))
						{
							thisValue = Dereference(thisValue);
						}
						else
						{
							if (otherValue is FeatureStruct)
								thisValue = new FeatureStruct();
							else if (otherValue is StringFeatureValue)
								thisValue = new StringFeatureValue();
							else
								thisValue = new SymbolicFeatureValue((SymbolicFeature) featVal.Key);
							_definite[featVal.Key] = thisValue;
						}
						if (!thisValue.AddImpl(otherValue, varBindings, visited))
							_definite.Remove(featVal.Key);
					}
				}
			}
			return _definite.Count > 0;
		}

		public void Subtract(FeatureStruct other)
		{
			Subtract(other, new VariableBindings());
		}

		public void Subtract(FeatureStruct other, VariableBindings varBindings)
		{
			CheckFrozen();
			// TODO: support disjunctive feature structures
			if (_indefinite.Count > 0 || other._indefinite.Count > 0)
				throw new InvalidOperationException("This operation is not supported on disjunctive feature structures.");

			SubtractImpl(other, varBindings, new Dictionary<FeatureStruct, ISet<FeatureStruct>>());
		}

		internal override bool SubtractImpl(FeatureValue other, VariableBindings varBindings, IDictionary<FeatureStruct, ISet<FeatureStruct>> visited)
		{
			FeatureStruct otherFS;
			if (Dereference(other, out otherFS))
			{
				ISet<FeatureStruct> visitedOthers = visited.GetValue(this, () => new HashSet<FeatureStruct>());
				if (!visitedOthers.Contains(otherFS))
				{
					visitedOthers.Add(otherFS);

					foreach (KeyValuePair<Feature, FeatureValue> featVal in otherFS._definite)
					{
						FeatureValue otherValue = Dereference(featVal.Value);
						FeatureValue thisValue;
						if (_definite.TryGetValue(featVal.Key, out thisValue))
						{
							thisValue = Dereference(thisValue);
							if (!thisValue.SubtractImpl(otherValue, varBindings, visited))
								_definite.Remove(featVal.Key);
						}
					}
				}
			}
			return _definite.Count > 0;
		}

		public void AddDisjunction(params FeatureStruct[] disjuncts)
		{
			CheckFrozen();
			if (disjuncts.Length < 2)
				throw new ArgumentException("At least two disjuncts must be specified.", "disjuncts");
			_indefinite.Add(new Disjunction(disjuncts));
		}

		public void AddDisjunction(IEnumerable<FeatureStruct> disjuncts)
		{
			CheckFrozen();
			FeatureStruct[] disjunctArray = disjuncts.ToArray();
			if (disjunctArray.Length < 2)
				throw new ArgumentException("At least two disjuncts must be specified.", "disjuncts");
			_indefinite.Add(new Disjunction(disjunctArray));
		}

		public void Clear()
		{
			CheckFrozen();
			_definite.Clear();
			_indefinite.Clear();
		}

		public void ClearValues()
		{
			CheckFrozen();
			_definite.Clear();
		}

		public void ClearDisjunctions()
		{
			CheckFrozen();
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

		public StringFeatureValue GetValue(StringFeature feature)
		{
			StringFeatureValue value;
			if (TryGetValue(feature, out value))
				return value;

			throw new ArgumentException("The specified value could not be found.", "feature");
		}

		public SymbolicFeatureValue GetValue(SymbolicFeature feature)
		{
			SymbolicFeatureValue value;
			if (TryGetValue(feature, out value))
				return value;

			throw new ArgumentException("The specified value could not be found.", "feature");
		}

		public FeatureStruct GetValue(ComplexFeature feature)
		{
			FeatureStruct value;
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

		public bool IsUnifiable(FeatureStruct other)
		{
			return IsUnifiable(other, false);
		}

		public bool IsUnifiable(FeatureStruct other, bool useDefaults)
		{
			return IsUnifiable(other, useDefaults, null);
		}

		public bool IsUnifiable(FeatureStruct other, VariableBindings varBindings)
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
		public bool IsUnifiable(FeatureStruct other, bool useDefaults, VariableBindings varBindings)
		{
			other = Dereference(other);

			if (_indefinite.Count == 0 && other._indefinite.Count == 0)
			{
				VariableBindings definiteVarBindings = varBindings == null ? new VariableBindings() : varBindings.DeepClone();
				if (IsDefiniteUnifiable(other, useDefaults, definiteVarBindings))
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

		public bool Unify(FeatureStruct other, out FeatureStruct output)
		{
			return Unify(other, false, out output);
		}

		public bool Unify(FeatureStruct other, bool useDefaults, out FeatureStruct output)
		{
			return Unify(other, useDefaults, null, out output);
		}

		public bool Unify(FeatureStruct other, VariableBindings varBindings, out FeatureStruct output)
		{
			return Unify(other, false, varBindings, out output);
		}

		public bool Unify(FeatureStruct other, bool useDefaults, VariableBindings varBindings, out FeatureStruct output)
		{
			return Unify(other, useDefaults, varBindings, true, out output);
		}

		public bool Unify(FeatureStruct other, bool useDefaults, VariableBindings varBindings, bool checkDisjunctiveConsistency, out FeatureStruct output)
		{
			other = Dereference(other);

			VariableBindings tempVarBindings = varBindings == null ? new VariableBindings() : varBindings.DeepClone();
			FeatureValue newFV;
			if (!UnifyDefinite(other, useDefaults, tempVarBindings, out newFV))
			{
				output = null;
				return false;
			}

			var newFS = (FeatureStruct) newFV;
			if (newFS._indefinite.Count > 0)
			{
				if (!CheckIndefinite(newFS, newFS, useDefaults, tempVarBindings, new HashSet<FeatureValue>(), out newFS))
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

		public bool Subsumes(FeatureStruct other)
		{
			FeatureStruct result;
			if (Unify(other, out result))
				return other.ValueEquals(result);
			return false;
		}

		public bool CheckDisjunctiveConsistency(bool useDefaults, VariableBindings varBindings, out FeatureStruct output)
		{
			output = this;
			if (_indefinite.Count > 0)
			{
				for (int n = 1; n < output._indefinite.Count; n++)
					NWiseConsistency(output, n, useDefaults, varBindings, new HashSet<FeatureValue>(),  out output);
			}
			return true;
		}

		internal override bool DestructiveUnify(FeatureValue other, bool useDefaults, bool preserveInput,
			IDictionary<FeatureValue, FeatureValue> copies, VariableBindings varBindings)
		{
			FeatureStruct otherFS;
			if (!Dereference(other, out otherFS))
				return false;

			if (this == otherFS)
				return true;

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
					thisValue = featVal.Key.DefaultValue.DeepCloneImpl(null);
					_definite[featVal.Key] = thisValue;
					if (!thisValue.DestructiveUnify(otherValue, true, preserveInput, copies, varBindings))
						return false;
				}
				else
				{
					_definite[featVal.Key] = preserveInput ? otherValue.DeepCloneImpl(copies) : otherValue;
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
					thisValue = featVal.Key.DefaultValue.DeepCloneImpl(null);
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
					copy._definite[featVal.Key] = otherValue.DeepCloneImpl(copies);
				}
			}

			foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite)
			{
				if (!otherFS._definite.ContainsKey(featVal.Key))
					copy._definite[featVal.Key] = Dereference(featVal.Value).DeepCloneImpl(copies);
			}

			CopyDisjunctions(this, copy, copies);
			CopyDisjunctions(otherFS, copy, copies);

			output = copy;
			return true;
		}

		private static void CopyDisjunctions(FeatureStruct src, FeatureStruct dest, IDictionary<FeatureValue, FeatureValue> copies)
		{
			foreach (Disjunction disjunction in src._indefinite)
				dest._indefinite.Add(disjunction.DeepCloneImpl(copies));
		}

		internal override FeatureValue DeepCloneImpl(IDictionary<FeatureValue, FeatureValue> copies)
		{
			if (copies != null)
			{
				FeatureValue clone;
				if (copies.TryGetValue(this, out clone))
					return clone;
				return new FeatureStruct(this, copies);
			}

			return DeepClone();
		}

		private bool CheckIndefinite(FeatureStruct fs, FeatureStruct cond, bool useDefaults, VariableBindings varBindings, ISet<FeatureValue> values,
			out FeatureStruct newFS)
		{
			var indefinite = new List<Disjunction>(fs._indefinite);
			newFS = fs;
			bool uncheckedParts = true;
			while (uncheckedParts)
			{
				var tempValues = new HashSet<FeatureValue>(values);
				newFS.GetAllValues(tempValues, false);

				uncheckedParts = false;
				newFS._indefinite.Clear();

				foreach (Disjunction disjunction in indefinite)
				{
					var newDisjunction = new List<FeatureStruct>();
					foreach (FeatureStruct disjunct in disjunction)
					{
						if (cond.IsDefiniteUnifiable(disjunct, useDefaults, varBindings.DeepClone()))
						{
							if (disjunct._indefinite.Count > 0)
							{
								FeatureStruct newDisjunct;
								if (CheckIndefinite(disjunct, cond, useDefaults, varBindings.DeepClone(), tempValues, out newDisjunct))
								{
									if (!Contains(tempValues, newDisjunction, newDisjunct))
										newDisjunction.Add(newDisjunct);
								}
							}
							else
							{
								if (!Contains(tempValues, newDisjunction, disjunct))
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
				    	var nd = new Disjunction(newDisjunction);
						if (!Contains(values, newFS._indefinite, nd))
				    		newFS._indefinite.Add(nd);
				    }
				}
				cond = newFS;
				indefinite.Clear();
				indefinite.AddRange(newFS._indefinite);
			}

			return true;
		}

		private bool NWiseConsistency(FeatureStruct fs, int n, bool useDefaults, VariableBindings varBindings, ISet<FeatureValue> values, out FeatureStruct newFS)
		{
			newFS = fs;
			if (fs._indefinite.Count <= n)
				return true;

			var indefinite = new List<Disjunction>(newFS._indefinite);
			newFS._indefinite.Clear();

			while (indefinite.Count > 0)
			{
				var tempValues = new HashSet<FeatureValue>(values);
				newFS.GetAllValues(tempValues, false);

				Disjunction disjunction = indefinite.First();
				indefinite.RemoveAt(0);
				
				var newDisjunction = new List<FeatureStruct>();
				FeatureStruct lastFS = null;
				VariableBindings lastVarBindings = null;
				foreach (FeatureStruct disjunct in disjunction)
				{
					VariableBindings tempVarBindings = varBindings.DeepClone();
					FeatureValue hypFV;
					if (fs.UnifyDefinite(disjunct, useDefaults, tempVarBindings, out hypFV))
					{
						var hypFS = (FeatureStruct) hypFV;
						foreach (Disjunction disj in indefinite)
							hypFS._indefinite.Add(disj);

						FeatureStruct nFS;
						if (n == 1 ? CheckIndefinite(hypFS, hypFS, useDefaults, tempVarBindings, tempValues, out nFS)
							: NWiseConsistency(hypFS, n - 1, useDefaults, tempVarBindings, tempValues, out nFS))
						{
							newDisjunction.Add(disjunct);
							//newDisjunction.Add(nFS);
							lastFS = nFS;
							lastVarBindings = tempVarBindings;
						}
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
					var nd = new Disjunction(newDisjunction);
					if (!Contains(values, newFS._indefinite, nd))
						newFS._indefinite.Add(nd);
			    }
			}

			return true;
		}

		internal override void GetAllValues(ISet<FeatureValue> values, bool indefinite)
		{
			if (!values.Contains(this))
			{
				values.Add(this);

				foreach (FeatureValue value in _definite.Values)
				{
					FeatureValue v = Dereference(value);
					v.GetAllValues(values, indefinite);
				}
				if (indefinite)
				{
					foreach (Disjunction disjunction in _indefinite)
						disjunction.GetAllValues(values);
				}
			}
		}

		private bool Contains(ISet<FeatureValue> values, List<FeatureStruct> featureStructs, FeatureStruct fs)
		{
			if (featureStructs.Count == 0)
				return false;
			foreach (FeatureStruct existing in featureStructs)
			{
				var visitedSelf = new HashSet<FeatureValue>(values);
				var visitedOther = new HashSet<FeatureValue>(values);
				var visitedPairs = values.ToDictionary(fv => fv);

				if (fs.ValueEqualsImpl(existing, visitedSelf, visitedOther, visitedPairs))
					return true;
			}

			return false;
		}

		private bool Contains(ISet<FeatureValue> values, List<Disjunction> disjunctions, Disjunction disjunction)
		{
			if (disjunctions.Count == 0)
				return false;

			var visitedSelf = new HashSet<FeatureValue>(values);
			var visitedOther = new HashSet<FeatureValue>(values);
			var visitedPairs = values.ToDictionary(fv => fv);
			foreach (Disjunction existing in disjunctions)
			{
				if (disjunction.EqualsImpl(existing, visitedSelf, visitedOther, visitedPairs))
					return true;
			}

			return false;
		}

		public bool Negation(out FeatureStruct output)
		{
			FeatureValue fv;
			if (NegationImpl(new Dictionary<FeatureValue, FeatureValue>(), out fv))
			{
				output = (FeatureStruct) fv;
				return true;
			}

			output = null;
			return false;
		}

		internal override bool NegationImpl(IDictionary<FeatureValue, FeatureValue> visited, out FeatureValue output)
		{
			FeatureValue negation;
			if (visited.TryGetValue(this, out negation))
			{
				output = negation;
				return true;
			}

			int disjunctionCount = _indefinite.Count + _definite.Count;
			if (disjunctionCount == 0)
			{
				output = null;
				return false;
			}

			FeatureStruct outputFS = null;
			var newDisjunction = new List<FeatureStruct>();
			foreach (Disjunction disjunction in _indefinite)
			{
				FeatureStruct fs;
				if (!disjunction.NegationImpl(out fs))
				{
					output = null;
					return false;
				}

				if (disjunctionCount == 1)
				{
					outputFS = fs;
					visited[this] = outputFS;
				}

				newDisjunction.Add(fs);
			}

			if (disjunctionCount > 1)
			{
				outputFS = new FeatureStruct();
				visited[this] = outputFS;
			}

			foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite)
			{
				var fs = new FeatureStruct();
				if (disjunctionCount == 1)
				{
					outputFS = fs;
					visited[this] = outputFS;
				}

				FeatureValue value = Dereference(kvp.Value);
				FeatureValue negValue;
				if (!value.NegationImpl(visited, out negValue))
				{
					output = null;
					return false;
				}
				fs.AddValue(kvp.Key, negValue);
				newDisjunction.Add(fs);
			}

			if (disjunctionCount > 1 && outputFS != null)
				outputFS._indefinite.Add(new Disjunction(newDisjunction));

			output = outputFS;
			return true;
		}

		internal override void FindReentrances(IDictionary<FeatureValue, bool> reentrances)
		{
			if (reentrances.ContainsKey(this))
			{
				reentrances[this] = true;
			}
			else
			{
				reentrances[this] = false;
				foreach (FeatureValue value in _definite.Values)
				{
					FeatureValue v = Dereference(value);
					v.FindReentrances(reentrances);
				}
				foreach (Disjunction disjunction in _indefinite)
					disjunction.FindReentrances(reentrances);
			}
		}

		public new FeatureStruct DeepClone()
		{
			return new FeatureStruct(this);
		}

		internal override bool ValueEqualsImpl(FeatureValue other, ISet<FeatureValue> visitedSelf, ISet<FeatureValue> visitedOther, IDictionary<FeatureValue, FeatureValue> visitedPairs)
		{
			if (other == null)
				return false;

			FeatureStruct otherFS;
			if (!Dereference(other, out otherFS))
				return false;

			if (this == otherFS)
				return true;

			if (visitedSelf.Contains(this) || visitedOther.Contains(otherFS))
			{
				FeatureValue fv;
				if (visitedPairs.TryGetValue(this, out fv))
					return fv == otherFS;
				return false;
			}

			visitedSelf.Add(this);
			visitedOther.Add(otherFS);
			visitedPairs[this] = otherFS;

			if (_definite.Count != otherFS._definite.Count)
				return false;

			foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite)
			{
				FeatureValue thisValue = Dereference(kvp.Value);
				FeatureValue otherValue;
				if (!otherFS._definite.TryGetValue(kvp.Key, out otherValue))
					return false;
				otherValue = Dereference(otherValue);
				if (!thisValue.ValueEqualsImpl(otherValue, visitedSelf, visitedOther, visitedPairs))
					return false;
			}

			if (_indefinite.Count != otherFS._indefinite.Count)
				return false;

			if (_indefinite.Count > 0)
			{
				var matched = new HashSet<int>();
				foreach (Disjunction thisDisjunction in _indefinite)
				{
					bool found = false;
					for (int i = 0; i < otherFS._indefinite.Count; i++)
					{
						if (matched.Contains(i))
							continue;

						if (thisDisjunction.EqualsImpl(otherFS._indefinite[i], visitedSelf, visitedOther, visitedPairs))
						{
							matched.Add(i);
							found = true;
							break;
						}
					}

					if (!found)
						return false;
				}
			}

			return true;
		}

		public bool ValueEquals(FeatureStruct other)
		{
			if (this == other)
				return true;

			return other != null && ValueEqualsImpl(other, new HashSet<FeatureValue>(), new HashSet<FeatureValue>(),
				new Dictionary<FeatureValue, FeatureValue>());
		}

		public int GetFrozenHashCode()
		{
			if (!IsFrozen)
				throw new InvalidOperationException("The feature structure does not have a valid hash code, because it is mutable.");
			if (!_hashCode.HasValue)
				_hashCode = FreezeImpl(new HashSet<FeatureValue>());
			return _hashCode.Value;
		}

		public override bool ValueEquals(FeatureValue other)
		{
			var otherFS = other as FeatureStruct;
			return otherFS != null && ValueEquals(otherFS);
		}

		public bool IsFrozen { get; private set; }

		private void CheckFrozen()
		{
			if (IsFrozen)
				throw new InvalidOperationException("The feature structure is immutable.");
		}

		public void Freeze()
		{
			if (IsFrozen)
				return;

			_hashCode = FreezeImpl(new HashSet<FeatureValue>());
		}

		internal override int FreezeImpl(ISet<FeatureValue> visited)
		{
			if (visited.Contains(this))
				return 1;

			visited.Add(this);
			IsFrozen = true;

			int code = 23;
			foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite.OrderBy(kvp => kvp.Key.ID))
			{
				code = code * 31 + kvp.Key.GetHashCode();
				FeatureValue value = Dereference(kvp.Value);
				code = code * 31 + value.FreezeImpl(visited);
			}

			return _indefinite.Aggregate(code, (disjCode, disjunction) => disjCode ^ disjunction.FreezeImpl(visited));
		}

		public override string ToString()
		{
			if (IsEmpty)
				return "ANY";

			var reentrances = new Dictionary<FeatureValue, bool>();
			FindReentrances(reentrances);
			var reentranceIds = new Dictionary<FeatureValue, int>();
			int id = 1;
			foreach (FeatureValue value in reentrances.Where(kvp => kvp.Value).Select(kvp => kvp.Key))
				reentranceIds[value] = id++;
			return ToStringImpl(new HashSet<FeatureValue>(), reentranceIds);
		}

		internal override string ToStringImpl(ISet<FeatureValue> visited, IDictionary<FeatureValue, int> reentranceIds)
		{
			if (visited.Contains(this))
				return string.Format("<{0}>", reentranceIds[this]);

			visited.Add(this);

			var sb = new StringBuilder();
			int id;
			if (reentranceIds.TryGetValue(this, out id))
			{
				sb.Append(id);
				sb.Append("=");
			}

			int groupCount = _indefinite.Count;
			if (_definite.Count > 0)
				groupCount++;

			if (groupCount > 1)
				sb.Append("(");
			if (_definite.Count > 0)
			{
				bool firstFeature = true;
				if (_definite.Count > 0)
					sb.Append("[");
				foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite)
				{
					FeatureValue value = Dereference(kvp.Value);
					if (!firstFeature)
						sb.Append(", ");
					sb.Append(kvp.Key.Description);
					sb.Append(":");
					sb.Append(value.ToStringImpl(visited, reentranceIds));
					firstFeature = false;
				}
				if (_definite.Count > 0)
					sb.Append("]");

				if (_indefinite.Count > 0)
					sb.Append(" && ");
			}

			bool firstDisjunction = true;
			foreach (Disjunction disjunction in _indefinite)
			{
				if (!firstDisjunction)
					sb.Append(" && ");
				sb.Append("(");
				sb.Append(disjunction.ToStringImpl(visited, reentranceIds));
				sb.Append(")");
				firstDisjunction = false;
			}
			if (groupCount > 1)
				sb.Append(")");

			return sb.ToString();
		}
	}
}
