using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public enum RewriteApplicationMode
	{
		Iterative,
		Simultaneous
	}

	public class RewriteRule : IPhonologicalRule
	{
		private readonly List<RewriteSubrule> _subrules;

		public RewriteRule()
		{
			Lhs = Pattern<Word, ShapeNode>.New().Value;
			_subrules = new List<RewriteSubrule>();
		}

		public string Name { get; set; }

		public Pattern<Word, ShapeNode> Lhs { get; set; }

		public IList<RewriteSubrule> Subrules
		{
			get { return _subrules; }
		} 

		public Direction Direction { get; set; }

		public RewriteApplicationMode ApplicationMode { get; set; }

		public IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new AnalysisRewriteRule(spanFactory, morpher, this);
		}

		public IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new SynthesisRewriteRule(spanFactory, morpher, this);
		}

		public override string ToString()
		{
			return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
		}
	}
}
