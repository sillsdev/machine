using System;
using System.ComponentModel;
using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Xaml;
using GalaSoft.MvvmLight.Threading;

namespace SIL.Machine.Translation.TestApp
{
	public class ProgressDialog : Dialog<bool>
	{
		public ProgressDialog()
		{
			XamlReader.Load(this);
		}

		protected Label MessageLabel { get; set; }
		protected ProgressBar ProgressBar { get; set; }

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			Rectangle mainFormBounds = Application.Instance.MainForm.Bounds;
			Location = new Point(Math.Max(mainFormBounds.X, mainFormBounds.X + (mainFormBounds.Width - Width) / 2),
				Math.Max(mainFormBounds.Y, mainFormBounds.Y + (mainFormBounds.Height - Height) / 2));
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			var vm = (ProgressViewModel) DataContext;
			Title = vm.DisplayName;
			ProgressBar.Indeterminate = vm.IsIndeterminate;
			vm.PropertyChanged += ViewModelPropertyChanged;
			vm.Execute();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			var vm = (ProgressViewModel) DataContext;
			if (vm.Executing)
			{
				vm.CancelRequested = true;
				e.Cancel = true;
			}
		}

		private void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "Executing":
					if (!((ProgressViewModel) sender).Executing)
					{
						DispatcherHelper.CheckBeginInvokeOnUI(() =>
						{
							var vm = (ProgressViewModel) DataContext;
							if (vm.Exception != null)
								throw vm.Exception;
							Close(!vm.CancelRequested);
						});
					}
					break;

				case "Text":
					DispatcherHelper.CheckBeginInvokeOnUI(() =>
					{
						var vm = (ProgressViewModel) DataContext;
						MessageLabel.Text = vm.Text;
					});
					break;

				case "PercentCompleted":
					DispatcherHelper.CheckBeginInvokeOnUI(() =>
					{
						var vm = (ProgressViewModel) DataContext;
						ProgressBar.Value = (int) Math.Round(vm.PercentCompleted, 0, MidpointRounding.AwayFromZero);
						Title = string.Format("{0}% completed", ProgressBar.Value);
					});
					break;

				case "IsIndeterminate":
					DispatcherHelper.CheckBeginInvokeOnUI(() =>
					{
						var vm = (ProgressViewModel) DataContext;
						ProgressBar.Indeterminate = vm.IsIndeterminate;
						Title = vm.DisplayName;
					});
					break;
			}
		}
	}
}
