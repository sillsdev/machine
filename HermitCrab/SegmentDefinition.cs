using SIL.APRE;
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
		private readonly FeatureStruct _analysisFeatureStruct;
		private readonly FeatureStruct _synthFeatureStruct;

		public SegmentDefinition(string strRep, CharacterDefinitionTable charDefTable, FeatureStruct synthFeatureStruct,
			FeatureStruct analysisFeatureStruct)
		{
			_strRep = strRep;
			_charDefTable = charDefTable;
			_synthFeatureStruct = synthFeatureStruct;
			_analysisFeatureStruct = analysisFeatureStruct;
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

		public FeatureStruct AnalysisFeatureStruct
		{
			get
			{
				return _analysisFeatureStruct;
			}
		}

		public FeatureStruct SynthFeatureStruct
		{
			get
			{
				return _synthFeatureStruct;
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
