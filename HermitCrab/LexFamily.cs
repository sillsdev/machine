using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SIL.Collections;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a lexical family.
	/// </summary>
	public class LexFamily : IDBearerBase
	{
		private readonly ObservableCollection<LexEntry> _entries;

		public LexFamily(string id)
			: base(id)
		{
			_entries = new ObservableCollection<LexEntry>();
			_entries.CollectionChanged += EntriesChanged;
		}

		private void EntriesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (LexEntry entry in e.OldItems)
					entry.Family = null;
			}
			if (e.NewItems != null)
			{
				foreach (LexEntry entry in e.NewItems)
					entry.Family = this;
			}
		}

		public ICollection<LexEntry> Entries
		{
			get { return _entries; }
		}
	}
}
