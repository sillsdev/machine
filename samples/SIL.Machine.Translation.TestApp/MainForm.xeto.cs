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

			var vm = (MainFormViewModel) DataContext;
			TextView.Bind(tv => tv.DataContext, vm, m => m.CurrentText);
			RebuildProgressView.Bind(rpv => rpv.DataContext, vm, m => m.RebuildProgress);
			RebuildButton.Bind(c => c.Visible, vm.RebuildProgress, Binding.Property((ProgressViewModel m) => m.Executing).Convert(b => !b));
		}

		protected TextView TextView { get; set; }
		protected ProgressView RebuildProgressView { get; set; }
		protected Button RebuildButton { get; set; }

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
