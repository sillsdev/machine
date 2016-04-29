using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.FeatureModel
{
	public class SymbolicFeature : Feature
	{
		private readonly PossibleSymbolCollection _possibleSymbols;
		private readonly ulong _mask;

		public SymbolicFeature(string id, params FeatureSymbol[] possibleSymbols)
			: this(id, (IEnumerable<FeatureSymbol>) possibleSymbols)
		{
		}

		public SymbolicFeature(string id, IEnumerable<FeatureSymbol> possibleSymbols)
			: base(id)
		{
			_possibleSymbols = new PossibleSymbolCollection(possibleSymbols);
			int i = 0;
			foreach (FeatureSymbol symbol in _possibleSymbols)
			{
				symbol.Feature = this;
				symbol.Index = i++;
			}
			_mask = (1UL << _possibleSymbols.Count) - 1UL;
		}

		/// <summary>
		/// Gets all possible values.
		/// </summary>
		/// <value>All possible values.</value>
		public IReadOnlyKeyedCollection<string, FeatureSymbol> PossibleSymbols
		{
			get { return _possibleSymbols; }
		}

		public string DefaultSymbolID
		{
			set { DefaultValue = new SymbolicFeatureValue(_possibleSymbols[value]); }
		}

		internal ulong Mask
		{
			get { return _mask; }
		}
	}
}
