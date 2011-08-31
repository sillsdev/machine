using System.Collections.Generic;
using SIL.APRE;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a lexical family.
	/// </summary>
	public class LexFamily : IDBearerBase
	{
		private readonly IDBearerSet<LexEntry> _entries;

		public LexFamily(string id, string desc)
			: base(id, desc)
		{
			_entries = new IDBearerSet<LexEntry>();
		}

		public IEnumerable<LexEntry> Entries
		{
			get
			{
				return _entries;
			}
		}

		public void AddEntry(LexEntry entry)
		{
			entry.Family = this;
			_entries.Add(entry);
		}
	}
}
