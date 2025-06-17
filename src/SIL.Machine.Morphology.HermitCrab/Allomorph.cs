using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// This class represents an allomorph of a morpheme. Allomorphs can be phonologically
    /// conditioned using environments and are applied disjunctively within a morpheme.
    /// </summary>
    public abstract class Allomorph : IComparable<Allomorph>
    {
        private readonly HashSet<AllomorphEnvironment> _environments;
        private readonly HashSet<AllomorphCoOccurrenceRule> _allomorphCoOccurrenceRules;
        private readonly Properties _properties;
        private readonly string _id;

        /// <summary>
        /// Initializes a new instance of the <see cref="Allomorph"/> class.
        /// </summary>
        protected Allomorph()
        {
            Index = -1;
            _environments = new HashSet<AllomorphEnvironment>();
            _allomorphCoOccurrenceRules = new HashSet<AllomorphCoOccurrenceRule>();
            _properties = new Properties();
            _id = Guid.NewGuid().ToString();
        }

        internal string ID
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets or sets the morpheme.
        /// </summary>
        /// <value>The morpheme.</value>
        public Morpheme Morpheme { get; internal set; }

        /// <summary>
        /// Gets or sets the index of this allomorph in the morpheme.
        /// </summary>
        /// <value>The index.</value>
        public int Index { get; internal set; }

        /// <summary>
        /// Gets the environments.
        /// </summary>
        /// <value>The required environments.</value>
        public ICollection<AllomorphEnvironment> Environments
        {
            get { return _environments; }
        }

        /// <summary>
        /// Gets the allomorph co-occurrence rules.
        /// </summary>
        /// <value>The allomorph co-occurrence rules.</value>
        public ICollection<AllomorphCoOccurrenceRule> AllomorphCoOccurrenceRules
        {
            get { return _allomorphCoOccurrenceRules; }
        }

        /// <summary>
        /// Gets the custom properties.
        /// </summary>
        /// <value>The properties.</value>
        public IDictionary<string, object> Properties
        {
            get { return _properties; }
        }

        /// <summary>
        /// Was this allomorph guessed by a lexical pattern?
        /// </summary>
        public bool Guessed { get; set; }

        public bool FreeFluctuatesWith(Allomorph other)
        {
            if (this == other)
                return true;

            if (Morpheme != other.Morpheme)
                return false;

            int minIndex = Math.Min(Index, other.Index);
            int maxIndex = Math.Max(Index, other.Index);
            for (int i = minIndex; i < maxIndex; i++)
            {
                Allomorph cur = Morpheme.GetAllomorph(i);
                Allomorph next = Morpheme.GetAllomorph(i + 1);
                if (!cur.ConstraintsEqual(next))
                    return false;
            }
            return true;
        }

        protected virtual bool ConstraintsEqual(Allomorph other)
        {
            return _environments.SetEquals(other._environments);
        }

        internal bool IsWordValid(Morpher morpher, Word word)
        {
            if (!CheckAllomorphConstraints(morpher, this, word))
                return false;

            foreach (Annotation<ShapeNode> morph in word.GetMorphs(this))
            {
                if (Environments.Count > 0 && !Environments.Any(e => e.IsWordValid(word, morph)))
                {
                    if (morpher.TraceManager.IsTracing)
                    {
                        morpher.TraceManager.Failed(
                            morpher.Language,
                            word,
                            FailureReason.Environments,
                            this,
                            Environments
                        );
                    }
                    return false;
                }

                foreach (int i in word.GetDisjunctiveAllomorphApplications(morph) ?? Enumerable.Range(0, Index))
                {
                    Allomorph disjunctiveAllomorph = Morpheme.GetAllomorph(i);

                    if (
                        !FreeFluctuatesWith(disjunctiveAllomorph)
                        && (
                            disjunctiveAllomorph.Environments.Count == 0
                            || disjunctiveAllomorph.Environments.Any(e => e.IsWordValid(word, morph))
                        )
                        && disjunctiveAllomorph.CheckAllomorphConstraints(null, this, word)
                    )
                    {
                        if (morpher.TraceManager.IsTracing)
                        {
                            morpher.TraceManager.Failed(
                                morpher.Language,
                                word,
                                FailureReason.DisjunctiveAllomorph,
                                this,
                                disjunctiveAllomorph
                            );
                        }
                        return false;
                    }
                }
            }

            return true;
        }

        protected virtual bool CheckAllomorphConstraints(Morpher morpher, Allomorph allomorph, Word word)
        {
            if (AllomorphCoOccurrenceRules.Count > 0)
            {
                foreach (var rule in AllomorphCoOccurrenceRules)
                {
                    if (!rule.IsWordValid(allomorph, word))
                    {
                        if (morpher != null && morpher.TraceManager.IsTracing)
                        {
                            morpher.TraceManager.Failed(
                                morpher.Language,
                                word,
                                FailureReason.AllomorphCoOccurrenceRules,
                                this,
                                rule
                            );
                        }
                        return false;
                    }
                }
            }

            if (Morpheme.MorphemeCoOccurrenceRules.Count > 0)
            {
                foreach (MorphemeCoOccurrenceRule rule in Morpheme.MorphemeCoOccurrenceRules)
                {
                    // We need to check each one in turn and report any failure
                    if (!rule.IsWordValid(Morpheme, word))
                    {
                        if (morpher != null && morpher.TraceManager.IsTracing)
                        {
                            morpher.TraceManager.Failed(
                            morpher.Language,
                            word,
                            FailureReason.MorphemeCoOccurrenceRules,
                            this,
                            rule
                            );
                        }
                        return false;
                    }
                }
            }

            return true;
        }

        public int CompareTo(Allomorph other)
        {
            if (other == null)
                return 1;

            int res = Morpheme.GetHashCode().CompareTo(other.Morpheme.GetHashCode());
            if (res != 0)
                return res;

            return Index.CompareTo(other.Index);
        }
    }
}
