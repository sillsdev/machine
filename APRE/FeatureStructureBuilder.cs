using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.APRE
{
	public class FeatureStructureBuilder
	{
		public static implicit operator FeatureStructure(FeatureStructureBuilder builder)
		{
			return builder.ToFeatureStructure();
		}

		private readonly FeatureSystem _featSys;
		private readonly FeatureStructure _fs;
		private readonly FeatureStructure _rootFs;
		private bool _not;

		public FeatureStructureBuilder(FeatureSystem featSys)
		{
			_featSys = featSys;
			_fs = new FeatureStructure();
			_rootFs = _fs;
		}

		internal FeatureStructureBuilder(FeatureSystem featSys, FeatureStructure rootFs)
		{
			_featSys = featSys;
			_fs = new FeatureStructure();
			_rootFs = rootFs;
		}

		public FeatureStructureBuilder Symbol(string symbolId1, params string[] symbolIds)
		{
			FeatureSymbol symbol = _featSys.GetSymbol(symbolId1);
			IEnumerable<FeatureSymbol> symbols = symbolIds.Select(id => _featSys.GetSymbol(id)).ToArray();
			if (symbols.Any(s => s.Feature != symbol.Feature))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbolIds");
			AddSymbols(symbol, symbols);
			return this;
		}

		public FeatureStructureBuilder Symbol(FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (symbols.Any(s => s.Feature != symbol1.Feature))
				throw new ArgumentException("All specified symbols must be associated with the same feature.", "symbols");
			AddSymbols(symbol1, symbols);
			return this;
		}

		public FeatureStructureBuilder Symbol(string featureId, string symbolId1, params string[] symbolIds)
		{
			var feature = (SymbolicFeature)_featSys.GetFeature(featureId);
			FeatureSymbol symbol = _featSys.GetSymbol(symbolId1);
			IEnumerable<FeatureSymbol> symbols = symbolIds.Select(id => _featSys.GetSymbol(id)).ToArray();
			if (symbol.Feature != feature || symbols.Any(s => s.Feature != feature))
				throw new ArgumentException("All specified symbols must be associated with the specified feature.", "symbolIds");
			AddSymbols(symbol, symbols);
			return this;
		}

		public FeatureStructureBuilder Symbol(SymbolicFeature feature, FeatureSymbol symbol1, params FeatureSymbol[] symbols)
		{
			if (symbol1.Feature != feature || symbols.Any(s => s.Feature != feature))
				throw new ArgumentException("All specified symbols must be associated with the specified feature.", "symbols");
			AddSymbols(symbol1, symbols);
			return this;
		}

		private void AddSymbols(FeatureSymbol symbol1, IEnumerable<FeatureSymbol> symbols)
		{
			symbols = symbols.Concat(symbol1);
			_fs.AddValue(symbol1.Feature, new SymbolicFeatureValue(_not ? symbol1.Feature.PossibleSymbols.Except(symbols) : symbols));
			_not = false;
		}

		public FeatureStructureBuilder AnySymbol(string featureId)
		{
			return AnySymbol((SymbolicFeature)_featSys.GetFeature(featureId));
		}

		public FeatureStructureBuilder AnySymbol(SymbolicFeature feature)
		{
			_fs.AddValue(feature, _not ? new SymbolicFeatureValue(feature) : new SymbolicFeatureValue(feature.PossibleSymbols));
			_not = false;
			return this;
		}

		public FeatureStructureBuilder String(string featureId, string string1, params string[] strings)
		{
			return String((StringFeature)_featSys.GetFeature(featureId), string1, strings);
		}

		public FeatureStructureBuilder String(StringFeature feature, string string1, params string[] strings)
		{
			_fs.AddValue(feature, new StringFeatureValue(strings.Concat(string1), _not));
			_not = false;
			return this;
		}

		public FeatureStructureBuilder AnyString(string featureId)
		{
			return AnyString((StringFeature)_featSys.GetFeature(featureId));
		}

		public FeatureStructureBuilder AnyString(StringFeature feature)
		{
			_fs.AddValue(feature, new StringFeatureValue(Enumerable.Empty<string>(), !_not));
			_not = false;
			return this;
		}

		public FeatureStructureBuilder Not()
		{
			_not = true;
			return this;
		}

		public FeatureStructureBuilder FeatureStructure(ComplexFeature feature, Action<FeatureStructureBuilder> build)
		{
			if (_not)
				throw new InvalidOperationException("A negated feature structure cannot be created.");

			var fsBuilder = new FeatureStructureBuilder(_featSys, _rootFs);
			_fs.AddValue(feature, fsBuilder);
			build(fsBuilder);
			return this;
		}

		public FeatureStructureBuilder FeatureStructure(string featureId, Action<FeatureStructureBuilder> build)
		{
			return FeatureStructure((ComplexFeature) _featSys.GetFeature(featureId), build);
		}

		public FeatureStructureBuilder Pointer(Feature feature, params Feature[] path)
		{
			return Pointer(feature, (IEnumerable<Feature>) path);
		}

		public FeatureStructureBuilder Pointer(Feature feature, IEnumerable<Feature> path)
		{
			if (_not)
				throw new InvalidOperationException("A negated pointer cannot be created.");

			_fs.AddValue(feature, _rootFs.GetValue(path));
			return this;
		}

		public FeatureStructureBuilder Pointer(string featureId, params string[] idPath)
		{
			return Pointer(featureId, (IEnumerable<string>) idPath);
		}

		public FeatureStructureBuilder Pointer(string featureId, IEnumerable<string> idPath)
		{
			return Pointer(_featSys.GetFeature(featureId), idPath.Select(id => _featSys.GetFeature(id)));
		}

		public FeatureStructureBuilder Variable(Feature feature, string name)
		{
			_fs.AddValue(feature, new VariableFeatureValue(name, !_not));
			_not = false;
			return this;
		}

		public FeatureStructureBuilder Variable(string featureId, string name)
		{
			return Variable(_featSys.GetFeature(featureId), name);
		}

		public FeatureStructure ToFeatureStructure()
		{
			return _fs;
		}
	}
}
