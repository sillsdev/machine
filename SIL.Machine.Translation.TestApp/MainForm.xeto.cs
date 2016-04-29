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
		}

		protected TextView TextView { get; set; }

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
