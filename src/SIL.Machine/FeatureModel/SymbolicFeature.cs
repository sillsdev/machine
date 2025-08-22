using System.Collections;
using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.FeatureModel
{
    public class SymbolicFeature : Feature
    {
        private readonly PossibleSymbolCollection _possibleSymbols;
        private readonly ulong _mask;

        private readonly BitArray _maskBA = new BitArray(sizeof(ulong) * 8, false);

        public SymbolicFeature(string id, params FeatureSymbol[] possibleSymbols)
            : this(id, (IEnumerable<FeatureSymbol>)possibleSymbols) { }

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
            int symbolCount = _possibleSymbols.Count;
            if (symbolCount > SymbolicFeatureValueFactory.Instance.NeedToUseBitArray)
                _maskBA = new BitArray(symbolCount, true);
            else
                _mask = (1UL << symbolCount) - 1UL;
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
            set { DefaultValue = SymbolicFeatureValueFactory.Instance.Create(_possibleSymbols[value]); }
        }

        internal ulong Mask
        {
            get { return _mask; }
        }

        internal BitArray MaskBA
        {
            get { return _maskBA; }
        }
    }
}
