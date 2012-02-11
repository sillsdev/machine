using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a segment definition for a character definition table.
	/// </summary>
	public class SymbolDefinition
	{
		private readonly FeatureSymbol _type;
		private readonly string _strRep;
		private readonly FeatureStruct _fs;

		public SymbolDefinition(string strRep, FeatureSymbol type, FeatureStruct fs)
		{
			_type = type;
			_strRep = strRep;
			_fs = fs;
		}

		public FeatureSymbol Type
		{
			get { return _type; }
		}

		public string StrRep
		{
			get { return _strRep; }
		}

		public FeatureStruct FeatureStruct
		{
			get { return _fs; }
		}

		public override string ToString()
		{
			return _strRep;
		}
	}
}
