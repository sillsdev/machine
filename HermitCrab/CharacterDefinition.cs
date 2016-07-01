using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	public class CharacterDefinition
	{
		private readonly ReadOnlyCollection<string> _representations;
		private readonly FeatureStruct _fs;

		public CharacterDefinition(IEnumerable<string> representations, FeatureStruct fs)
		{
			_representations = new ReadOnlyCollection<string>(representations.ToArray());
			_fs = fs;
		}

		public CharacterDefinitionTable CharacterDefinitionTable { get; internal set; }

		public FeatureSymbol Type
		{
			get { return (FeatureSymbol) _fs.GetValue(HCFeatureSystem.Type); }
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
