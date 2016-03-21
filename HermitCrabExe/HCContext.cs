using System.IO;
using SIL.Machine.Annotations;

namespace SIL.HermitCrab
{
	public class HCContext
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
			_morpher = new Morpher(new ShapeSpanFactory(), new TraceManager(), _language);
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
	}
}
