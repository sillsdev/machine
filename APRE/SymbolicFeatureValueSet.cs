using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE
{
	/// <summary>
	/// This represents a set of symbolic feature values. Each possible feature value in the feature system is encoded in
	/// a single bit in a unsigned long integer. This allows fast manipulation of the set of feature values using
	/// bitwise operators. Each bit indicates whether the corresponding value has been set. The feature value set allows
	/// multiple values for the same feature to be set. This is used to uninstantiate features during the analysis
	/// phase. It is primarily used for phonetic features.
	/// </summary>
	public class SymbolicFeatureValueSet : FeatureStructure
	{
		private const int NumBits = sizeof(ulong) * 8;
		public const int MaxNumValues = NumBits * 2;

		private FeatureBundle _featureValues;
		private FeatureBundle _antiFeatureValues;

		/// <summary>
		/// Initializes a new instance of the <see cref="SymbolicFeatureValueSet"/> class.
		/// </summary>
		/// <param name="featVals">The feature values.</param>
		/// <param name="featSys">The feat system.</param>
		public SymbolicFeatureValueSet(IEnumerable<FeatureSymbol> featVals, FeatureSystem featSys)
			: base(featSys)
		{
			_featureValues = new FeatureBundle();
			foreach (FeatureSymbol value in featVals)
				_featureValues.Set(value, true);

			_antiFeatureValues = new FeatureBundle();
			foreach (SymbolicFeature feature in GetFeatures(_featureValues))
			{
				foreach (FeatureSymbol symbol in feature.PossibleSymbols)
				{
					if (!_featureValues.Get(symbol))
						_antiFeatureValues.Set(symbol, true);
				}
			}
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="sfvs">The feature bundle.</param>
		public SymbolicFeatureValueSet(SymbolicFeatureValueSet sfvs)
			: base(sfvs.FeatureSystem)
		{
			_featureValues = sfvs._featureValues.Clone();
			_antiFeatureValues = sfvs._antiFeatureValues.Clone();
		}

		public override FeatureValueType Type
		{
			get
			{
				return FeatureValueType.Complex;
			}
		}

		public override bool IsAmbiguous
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Gets all that have instantiated values in this feature bundle.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<Feature> Features
		{
			get { return GetFeatures(_featureValues); }
		}

		private IEnumerable<Feature> GetFeatures(FeatureBundle fb)
		{
			return (from symbol in FeatureSystem.Symbols
					where fb.Get(symbol)
					select symbol.Feature).Distinct();
		}

		public override int NumFeatures
		{
			get { return Features.Count(); }
		}

		public override void Add(Feature feature, FeatureValue value)
		{
			var sfv = value as SymbolicFeatureValue;
			if (sfv != null)
			{
				foreach (FeatureSymbol symbol in sfv.Values)
					_featureValues.Set(symbol, true);

				var sf = (SymbolicFeature) feature;
				foreach (FeatureSymbol symbol in sf.PossibleSymbols)
				{
					bool set = sfv.Values.Contains(symbol);
					_featureValues.Set(symbol, set);
					_antiFeatureValues.Set(symbol, !set);
				}
			}
		}

		public override void Add(IEnumerable<Feature> path, FeatureValue value)
		{
			Feature feature = path.First();
			IEnumerable<Feature> remaining = path.Skip(1);
			if (remaining.Any())
				throw new ArgumentException("The feature path is invalid.", "path");
			Add(feature, value);
		}

		public override void Clear()
		{
			_featureValues.SetAll(false);
			_antiFeatureValues.SetAll(false);
		}

		/// <summary>
		/// Gets the values associated with the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>The values.</returns>
		public override FeatureValue GetValue(Feature feature)
		{
			var values = new HashSet<FeatureSymbol>();
			var symbolicFeat = (SymbolicFeature) feature;
			foreach (FeatureSymbol value in symbolicFeat.PossibleSymbols)
			{
				if (_featureValues.Get(value))
					values.Add(value);
			}
			return values.Count == 0 ? null : new SymbolicFeatureValue(values);
		}

		public override FeatureValue GetValue(IEnumerable<Feature> path)
		{
			Feature feature = path.First();
			IEnumerable<Feature> remaining = path.Skip(1);
			if (remaining.Any())
				return null;
			return GetValue(feature);
		}

		public override bool Matches(FeatureStructure fs)
		{
			return Equals(fs);
		}

		/// <summary>
		/// Determines whether this feature bundle is unifiable with the specified feature bundle.
		/// </summary>
		/// <param name="fs">The feature bundle.</param>
		/// <returns>
		/// 	<c>true</c> if the feature bundles are unifiable, otherwise <c>false</c>.
		/// </returns>
		public override bool IsUnifiable(FeatureStructure fs)
		{
			var sfvs = fs as SymbolicFeatureValueSet;
			if (sfvs != null)
				return _featureValues.CanUnify(sfvs._featureValues);

			foreach (SymbolicFeature feature in fs.Features)
			{
				var value = (SymbolicFeatureValue) fs.GetValue(feature);
				if (value.Values.All(symbol => !_featureValues.Get(symbol)))
					return false;
			}
			return true;
		}

		public override bool UnifyWith(FeatureStructure other, bool useDefaults)
		{
			if (IsUnifiable(other))
			{
				foreach (SymbolicFeature feature in other.Features)
				{
					var curValue = (SymbolicFeatureValue) GetValue(feature);
					var otherValue = (SymbolicFeatureValue) other.GetValue(feature);

					foreach (FeatureSymbol symbol in curValue.Values.Intersect(otherValue.Values))
					{
						
					}
				}
				return true;
			}
			return false;
		}

		public override void Instantiate(FeatureStructure other)
		{
			var sfvs = other as SymbolicFeatureValueSet;
			if (sfvs != null)
			{
				_featureValues.UnionWith(sfvs._featureValues);
				_antiFeatureValues.ExceptWith(sfvs._antiFeatureValues);
			}
			else
			{
				
			}
		}

		public override void Uninstantiate(FeatureStructure other)
		{
			var sfvs = other as SymbolicFeatureValueSet;
			if (sfvs != null)
			{
				_featureValues.UnionWith(sfvs._antiFeatureValues);
			}
			else
			{
				
			}
		}

		public override void UninstantiateAll()
		{
			_featureValues.SetAll(true);
		}

		public override FeatureValue Clone()
		{
			return new SymbolicFeatureValueSet(this);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as SymbolicFeatureValueSet);
		}

		public bool Equals(SymbolicFeatureValueSet other)
		{
			if (other == null)
				return false;
			return _featureValues.Equals(other._featureValues);
		}

		public override int GetHashCode()
		{
			return _featureValues.GetHashCode();
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			bool firstFeature = true;
			foreach (Feature feat in Features)
			{
				bool firstValue = true;
				var valuesSb = new StringBuilder();
				var vi = (SymbolicFeatureValue)GetValue(feat);
				foreach (FeatureSymbol value in vi.Values)
				{
					if (!firstValue)
						valuesSb.Append(", ");
					valuesSb.Append(value.Description);
					firstValue = false;
				}

				if (!firstValue)
				{
					if (!firstFeature)
						sb.Append(", ");
					sb.Append(feat.Description);
					sb.Append("->(");
					sb.Append(valuesSb);
					sb.Append(")");
					firstFeature = false;
				}
			}
			return sb.ToString();
		}

		private struct FeatureBundle : ICloneable
		{
			private ulong _flags1;
			private ulong _flags2;

			public FeatureBundle(FeatureBundle fb)
			{
				_flags1 = fb._flags1;
				_flags2 = fb._flags2;
			}

			public bool CanUnify(FeatureBundle fb)
			{
				return ((fb._flags1 & ~_flags1) == 0) && ((fb._flags2 & ~_flags2) == 0);
			}

			public bool Intersects(FeatureBundle fb)
			{
				return ((_flags1 & fb._flags1) != 0) || ((_flags2 & fb._flags2) != 0);
			}

			/// <summary>
			/// Determines if the value at the specified index is set.
			/// </summary>
			/// <param name="symbol">The feature value.</param>
			/// <returns><c>true</c> if the value is set, otherwise <c>false</c></returns>
			public bool Get(FeatureSymbol symbol)
			{
				if (symbol.SymbolIndex < NumBits)
					return (_flags1 & (1UL << symbol.SymbolIndex)) != 0;

				return (_flags2 & (1UL << (symbol.SymbolIndex - NumBits))) != 0;
			}

			/// <summary>
			/// Sets or unsets the value at the specified index.
			/// </summary>
			/// <param name="symbol">The feature value.</param>
			/// <param name="v">if <c>true</c> the value is set, otherwise it is unset.</param>
			public void Set(FeatureSymbol symbol, bool v)
			{
				if (symbol.SymbolIndex < NumBits)
				{
					ulong mask = 1UL << symbol.SymbolIndex;
					if (v)
						_flags1 |= mask;
					else
						_flags1 &= ~mask;
				}
				else
				{
					ulong mask = 1UL << symbol.SymbolIndex - NumBits;
					if (v)
						_flags2 |= mask;
					else
						_flags2 &= ~mask;
				}
			}

			/// <summary>
			/// Sets or unsets all of the values.
			/// </summary>
			/// <param name="v">if <c>true</c> all values will be set, otherwise they will be unset.</param>
			public void SetAll(bool v)
			{
				if (v)
				{
					_flags1 = 0xffffffffffffffffUL;
					_flags2 = 0xffffffffffffffffUL;
				}
				else
				{
					_flags1 = 0;
					_flags2 = 0;
				}
			}

			public void UnionWith(FeatureBundle other)
			{
				_flags1 |= other._flags1;
				_flags2 |= other._flags2;
			}

			public void ExceptWith(FeatureBundle other)
			{
				_flags1 &= ~other._flags1;
				_flags2 &= ~other._flags2;
			}

			public override bool Equals(object obj)
			{
				if (!(obj is FeatureBundle))
					return false;
				return Equals((FeatureBundle) obj);
			}

			public bool Equals(FeatureBundle other)
			{
				return _flags1 == other._flags1 && _flags2 == other._flags2;
			}

			public override int GetHashCode()
			{
				return _flags1.GetHashCode() ^ _flags2.GetHashCode();
			}

			public FeatureBundle Clone()
			{
				return new FeatureBundle(this);
			}

			object ICloneable.Clone()
			{
				return Clone();
			}
		}
	}
}
