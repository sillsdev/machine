using System;
using System.ComponentModel;
using Eto.Forms;
using Eto.Serialization.Xaml;
using GalaSoft.MvvmLight.Threading;

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
			MessageLabel = new Label
			{
				Width = ProgressContainer.Width - 3,
				Height = ProgressContainer.Height,
				VerticalAlignment = VerticalAlignment.Center
			};
			ProgressContainer.Add(MessageLabel, 3, 0);
		}

		protected PixelLayout ProgressContainer { get; set; }
		protected Label MessageLabel { get; set; }
		protected ProgressBar ProgressBar { get; set; }

		protected override void OnDataContextChanged(EventArgs e)
		{
			base.OnDataContextChanged(e);
			var vm = DataContext as ProgressViewModel;
			if (vm != null)
			{
				ProgressBar.Indeterminate = vm.IsIndeterminate;
				vm.PropertyChanged += ViewModelPropertyChanged;
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
					});
					break;

				case "IsIndeterminate":
					DispatcherHelper.CheckBeginInvokeOnUI(() =>
					{
						var vm = (ProgressViewModel) DataContext;
						ProgressBar.Indeterminate = vm.IsIndeterminate;
					});
					break;
			}
		}
	}
}
