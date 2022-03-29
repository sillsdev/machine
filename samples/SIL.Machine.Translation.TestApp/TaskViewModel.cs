using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation.TestApp
{
	public class TaskViewModel : ViewModelBase
	{
		private readonly Func<IProgress<ProgressStatus>, CancellationToken, Task> _execute;
		private readonly AsyncRelayCommand _startTaskCommand;
		private readonly RelayCommand _cancelCommand;
		private CancellationTokenSource _cts;

		private bool _isExecuting;
		private int _percentCompleted;
		private string _message;

		public TaskViewModel(Func<IProgress<ProgressStatus>, CancellationToken, Task> execute, Func<bool> canExecute)
		{
			_execute = execute;
			_startTaskCommand = new AsyncRelayCommand(ExecuteAsync, canExecute);
			_cancelCommand = new RelayCommand(Cancel, CanCancel);
		}

		private async Task ExecuteAsync()
		{
			using (_cts = new CancellationTokenSource())
			{
				IsExecuting = true;
				_cancelCommand.UpdateCanExecute();
				var progress = new Progress<ProgressStatus>(p =>
					{
						PercentCompleted = (int)Math.Round((p.PercentCompleted ?? 0) * 100,
							MidpointRounding.AwayFromZero);
						Message = p.Message;
					});
				CancellationToken token = _cts.Token;
				Task task = null;
				try
				{
					task = _execute(progress, token);
					await task;
				}
				catch
				{
				}

				if (task != null && task.IsFaulted && task.Exception != null)
					throw task.Exception;

				IsExecuting = false;
				_cancelCommand.UpdateCanExecute();
			}
		}

		private bool CanCancel()
		{
			return IsExecuting && !_cts.IsCancellationRequested;
		}

		private void Cancel()
		{
			_cts.Cancel();
			_cancelCommand.UpdateCanExecute();
			Message = "Canceling";
		}

		public int PercentCompleted
		{
			get => _percentCompleted;
			private set => Set(nameof(PercentCompleted), ref _percentCompleted, value);
		}

		public string Message
		{
			get => _message;
			private set => Set(nameof(Message), ref _message, value);
		}

		public bool IsExecuting
		{
			get => _isExecuting;
			private set
			{
				if (Set(nameof(IsExecuting), ref _isExecuting, value))
					RaisePropertyChanged(nameof(IsNotExecuting));
			}
		}

		public bool IsNotExecuting => !_isExecuting;

		public ICommand StartTaskCommand => _startTaskCommand;
		public ICommand CancelCommand => _cancelCommand;

		public void UpdateCanExecute()
		{
			_startTaskCommand.UpdateCanExecute();
		}
	}
}
