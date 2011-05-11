using System.Collections.Generic;
using SIL.APRE;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class represents a lexicon.
    /// </summary>
    public class Lexicon
    {
        private readonly IDBearerSet<LexEntry> _entries;
        private readonly IDBearerSet<LexFamily> _families;

        public Lexicon()
        {
            _entries = new IDBearerSet<LexEntry>();
            _families = new IDBearerSet<LexFamily>();
        }

        /// <summary>
        /// Gets the lexical families.
        /// </summary>
        /// <value>The lexical families.</value>
        public IEnumerable<LexFamily> Families
        {
            get
            {
                return _families;
            }
        }

        /// <summary>
        /// Gets the lexical entries.
        /// </summary>
        /// <value>The lexical entries.</value>
        public IEnumerable<LexEntry> Entries
        {
            get
            {
                return _entries;
            }
        }

        /// <summary>
        /// Gets the lexical entry associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The lexical entry.</returns>
        public LexEntry GetEntry(string id)
        {
            LexEntry entry;
            if (_entries.TryGetValue(id, out entry))
                return entry;
            return null;
        }

        /// <summary>
        /// Gets the lexical family associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The lexical family.</returns>
        public LexFamily GetFamily(string id)
        {
            LexFamily family;
            if (_families.TryGetValue(id, out family))
                return family;
            return null;
        }

        /// <summary>
        /// Adds the lexical family.
        /// </summary>
        /// <param name="family">The lexical family.</param>
        public void AddFamily(LexFamily family)
        {
            _families.Add(family);
        }

        /// <summary>
        /// Adds the lexical entry.
        /// </summary>
        /// <param name="entry">The lexical entry.</param>
        public void AddEntry(LexEntry entry)
        {
            _entries.Add(entry);
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            _families.Clear();
            _entries.Clear();
        }
    }
}