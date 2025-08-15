using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.FeatureModel
{
    public class SymbolicFeatureValueFactory
    {
        private static readonly Lazy<SymbolicFeatureValueFactory> Lazy = new Lazy<SymbolicFeatureValueFactory>(
            () => new SymbolicFeatureValueFactory()
        );

        public static SymbolicFeatureValueFactory Instance
        {
            get { return Lazy.Value; }
        }

        // this can be set to 0 for testing the BitArray code
        public int NeedToUseBitArray { get; set; } = sizeof(ulong) * 8;

        // To test using the BitArray method, comment the preceding line and uncomment the following line
        //public int NeedToUseBitArray { get; set; } = 0;

        private SymbolicFeatureValueFactory() { }

        public SymbolicFeatureValue Create(SymbolicFeature feature)
        {
            if (feature.PossibleSymbols.Count > NeedToUseBitArray)
                return new SymbolicFeatureValueBA(feature);
            return new SymbolicFeatureValue(feature);
        }

        public SymbolicFeatureValue Create(IEnumerable<FeatureSymbol> values)
        {
            FeatureSymbol[] symbols = values.ToArray();
            if (symbols.Length == 0)
                throw new ArgumentException("values cannot be empty", "values");
            if (symbols[0].Feature.PossibleSymbols.Count > NeedToUseBitArray)
                return new SymbolicFeatureValueBA(values);
            return new SymbolicFeatureValue(values);
        }

        public SymbolicFeatureValue Create(FeatureSymbol value)
        {
            if (value.Feature.PossibleSymbols.Count > NeedToUseBitArray)
                return new SymbolicFeatureValueBA(value);
            return new SymbolicFeatureValue(value);
        }

        public SymbolicFeatureValue Create(SymbolicFeature feature, string varName, bool agree)
        {
            if (feature.PossibleSymbols.Count > NeedToUseBitArray)
                return new SymbolicFeatureValueBA(feature, varName, agree);
            return new SymbolicFeatureValue(feature, varName, agree);
        }
    }
}
