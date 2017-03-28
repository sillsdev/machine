using System;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace SIL.Machine.Translation.TestApp
{
	public class ProgressView : Panel
	{
		public ProgressView()
		{
			XamlReader.Load(this);

			ProgressBar = new ProgressBar
			{
				Width = ProgressContainer.Width,
				Height = ProgressContainer.Height
			};
			ProgressContainer.Add(ProgressBar, 0, 0);
			ProgressBar.BindDataContext(c => c.Indeterminate, (ProgressViewModel vm) => vm.IsIndeterminate);
			ProgressBar.BindDataContext(c => c.Value,
				Binding.Property((ProgressViewModel vm) => vm.PercentCompleted).Convert(d => (int) Math.Round(d, 0, MidpointRounding.AwayFromZero)));
			MessageLabel = new Label
			{
				Width = ProgressContainer.Width - 3,
				Height = ProgressContainer.Height,
				VerticalAlignment = VerticalAlignment.Center
			};
			ProgressContainer.Add(MessageLabel, 3, 0);
			MessageLabel.TextBinding.BindDataContext((ProgressViewModel vm) => vm.Text);
		}

		protected PixelLayout ProgressContainer { get; set; }
		protected Label MessageLabel { get; set; }
		protected ProgressBar ProgressBar { get; set; }
	}
}
