using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.FeatureModel
{
    public class SymbolicFeature : Feature
    {
        private readonly PossibleSymbolCollection _possibleSymbols;

        // Process-wide counter for globally-unique flat indices (see FlatIndex).
        private static int NextFlatIndex = -1;
        private int _flatIndex = -1;

        /// <summary>
        /// Globally-unique dense index used to place this feature's allowed-symbol bits in a
        /// FeatureStruct's flat unify vector. Assigned lazily and once (so it works regardless of
        /// whether/when the owning FeatureSystem is frozen — loaded grammars don't always freeze it).
        /// Returns -1 for features with > 64 symbols, which forces the slow unification path.
        /// </summary>
        internal int FlatIndex
        {
            get
            {
                if (_flatIndex < 0 && _possibleSymbols.Count <= sizeof(ulong) * 8)
                {
                    int idx = System.Threading.Interlocked.Increment(ref NextFlatIndex);
                    System.Threading.Interlocked.CompareExchange(ref _flatIndex, idx, -1);
                }
                return _flatIndex;
            }
        }

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
    }
}
