using SIL.APRE.FeatureModel;

namespace SIL.HermitCrab
{
	public class BoundaryDefinition
	{
		private readonly string _strRep;
		private readonly CharacterDefinitionTable _charDefTable;
		private readonly FeatureStruct _fs;

		public BoundaryDefinition(string strRep, CharacterDefinitionTable charDefTable, FeatureStruct fs)
		{
			_strRep = strRep;
			_charDefTable = charDefTable;
			_fs = fs;
		}

		public string StrRep
		{
			get { return _strRep; }
		}

		public CharacterDefinitionTable CharacterDefinitionTable
		{
			get { return _charDefTable; }
		}

		public FeatureStruct FeatureStruct
		{
			get { return _fs; }
		}

		public override int GetHashCode()
		{
			return _strRep.GetHashCode() ^ _charDefTable.ID.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as BoundaryDefinition);
		}

		public bool Equals(BoundaryDefinition other)
		{
			if (other == null)
				return false;
			return _strRep == other._strRep && _charDefTable == other._charDefTable;
		}

		public override string ToString()
		{
			return _strRep;
		}
	}
}
