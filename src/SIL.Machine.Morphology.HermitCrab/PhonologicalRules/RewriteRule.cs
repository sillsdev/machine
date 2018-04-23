using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
	public enum RewriteApplicationMode
	{
		Iterative,
		Simultaneous
	}

	public class RewriteRule : HCRuleBase, IPhonologicalRule
	{
		private readonly List<RewriteSubrule> _subrules;

		public RewriteRule()
		{
			Lhs = Pattern<Word, ShapeNode>.New().Value;
			_subrules = new List<RewriteSubrule>();
		}

		public Pattern<Word, ShapeNode> Lhs { get; set; }

		public IList<RewriteSubrule> Subrules
		{
			get { return _subrules; }
		} 

		public Direction Direction { get; set; }

		public RewriteApplicationMode ApplicationMode { get; set; }

		public override IRule<Word, ShapeNode> CompileAnalysisRule(Morpher morpher)
		{
			return new AnalysisRewriteRule(morpher, this);
		}

		public override IRule<Word, ShapeNode> CompileSynthesisRule(Morpher morpher)
		{
			return new SynthesisRewriteRule(morpher, this);
		}
	}
}
