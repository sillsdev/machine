using System;

namespace SIL.APRE.FeatureModel
{
	public class SymbolicFeatureBuilder
	{
		public static implicit operator SymbolicFeature(SymbolicFeatureBuilder builder)
		{
			return builder.ToSymbolicFeature();
		}

		private readonly SymbolicFeature _feature;
		private FeatureSymbol _lastSymbol;

		public SymbolicFeatureBuilder(string id, string desc)
		{
			_feature = new SymbolicFeature(id, desc);
		}

		public SymbolicFeatureBuilder(string id)
		{
			_feature = new SymbolicFeature(id);
		}

		public SymbolicFeatureBuilder Symbol(string id, string desc)
		{
			_lastSymbol = new FeatureSymbol(id, desc);
			_feature.AddPossibleSymbol(_lastSymbol);
			return this;
		}

		public SymbolicFeatureBuilder Symbol(string id)
		{
			_lastSymbol = new FeatureSymbol(id);
			_feature.AddPossibleSymbol(_lastSymbol);
			return this;
		}

		public SymbolicFeatureBuilder Default()
		{
			if (_lastSymbol == null)
				throw new ArgumentException("There is no symbol to be made default.");
			_feature.DefaultValue = new SymbolicFeatureValue(_lastSymbol);
			return this;
		}

		public SymbolicFeature ToSymbolicFeature()
		{
			return _feature;
		}
	}
}
