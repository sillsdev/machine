using System.Collections.Generic;
using System.Linq;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

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

        private readonly CharacterDefinitionTable _charDefTable;
        private bool _isCyclic;

    	private readonly SegmentDefinitionTrie<RootAllomorph> _entryTrie;
        private readonly List<MorphologicalRule> _mrules;
        private readonly List<PhonologicalRule> _prules;

    	private readonly DefaultRuleCascade<Word, ShapeNode> _synthesisRule;
    	private readonly DefaultRuleCascade<Word, ShapeNode> _pruleSynthesisRule;
		private readonly DefaultRuleCascade<Word, ShapeNode> _mruleSynthesisRule;

    	private readonly DefaultRuleCascade<Word, ShapeNode> _analysisRule; 
    	private readonly DefaultRuleCascade<Word, ShapeNode> _pruleAnalysisRule;
    	private readonly DefaultRuleCascade<Word, ShapeNode> _mruleAnalysisRule; 

        private readonly IDBearerSet<AffixTemplate> _templates;

    	private readonly IDBearerSet<LexEntry> _entries; 
    	private readonly Pattern<Shape, ShapeNode> _entriesPattern;

    	/// <summary>
    	/// Initializes a new instance of the <see cref="Stratum"/> class.
    	/// </summary>
    	/// <param name="id">The ID.</param>
    	/// <param name="charDefTable"></param>
    	public Stratum(string id, CharacterDefinitionTable charDefTable)
            : base(id)
    	{
    		_charDefTable = charDefTable;

            _mrules = new List<MorphologicalRule>();
            _prules = new List<PhonologicalRule>();

			_pruleSynthesisRule = new DefaultRuleCascade<Word, ShapeNode>();
			_mruleSynthesisRule = new DefaultRuleCascade<Word, ShapeNode>();
			_synthesisRule = new DefaultRuleCascade<Word, ShapeNode>(new[] {_pruleSynthesisRule, _mruleSynthesisRule});

			_pruleAnalysisRule = new DefaultRuleCascade<Word, ShapeNode>();
			_mruleAnalysisRule = new DefaultRuleCascade<Word, ShapeNode>();
			_analysisRule = new DefaultRuleCascade<Word, ShapeNode>(new[] {_mruleAnalysisRule, _pruleAnalysisRule});

            _templates = new IDBearerSet<AffixTemplate>();
            _entryTrie = new SegmentDefinitionTrie<RootAllomorph>(Direction.LeftToRight);

			_entries = new IDBearerSet<LexEntry>();
			_entriesPattern = new Pattern<Shape, ShapeNode>(charDefTable.SpanFactory) {Filter = ann => ann.Type == HCFeatureSystem.SegmentType, Quasideterministic = true};
        }

        /// <summary>
        /// Gets or sets the character definition table.
        /// </summary>
        /// <value>The character definition table.</value>
        public CharacterDefinitionTable CharacterDefinitionTable
        {
            get
            {
                return _charDefTable;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is cyclic.
        /// </summary>
        /// <value><c>true</c> if this instance is cyclic; otherwise, <c>false</c>.</value>
        public bool IsCyclic
        {
            get
            {
                return _isCyclic;
            }

            set
            {
                _isCyclic = value;
            }
        }

        /// <summary>
        /// Gets or sets the phonological rule order.
        /// </summary>
        /// <value>The phonological rule order.</value>
        public RuleCascadeOrder PhonologicalRuleOrder
        {
            get { return _pruleSynthesisRule.RuleCascadeOrder; }

            set
            {
            	_pruleSynthesisRule.RuleCascadeOrder = value;
            	_pruleAnalysisRule.RuleCascadeOrder = value;
            }
        }

        /// <summary>
        /// Gets or sets the morphological rule order.
        /// </summary>
        /// <value>The morphological rule order.</value>
        public RuleCascadeOrder MorphologicalRuleOrder
        {
            get { return _mruleSynthesisRule.RuleCascadeOrder; }

            set
            {
            	_mruleSynthesisRule.RuleCascadeOrder = value;
            	_mruleAnalysisRule.RuleCascadeOrder = value;
            }
        }

    	public IRule<Word, ShapeNode> AnalysisRule
    	{
    		get { return _analysisRule; }
    	}

    	public IRule<Word, ShapeNode> SynthesisRule
    	{
    		get { return _synthesisRule; }
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
			_pruleSynthesisRule.AddRule(prule.SynthesisRule);
			_pruleAnalysisRule.InsertRule(0, prule.AnalysisRule);
        }

        /// <summary>
        /// Adds the morphological rule.
        /// </summary>
        /// <param name="mrule">The morphological rule.</param>
        public void AddMorphologicalRule(MorphologicalRule mrule)
        {
            mrule.Stratum = this;
            _mrules.Add(mrule);
			_mruleSynthesisRule.AddRule(mrule.SynthesisRule);
			_mruleAnalysisRule.InsertRule(0, mrule.AnalysisRule);
        }

        /// <summary>
        /// Adds the lexical entry.
        /// </summary>
        /// <param name="entry">The lexical entry.</param>
        public void AddEntry(LexEntry entry)
        {
            entry.Stratum = this;
        	_entries.Add(entry);
			foreach (RootAllomorph allomorph in entry.Allomorphs)
				_entriesPattern.Children.Add(CreateExpression(allomorph));
        }

		private Expression<Shape, ShapeNode> CreateExpression(RootAllomorph allomorph)
		{
			var expr = new Expression<Shape, ShapeNode>(allomorph.Morpheme.ID);
			expr.Children.Add(new Constraint<Shape, ShapeNode>(HCFeatureSystem.AnchorType, FeatureStruct.New(HCFeatureSystem.Instance).Symbol(HCFeatureSystem.LeftSide).Value));
			foreach (ShapeNode node in allomorph.Shape.Where(node => node.Annotation.Type == HCFeatureSystem.SegmentType))
				expr.Children.Add(new Constraint<Shape, ShapeNode>(HCFeatureSystem.SegmentType, node.Annotation.FeatureStruct.Clone()));
			expr.Children.Add(new Constraint<Shape, ShapeNode>(HCFeatureSystem.AnchorType, FeatureStruct.New(HCFeatureSystem.Instance).Symbol(HCFeatureSystem.RightSide).Value));
			return expr;
		}

    	/// <summary>
    	/// Searches for the lexical entry that matches the specified shape.
    	/// </summary>
    	/// <param name="input"></param>
		/// <param name="output">The matching lexical entries.</param>
    	/// <returns></returns>
    	public bool SearchEntries(Shape input, out IEnumerable<LexEntry> output)
		{
			IEnumerable<PatternMatch<ShapeNode>> matches;
			if (_entriesPattern.IsMatch(input, out matches))
			{
				output = matches.Select(match => _entries[match.ExpressionPath.Single()]);
				return true;
			}

			output = null;
			return false;
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

        /// <summary>
        /// If the list of Realizational Features is non-empty, choose from either the input stem or its relatives
        /// of this stratum that stem which incorporates the most realizational features (without being incompatible
        /// with any realizational feature or with the head and foot features of the input stem).
        /// </summary>
        /// <param name="ws">The input word synthesis.</param>
        /// <returns>The resulting word synthesis.</returns>
        WordSynthesis ChooseInflStem(WordSynthesis ws)
        {
            if (ws.RealizationalFeatures.NumValues == 0 || ws.Root.Family == null)
                return ws;

            WordSynthesis best = ws;
#if WANTPORT
            // iterate thru all relatives
            foreach (LexEntry relative in ws.Root.Family.Entries)
            {
                if (relative != ws.Root && relative.Stratum == ws.Stratum
                    && ws.RealizationalFeatures.IsCompatible(relative.HeadFeatures)
                    && ws.PartOfSpeech == relative.PartOfSpeech && relative.FootFeatures.Equals(ws.FootFeatures))
                {
                    FeatureValues remainder;
                    if (best.HeadFeatures.GetSupersetRemainder(relative.HeadFeatures, out remainder) && remainder.NumFeatures > 0
                        && ws.RealizationalFeatures.IsCompatible(remainder))
                    {
                        if (Morpher.TraceBlocking)
                            // create blocking trace record, should this become the current trace?
                            ws.CurrentTrace.AddChild(new BlockingTrace(BlockingTrace.BlockType.Template, relative));
                        best = new WordSynthesis(relative.PrimaryAllomorph, ws.RealizationalFeatures, ws.CurrentTrace);
                    }
                }
            }
#endif
            return best;
        }
    }
}
