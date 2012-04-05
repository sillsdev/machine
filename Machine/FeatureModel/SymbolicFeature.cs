namespace SIL.Machine.FeatureModel
{
	public class SymbolicFeature : Feature
	{
		private readonly PossibleSymbolCollection _possibleSymbols;

		public SymbolicFeature(string id)
			: base(id)
		{
			_possibleSymbols = new PossibleSymbolCollection(this);
		}

		/// <summary>
		/// Gets all possible values.
		/// </summary>
		/// <value>All possible values.</value>
		public PossibleSymbolCollection PossibleSymbols
		{
			get { return _possibleSymbols; }
		}
	}
}
