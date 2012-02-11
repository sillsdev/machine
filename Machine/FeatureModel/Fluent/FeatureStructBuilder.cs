using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.FeatureModel.Fluent
{
	public class FeatureStructBuilder : IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureValueSyntax, IFeatureStructSyntax,
		IFeatureValueSyntax
	{
		private readonly FeatureSystem _featSys;
		private readonly FeatureStruct _fs;
		private readonly IDictionary<int, FeatureValue> _ids; 

		private Feature _lastFeature;
		private bool _not;

		public FeatureStructBuilder()
			: this(new FeatureStruct())
		{
		}

		public FeatureStructBuilder(FeatureSystem featSys)
			: this(featSys, new FeatureStruct())
		{
		}

		public FeatureStructBuilder(FeatureStruct fs)
			: this(null, fs)
		{
		}

		public FeatureStructBuilder(FeatureSystem featSys, FeatureStruct fs)
			: this(featSys, fs, new Dictionary<int, FeatureValue>())
		{
		}

		internal FeatureStructBuilder(FeatureSystem featSys, FeatureStruct fs, IDictionary<int, FeatureValue> ids)
		{
			_featSys = featSys;
			_fs = fs;
			_ids = ids;
		}

		public IDisjunctiveFeatureValueSyntax Feature(string featureID)
		{
			if (_featSys == null)
				throw new NotSupportedException("A feature system must be specified.");
			_lastFeature = _featSys.GetFeature(featureID);
			return this;
		}

		public IDisjunctiveFeatureValueSyntax Feature(Feature feature)
		{
			_lastFeature = feature;
			return this;
		}

		public IDisjunctiveFeatureStructSyntax Symbol(string symbolID1, params string[] symbolIDs)
		{
			if (_featSys == null)
				throw new NotSupportedException("A feature system must be specified.");
			if (!AddSymbols(symbolID1, symbolIDs, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbolIDs");
			return this;
		}

		public IDisjunctiveFeatureStructSyntax Symbol(int id, string symbolID1, params string[] symbolIDs)
		{
			if (_featSys == null)
				throw new NotSupportedException("A feature system must be specified.");
			if (!AddSymbols(symbolID1, symbolIDs, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbolIDs");
			return this;
		}

		public IDisjunctiveFeatureStructSyntax Symbol(FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(symbol1.Feature, symbol1, symbols, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		public IDisjunctiveFeatureStructSyntax Symbol(int id, FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(symbol1.Feature, symbol1, symbols, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		public IDisjunctiveFeatureStructSyntax And(Func<IFirstDisjunctSyntax, IFinalDisjunctSyntax> build)
		{
			var disjunctionBuilder = new DisjunctionBuilder(_featSys, _ids);
			build(disjunctionBuilder);
			_fs.AddDisjunction(disjunctionBuilder.Disjuncts);
			return this;
		}

		public FeatureStruct Value
		{
			get { return _fs; }
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

		IFeatureStructSyntax IFeatureStructSyntax.Symbol(string symbolID1, params string[] symbolIDs)
		{
			if (_featSys == null)
				throw new NotSupportedException("A feature system must be specified.");
			if (!AddSymbols(symbolID1, symbolIDs, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbolIDs");
			return this;
		}

		IFeatureStructSyntax IFeatureStructSyntax.Symbol(int id, string symbolID1, params string[] symbolIDs)
		{
			if (_featSys == null)
				throw new NotSupportedException("A feature system must be specified.");
			if (!AddSymbols(symbolID1, symbolIDs, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbolIDs");
			return this;
		}

		IFeatureStructSyntax IFeatureStructSyntax.Symbol(FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(symbol1.Feature, symbol1, symbols, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IFeatureStructSyntax IFeatureStructSyntax.Symbol(int id, FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(symbol1.Feature, symbol1, symbols, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveNegatableFeatureValueSyntax.EqualTo(string string1, params string[] strings)
		{
			if (_lastFeature is ComplexFeature)
				throw new NotSupportedException("The specified feature cannot be complex.");

			if (_featSys == null && _lastFeature is SymbolicFeature)
				throw new NotSupportedException("A feature system must be specified.");

			if (!Add(string1, strings, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "strings");

			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveNegatableFeatureValueSyntax.EqualTo(int id, string string1, params string[] strings)
		{
			if (_lastFeature is ComplexFeature)
				throw new NotSupportedException("The specified feature cannot be complex.");

			if (_featSys == null && _lastFeature is SymbolicFeature)
				throw new NotSupportedException("A feature system must be specified.");

			if (!Add(string1, strings, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "strings");

			return this;
		}

		private bool Add(string string1, IEnumerable<string> strings, int id)
		{
			if (_lastFeature is StringFeature)
			{
				var value = new StringFeatureValue(strings.Concat(string1), _not);
				_fs.AddValue(_lastFeature, value);
				_not = false;
				if (id > -1)
					_ids[id] = value;
			}
			else if (_lastFeature is SymbolicFeature)
			{
				if (!AddSymbols(_lastFeature, string1, strings, id))
					return false;
			}
			return true;
		}

		private bool AddSymbols(Feature feature, FeatureSymbol symbol1, IEnumerable<FeatureSymbol> symbols, int id)
		{
			FeatureSymbol[] allSymbols = symbols.Concat(symbol1).ToArray();
			if (allSymbols.Any(s => s.Feature != feature))
				return false;
			var symbolFeature = (SymbolicFeature) feature;
			var value = new SymbolicFeatureValue(_not ? symbolFeature.PossibleSymbols.Except(allSymbols) : allSymbols);
			_fs.AddValue(symbolFeature, value);
			_not = false;
			if (id > -1)
				_ids[id] = value;
			return true;
		}

		private bool AddSymbols(string symbolID1, IEnumerable<string> symbolIDs, int id)
		{
			FeatureSymbol symbol1 = _featSys.GetSymbol(symbolID1);
			IEnumerable<FeatureSymbol> symbols = symbolIDs.Select(symID => _featSys.GetSymbol(symID)).ToArray();
			return AddSymbols(symbol1.Feature, symbol1, symbols, id);
		}

		private bool AddSymbols(Feature feature, string symbolID1, IEnumerable<string> symbolIDs, int id)
		{
			FeatureSymbol symbol1 = _featSys.GetSymbol(symbolID1);
			IEnumerable<FeatureSymbol> symbols = symbolIDs.Select(symID => _featSys.GetSymbol(symID)).ToArray();
			return AddSymbols(feature, symbol1, symbols, id);
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveNegatableFeatureValueSyntax.EqualTo(FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(_lastFeature, symbol1, symbols, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveNegatableFeatureValueSyntax.EqualTo(int id, FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(_lastFeature, symbol1, symbols, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveNegatableFeatureValueSyntax.EqualToVariable(string name)
		{
			if (_lastFeature is ComplexFeature)
				throw new ArgumentException("The specified feature cannot be complex.");

			AddVariable(name, -1);
			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveNegatableFeatureValueSyntax.EqualToVariable(int id, string name)
		{
			if (_lastFeature is ComplexFeature)
				throw new ArgumentException("The specified feature cannot be complex.");

			AddVariable(name, id);
			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(string string1, params string[] strings)
		{
			if (_lastFeature is ComplexFeature)
				throw new NotSupportedException("The specified feature cannot be complex.");

			if (_featSys == null && _lastFeature is SymbolicFeature)
				throw new NotSupportedException("A feature system must be specified.");

			if (!Add(string1, strings, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "strings");

			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(int id, string string1, params string[] strings)
		{
			if (_lastFeature is ComplexFeature)
				throw new NotSupportedException("The specified feature cannot be complex.");

			if (_featSys == null && _lastFeature is SymbolicFeature)
				throw new NotSupportedException("A feature system must be specified.");

			if (!Add(string1, strings, id))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "strings");

			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(_lastFeature, symbol1, symbols, -1))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(int id, FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(_lastFeature, symbol1, symbols, id))
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

		IDisjunctiveNegatableFeatureValueSyntax IDisjunctiveFeatureValueSyntax.Not
		{
			get
			{
				_not = true;
				return this;
			}
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveFeatureValueSyntax.EqualToFeatureStruct(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build)
		{
			BuildFeatureStruct(build, -1);
			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveFeatureValueSyntax.EqualToFeatureStruct(int id, Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build)
		{
			BuildFeatureStruct(build, id);
			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveFeatureValueSyntax.ReferringTo(int id)
		{
			FeatureValue value;
			if (!_ids.TryGetValue(id, out value))
				throw new ArgumentException("The ID has not been specified for a previous value.", "id");
			_fs.AddValue(_lastFeature, value);
			return this;
		}

		INegatableFeatureValueSyntax IFeatureValueSyntax.Not
		{
			get
			{
				_not = true;
				return this;
			}
		}

		IFeatureStructSyntax IFeatureValueSyntax.EqualToFeatureStruct(Func<IFeatureStructSyntax, IFeatureStructSyntax> build)
		{
			BuildFeatureStruct(build, -1);
			return this;
		}

		IFeatureStructSyntax IFeatureValueSyntax.EqualToFeatureStruct(int id, Func<IFeatureStructSyntax, IFeatureStructSyntax> build)
		{
			BuildFeatureStruct(build, id);
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

		private void BuildFeatureStruct(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build, int id)
		{
			var fsBuilder = new FeatureStructBuilder(_featSys, new FeatureStruct(), _ids);
			_fs.AddValue(_lastFeature, fsBuilder._fs);
			if (id > -1)
				_ids[id] = fsBuilder._fs;
			build(fsBuilder);
		}

		private void BuildFeatureStruct(Func<IFeatureStructSyntax, IFeatureStructSyntax> build, int id)
		{
			var fsBuilder = new FeatureStructBuilder(_featSys, new FeatureStruct(), _ids);
			_fs.AddValue(_lastFeature, fsBuilder._fs);
			if (id > -1)
				_ids[id] = fsBuilder._fs;
			build(fsBuilder);
		}
	}
}
