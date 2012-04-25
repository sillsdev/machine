using System.IO;
using SIL.Machine;

namespace SIL.HermitCrab
{
	public class HCContext
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly Language _language;
		private Morpher _morpher;
		private readonly TextWriter _outWriter;

		public HCContext(Language language, TextWriter outWriter)
		{
			_spanFactory = new ShapeSpanFactory();
			_language = language;
			_outWriter = outWriter;
		}

		public void Compile()
		{
			_morpher = new Morpher(_spanFactory, _language);
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
