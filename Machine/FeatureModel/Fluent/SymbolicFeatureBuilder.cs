namespace SIL.Machine.FeatureModel.Fluent
{
	public class SymbolicFeatureBuilder : IDefaultSymbolicFeatureSyntax
	{
		private readonly SymbolicFeature _feature;
		private FeatureSymbol _lastSymbol;

		public SymbolicFeatureBuilder(string id, string desc)
		{
			_feature = new SymbolicFeature(id) {Description = desc};
		}

		public SymbolicFeatureBuilder(string id)
		{
			_feature = new SymbolicFeature(id);
		}

		public IDefaultSymbolicFeatureSyntax Symbol(string id, string desc)
		{
			_lastSymbol = new FeatureSymbol(id) {Description = desc};
			_feature.AddPossibleSymbol(_lastSymbol);
			return this;
		}

		public IDefaultSymbolicFeatureSyntax Symbol(string id)
		{
			_lastSymbol = new FeatureSymbol(id);
			_feature.AddPossibleSymbol(_lastSymbol);
			return this;
		}

		ISymbolicFeatureSyntax IDefaultSymbolicFeatureSyntax.Default
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
