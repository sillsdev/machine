using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.APRE.FeatureModel
{
	public class DisjunctiveFeatureStructBuilder : IDisjunctiveFeatureStructBuilder, IDisjunctiveFeatureValueBuilder, IFeatureStructBuilder,
		IFeatureValueBuilder
	{
		private readonly FeatureSystem _featSys;
		private readonly FeatureStruct _fs;
		private readonly FeatureStruct _rootFs;

		private Feature _lastFeature;
		private bool _not;

		public DisjunctiveFeatureStructBuilder(FeatureSystem featSys)
		{
			_featSys = featSys;
			_fs = new FeatureStruct();
			_rootFs = _fs;
		}

		internal DisjunctiveFeatureStructBuilder(FeatureSystem featSys, FeatureStruct rootFs)
		{
			_featSys = featSys;
			_fs = new FeatureStruct();
			_rootFs = rootFs;
		}

		public IDisjunctiveFeatureValueBuilder Feature(string featureID)
		{
			_lastFeature = _featSys.GetFeature(featureID);
			return this;
		}

		public IDisjunctiveFeatureValueBuilder Feature(Feature feature)
		{
			_lastFeature = feature;
			return this;
		}

		public IDisjunctiveFeatureStructBuilder Symbol(string symbolID1, params string[] symbolIDs)
		{
			if (!AddSymbols(symbolID1, symbolIDs))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbolIDs");
			return this;
		}

		public IDisjunctiveFeatureStructBuilder Symbol(FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(symbol1, symbols))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		public IDisjunctiveFeatureStructBuilder And(Func<IFirstDisjunctBuilder, IFinalDisjunctBuilder> build)
		{
			var disjunctionBuilder = new DisjunctionBuilder(_featSys, _rootFs);
			IFinalDisjunctBuilder result = build(disjunctionBuilder);
			_fs.AddDisjunction(result.ToDisjunction());
			return this;
		}

		public FeatureStruct Value
		{
			get { return _fs; }
		}

		IFeatureValueBuilder IFeatureStructBuilder.Feature(string featureID)
		{
			_lastFeature = _featSys.GetFeature(featureID);
			return this;
		}

		IFeatureValueBuilder IFeatureStructBuilder.Feature(Feature feature)
		{
			_lastFeature = feature;
			return this;
		}

		IFeatureStructBuilder IFeatureStructBuilder.Symbol(string symbolID1, params string[] symbolIDs)
		{
			if (!AddSymbols(symbolID1, symbolIDs))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbolIDs");
			return this;
		}

		IFeatureStructBuilder IFeatureStructBuilder.Symbol(FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(symbol1, symbols))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IDisjunctiveFeatureStructBuilder IDisjunctiveNegatableFeatureValueBuilder.EqualTo(string string1, params string[] strings)
		{
			if (_lastFeature.ValueType == FeatureValueType.Complex)
				throw new ArgumentException("The specified feature cannot be complex.");

			if (!Add(string1, strings))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "strings");

			return this;
		}

		private bool Add(string string1, IEnumerable<string> strings)
		{
			FeatureValue value = null;
			switch (_lastFeature.ValueType)
			{
				case FeatureValueType.String:
					value = new StringFeatureValue(strings.Concat(string1), _not);
					break;

				case FeatureValueType.Symbol:
					if (!AddSymbols(string1, strings))
						return false;
					break;
			}

			_fs.AddValue(_lastFeature, value);
			_not = false;
			return true;
		}

		private bool AddSymbols(FeatureSymbol symbol1, IEnumerable<FeatureSymbol> symbols)
		{
			if (symbols.Any(s => s.Feature != symbol1.Feature))
				return false;
			var symbolFeature = (SymbolicFeature)_lastFeature;
			symbols = symbols.Concat(symbol1);
			_fs.AddValue(symbol1.Feature, new SymbolicFeatureValue(_not ? symbolFeature.PossibleSymbols.Except(symbols) : symbols));
			return true;
		}

		private bool AddSymbols(string symbolID1, IEnumerable<string> symbolIDs)
		{
			FeatureSymbol symbol1 = _featSys.GetSymbol(symbolID1);
			IEnumerable<FeatureSymbol> symbols = symbolIDs.Select(id => _featSys.GetSymbol(id)).ToArray();
			return AddSymbols(symbol1, symbols);
		}

		IDisjunctiveFeatureStructBuilder IDisjunctiveNegatableFeatureValueBuilder.EqualTo(FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			IEnumerable<FeatureSymbol> allSymbols = symbols.Concat(symbol1);
			if (allSymbols.Any(s => s.Feature != _lastFeature))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			var symbolFeature = (SymbolicFeature)_lastFeature;
			_fs.AddValue(_lastFeature, new SymbolicFeatureValue(_not ? symbolFeature.PossibleSymbols.Except(allSymbols) : allSymbols));
			_not = false;
			return this;
		}

		IDisjunctiveFeatureStructBuilder IDisjunctiveNegatableFeatureValueBuilder.EqualToAny
		{
			get
			{
				if (_lastFeature.ValueType == FeatureValueType.Complex)
					throw new ArgumentException("The specified feature cannot be complex.");
				AddAny();
				return this;
			}
		}

		private void AddAny()
		{
			FeatureValue value = null;
			switch (_lastFeature.ValueType)
			{
				case FeatureValueType.String:
					value = new StringFeatureValue(Enumerable.Empty<string>(), !_not);
					break;

				case FeatureValueType.Symbol:
					var symbolFeature = (SymbolicFeature)_lastFeature;
					value = _not ? new SymbolicFeatureValue(symbolFeature) : new SymbolicFeatureValue(symbolFeature.PossibleSymbols);
					break;
			}

			_fs.AddValue(_lastFeature, value);
			_not = false;
		}

		IDisjunctiveFeatureStructBuilder IDisjunctiveNegatableFeatureValueBuilder.EqualToVariable(string name)
		{
			_fs.AddValue(_lastFeature, new VariableFeatureValue(name, !_not));
			_not = false;
			return this;
		}

		IFeatureStructBuilder INegatableFeatureValueBuilder.EqualTo(string string1, params string[] strings)
		{
			if (_lastFeature.ValueType == FeatureValueType.Complex)
				throw new ArgumentException("The specified feature cannot be complex.");

			if (!Add(string1, strings))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "strings");

			return this;
		}

		IFeatureStructBuilder INegatableFeatureValueBuilder.EqualTo(FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (!AddSymbols(symbol1, symbols))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			return this;
		}

		IFeatureStructBuilder INegatableFeatureValueBuilder.EqualToAny
		{
			get
			{
				if (_lastFeature.ValueType == FeatureValueType.Complex)
					throw new ArgumentException("The specified feature cannot be complex.");
				AddAny();
				return this;
			}
		}

		IFeatureStructBuilder INegatableFeatureValueBuilder.EqualToVariable(string name)
		{
			_fs.AddValue(_lastFeature, new VariableFeatureValue(name, !_not));
			_not = false;
			return this;
		}

		IDisjunctiveNegatableFeatureValueBuilder IDisjunctiveFeatureValueBuilder.Not
		{
			get
			{
				_not = true;
				return this;
			}
		}

		IDisjunctiveFeatureStructBuilder IDisjunctiveFeatureValueBuilder.EqualToFeatureStruct(Func<IDisjunctiveFeatureStructBuilder, IDisjunctiveFeatureStructBuilder> build)
		{
			var fsBuilder = new DisjunctiveFeatureStructBuilder(_featSys, _rootFs);
			_fs.AddValue(_lastFeature, fsBuilder._fs);
			build(fsBuilder);
			return this;
		}

		IDisjunctiveFeatureStructBuilder IDisjunctiveFeatureValueBuilder.ReferringTo(params Feature[] path)
		{
			_fs.AddValue(_lastFeature, _rootFs.GetValue(path));
			return this;
		}

		IDisjunctiveFeatureStructBuilder IDisjunctiveFeatureValueBuilder.ReferringTo(params string[] idPath)
		{
			_fs.AddValue(_lastFeature, _rootFs.GetValue(idPath));
			return this;
		}

		INegatableFeatureValueBuilder IFeatureValueBuilder.Not
		{
			get
			{
				_not = true;
				return this;
			}
		}

		IFeatureStructBuilder IFeatureValueBuilder.EqualToFeatureStruct(Func<IFeatureStructBuilder, IFeatureStructBuilder> build)
		{
			var fsBuilder = new DisjunctiveFeatureStructBuilder(_featSys, _rootFs);
			_fs.AddValue(_lastFeature, fsBuilder._fs);
			build(fsBuilder);
			return this;
		}

		IFeatureStructBuilder IFeatureValueBuilder.ReferringTo(params Feature[] path)
		{
			_fs.AddValue(_lastFeature, _rootFs.GetValue(path));
			return this;
		}

		IFeatureStructBuilder IFeatureValueBuilder.ReferringTo(params string[] idPath)
		{
			_fs.AddValue(_lastFeature, _rootFs.GetValue(idPath));
			return this;
		}
	}
}
