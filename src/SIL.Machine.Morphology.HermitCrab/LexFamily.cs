using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SIL.Machine.Morphology.HermitCrab
{
	/// <summary>
	/// This class represents a lexical family.
	/// </summary>
	public class LexFamily
	{
		private readonly ObservableCollection<LexEntry> _entries;

		public LexFamily()
		{
			_entries = new ObservableCollection<LexEntry>();
			_entries.CollectionChanged += EntriesChanged;
		}

		public string Name { get; set; }

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

		public override string ToString()
		{
			return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
		}
	}
}
