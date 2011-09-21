using SIL.APRE.FeatureModel;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a segment definition for a character definition table.
	/// </summary>
	public class SegmentDefinition
	{
		private readonly string _strRep;
		private readonly CharacterDefinitionTable _charDefTable;
		private readonly FeatureStruct _fs;

		public SegmentDefinition(string strRep, CharacterDefinitionTable charDefTable, FeatureStruct fs)
		{
			_strRep = strRep;
			_charDefTable = charDefTable;
			_fs = fs;
		}

		public string StrRep
		{
			get
			{
				return _strRep;
			}
		}

		public CharacterDefinitionTable CharacterDefinitionTable
		{
			get
			{
				return _charDefTable;
			}
		}

		public FeatureStruct FeatureStruct
		{
			get
			{
				return _fs;
			}
		}

		public override int GetHashCode()
		{
			return _strRep.GetHashCode() ^ _charDefTable.ID.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as SegmentDefinition);
		}

		public bool Equals(SegmentDefinition other)
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
