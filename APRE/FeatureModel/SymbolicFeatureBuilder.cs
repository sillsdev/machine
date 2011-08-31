namespace SIL.APRE.FeatureModel
{
	public class SymbolicFeatureBuilder : IDefaultableSymbolicFeatureBuilder
	{
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

		public IDefaultableSymbolicFeatureBuilder Symbol(string id, string desc)
		{
			_lastSymbol = new FeatureSymbol(id, desc);
			_feature.AddPossibleSymbol(_lastSymbol);
			return this;
		}

		public IDefaultableSymbolicFeatureBuilder Symbol(string id)
		{
			_lastSymbol = new FeatureSymbol(id);
			_feature.AddPossibleSymbol(_lastSymbol);
			return this;
		}

		ISymbolicFeatureBuilder IDefaultableSymbolicFeatureBuilder.Default
		{
			get
			{
				_feature.DefaultValue = new SymbolicFeatureValue(_lastSymbol);
				return this;
			}
		}

		public SymbolicFeature Value
		{
			get { return _feature; }
		}
	}
}
