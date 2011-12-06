using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a segment definition for a character definition table.
	/// </summary>
	public class SymbolDefinition
	{
		private readonly string _type;
		private readonly string _strRep;
		private readonly FeatureStruct _fs;

		public SymbolDefinition(string strRep, string type, FeatureStruct fs)
		{
			_type = type;
			_strRep = strRep;
			_fs = fs;
		}

		public string Type
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
