using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Eto.Forms;
using GalaSoft.MvvmLight;
using SIL.Progress;

namespace SIL.Machine.Translation.TestApp
{
	public class ProgressViewModel : ViewModelBase, IProgress, IProgressIndicator
	{
		private readonly Action<ProgressViewModel> _action;
		private int _percentCompleted;
		private string _text;
		private bool _executing;
		private bool _cancelRequested;
		private readonly RelayCommand<object> _cancelCommand;
		private bool _indeterminate;
		private string _displayName;
		private Exception _exception;

		public ProgressViewModel(Action<ProgressViewModel> action)
		{
			_action = action;
			_cancelCommand = new RelayCommand<object>(o => CancelRequested = true);
		}

		public string DisplayName
		{
			get { return _displayName; }
			set { Set(() => DisplayName, ref _displayName, value); }
		}

		public string Text
		{
			get { return _text; }
			set { Set(() => Text, ref _text, value); }
		}

		public int PercentCompleted
		{
			get { return _percentCompleted; }
			set { Set(() => PercentCompleted, ref _percentCompleted, value); }
		}

		public bool Executing
		{
			get { return _executing; }
			set { Set(() => Executing, ref _executing, value); }
		}

		public bool IsIndeterminate
		{
			get { return _indeterminate; }
			set { Set(() => IsIndeterminate, ref _indeterminate, value); }
		}

		public bool CancelRequested
		{
			get { return _cancelRequested; }
			set { Set(() => CancelRequested, ref _cancelRequested, value); }
		}

		public Exception Exception
		{
			get { return _exception; }
		}

		public ICommand CancelCommand
		{
			get { return _cancelCommand; }
		}

		public void Execute()
		{
			Task.Factory.StartNew(() =>
			{
				((IProgressIndicator) this).Initialize();
				try
				{
					_action(this);
				}
				catch (Exception ex)
				{
					_exception = ex;
				}
				finally
				{
					((IProgressIndicator) this).Finish();
				}
			});
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
			_exception = error;
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
			get { return this; }
			set { throw new NotSupportedException(); }
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
