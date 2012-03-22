using System.Collections.Specialized;
using SIL.Collections;

namespace SIL.Machine.FeatureModel
{
	public class SymbolicFeature : Feature
	{
		private readonly ObservableIDBearerSet<FeatureSymbol> _possibleSymbols;

		public SymbolicFeature(string id)
			: base(id)
		{
			_possibleSymbols = new ObservableIDBearerSet<FeatureSymbol>();
			_possibleSymbols.CollectionChanged += PossibleSymbolsChanged;
		}

		private void PossibleSymbolsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (FeatureSymbol symbol in e.OldItems)
					symbol.Feature = null;
			}

			if (e.NewItems != null)
			{
				foreach (FeatureSymbol symbol in e.NewItems)
					symbol.Feature = this;
			}
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
