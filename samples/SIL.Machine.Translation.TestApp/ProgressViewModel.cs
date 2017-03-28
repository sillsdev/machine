using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Eto.Forms;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Threading;
using SIL.Progress;

namespace SIL.Machine.Translation.TestApp
{
	public class ProgressViewModel : ViewModelBase, IProgress, IProgressIndicator
	{
		private readonly Action<ProgressViewModel> _action;
		private double _percentCompleted;
		private string _text;
		private bool _executing;
		private bool _cancelRequested;
		private readonly RelayCommand<object> _cancelCommand;
		private bool _indeterminate;

		public ProgressViewModel(Action<ProgressViewModel> action)
		{
			_action = action;
			_cancelCommand = new RelayCommand<object>(Cancel, CanCancel);
		}

		public string Text
		{
			get => _text;
			set { Set(() => Text, ref _text, value); }
		}

		public double PercentCompleted
		{
			get => _percentCompleted;
			set => Set(nameof(PercentCompleted), ref _percentCompleted, value);
		}

		public bool Executing
		{
			get => _executing;
			set => Set(nameof(Executing), ref _executing, value);
		}

		public bool IsIndeterminate
		{
			get => _indeterminate;
			set => Set(nameof(IsIndeterminate), ref _indeterminate, value);
		}

		public bool CancelRequested
		{
			get => _cancelRequested;
			set => Set(nameof(CancelRequested), ref _cancelRequested, value);
		}

		public override void RaisePropertyChanged(string propertyName = null)
		{
			DispatcherHelper.CheckBeginInvokeOnUI(() => base.RaisePropertyChanged(propertyName));
		}

		public Exception Exception { get; private set; }

		public ICommand CancelCommand => _cancelCommand;

		public void Execute()
		{
			CancelRequested = false;
			Task.Factory.StartNew(() =>
			{
				((IProgressIndicator) this).Initialize();
				try
				{
					_action(this);
				}
				catch (Exception ex)
				{
					Exception = ex;
				}
				finally
				{
					((IProgressIndicator) this).Finish();
				}
			});
		}

		private void Cancel(object o)
		{
			CancelRequested = true;
			Text = "Canceling";
			_cancelCommand.UpdateCanExecute();
		}

		private bool CanCancel(object o)
		{
			return !CancelRequested;
		}

		void IProgress.WriteStatus(string message, params object[] args)
		{
			((IProgress) this).WriteMessage(message, args);
		}

		void IProgress.WriteMessage(string message, params object[] args)
		{
			Text = string.Format(message, args);
		}

		void IProgress.WriteMessageWithColor(string colorName, string message, params object[] args)
		{
			((IProgress) this).WriteMessage(message, args);
		}

		void IProgress.WriteWarning(string message, params object[] args)
		{
			((IProgress) this).WriteMessage(message, args);
		}

		void IProgress.WriteException(Exception error)
		{
			Exception = error;
		}

		void IProgress.WriteError(string message, params object[] args)
		{
			((IProgress) this).WriteMessage(message, args);
		}

		void IProgress.WriteVerbose(string message, params object[] args)
		{
			((IProgress) this).WriteMessage(message, args);
		}

		bool IProgress.ShowVerbose { set { } }
		bool IProgress.ErrorEncountered { get; set; }

		IProgressIndicator IProgress.ProgressIndicator
		{
			get => this;
			set => throw new NotSupportedException();
		}

		void IProgressIndicator.Finish()
		{
			PercentCompleted = 100;
			Executing = false;
		}

		void IProgressIndicator.Initialize()
		{
			PercentCompleted = 0;
			Executing = true;
		}

		void IProgressIndicator.IndicateUnknownProgress()
		{
			IsIndeterminate = true;
		}

		SynchronizationContext IProgressIndicator.SyncContext { get; set; }
		SynchronizationContext IProgress.SyncContext { get; set; }
	}
}
