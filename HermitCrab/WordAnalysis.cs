using System;
using System.Collections.Generic;
using System.Text;
using SIL.APRE;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class represents all of the information for the analysis of a word.
    /// </summary>
    public class WordAnalysis : ICloneable
    {
        private PhoneticShape _shape;
        private readonly IDBearerSet<PartOfSpeech> _partsOfSpeech;
        private LexEntry.RootAllomorph _rootAllomorph;
        private WordAnalysis _nonHead;
        private readonly List<MorphologicalRule> _mrules;
        private readonly Dictionary<MorphologicalRule, int> _mrulesUnapplied;
        private FeatureStructure _rzFeatures;
        private Trace _curTrace;
        private Stratum _stratum;

        /// <summary>
        /// Initializes a new instance of the <see cref="WordAnalysis"/> class.
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="curTrace">The current trace record.</param>
        internal WordAnalysis(PhoneticShape shape, Stratum stratum, Trace curTrace)
        {
            _shape = shape;
            _partsOfSpeech = new IDBearerSet<PartOfSpeech>();
            _mrules = new List<MorphologicalRule>();
            _mrulesUnapplied = new Dictionary<MorphologicalRule, int>();
            _rzFeatures = new FeatureDictionary();
            _stratum = stratum;
            _curTrace = curTrace;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="wa">The word analysis.</param>
        public WordAnalysis(WordAnalysis wa)
        {
            _shape = wa._shape.Clone();
            _partsOfSpeech = new IDBearerSet<PartOfSpeech>(wa._partsOfSpeech);
            _rootAllomorph = wa._rootAllomorph;
            if (wa._nonHead != null)
                _nonHead = wa._nonHead.Clone();
            _mrules = new List<MorphologicalRule>(wa._mrules);
            _mrulesUnapplied = new Dictionary<MorphologicalRule, int>(wa._mrulesUnapplied);
            _rzFeatures = wa._rzFeatures.Clone();
            _curTrace = wa._curTrace;
            _stratum = wa._stratum;
        }

        /// <summary>
        /// Gets or sets the phonetic shape.
        /// </summary>
        /// <value>The phonetic shape.</value>
        public PhoneticShape Shape
        {
            get
            {
                return _shape;
            }

            internal set
            {
                _shape = value;
            }
        }

        /// <summary>
        /// Gets or sets the root allomorph.
        /// </summary>
        /// <value>The root allomorph.</value>
        public LexEntry.RootAllomorph RootAllomorph
        {
            get
            {
                return _rootAllomorph;
            }

            internal set
            {
                _rootAllomorph = value;
            }
        }

        /// <summary>
        /// Gets or sets the non-head analysis.
        /// </summary>
        /// <value>The non-head analysis.</value>
        public WordAnalysis NonHead
        {
            get
            {
                return _nonHead;
            }

            internal set
            {
                _nonHead = value;
            }
        }

        /// <summary>
        /// Gets the morphological rules.
        /// </summary>
        /// <value>The morphological rules.</value>
        public IEnumerable<MorphologicalRule> UnappliedMorphologicalRules
        {
            get
            {
                return _mrules;
            }
        }

        /// <summary>
        /// Gets or sets the realizational features.
        /// </summary>
        /// <value>The realizational features.</value>
        public FeatureStructure RealizationalFeatures
        {
            get
            {
                return _rzFeatures;
            }

            internal set
            {
                _rzFeatures = value;
            }
        }

        /// <summary>
        /// Gets or sets the current trace record.
        /// </summary>
        /// <value>The current trace record.</value>
        internal Trace CurrentTrace
        {
            get
            {
                return _curTrace;
            }

            set
            {
                _curTrace = value;
            }
        }

        /// <summary>
        /// Gets or sets the stratum.
        /// </summary>
        /// <value>The stratum.</value>
        public Stratum Stratum
        {
            get
            {
                return _stratum;
            }

            internal set
            {
                _stratum = value;
            }
        }

        /// <summary>
        /// Adds the part of speech.
        /// </summary>
        /// <param name="pos">The part of speech.</param>
        internal void AddPartOfSpeech(PartOfSpeech pos)
        {
            _partsOfSpeech.Add(pos);
        }

        /// <summary>
        /// Checks if the specified part of speech matches the set of instantiated parts of speech.
        /// </summary>
        /// <param name="pos">The part of speech.</param>
        /// <returns><c>true</c> if the specified part of speech matches, otherwise <c>false</c>.</returns>
        public bool MatchPartOfSpeech(PartOfSpeech pos)
        {
            return _partsOfSpeech.Count == 0 || _partsOfSpeech.Contains(pos);
        }

        /// <summary>
        /// Uninstantiates the part of speech.
        /// </summary>
        internal void UninstantiatePartOfSpeech()
        {
            _partsOfSpeech.Clear();
        }

        /// <summary>
        /// Notifies this analysis that the specified morphological rule was unapplied.
        /// </summary>
        /// <param name="mrule">The morphological rule.</param>
        internal void MorphologicalRuleUnapplied(MorphologicalRule mrule)
        {
            int numUnapplies = GetNumUnappliesForMorphologicalRule(mrule);
            _mrulesUnapplied[mrule] = numUnapplies + 1;
            _mrules.Insert(0, mrule);
        }

        /// <summary>
        /// Gets the number of times the specified morphological rule has been unapplied.
        /// </summary>
        /// <param name="mrule">The morphological rule.</param>
        /// <returns>The number of unapplications.</returns>
        internal int GetNumUnappliesForMorphologicalRule(MorphologicalRule mrule)
        {
            int numUnapplies;
            if (!_mrulesUnapplied.TryGetValue(mrule, out numUnapplies))
                numUnapplies = 0;
            return numUnapplies;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return Equals(obj as WordAnalysis);
        }

        public bool Equals(WordAnalysis other)
        {
            if (other == null)
                return false;

            if (_mrulesUnapplied.Count != other._mrulesUnapplied.Count)
                return false;

            foreach (KeyValuePair<MorphologicalRule, int> kvp in _mrulesUnapplied)
            {
                int numUnapplies;
                if (!other._mrulesUnapplied.TryGetValue(kvp.Key, out numUnapplies) || numUnapplies != kvp.Value)
                    return false;
            }

            if (_nonHead != null)
            {
                if (!_nonHead.Equals(other._nonHead))
                    return false;
            }
            else if (other._nonHead != null)
            {
                return false;
            }

            return _shape.Equals(other._shape) && _rzFeatures.Equals(other._rzFeatures);
        }

        public override int GetHashCode()
        {
            int mruleHashCode = 0;
            foreach (KeyValuePair<MorphologicalRule, int> kvp in _mrulesUnapplied)
                mruleHashCode ^= kvp.Key.GetHashCode() ^ kvp.Value.GetHashCode();

            return mruleHashCode ^ _shape.GetHashCode() ^ _rzFeatures.GetHashCode()
                ^ (_nonHead == null ? 0 : _nonHead.GetHashCode());
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            bool firstItem = true;
            foreach (MorphologicalRule rule in _mrules)
            {
                if (!firstItem)
                    sb.Append(", ");
                sb.Append(rule);
                firstItem = false;
            }

            return string.Format(HCStrings.kstidWordAnalysis,
                _stratum == null ? _shape.ToString() : _stratum.CharacterDefinitionTable.ToRegexString(_shape, ModeType.Analysis, true),
                _partsOfSpeech, sb, _stratum);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public WordAnalysis Clone()
        {
            return new WordAnalysis(this);
        }
    }
}
