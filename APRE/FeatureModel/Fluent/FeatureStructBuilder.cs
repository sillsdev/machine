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
		private readonly FeatureStruct _rootFS;

		private Feature _lastFeature;
		private bool _not;

		public FeatureStructBuilder(FeatureSystem featSys)
			: this(featSys, new FeatureStruct())
		{
		}

		public FeatureStructBuilder(FeatureSystem featSys, FeatureStruct fs)
			: this(featSys, fs, fs)
		{
		}

		internal FeatureStructBuilder(FeatureSystem featSys, FeatureStruct fs, FeatureStruct rootFS)
		{
			_featSys = featSys;
			_fs = fs;
			_rootFS = rootFS;
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
			var disjunctionBuilder = new DisjunctionBuilder(_featSys, _rootFS);
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
			if (_lastFeature is StringFeature)
			{
				_fs.AddValue(_lastFeature, new StringFeatureValue(strings.Concat(string1), _not));
				_not = false;
			}
			else if (_lastFeature is SymbolicFeature)
			{
				if (!AddSymbols(_lastFeature, string1, strings))
					return false;
			}
			return true;
		}

		private bool AddSymbols(Feature feature, FeatureSymbol symbol1, IEnumerable<FeatureSymbol> symbols)
		{
			FeatureSymbol[] allSymbols = symbols.Concat(symbol1).ToArray();
			if (allSymbols.Any(s => s.Feature != feature))
				return false;
			var symbolFeature = (SymbolicFeature) feature;
			_fs.AddValue(symbolFeature, new SymbolicFeatureValue(_not ? symbolFeature.PossibleSymbols.Except(allSymbols) : allSymbols));
			_not = false;
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
			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveNegatableFeatureValueSyntax.EqualToVariable(string name)
		{
			if (_lastFeature is ComplexFeature)
				throw new ArgumentException("The specified feature cannot be complex.");

			AddVariable(name);
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
			if (_lastFeature is ComplexFeature)
				throw new ArgumentException("The specified feature cannot be complex.");

			AddVariable(name);
			return this;
		}

		private void AddVariable(string name)
		{
			FeatureValue vfv;
			if (_lastFeature is StringFeature)
				vfv = new StringFeatureValue(name, !_not);
			else
				vfv = new SymbolicFeatureValue((SymbolicFeature)_lastFeature, name, !_not);
			_fs.AddValue(_lastFeature, vfv);
			_not = false;
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
			var fsBuilder = new FeatureStructBuilder(_featSys, _rootFS);
			_fs.AddValue(_lastFeature, fsBuilder._fs);
			build(fsBuilder);
			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveFeatureValueSyntax.ReferringTo(params Feature[] path)
		{
			_fs.AddValue(_lastFeature, _rootFS.GetValue(path));
			return this;
		}

		IDisjunctiveFeatureStructSyntax IDisjunctiveFeatureValueSyntax.ReferringTo(params string[] idPath)
		{
			_fs.AddValue(_lastFeature, _rootFS.GetValue(idPath));
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
			var fsBuilder = new FeatureStructBuilder(_featSys, _rootFS);
			_fs.AddValue(_lastFeature, fsBuilder._fs);
			build(fsBuilder);
			return this;
		}

		IFeatureStructSyntax IFeatureValueSyntax.ReferringTo(params Feature[] path)
		{
			_fs.AddValue(_lastFeature, _rootFS.GetValue(path));
			return this;
		}

		IFeatureStructSyntax IFeatureValueSyntax.ReferringTo(params string[] idPath)
		{
			_fs.AddValue(_lastFeature, _rootFS.GetValue(idPath));
			return this;
		}
	}
}
