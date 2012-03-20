using SIL.Collections;

namespace SIL.Machine.FeatureModel
{
	public class SymbolicFeature : Feature
	{
		private readonly IDBearerSet<FeatureSymbol> _possibleSymbols;

		public SymbolicFeature(string id)
			: base(id)
		{
			_possibleSymbols = new IDBearerSet<FeatureSymbol>();
		}

		/// <summary>
		/// Gets all possible values.
		/// </summary>
		/// <value>All possible values.</value>
		public IDBearerSet<FeatureSymbol> PossibleSymbols
		{
			get { return _possibleSymbols; }
		}
	}
}
