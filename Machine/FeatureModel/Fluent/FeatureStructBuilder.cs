using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.FeatureModel.Fluent
{
	public class FeatureStructBuilder : IFeatureStructSyntax, IFeatureValueSyntax
	{
		private readonly FeatureSystem _featSys;
		private readonly FeatureStruct _fs;
		private readonly IDictionary<int, FeatureValue> _ids;
		private readonly bool _mutable;

		private Feature _lastFeature;
		private bool _not;

		public FeatureStructBuilder()
			: this(new FeatureStruct())
		{
		}

		public FeatureStructBuilder(bool mutable)
			: this(new FeatureStruct(), mutable)
		{
		}

		public FeatureStructBuilder(FeatureSystem featSys)
			: this(featSys, new FeatureStruct())
		{
		}

		public FeatureStructBuilder(FeatureSystem featSys, bool mutable)
			: this(featSys, new FeatureStruct(), mutable)
		{
		}

		public FeatureStructBuilder(FeatureStruct fs)
			: this(null, fs)
		{
		}

		public FeatureStructBuilder(FeatureStruct fs, bool mutable)
			: this(null, fs, mutable)
		{
		}

		public FeatureStructBuilder(FeatureSystem featSys, FeatureStruct fs)
			: this(featSys, fs, false)
		{
		}

		public FeatureStructBuilder(FeatureSystem featSys, FeatureStruct fs, bool mutable)
			: this(featSys, fs, new Dictionary<int, FeatureValue>(), mutable)
		{
		}

		internal FeatureStructBuilder(FeatureSystem featSys, FeatureStruct fs, IDictionary<int, FeatureValue> ids, bool mutable)
		{
			_featSys = featSys;
			_fs = fs;
			_ids = ids;
			_mutable = mutable;
		}

		public FeatureStruct Value
		{
			get
			{
				if (!_mutable)
					_fs.Freeze();
				return _fs;
			}
		}

		IFeatureValueSyntax IFeatureStructSyntax.Feature(string featureID)
		{
			if (_featSys == null)
				throw new NotSupportedException("A feature system must be specified.");
			_lastFeature = _featSys.GetFeature(featureID);
			return this;
		}

		IFeatureValueSyntax IFeatureStructSyntax.Feature(Feature feature)
		{
			_lastFeature = feature;
			return this;
		}

		IFeatureStructSyntax IFeatureStructSyntax.Symbol(params string[] symbolIDs)
		{
			if (_featSys == null)
				throw new NotSupportedException("A feature system must be specified.");

			if (symbolIDs.Length == 0)
				throw new ArgumentException("At least one symbol ID should be specified.", "symbolIDs");

			if (!AddSymbols(symbolIDs, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbolIDs");
			return this;
		}

		IFeatureStructSyntax IFeatureStructSyntax.Symbol(IEnumerable<string> symbolIDs)
		{
			if (_featSys == null)
				throw new NotSupportedException("A feature system must be specified.");

			string[] symbolIDsArray = symbolIDs.ToArray();
			if (symbolIDsArray.Length == 0)
				throw new ArgumentException("At least one symbol ID should be specified.", "symbolIDs");

			if (!AddSymbols(symbolIDsArray, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbolIDs");
			return this;
		}

		IFeatureStructSyntax IFeatureStructSyntax.Symbol(int id, params string[] symbolIDs)
		{
			if (_featSys == null)
				throw new NotSupportedException("A feature system must be specified.");

			if (symbolIDs.Length == 0)
				throw new ArgumentException("At least one symbol ID should be specified.", "symbolIDs");

			if (!AddSymbols(symbolIDs, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbolIDs");
			return this;
		}

		IFeatureStructSyntax IFeatureStructSyntax.Symbol(int id, IEnumerable<string> symbolIDs)
		{
			if (_featSys == null)
				throw new NotSupportedException("A feature system must be specified.");

			string[] symbolIDsArray = symbolIDs.ToArray();
			if (symbolIDsArray.Length == 0)
				throw new ArgumentException("At least one symbol ID should be specified.", "symbolIDs");

			if (!AddSymbols(symbolIDsArray, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbolIDs");
			return this;
		}

		IFeatureStructSyntax IFeatureStructSyntax.Symbol(params FeatureSymbol[] symbols)
		{
			if (symbols.Length == 0)
				throw new ArgumentException("At least one symbol should be specified.", "symbols");

			if (!AddSymbols(symbols[0].Feature, symbols, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IFeatureStructSyntax IFeatureStructSyntax.Symbol(IEnumerable<FeatureSymbol> symbols)
		{
			FeatureSymbol[] symbolsArray = symbols.ToArray();
			if (symbolsArray.Length == 0)
				throw new ArgumentException("At least one symbol should be specified.", "symbols");

			if (!AddSymbols(symbolsArray[0].Feature, symbolsArray, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IFeatureStructSyntax IFeatureStructSyntax.Symbol(int id, params FeatureSymbol[] symbols)
		{
			if (symbols.Length == 0)
				throw new ArgumentException("At least one symbol should be specified.", "symbols");

			if (!AddSymbols(symbols[0].Feature, symbols, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IFeatureStructSyntax IFeatureStructSyntax.Symbol(int id, IEnumerable<FeatureSymbol> symbols)
		{
			FeatureSymbol[] symbolsArray = symbols.ToArray();
			if (symbolsArray.Length == 0)
				throw new ArgumentException("At least one symbol should be specified.", "symbols");

			if (!AddSymbols(symbolsArray[0].Feature, symbolsArray, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		private bool Add(string[] strings, int id)
		{
			if (_lastFeature is StringFeature)
			{
				var value = new StringFeatureValue(strings, _not);
				_fs.AddValue(_lastFeature, value);
				_not = false;
				if (id > -1)
					_ids[id] = value;
			}
			else if (_lastFeature is SymbolicFeature)
			{
				if (!AddSymbols(_lastFeature, strings, id))
					return false;
			}
			return true;
		}

		private bool AddSymbols(Feature feature, FeatureSymbol[] symbols, int id)
		{
			if (symbols.Any(s => s.Feature != feature))
				return false;
			var symbolFeature = (SymbolicFeature) feature;
			var value = new SymbolicFeatureValue(_not ? symbolFeature.PossibleSymbols.Except(symbols) : symbols);
			_fs.AddValue(symbolFeature, value);
			_not = false;
			if (id > -1)
				_ids[id] = value;
			return true;
		}

		private bool AddSymbols(string[] symbolIDs, int id)
		{
			FeatureSymbol[] symbols = symbolIDs.Select(symID => _featSys.GetSymbol(symID)).ToArray();
			return AddSymbols(symbols[0].Feature, symbols, id);
		}

		private bool AddSymbols(Feature feature, string[] symbolIDs, int id)
		{
			return AddSymbols(feature, symbolIDs.Select(symID => _featSys.GetSymbol(symID)).ToArray(), id);
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(params string[] strings)
		{
			if (_lastFeature is ComplexFeature)
				throw new NotSupportedException("The specified feature cannot be complex.");

			if (_featSys == null && _lastFeature is SymbolicFeature)
				throw new NotSupportedException("A feature system must be specified.");

			if (strings.Length == 0)
				throw new ArgumentException("At least one string should be specified.", "strings");

			if (!Add(strings, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "strings");

			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(IEnumerable<string> strings)
		{
			if (_lastFeature is ComplexFeature)
				throw new NotSupportedException("The specified feature cannot be complex.");

			if (_featSys == null && _lastFeature is SymbolicFeature)
				throw new NotSupportedException("A feature system must be specified.");

			string[] stringsArray = strings.ToArray();
			if (stringsArray.Length == 0)
				throw new ArgumentException("At least one string should be specified.", "strings");

			if (!Add(stringsArray, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "strings");

			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(int id, params string[] strings)
		{
			if (_lastFeature is ComplexFeature)
				throw new NotSupportedException("The specified feature cannot be complex.");

			if (_featSys == null && _lastFeature is SymbolicFeature)
				throw new NotSupportedException("A feature system must be specified.");

			if (strings.Length == 0)
				throw new ArgumentException("At least one string should be specified.", "strings");

			if (!Add(strings, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "strings");

			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(int id, IEnumerable<string> strings)
		{
			if (_lastFeature is ComplexFeature)
				throw new NotSupportedException("The specified feature cannot be complex.");

			if (_featSys == null && _lastFeature is SymbolicFeature)
				throw new NotSupportedException("A feature system must be specified.");

			string[] stringsArray = strings.ToArray();
			if (stringsArray.Length == 0)
				throw new ArgumentException("At least one string should be specified.", "strings");

			if (!Add(stringsArray, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "strings");

			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(params FeatureSymbol[] symbols)
		{
			if (symbols.Length == 0)
				throw new ArgumentException("At least one symbol should be specified.", "symbols");

			if (!AddSymbols(_lastFeature, symbols, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(IEnumerable<FeatureSymbol> symbols)
		{
			FeatureSymbol[] symbolsArray = symbols.ToArray();
			if (symbolsArray.Length == 0)
				throw new ArgumentException("At least one symbol should be specified.", "symbols");

			if (!AddSymbols(_lastFeature, symbolsArray, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(int id, params FeatureSymbol[] symbols)
		{
			if (symbols.Length == 0)
				throw new ArgumentException("At least one symbol should be specified.", "symbols");

			if (!AddSymbols(_lastFeature, symbols, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(int id, IEnumerable<FeatureSymbol> symbols)
		{
			FeatureSymbol[] symbolsArray = symbols.ToArray();
			if (symbolsArray.Length == 0)
				throw new ArgumentException("At least one symbol should be specified.", "symbols");

			if (!AddSymbols(_lastFeature, symbolsArray, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualToVariable(string name)
		{
			if (_lastFeature is ComplexFeature)
				throw new ArgumentException("The specified feature cannot be complex.");

			AddVariable(name, -1);
			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualToVariable(int id, string name)
		{
			if (_lastFeature is ComplexFeature)
				throw new ArgumentException("The specified feature cannot be complex.");

			AddVariable(name, id);
			return this;
		}

		private void AddVariable(string name, int id)
		{
			FeatureValue vfv;
			if (_lastFeature is StringFeature)
				vfv = new StringFeatureValue(name, !_not);
			else
				vfv = new SymbolicFeatureValue((SymbolicFeature)_lastFeature, name, !_not);
			_fs.AddValue(_lastFeature, vfv);
			_not = false;
			if (id > -1)
				_ids[id] = vfv;
		}

		INegatableFeatureValueSyntax IFeatureValueSyntax.Not
		{
			get
			{
				_not = true;
				return this;
			}
		}

		IFeatureStructSyntax IFeatureValueSyntax.EqualTo(Func<IFeatureStructSyntax, IFeatureStructSyntax> build)
		{
			BuildFeatureStruct(build, -1);
			return this;
		}

		IFeatureStructSyntax IFeatureValueSyntax.EqualTo(FeatureStruct fs)
		{
			if (!(_lastFeature is ComplexFeature))
				throw new NotSupportedException("The specified feature must be complex.");

			_fs.AddValue(_lastFeature, fs);
			return this;
		}

		IFeatureStructSyntax IFeatureValueSyntax.EqualTo(int id, Func<IFeatureStructSyntax, IFeatureStructSyntax> build)
		{
			BuildFeatureStruct(build, id);
			return this;
		}

		IFeatureStructSyntax IFeatureValueSyntax.EqualTo(int id, FeatureStruct fs)
		{
			if (!(_lastFeature is ComplexFeature))
				throw new NotSupportedException("The specified feature must be complex.");

			_fs.AddValue(_lastFeature, fs);
			if (id > -1)
				_ids[id] = fs;
			return this;
		}

		IFeatureStructSyntax IFeatureValueSyntax.ReferringTo(int id)
		{
			FeatureValue value;
			if (!_ids.TryGetValue(id, out value))
				throw new ArgumentException("The ID has not been specified for a previous value.", "id");
			_fs.AddValue(_lastFeature, value);
			return this;
		}

		private void BuildFeatureStruct(Func<IFeatureStructSyntax, IFeatureStructSyntax> build, int id)
		{
			var fsBuilder = new FeatureStructBuilder(_featSys, new FeatureStruct(), _ids, true);
			_fs.AddValue(_lastFeature, fsBuilder._fs);
			if (id > -1)
				_ids[id] = fsBuilder._fs;
			build(fsBuilder);
		}
	}
}
