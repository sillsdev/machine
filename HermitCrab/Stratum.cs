using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class encapsulates the character definition table, rules, and lexicon for
    /// a particular stratum.
    /// </summary>
    public class Stratum : IDBearerBase
    {
        /// <summary>
        /// The surface stratum ID
        /// </summary>
        public const string SurfaceStratumID = "surface";

    	private readonly SpanFactory<ShapeNode> _spanFactory; 
        private readonly SymbolDefinitionTable _symDefTable;

    	private readonly List<MorphologicalRule> _mrules;
        private readonly List<PhonologicalRule> _prules;

    	private RuleCascade<Word, ShapeNode> _synthesisRule;
    	private RuleCascade<Word, ShapeNode> _analysisRule; 

        private readonly IDBearerSet<AffixTemplate> _templates;

    	private readonly IDBearerSet<LexEntry> _entries;
    	private Matcher<Shape, ShapeNode> _entriesMatcher;

    	/// <summary>
    	/// Initializes a new instance of the <see cref="Stratum"/> class.
    	/// </summary>
    	/// <param name="id">The ID.</param>
    	/// <param name="spanFactory"></param>
    	/// <param name="symDefTable"></param>
    	public Stratum(string id, SpanFactory<ShapeNode> spanFactory, SymbolDefinitionTable symDefTable)
            : base(id)
    	{
    		_spanFactory = spanFactory;
    		_symDefTable = symDefTable;

            _mrules = new List<MorphologicalRule>();
            _prules = new List<PhonologicalRule>();

			MorphologicalRuleOrder = RuleCascadeOrder.Permutation;

            _templates = new IDBearerSet<AffixTemplate>();

			_entries = new IDBearerSet<LexEntry>();
        }

        /// <summary>
        /// Gets the symbol definition table.
        /// </summary>
        /// <value>The symbol definition table.</value>
        public SymbolDefinitionTable SymbolDefinitionTable
        {
            get
            {
                return _symDefTable;
            }
        }

    	/// <summary>
    	/// Gets or sets a value indicating whether this instance is cyclic.
    	/// </summary>
    	/// <value><c>true</c> if this instance is cyclic; otherwise, <c>false</c>.</value>
    	public bool IsCyclic { get; set; }

    	/// <summary>
    	/// Gets or sets the phonological rule order.
    	/// </summary>
    	/// <value>The phonological rule order.</value>
		public RuleCascadeOrder PhonologicalRuleOrder { get; set; }

    	/// <summary>
    	/// Gets or sets the morphological rule order.
    	/// </summary>
    	/// <value>The morphological rule order.</value>
		public RuleCascadeOrder MorphologicalRuleOrder { get; set; }

    	public IRule<Word, ShapeNode> AnalysisRule
    	{
    		get { return _analysisRule; }
    	}

    	public IRule<Word, ShapeNode> SynthesisRule
    	{
    		get { return _synthesisRule; }
    	}

		public void Compile()
		{
			var pruleSynthesisRule = new RuleCascade<Word, ShapeNode>(_prules.Select(prule => prule.SynthesisRule), PhonologicalRuleOrder);
			var mruleSynthesisRule = new RuleCascade<Word, ShapeNode>(_mrules.Select(mrule => mrule.SynthesisRule), MorphologicalRuleOrder, true);
			_synthesisRule = new RuleCascade<Word, ShapeNode>(new[] { pruleSynthesisRule, mruleSynthesisRule });

			var pruleAnalysisRule = new RuleCascade<Word, ShapeNode>(_prules.Select(prule => prule.AnalysisRule).Reverse(), PhonologicalRuleOrder);
			var mruleAnalysisRule = new RuleCascade<Word, ShapeNode>(_mrules.Select(mrule => mrule.AnalysisRule).Reverse(), MorphologicalRuleOrder, true);
			_analysisRule = new RuleCascade<Word, ShapeNode>(new[] { mruleAnalysisRule, pruleAnalysisRule });

			var pattern = new Pattern<Shape, ShapeNode>(_entries.SelectMany(entry => entry.Allomorphs, (entry, allo) => CreateSubpattern(allo)));
			_entriesMatcher = new Matcher<Shape, ShapeNode>(_spanFactory, pattern, new MatcherSettings<ShapeNode>
			                                                                       	{
																						Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.AnchorType),
																						Quasideterministic = true
			                                                                       	});
		}

		private Pattern<Shape, ShapeNode> CreateSubpattern(RootAllomorph allomorph)
		{
			var subpattern = new Pattern<Shape, ShapeNode>(allomorph.Morpheme.ID);
			subpattern.Children.Add(new Constraint<Shape, ShapeNode>(FeatureStruct.New().Symbol(HCFeatureSystem.AnchorType).Symbol(HCFeatureSystem.LeftSide).Value));
			foreach (ShapeNode node in allomorph.Shape.Where(node => node.Annotation.Type() == HCFeatureSystem.SegmentType))
				subpattern.Children.Add(new Constraint<Shape, ShapeNode>(node.Annotation.FeatureStruct.Clone()));
			subpattern.Children.Add(new Constraint<Shape, ShapeNode>(FeatureStruct.New().Symbol(HCFeatureSystem.AnchorType).Symbol(HCFeatureSystem.RightSide).Value));
			return subpattern;
		}

        /// <summary>
        /// Gets the affix templates.
        /// </summary>
        /// <value>The affix templates.</value>
        public IEnumerable<AffixTemplate> AffixTemplates
        {
            get
            {
                return _templates;
            }
        }

        /// <summary>
        /// Adds the phonological rule.
        /// </summary>
        /// <param name="prule">The phonological rule.</param>
        public void AddPhonologicalRule(PhonologicalRule prule)
        {
            _prules.Add(prule);
        }

        /// <summary>
        /// Adds the morphological rule.
        /// </summary>
        /// <param name="mrule">The morphological rule.</param>
        public void AddMorphologicalRule(MorphologicalRule mrule)
        {
            mrule.Stratum = this;
            _mrules.Add(mrule);
        }

        /// <summary>
        /// Adds the lexical entry.
        /// </summary>
        /// <param name="entry">The lexical entry.</param>
        public void AddEntry(LexEntry entry)
        {
            entry.Stratum = this;
        	_entries.Add(entry);
        }

    	/// <summary>
    	/// Searches for the lexical entry that matches the specified shape.
    	/// </summary>
    	/// <param name="input"></param>
		/// <returns>The matching lexical entries.</returns>
    	public IEnumerable<LexEntry> SearchEntries(Shape input)
    	{
    		return _entriesMatcher.AllMatches(input).Select(match => _entries[match.PatternPath.Single()]);
		}

        /// <summary>
        /// Adds the affix template.
        /// </summary>
        /// <param name="template">The affix template.</param>
        public void AddAffixTemplate(AffixTemplate template)
        {
            _templates.Add(template);
        }

        /// <summary>
        /// Clears the affix templates.
        /// </summary>
        public void ClearAffixTemplates()
        {
            _templates.Clear();
        }
    }
}
