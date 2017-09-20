using System.IO;

namespace SIL.Machine.Morphology.HermitCrab
{
	internal class HCContext
	{
		private readonly Language _language;
		private Morpher _morpher;
		private readonly TextWriter _outWriter;

		public HCContext(Language language, TextWriter outWriter)
		{
			_language = language;
			_outWriter = outWriter;
		}

		public void Compile()
		{
			_morpher = new Morpher(new TraceManager(), _language);
		}

		public Language Language
		{
			get { return _language; }
		}

		public Morpher Morpher
		{
			get { return _morpher; }
		}

		public TextWriter Out
		{
			get { return _outWriter; }
		}

		public int ParseCount { get; set; }
		public int SuccessfulParseCount { get; set; }
		public int FailedParseCount { get; set; }
		public int ErrorParseCount { get; set; }

		public int TestCount { get; set; }
		public int PassedTestCount { get; set; }
		public int FailedTestCount { get; set; }
		public int ErrorTestCount { get; set; }

		public void ResetParseStats()
		{
			ParseCount = 0;
			SuccessfulParseCount = 0;
			FailedParseCount = 0;
			ErrorParseCount = 0;
		}

		public void ResetTestStats()
		{
			TestCount = 0;
			PassedTestCount = 0;
			FailedTestCount = 0;
			ErrorTestCount = 0;
		}
	}
}
