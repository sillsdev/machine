using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
    public class RewriteRule : IDBearerBase, IPhonologicalRule
    {
    	private readonly List<RewriteSubrule> _subrules;

    	public RewriteRule(string id)
			: base(id)
    	{
			ApplicationMode = ApplicationMode.Iterative;
    		Lhs = Pattern<Word, ShapeNode>.New().Value;
			_subrules = new List<RewriteSubrule>();
    	}

		public Pattern<Word, ShapeNode> Lhs { get; set; }

    	public IList<RewriteSubrule> Subrules
    	{
    		get { return _subrules; }
    	} 

		public Direction Direction { get; set; }

		public ApplicationMode ApplicationMode { get; set; }

		public int DelReapplications { get; set; }

    	public IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
    	{
    		return new AnalysisRewriteRule(spanFactory, morpher, this);
    	}

		public IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new SynthesisRewriteRule(spanFactory, morpher, this);
		}
    }
}
