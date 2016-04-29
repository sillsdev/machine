using System.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.HermitCrab
{
	public abstract class HCRuleBase : IHCRule
	{
		private readonly IDictionary _properties;

		protected HCRuleBase()
		{
			_properties = new Hashtable();
		}

		public string Name { get; set; }

		public abstract IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher);

		public abstract IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher);

		public IDictionary Properties
		{
			get { return _properties; }
		}

		public override string ToString()
		{
			return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
		}
	}
}
