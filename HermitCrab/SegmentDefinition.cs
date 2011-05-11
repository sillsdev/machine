using System.Collections.Generic;
using System.Linq;
using SIL.APRE;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a segment definition for a character definition table.
	/// </summary>
	public class SegmentDefinition
	{
		private readonly string _strRep;
		private readonly CharacterDefinitionTable _charDefTable;
		private readonly FeatureStructure _analysisFeatureStructure;
		private readonly FeatureStructure _synthFeatureStructure;
		private readonly FeatureStructure _antiFeatureStructure;

		public SegmentDefinition(string strRep, CharacterDefinitionTable charDefTable, IEnumerable<FeatureValue> featureValues,
			FeatureSystem featSys)
		{
			_strRep = strRep;
			_charDefTable = charDefTable;
			_synthFeatureStructure = featSys.CreateFeatureStructure(featureValues.Cast<object>());
			_antiFeatureStructure = featSys.CreateAntiFeatureStructure(_synthFeatureStructure);
			_analysisFeatureStructure = featSys.CreateAnalysisFeatureStructure(_synthFeatureStructure);
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

		public FeatureStructure AnalysisFeatureStructure
		{
			get
			{
				return _analysisFeatureStructure;
			}
		}

		public FeatureStructure SynthFeatureStructure
		{
			get
			{
				return _synthFeatureStructure;
			}
		}

		public FeatureStructure AntiFeatureStructure
		{
			get
			{
				return _antiFeatureStructure;
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
