using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// This class represents a lexical entry.
    /// </summary>
    public class LexEntry : Morpheme
    {
        private readonly ObservableCollection<RootAllomorph> _allomorphs;

        /// <summary>
        /// Initializes a new instance of the <see cref="LexEntry"/> class.
        /// </summary>
        public LexEntry()
        {
            MprFeatures = new MprFeatureSet();
            SyntacticFeatureStruct = FeatureStruct.New().Value;
            _allomorphs = new ObservableCollection<RootAllomorph>();
            _allomorphs.CollectionChanged += AllomorphsChanged;
        }

        private void AllomorphsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (RootAllomorph allo in e.OldItems)
                {
                    allo.Morpheme = null;
                    allo.Index = -1;
                }
            }

            if (e.NewItems != null)
                foreach (RootAllomorph allo in e.NewItems)
                    allo.Morpheme = this;

            int index = Math.Min(
                e.NewStartingIndex == -1 ? int.MaxValue : e.NewStartingIndex,
                e.OldStartingIndex == -1 ? int.MaxValue : e.OldStartingIndex
            );
            for (int i = index; i < _allomorphs.Count; i++)
                _allomorphs[i].Index = i;
        }

        /// <summary>
        /// Gets the primary allomorph. This is the first allomorph.
        /// </summary>
        /// <value>The primary allomorph.</value>
        public RootAllomorph PrimaryAllomorph
        {
            get
            {
                if (_allomorphs.Count == 0)
                    return null;
                return _allomorphs[0];
            }
        }

        /// <summary>
        /// Gets the allomorphs.
        /// </summary>
        /// <value>The allomorphs.</value>
        public IList<RootAllomorph> Allomorphs
        {
            get { return _allomorphs; }
        }

        /// <summary>
        /// Gets the MPR features.
        /// </summary>
        /// <value>The MPR features.</value>
        public MprFeatureSet MprFeatures { get; set; }

        /// <summary>
        /// Gets the head features.
        /// </summary>
        /// <value>The head features.</value>
        public FeatureStruct SyntacticFeatureStruct { get; set; }

        /// <summary>
        /// Gets or sets the lexical family.
        /// </summary>
        /// <value>The lexical family.</value>
        public LexFamily Family { get; internal set; }

        public override string Category
        {
            get
            {
                FeatureSymbol pos = SyntacticFeatureStruct.PartsOfSpeech().FirstOrDefault();
                return pos?.ID;
            }
        }

        public override MorphemeType MorphemeType
        {
            get { return MorphemeType.Stem; }
        }

        public override int AllomorphCount => _allomorphs.Count;

        public override Allomorph GetAllomorph(int index)
        {
            return _allomorphs[index];
        }

        public override string ToString()
        {
            return _allomorphs.Count == 0 || Stratum == null ? base.ToString() : PrimaryAllomorph.Segments.ToString();
        }
    }
}
