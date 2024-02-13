using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// This class represents all of the information required to parse words in a particular language.
    /// It encapsulates the feature systems, rules, character definition tables, etc. for the language.
    /// </summary>
    public class Language : HCRuleBase
    {
        private readonly ObservableCollection<Stratum> _strata;
        private readonly List<NaturalClass> _naturalClasses;
        private readonly List<StemName> _stemNames;
        private readonly List<MprFeature> _mprFeatures;
        private readonly List<MprFeatureGroup> _mprFeatureGroups;
        private readonly List<CharacterDefinitionTable> _tables;
        private readonly List<LexFamily> _families;
        private readonly List<IPhonologicalRule> _prules;
        private readonly List<(Morpheme, MorphemeCoOccurrenceRule)> _morphemeCoOccurRules;
        private readonly List<(Allomorph, AllomorphCoOccurrenceRule)> _allomorphCoOccurRules;

        /// <summary>
        /// Initializes a new instance of the <see cref="Language"/> class.
        /// </summary>
        public Language()
        {
            _strata = new ObservableCollection<Stratum>();
            _strata.CollectionChanged += StrataChanged;
            PhonologicalFeatureSystem = new FeatureSystem();
            SyntacticFeatureSystem = new SyntacticFeatureSystem();
            _naturalClasses = new List<NaturalClass>();
            _stemNames = new List<StemName>();
            _mprFeatures = new List<MprFeature>();
            _mprFeatureGroups = new List<MprFeatureGroup>();
            _tables = new List<CharacterDefinitionTable>();
            _families = new List<LexFamily>();
            _prules = new List<IPhonologicalRule>();
            _morphemeCoOccurRules = new List<(Morpheme, MorphemeCoOccurrenceRule)>();
            _allomorphCoOccurRules = new List<(Allomorph, AllomorphCoOccurrenceRule)>();
        }

        private void StrataChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (Stratum stratum in e.OldItems)
                    stratum.Depth = -1;
            for (int i = 0; i < _strata.Count; i++)
                _strata[i].Depth = i;
        }

        /// <summary>
        /// Gets the surface stratum.
        /// </summary>
        /// <value>The surface stratum.</value>
        public Stratum SurfaceStratum
        {
            get
            {
                if (_strata.Count == 0)
                    return null;
                return _strata[_strata.Count - 1];
            }
        }

        /// <summary>
        /// Gets the phonological feature system.
        /// </summary>
        /// <value>The phonological feature system.</value>
        public FeatureSystem PhonologicalFeatureSystem { get; set; }

        /// <summary>
        /// Gets the syntactic feature system.
        /// </summary>
        /// <value>The syntactic feature system.</value>
        public SyntacticFeatureSystem SyntacticFeatureSystem { get; set; }

        /// <summary>
        /// Gets all strata, including the surface stratum.
        /// </summary>
        /// <value>The strata.</value>
        public IList<Stratum> Strata
        {
            get { return _strata; }
        }

        public ICollection<NaturalClass> NaturalClasses
        {
            get { return _naturalClasses; }
        }

        public ICollection<StemName> StemNames
        {
            get { return _stemNames; }
        }

        public ICollection<MprFeature> MprFeatures
        {
            get { return _mprFeatures; }
        }

        public ICollection<MprFeatureGroup> MprFeatureGroups
        {
            get { return _mprFeatureGroups; }
        }

        public ICollection<CharacterDefinitionTable> CharacterDefinitionTables
        {
            get { return _tables; }
        }

        public ICollection<LexFamily> Families
        {
            get { return _families; }
        }

        public ICollection<IPhonologicalRule> PhonologicalRules
        {
            get { return _prules; }
        }

        public ICollection<(Morpheme Key, MorphemeCoOccurrenceRule Rule)> MorphemeCoOccurrenceRules
        {
            get { return _morphemeCoOccurRules; }
        }

        public ICollection<(Allomorph Key, AllomorphCoOccurrenceRule Rule)> AllomorphCoOccurrenceRules
        {
            get { return _allomorphCoOccurRules; }
        }

        public override IRule<Word, ShapeNode> CompileAnalysisRule(Morpher morpher)
        {
            return new AnalysisLanguageRule(morpher, this);
        }

        public override IRule<Word, ShapeNode> CompileSynthesisRule(Morpher morpher)
        {
            return new PipelineRuleCascade<Word, ShapeNode>(
                _strata.Select(stratum => stratum.CompileSynthesisRule(morpher)),
                FreezableEqualityComparer<Word>.Default
            );
        }
    }
}
