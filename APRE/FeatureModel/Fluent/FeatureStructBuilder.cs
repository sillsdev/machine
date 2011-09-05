using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.APRE.FeatureModel.Fluent
{
	public class FeatureStructBuilder : IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureValueSyntax, IFeatureStructSyntax,
		IFeatureValueSyntax
	{
		private readonly FeatureSystem _featSys;
		private readonly FeatureStruct _fs;
		private readonly FeatureStruct _rootFs;

		private Feature _lastFeature;
		private bool _not;

		public FeatureStructBuilder(FeatureSystem featSys)
		{
			_featSys = featSys;
			_fs = new FeatureStruct();
			_rootFs = _fs;
		}

		internal FeatureStructBuilder(FeatureSystem featSys, FeatureStruct rootFs)
		{
			_featSys = featSys;
			_fs = new FeatureStruct();
			_rootFs = rootFs;
		}

		public IDisjunctiveFeatureValueSyntax Feature(string featureID)
		{
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
			if (!AddSymbols(symbolID1, symbolIDs))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbolIDs");
			return this;
		}

		public IDisjunctiveFeatureStructSyntax Symbol(FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(symbol1.Feature, symbol1, symbols))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		public IDisjunctiveFeatureStructSyntax And(Func<IFirstDisjunctSyntax, IFinalDisjunctSyntax> build)
		{
			var disjunctionBuilder = new DisjunctionBuilder(_featSys, _rootFs);
			IFinalDisjunctSyntax result = build(disjunctionBuilder);
			_fs.AddDisjunction(result.ToDisjunction());
			return this;
		}

		public FeatureStruct Value
		{
			get { return _fs; }
		}

		IFeatureValueSyntax IFeatureStructSyntax.Feature(string featureID)
		{
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
			if (!AddSymbols(symbolID1, symbolIDs))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbolIDs");
			return this;
		}

		IFeatureStructSyntax IFeatureStructSyntax.Symbol(FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(symbol1.Feature, symbol1, symbols))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveNegatableFeatureValueSyntax.EqualTo(string string1, params string[] strings)
		{
			if (_lastFeature is ComplexFeature)
				throw new ArgumentException("The specified feature cannot be complex.");

			if (!Add(string1, strings))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "strings");

			return this;
		}

		private bool Add(string string1, IEnumerable<string> strings)
		{
			FeatureValue value = null;
			if (_lastFeature is StringFeature)
			{
				value = new StringFeatureValue(strings.Concat(string1), _not);
			}
			else if (_lastFeature is SymbolicFeature)
			{
				if (!AddSymbols(_lastFeature, string1, strings))
					return false;
			}

			_fs.AddValue(_lastFeature, value);
			_not = false;
			return true;
		}

		private bool AddSymbols(Feature feature, FeatureSymbol symbol1, IEnumerable<FeatureSymbol> symbols)
		{
			FeatureSymbol[] allSymbols = symbols.Concat(symbol1).ToArray();
			if (allSymbols.Any(s => s.Feature != feature))
				return false;
			var symbolFeature = (SymbolicFeature) feature;
			_fs.AddValue(symbolFeature, new SymbolicFeatureValue(_not ? symbolFeature.PossibleSymbols.Except(allSymbols) : allSymbols));
			return true;
		}

		private bool AddSymbols(string symbolID1, IEnumerable<string> symbolIDs)
		{
			FeatureSymbol symbol1 = _featSys.GetSymbol(symbolID1);
			IEnumerable<FeatureSymbol> symbols = symbolIDs.Select(id => _featSys.GetSymbol(id)).ToArray();
			return AddSymbols(symbol1.Feature, symbol1, symbols);
		}

		private bool AddSymbols(Feature feature, string symbolID1, IEnumerable<string> symbolIDs)
		{
			FeatureSymbol symbol1 = _featSys.GetSymbol(symbolID1);
			IEnumerable<FeatureSymbol> symbols = symbolIDs.Select(id => _featSys.GetSymbol(id)).ToArray();
			return AddSymbols(feature, symbol1, symbols);
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveNegatableFeatureValueSyntax.EqualTo(FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(_lastFeature, symbol1, symbols))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			_not = false;
			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveNegatableFeatureValueSyntax.EqualToVariable(string name)
		{
			_fs.AddValue(_lastFeature, new VariableFeatureValue(name, !_not));
			_not = false;
			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(string string1, params string[] strings)
		{
			if (_lastFeature is ComplexFeature)
				throw new ArgumentException("The specified feature cannot be complex.");

			if (!Add(string1, strings))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "strings");

			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualTo(FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(_lastFeature, symbol1, symbols))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IFeatureStructSyntax INegatableFeatureValueSyntax.EqualToVariable(string name)
		{
			_fs.AddValue(_lastFeature, new VariableFeatureValue(name, !_not));
			_not = false;
			return this;
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
			var fsBuilder = new FeatureStructBuilder(_featSys, _rootFs);
			_fs.AddValue(_lastFeature, fsBuilder._fs);
			build(fsBuilder);
			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveFeatureValueSyntax.ReferringTo(params Feature[] path)
		{
			_fs.AddValue(_lastFeature, _rootFs.GetValue(path));
			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveFeatureValueSyntax.ReferringTo(params string[] idPath)
		{
			_fs.AddValue(_lastFeature, _rootFs.GetValue(idPath));
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
			var fsBuilder = new FeatureStructBuilder(_featSys, _rootFs);
			_fs.AddValue(_lastFeature, fsBuilder._fs);
			build(fsBuilder);
			return this;
		}

		IFeatureStructSyntax IFeatureValueSyntax.ReferringTo(params Feature[] path)
		{
			_fs.AddValue(_lastFeature, _rootFs.GetValue(path));
			return this;
		}

		IFeatureStructSyntax IFeatureValueSyntax.ReferringTo(params string[] idPath)
		{
			_fs.AddValue(_lastFeature, _rootFs.GetValue(idPath));
			return this;
		}
	}
}
