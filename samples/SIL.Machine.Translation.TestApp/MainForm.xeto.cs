using System.ComponentModel;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace SIL.Machine.Translation.TestApp
{
	public class MainForm : Form
	{
		public MainForm()
		{
			XamlReader.Load(this);

			RebuildProgressBar = new ProgressBar
			{
				Width = RebuildProgressContainer.Width,
				Height = RebuildProgressContainer.Height
			};
			RebuildProgressContainer.Add(RebuildProgressBar, 0, 0);
			RebuildProgressBar.BindDataContext(c => c.Value, (TaskViewModel<ProgressData> vm) => vm.PercentCompleted);
			RebuildMessageLabel = new Label
			{
				Width = RebuildProgressContainer.Width - 3,
				Height = RebuildProgressContainer.Height,
				VerticalAlignment = VerticalAlignment.Center
			};
			RebuildProgressContainer.Add(RebuildMessageLabel, 3, 0);
			RebuildMessageLabel.TextBinding.BindDataContext((TaskViewModel<ProgressData> vm) => vm.Message);

			TextView.Bind(tv => tv.DataContext, (MainFormViewModel) DataContext, vm => vm.CurrentText);
		}

		protected TextView TextView { get; set; }
		protected Button RebuildButton { get; set; }
		protected PixelLayout RebuildProgressContainer { get; set; }
		protected Label RebuildMessageLabel { get; set; }
		protected ProgressBar RebuildProgressBar { get; set; }

		protected override void OnClosing(CancelEventArgs e)
		{
			var vm = (MainFormViewModel) DataContext;
			e.Cancel = !vm.CloseCommand.CanExecute(null);
			if (!e.Cancel)
				vm.CloseCommand.Execute(null);
			base.OnClosing(e);
		}
	}
}
