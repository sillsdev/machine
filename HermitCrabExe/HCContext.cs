using SIL.Machine;

namespace SIL.HermitCrab
{
	public class HCContext
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly Language _language;
		private Morpher _morpher;

		public HCContext(Language language)
		{
			_spanFactory = new ShapeSpanFactory();
			_language = language;
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
	}
}
