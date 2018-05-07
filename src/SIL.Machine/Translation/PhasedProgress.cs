using System;
using System.Collections.ObjectModel;

namespace SIL.Machine.Translation
{
	public class PhasedProgress : Collection<Phase>, IProgress<ProgressData>
	{
		private readonly IProgress<ProgressData> _progress;

		public PhasedProgress(IProgress<ProgressData> progress)
		{
			_progress = progress;
		}

		public void Add(string message = null)
		{
			Add(new Phase { Message = message });
		}

		protected override void InsertItem(int index, Phase item)
		{
			item.Progress = this;
			item.Index = index;
			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, Phase item)
		{
			item.Progress = this;
			item.Index = index;
			base.SetItem(index, item);
		}

		protected override void RemoveItem(int index)
		{
			Phase item = Items[index];
			item.Index = 0;
			item.Progress = null;
			base.RemoveItem(index);
		}

		protected override void ClearItems()
		{
			foreach (Phase item in this)
			{
				item.Index = 0;
				item.Progress = null;
			}
			base.ClearItems();
		}

		public void Report(ProgressData value)
		{
			_progress?.Report(value);
		}
	}
}
