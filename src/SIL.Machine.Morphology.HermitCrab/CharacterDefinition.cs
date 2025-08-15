using System.Collections.Generic;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    public class CharacterDefinition
    {
        private readonly ReadOnlyCollection<string> _representations;
        private readonly FeatureStruct _fs;

        internal CharacterDefinition(IList<string> representations, FeatureStruct fs)
        {
            _representations = new ReadOnlyCollection<string>(representations);
            _fs = fs;
        }

        public CharacterDefinitionTable CharacterDefinitionTable { get; internal set; }

        public FeatureSymbol Type
        {
            get
            {
                FeatureSymbol fsym = SymbolicFeatureValue.GetFeatureSymbolFromFeatureStruct(_fs, HCFeatureSystem.Type);
                return fsym;
            }
        }

        public ReadOnlyCollection<string> Representations
        {
            get { return _representations; }
        }

        public FeatureStruct FeatureStruct
        {
            get { return _fs; }
        }
    }
}
